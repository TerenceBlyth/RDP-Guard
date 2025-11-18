using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace RDPGuard
{
    /// <summary>
    /// Thread-safe event logger that prevents deadlocks through:
    /// 1. Async queue-based logging
    /// 2. Timeout-based operations
    /// 3. Circular buffer for high-frequency scenarios
    /// </summary>
    public class ThreadSafeEventLogger : IDisposable
    {
        private readonly ConcurrentQueue<LogEntry> _logQueue;
        private readonly Thread _loggingThread;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _throttleSemaphore;
        private readonly string _sourceName;
        private readonly int _maxQueueSize = 1000;
        private readonly int _throttleDelayMs = 100;
        private long _droppedMessages = 0;
        private bool _disposed = false;

        private class LogEntry
        {
            public string Message { get; set; }
            public EventLogEntryType Type { get; set; }
            public DateTime Timestamp { get; set; }
            public int EventId { get; set; }
        }

        public ThreadSafeEventLogger(string sourceName = "RDP-Guard")
        {
            _sourceName = sourceName;
            _logQueue = new ConcurrentQueue<LogEntry>();
            _throttleSemaphore = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();

            // Create event source if it doesn't exist
            CreateEventSourceIfNeeded();

            // Single logging thread to prevent EventLog COM deadlocks
            _loggingThread = new Thread(ProcessLogEntries)
            {
                IsBackground = true,
                Name = "EventLog-Processor"
            };
            _loggingThread.Start();
        }

        /// <summary>
        /// Logs an information message
        /// </summary>
        public void LogInformation(string message, int eventId = 1000)
        {
            EnqueueLog(message, EventLogEntryType.Information, eventId);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public void LogWarning(string message, int eventId = 2000)
        {
            EnqueueLog(message, EventLogEntryType.Warning, eventId);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        public void LogError(string message, int eventId = 3000)
        {
            EnqueueLog(message, EventLogEntryType.Error, eventId);
        }

        /// <summary>
        /// Logs an error with exception details
        /// </summary>
        public void LogError(string message, Exception ex, int eventId = 3000)
        {
            string fullMessage = $"{message}\n\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}";
            EnqueueLog(fullMessage, EventLogEntryType.Error, eventId);
        }

        /// <summary>
        /// Logs a successful block operation
        /// </summary>
        public void LogBlock(string ipAddress, string reason)
        {
            string message = $"Blocked IP Address: {ipAddress}\nReason: {reason}\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            EnqueueLog(message, EventLogEntryType.Warning, 4001);
        }

        /// <summary>
        /// Logs an unblock operation
        /// </summary>
        public void LogUnblock(string ipAddress, string reason)
        {
            string message = $"Unblocked IP Address: {ipAddress}\nReason: {reason}\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            EnqueueLog(message, EventLogEntryType.Information, 4002);
        }

        /// <summary>
        /// Logs a whitelisted IP attempt
        /// </summary>
        public void LogWhitelistedAttempt(string ipAddress)
        {
            string message = $"Failed login from whitelisted IP: {ipAddress}\nNo action taken.\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            EnqueueLog(message, EventLogEntryType.Information, 4003);
        }

        /// <summary>
        /// Gets the number of dropped messages due to queue overflow
        /// </summary>
        public long GetDroppedMessageCount()
        {
            return Interlocked.Read(ref _droppedMessages);
        }

        /// <summary>
        /// Gets the current queue size
        /// </summary>
        public int GetQueueSize()
        {
            return _logQueue.Count;
        }

        private void EnqueueLog(string message, EventLogEntryType type, int eventId)
        {
            // Check queue size to prevent memory exhaustion
            if (_logQueue.Count >= _maxQueueSize)
            {
                Interlocked.Increment(ref _droppedMessages);

                // Try to log dropped message warning periodically
                if (_droppedMessages % 100 == 0)
                {
                    // Emergency direct log (risky but necessary)
                    try
                    {
                        EventLog.WriteEntry(_sourceName,
                            $"WARNING: Log queue overflow. Dropped {_droppedMessages} messages.",
                            EventLogEntryType.Warning);
                    }
                    catch { /* Suppress to prevent cascade */ }
                }
                return;
            }

            var entry = new LogEntry
            {
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                EventId = eventId
            };

            _logQueue.Enqueue(entry);
        }

        private void ProcessLogEntries()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_logQueue.TryDequeue(out var entry))
                    {
                        // Throttle to prevent event log flooding
                        _throttleSemaphore.Wait(_cancellationTokenSource.Token);
                        try
                        {
                            WriteToEventLog(entry);

                            // Small delay to prevent overwhelming the event log
                            if (_logQueue.Count > 10)
                            {
                                Thread.Sleep(_throttleDelayMs);
                            }
                        }
                        finally
                        {
                            _throttleSemaphore.Release();
                        }
                    }
                    else
                    {
                        // No entries, sleep briefly
                        Thread.Sleep(500);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    // Log to console as fallback (for debugging)
                    Console.WriteLine($"Error in log processor: {ex.Message}");
                    Thread.Sleep(1000); // Back off on error
                }
            }

            // Flush remaining entries on shutdown
            FlushRemainingEntries();
        }

        private void WriteToEventLog(LogEntry entry)
        {
            try
            {
                // Use timeout to prevent deadlock
                var writeTask = System.Threading.Tasks.Task.Run(() =>
                {
                    EventLog.WriteEntry(_sourceName, entry.Message, entry.Type, entry.EventId);
                });

                if (!writeTask.Wait(5000))
                {
                    // Timeout - increment dropped counter
                    Interlocked.Increment(ref _droppedMessages);
                }
            }
            catch (Exception ex)
            {
                // Fallback to console for debugging
                Console.WriteLine($"Failed to write to event log: {ex.Message}");
                Console.WriteLine($"Original message: {entry.Message}");
                Interlocked.Increment(ref _droppedMessages);
            }
        }

        private void FlushRemainingEntries()
        {
            int flushed = 0;
            while (_logQueue.TryDequeue(out var entry) && flushed < 100)
            {
                try
                {
                    WriteToEventLog(entry);
                    flushed++;
                }
                catch
                {
                    // Suppress errors during shutdown
                    break;
                }
            }
        }

        private void CreateEventSourceIfNeeded()
        {
            try
            {
                if (!EventLog.SourceExists(_sourceName))
                {
                    EventLog.CreateEventSource(_sourceName, "Application");
                    Thread.Sleep(1000); // Wait for event source to be created
                }
            }
            catch (Exception ex)
            {
                // Log to console if we can't create event source
                Console.WriteLine($"Warning: Could not create event source '{_sourceName}': {ex.Message}");
                Console.WriteLine("Service may need to be run as Administrator to create event sources.");
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
                    _loggingThread?.Join(10000); // Wait up to 10 seconds for flush
                    _cancellationTokenSource?.Dispose();
                    _throttleSemaphore?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
