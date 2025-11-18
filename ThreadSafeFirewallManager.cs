using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NetFwTypeLib;

namespace RDPGuard
{
    /// <summary>
    /// Thread-safe firewall manager that prevents deadlocks through:
    /// 1. Async operation queuing
    /// 2. Single-threaded firewall API access
    /// 3. Timeout-based operations
    /// </summary>
    public class ThreadSafeFirewallManager : IDisposable
    {
        private readonly SemaphoreSlim _firewallSemaphore;
        private readonly ConcurrentDictionary<string, DateTime> _blockedIPs;
        private readonly ConcurrentQueue<FirewallOperation> _operationQueue;
        private readonly Thread _processingThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int _operationTimeoutMs = 10000;
        private readonly string _rulePrefix = "RDP-Guard Block:";
        private bool _disposed = false;

        private class FirewallOperation
        {
            public string IPAddress { get; set; }
            public OperationType Type { get; set; }
            public ManualResetEventSlim CompletionEvent { get; set; }
            public bool Success { get; set; }
            public DateTime QueuedTime { get; set; }
        }

        private enum OperationType
        {
            Block,
            Unblock,
            CheckExists
        }

        public ThreadSafeFirewallManager()
        {
            _firewallSemaphore = new SemaphoreSlim(1, 1);
            _blockedIPs = new ConcurrentDictionary<string, DateTime>();
            _operationQueue = new ConcurrentQueue<FirewallOperation>();
            _cancellationTokenSource = new CancellationTokenSource();

            // Single processing thread to serialize all firewall operations
            _processingThread = new Thread(ProcessFirewallOperations)
            {
                IsBackground = true,
                Name = "Firewall-Operation-Processor"
            };
            _processingThread.Start();
        }

