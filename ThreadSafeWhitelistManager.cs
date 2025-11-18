using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace RDPGuard
{
    /// <summary>
    /// Thread-safe whitelist manager that prevents deadlocks through proper lock ordering
    /// and timeout-based lock acquisition
    /// </summary>
    public class ThreadSafeWhitelistManager : IDisposable
    {
        private readonly string _whitelistPath;
        private readonly ReaderWriterLockSlim _rwLock;
        private HashSet<string> _whitelistedIPs;
        private DateTime _lastFileCheck;
        private readonly int _lockTimeoutMs = 5000;
        private readonly int _fileCheckIntervalSeconds = 60;
        private bool _disposed = false;

        public ThreadSafeWhitelistManager(string whitelistPath)
        {
            _whitelistPath = whitelistPath ?? "WhiteList.txt";
            _rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _whitelistedIPs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _lastFileCheck = DateTime.MinValue;
            LoadWhitelist();
        }

        /// <summary>
        /// Checks if an IP is whitelisted with timeout protection
        /// </summary>
        public bool IsWhitelisted(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            // Check if we need to reload the file
            CheckAndReloadIfNeeded();

            // Use read lock with timeout to prevent deadlock
            if (_rwLock.TryEnterReadLock(_lockTimeoutMs))
            {
                try
                {
                    return _whitelistedIPs.Contains(ipAddress);
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            else
            {
                // Lock timeout - log and return false (fail-safe)
                System.Diagnostics.EventLog.WriteEntry("RDP-Guard",
                    $"Whitelist read lock timeout for IP: {ipAddress}",
                    System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }
        }

        /// <summary>
        /// Adds an IP to the whitelist with timeout protection
        /// </summary>
        public bool AddToWhitelist(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            if (_rwLock.TryEnterWriteLock(_lockTimeoutMs))
            {
                try
                {
                    if (_whitelistedIPs.Add(ipAddress))
                    {
                        SaveWhitelistInternal();
                        return true;
                    }
                    return false; // Already exists
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
            else
            {
                System.Diagnostics.EventLog.WriteEntry("RDP-Guard",
                    $"Whitelist write lock timeout when adding IP: {ipAddress}",
                    System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Removes an IP from the whitelist with timeout protection
        /// </summary>
        public bool RemoveFromWhitelist(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            if (_rwLock.TryEnterWriteLock(_lockTimeoutMs))
            {
                try
                {
                    if (_whitelistedIPs.Remove(ipAddress))
                    {
                        SaveWhitelistInternal();
                        return true;
                    }
                    return false; // Didn't exist
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
            else
            {
                System.Diagnostics.EventLog.WriteEntry("RDP-Guard",
                    $"Whitelist write lock timeout when removing IP: {ipAddress}",
                    System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Reloads the whitelist from file with timeout protection
        /// </summary>
        public void ReloadWhitelist()
        {
            if (_rwLock.TryEnterWriteLock(_lockTimeoutMs))
            {
                try
                {
                    LoadWhitelistInternal();
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
            else
            {
                System.Diagnostics.EventLog.WriteEntry("RDP-Guard",
                    "Whitelist write lock timeout during reload",
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Gets a copy of all whitelisted IPs
        /// </summary>
        public List<string> GetWhitelistedIPs()
        {
            if (_rwLock.TryEnterReadLock(_lockTimeoutMs))
            {
                try
                {
                    return new List<string>(_whitelistedIPs);
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            else
            {
                System.Diagnostics.EventLog.WriteEntry("RDP-Guard",
                    "Whitelist read lock timeout when getting IPs",
                    System.Diagnostics.EventLogEntryType.Warning);
                return new List<string>();
            }
        }

        private void CheckAndReloadIfNeeded()
        {
            // Quick check without lock
            if ((DateTime.Now - _lastFileCheck).TotalSeconds < _fileCheckIntervalSeconds)
                return;

            // Use upgradeable read lock for the file check
            if (_rwLock.TryEnterUpgradeableReadLock(_lockTimeoutMs))
            {
                try
                {
                    // Double-check pattern
                    if ((DateTime.Now - _lastFileCheck).TotalSeconds < _fileCheckIntervalSeconds)
                        return;

                    if (File.Exists(_whitelistPath))
                    {
                        var lastWrite = File.GetLastWriteTime(_whitelistPath);
                        if (lastWrite > _lastFileCheck)
                        {
                            // Upgrade to write lock
                            if (_rwLock.TryEnterWriteLock(_lockTimeoutMs))
                            {
                                try
                                {
                                    LoadWhitelistInternal();
                                }
                                finally
                                {
                                    _rwLock.ExitWriteLock();
                                }
                            }
                        }
                    }
                    _lastFileCheck = DateTime.Now;
                }
                finally
                {
                    _rwLock.ExitUpgradeableReadLock();
                }
            }
        }

        private void LoadWhitelistInternal()
        {
            // Called only when write lock is held
            try
            {
                if (File.Exists(_whitelistPath))
                {
                    var lines = File.ReadAllLines(_whitelistPath);
                    _whitelistedIPs = new HashSet<string>(
                        lines.Where(line => !string.IsNullOrWhiteSpace(line))
                             .Select(line => line.Trim()),
                        StringComparer.OrdinalIgnoreCase);
                    _lastFileCheck = DateTime.Now;
                }
                else
                {
                    _whitelistedIPs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("RDP-Guard",
                    $"Error loading whitelist: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void LoadWhitelist()
        {
            if (_rwLock.TryEnterWriteLock(_lockTimeoutMs))
            {
                try
                {
                    LoadWhitelistInternal();
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
        }

        private void SaveWhitelistInternal()
        {
            // Called only when write lock is held
            try
            {
                File.WriteAllLines(_whitelistPath, _whitelistedIPs.OrderBy(ip => ip));
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("RDP-Guard",
                    $"Error saving whitelist: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _rwLock?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
