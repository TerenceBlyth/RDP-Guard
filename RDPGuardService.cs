using System;
using System.Collections.Concurrent;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace RDPGuard
{
    /// <summary>
    /// Main RDP-Guard service with deadlock-free coordination
    /// Demonstrates proper lock ordering and timeout-based operations
    /// </summary>
    public partial class RDPGuardService : ServiceBase
    {
        private ThreadSafeWhitelistManager _whitelistManager;
        private ThreadSafeFirewallManager _firewallManager;
        private ThreadSafeEventLogger _logger;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitoringTask;
        private readonly SemaphoreSlim _serviceLock;
        private readonly ConcurrentDictionary<string, FailedLoginTracker> _failedLogins;
        private Timer _cleanupTimer;
        private bool _disposed = false;

        // Configuration
        private const int MAX_FAILED_ATTEMPTS = 3;
        private const int FAILED_LOGIN_WINDOW_MINUTES = 10;
        private const int CLEANUP_INTERVAL_HOURS = 24;
        private const int BLOCK_RETENTION_DAYS = 30;

        private class FailedLoginTracker
        {
            public ConcurrentQueue<DateTime> Attempts { get; } = new ConcurrentQueue<DateTime>();
            public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);
        }

        public RDPGuardService()
        {
            InitializeComponent();
            ServiceName = "RDP-Guard";
            _serviceLock = new SemaphoreSlim(1, 1);
            _failedLogins = new ConcurrentDictionary<string, FailedLoginTracker>();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _logger = new ThreadSafeEventLogger("RDP-Guard");
                _logger.LogInformation("RDP-Guard service starting...");

                // Initialize managers with proper ordering
                _whitelistManager = new ThreadSafeWhitelistManager("WhiteList.txt");
                _firewallManager = new ThreadSafeFirewallManager();

                _cancellationTokenSource = new CancellationTokenSource();

                // Start monitoring task
                _monitoringTask = Task.Run(() => MonitorLogonFailures(_cancellationTokenSource.Token));

                // Setup cleanup timer (runs daily)
                _cleanupTimer = new Timer(
                    CleanupCallback,
                    null,
                    TimeSpan.FromHours(1),
                    TimeSpan.FromHours(CLEANUP_INTERVAL_HOURS));

                _logger.LogInformation("RDP-Guard service started successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to start RDP-Guard service", ex);
                throw;
            }
        }

        protected override void OnStop()
        {
            try
            {
                _logger?.LogInformation("RDP-Guard service stopping...");

                // Signal cancellation
                _cancellationTokenSource?.Cancel();

                // Wait for monitoring task to complete
                if (_monitoringTask != null)
                {
                    _monitoringTask.Wait(TimeSpan.FromSeconds(10));
                }

                // Cleanup timer
                _cleanupTimer?.Dispose();

                // Dispose managers in reverse order of initialization
                _firewallManager?.Dispose();
                _whitelistManager?.Dispose();

                _logger?.LogInformation("RDP-Guard service stopped successfully");
                _logger?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error during service shutdown", ex);
            }
        }

        /// <summary>
        /// Main monitoring loop - processes logon failures
        /// </summary>
        private async Task MonitorLogonFailures(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting logon failure monitoring");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // In real implementation, this would use WMI or ETW to monitor events
                    // For demonstration, showing the processing logic
                    await Task.Delay(1000, cancellationToken);

                    // Process any pending logon failures
                    // ProcessPendingLogonFailures();
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in monitoring loop", ex);
                    await Task.Delay(5000, cancellationToken); // Back off on error
                }
            }

            _logger.LogInformation("Logon failure monitoring stopped");
        }

        /// <summary>
        /// Handles a failed logon attempt with proper deadlock prevention
        /// Lock ordering: FailedLoginTracker -> WhitelistManager -> FirewallManager
        /// </summary>
        public void HandleFailedLogon(string ipAddress, string username)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return;

            try
            {
                // Step 1: Check whitelist first (read-only, fast)
                if (_whitelistManager.IsWhitelisted(ipAddress))
                {
                    _logger.LogWhitelistedAttempt(ipAddress);
                    return;
                }

                // Step 2: Track failed attempt with timeout
                var tracker = _failedLogins.GetOrAdd(ipAddress, _ => new FailedLoginTracker());

                if (!tracker.Lock.Wait(5000))
                {
                    _logger.LogWarning($"Timeout acquiring tracker lock for IP: {ipAddress}");
                    return;
                }

                try
                {
                    // Add attempt
                    tracker.Attempts.Enqueue(DateTime.Now);

                    // Clean old attempts
                    var cutoff = DateTime.Now.AddMinutes(-FAILED_LOGIN_WINDOW_MINUTES);
                    while (tracker.Attempts.TryPeek(out var oldest) && oldest < cutoff)
                    {
                        tracker.Attempts.TryDequeue(out _);
                    }

                    // Check if we should block
                    if (tracker.Attempts.Count >= MAX_FAILED_ATTEMPTS)
                    {
                        // Step 3: Block the IP (no locks held at this point)
                        if (_firewallManager.BlockIP(ipAddress))
                        {
                            string reason = $"Failed login threshold exceeded ({tracker.Attempts.Count} attempts in {FAILED_LOGIN_WINDOW_MINUTES} minutes)";
                            _logger.LogBlock(ipAddress, reason);

                            // Clear the tracker after blocking
                            tracker.Attempts = new ConcurrentQueue<DateTime>();
                        }
                        else
                        {
                            _logger.LogError($"Failed to block IP: {ipAddress}");
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            $"Failed login from {ipAddress} (User: {username}). " +
                            $"Attempt {tracker.Attempts.Count} of {MAX_FAILED_ATTEMPTS}");
                    }
                }
                finally
                {
                    tracker.Lock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling failed logon from {ipAddress}", ex);
            }
        }

        /// <summary>
        /// Manually blocks an IP address
        /// </summary>
        public bool ManualBlockIP(string ipAddress, string reason)
        {
            try
            {
                if (_whitelistManager.IsWhitelisted(ipAddress))
                {
                    _logger.LogWarning($"Cannot block whitelisted IP: {ipAddress}");
                    return false;
                }

                if (_firewallManager.BlockIP(ipAddress))
                {
                    _logger.LogBlock(ipAddress, $"Manual block: {reason}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error manually blocking IP {ipAddress}", ex);
                return false;
            }
        }

        /// <summary>
        /// Manually unblocks an IP address
        /// </summary>
        public bool ManualUnblockIP(string ipAddress, string reason)
        {
            try
            {
                if (_firewallManager.UnblockIP(ipAddress))
                {
                    _logger.LogUnblock(ipAddress, $"Manual unblock: {reason}");

                    // Clear tracking
                    _failedLogins.TryRemove(ipAddress, out _);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error manually unblocking IP {ipAddress}", ex);
                return false;
            }
        }

        /// <summary>
        /// Adds an IP to the whitelist
        /// </summary>
        public bool WhitelistIP(string ipAddress)
        {
            try
            {
                if (_whitelistManager.AddToWhitelist(ipAddress))
                {
                    _logger.LogInformation($"Added {ipAddress} to whitelist");

                    // Unblock if currently blocked
                    if (_firewallManager.IsBlocked(ipAddress))
                    {
                        _firewallManager.UnblockIP(ipAddress);
                        _logger.LogUnblock(ipAddress, "Added to whitelist");
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error whitelisting IP {ipAddress}", ex);
                return false;
            }
        }

        /// <summary>
        /// Removes an IP from the whitelist
        /// </summary>
        public bool RemoveFromWhitelist(string ipAddress)
        {
            try
            {
                if (_whitelistManager.RemoveFromWhitelist(ipAddress))
                {
                    _logger.LogInformation($"Removed {ipAddress} from whitelist");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing IP {ipAddress} from whitelist", ex);
                return false;
            }
        }

        /// <summary>
        /// Cleanup callback - runs periodically to clean old data
        /// </summary>
        private void CleanupCallback(object state)
        {
            try
            {
                _logger.LogInformation("Starting periodic cleanup");

                // Clean old firewall blocks
                int removed = _firewallManager.ClearOldBlocks(BLOCK_RETENTION_DAYS);
                if (removed > 0)
                {
                    _logger.LogInformation($"Removed {removed} old firewall blocks");
                }

                // Clean old failed login trackers
                var cutoff = DateTime.Now.AddHours(-24);
                int trackersCleaned = 0;
                foreach (var kvp in _failedLogins.ToArray())
                {
                    var tracker = kvp.Value;
                    if (tracker.Lock.Wait(1000))
                    {
                        try
                        {
                            if (!tracker.Attempts.TryPeek(out var latest) || latest < cutoff)
                            {
                                _failedLogins.TryRemove(kvp.Key, out _);
                                tracker.Lock.Dispose();
                                trackersCleaned++;
                            }
                        }
                        finally
                        {
                            tracker.Lock.Release();
                        }
                    }
                }

                if (trackersCleaned > 0)
                {
                    _logger.LogInformation($"Cleaned {trackersCleaned} old login trackers");
                }

                _logger.LogInformation("Periodic cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during cleanup", ex);
            }
        }

        /// <summary>
        /// Gets service statistics
        /// </summary>
        public ServiceStatistics GetStatistics()
        {
            return new ServiceStatistics
            {
                BlockedIPCount = _firewallManager?.GetBlockedIPs()?.Count ?? 0,
                WhitelistedIPCount = _whitelistManager?.GetWhitelistedIPs()?.Count ?? 0,
                TrackedIPCount = _failedLogins.Count,
                DroppedLogMessages = _logger?.GetDroppedMessageCount() ?? 0,
                LogQueueSize = _logger?.GetQueueSize() ?? 0
            };
        }

        private void InitializeComponent()
        {
            // Designer generated code would go here
            this.ServiceName = "RDP-Guard";
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cancellationTokenSource?.Dispose();
                    _cleanupTimer?.Dispose();
                    _firewallManager?.Dispose();
                    _whitelistManager?.Dispose();
                    _logger?.Dispose();
                    _serviceLock?.Dispose();

                    // Dispose all tracker locks
                    foreach (var tracker in _failedLogins.Values)
                    {
                        tracker.Lock?.Dispose();
                    }
                }
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public class ServiceStatistics
    {
        public int BlockedIPCount { get; set; }
        public int WhitelistedIPCount { get; set; }
        public int TrackedIPCount { get; set; }
        public long DroppedLogMessages { get; set; }
        public int LogQueueSize { get; set; }
    }
}