        /// <summary>
        /// Blocks an IP address with timeout protection
        /// </summary>
        public bool BlockIP(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            // Check if already blocked
            if (_blockedIPs.ContainsKey(ipAddress))
                return true;

            var operation = new FirewallOperation
            {
                IPAddress = ipAddress,
                Type = OperationType.Block,
                CompletionEvent = new ManualResetEventSlim(false),
                QueuedTime = DateTime.Now
            };

            _operationQueue.Enqueue(operation);

            // Wait for completion with timeout
            if (operation.CompletionEvent.Wait(_operationTimeoutMs))
            {
                operation.CompletionEvent.Dispose();
                if (operation.Success)
                {
                    _blockedIPs.TryAdd(ipAddress, DateTime.Now);
                }
                return operation.Success;
            }
            else
            {
                // Timeout
                operation.CompletionEvent.Dispose();
                EventLog.WriteEntry("RDP-Guard",
                    $"Firewall block operation timeout for IP: {ipAddress}",
                    EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Unblocks an IP address with timeout protection
        /// </summary>
        public bool UnblockIP(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            var operation = new FirewallOperation
            {
                IPAddress = ipAddress,
                Type = OperationType.Unblock,
                CompletionEvent = new ManualResetEventSlim(false),
                QueuedTime = DateTime.Now
            };

            _operationQueue.Enqueue(operation);

            // Wait for completion with timeout
            if (operation.CompletionEvent.Wait(_operationTimeoutMs))
            {
                operation.CompletionEvent.Dispose();
                if (operation.Success)
                {
                    _blockedIPs.TryRemove(ipAddress, out _);
                }
                return operation.Success;
            }
            else
            {
                // Timeout
                operation.CompletionEvent.Dispose();
                EventLog.WriteEntry("RDP-Guard",
                    $"Firewall unblock operation timeout for IP: {ipAddress}",
                    EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Checks if an IP is blocked
        /// </summary>
        public bool IsBlocked(string ipAddress)
        {
            return _blockedIPs.ContainsKey(ipAddress);
        }

        /// <summary>
        /// Gets all currently blocked IPs
        /// </summary>
        public Dictionary<string, DateTime> GetBlockedIPs()
        {
            return new Dictionary<string, DateTime>(_blockedIPs);
        }

        /// <summary>
        /// Clears old blocks (older than specified days)
        /// </summary>
        public int ClearOldBlocks(int daysOld)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var toRemove = _blockedIPs
                .Where(kvp => kvp.Value < cutoffDate)
                .Select(kvp => kvp.Key)
                .ToList();

            int removed = 0;
            foreach (var ip in toRemove)
            {
                if (UnblockIP(ip))
                    removed++;
            }

            return removed;
        }

        /// <summary>
        /// Single-threaded processing of all firewall operations
        /// This prevents COM threading issues and deadlocks
        /// </summary>
        private void ProcessFirewallOperations()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_operationQueue.TryDequeue(out var operation))
                    {
                        // Check for stale operations
                        if ((DateTime.Now - operation.QueuedTime).TotalMilliseconds > _operationTimeoutMs * 2)
                        {
                            operation.Success = false;
                            operation.CompletionEvent.Set();
                            continue;
                        }

                        switch (operation.Type)
                        {
                            case OperationType.Block:
                                operation.Success = BlockIPInternal(operation.IPAddress);
                                break;
                            case OperationType.Unblock:
                                operation.Success = UnblockIPInternal(operation.IPAddress);
                                break;
                            case OperationType.CheckExists:
                                operation.Success = RuleExistsInternal(operation.IPAddress);
                                break;
                        }

                        operation.CompletionEvent.Set();
                    }
                    else
                    {
                        // No operations, sleep briefly
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("RDP-Guard",
                        $"Error in firewall operation processor: {ex.Message}",
                        EventLogEntryType.Error);
                    Thread.Sleep(1000); // Back off on error
                }
            }
        }

        /// <summary>
        /// Internal method to block IP - must only be called from processing thread
        /// </summary>
        private bool BlockIPInternal(string ipAddress)
        {
            try
            {
                // Check if rule already exists
                if (RuleExistsInternal(ipAddress))
                    return true;

                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

                Type tNetFwRule = Type.GetTypeFromProgID("HNetCfg.FWRule");
                INetFwRule rule = (INetFwRule)Activator.CreateInstance(tNetFwRule);

                rule.Name = _rulePrefix + " " + ipAddress;
                rule.Description = $"Blocked by RDP-Guard on {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY;
                rule.RemoteAddresses = ipAddress;
                rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
                rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                rule.Enabled = true;
                rule.InterfaceTypes = "All";

                fwPolicy2.Rules.Add(rule);

                EventLog.WriteEntry("RDP-Guard",
                    $"Successfully blocked IP: {ipAddress}",
                    EventLogEntryType.Information);

                return true;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("RDP-Guard",
                    $"Error blocking IP {ipAddress}: {ex.Message}",
                    EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Internal method to unblock IP - must only be called from processing thread
        /// </summary>
        private bool UnblockIPInternal(string ipAddress)
        {
            try
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

                string ruleName = _rulePrefix + " " + ipAddress;

                // Find and remove the rule
                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    if (rule.Name == ruleName)
                    {
                        fwPolicy2.Rules.Remove(ruleName);
                        EventLog.WriteEntry("RDP-Guard",
                            $"Successfully unblocked IP: {ipAddress}",
                            EventLogEntryType.Information);
                        return true;
                    }
                }

                return false; // Rule not found
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("RDP-Guard",
                    $"Error unblocking IP {ipAddress}: {ex.Message}",
                    EventLogEntryType.Error);
                return false;
            }
        }

        /// <summary>
        /// Internal method to check if rule exists - must only be called from processing thread
        /// </summary>
        private bool RuleExistsInternal(string ipAddress)
        {
            try
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

                string ruleName = _rulePrefix + " " + ipAddress;

                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    if (rule.Name == ruleName)
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("RDP-Guard",
                    $"Error checking if rule exists for IP {ipAddress}: {ex.Message}",
                    EventLogEntryType.Error);
                return false;
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
                    _cancellationTokenSource?.Cancel();
                    _processingThread?.Join(5000); // Wait up to 5 seconds
                    _cancellationTokenSource?.Dispose();
                    _firewallSemaphore?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
