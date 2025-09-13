using KalshiBotData.Data.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace KalshiBotLogging
{
    /// <summary>
    /// Background service that asynchronously processes log entries from a concurrent queue
    /// and persists them to the database with batching support for improved performance.
    /// Also manages error handler queues for warnings and errors.
    /// Supports both regular application logging and overseer-specific logging contexts.
    /// Provides metrics and monitoring for queue depth and processing performance.
    /// </summary>
    public class DatabaseLoggingQueue : BackgroundService
    {
        private readonly ConcurrentQueue<LogEntryDTO> _logQueue = new ConcurrentQueue<LogEntryDTO>();
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isOverseer;
        private const int BatchSize = 50; // Configurable batch size for database operations
        private long _totalProcessedLogs;
        private long _totalProcessingTimeMs;

        /// <summary>
        /// Queue for warning-level log entries that need to be processed by the error handler.
        /// </summary>
        public ConcurrentQueue<ErrorHandlerTaskInfo> Warnings { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();

        /// <summary>
        /// Queue for error and critical-level log entries that need to be processed by the error handler.
        /// </summary>
        public ConcurrentQueue<ErrorHandlerTaskInfo> Errors { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();

        /// <summary>
        /// Gets the current depth of the database log queue for monitoring purposes.
        /// </summary>
        public int QueueDepth => _logQueue.Count;

        /// <summary>
        /// Gets the total number of logs processed since startup.
        /// </summary>
        public long TotalProcessedLogs => _totalProcessedLogs;

        /// <summary>
        /// Gets the average processing time per log entry in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs => _totalProcessedLogs > 0 ? (double)_totalProcessingTimeMs / _totalProcessedLogs : 0;

        /// <summary>
        /// Initializes a new instance of the DatabaseLoggingQueue.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection.</param>
        /// <param name="isOverseer">Indicates whether this queue is for overseer-specific logging.</param>
        public DatabaseLoggingQueue(IServiceProvider serviceProvider, bool isOverseer = false)
        {
            _serviceProvider = serviceProvider;
            _isOverseer = isOverseer;
        }

        /// <summary>
        /// Enqueues a log entry for asynchronous database storage with batching support.
        /// </summary>
        /// <param name="logEntry">The log entry to be stored in the database.</param>
        /// <returns>A task representing the asynchronous enqueue operation.</returns>
        public Task EnqueueDBLogsAsync(LogEntryDTO logEntry)
        {
            _logQueue.Enqueue(logEntry);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Enqueues log entries for processing by the error handler based on severity level.
        /// Warnings are queued separately from errors for different processing priorities.
        /// </summary>
        /// <param name="formattedMessage">The formatted log message.</param>
        /// <param name="logSourceCategory">The source category of the log entry.</param>
        /// <param name="severity">The severity level of the log entry.</param>
        /// <param name="originalException">The original exception if available.</param>
        /// <param name="timestamp">The timestamp of the log entry, defaults to current time.</param>
        public void EnqueueErrorHandlerLogs(
            string formattedMessage,
            string logSourceCategory,
            LogLevel severity,
            Exception? originalException = null,
            DateTime? timestamp = null)
        {
            var taskInfo = new ErrorHandlerTaskInfo
            {
                FormattedMessage = formattedMessage,
                LogSourceCategory = logSourceCategory,
                Severity = severity,
                OriginalException = originalException,
                Timestamp = timestamp ?? DateTime.Now // Consistent with LogEntry's DateTime.Now
            };

            if (severity == LogLevel.Warning)
            {
                Warnings.Enqueue(taskInfo);
            }
            else if ((severity == LogLevel.Error || severity == LogLevel.Critical) && !formattedMessage.Contains("UNHANDLED ERROR"))
            {
                // Error handler will handle cases where OriginalException might be null
                Errors.Enqueue(taskInfo);
            }
        }

        /// <summary>
        /// Executes the background processing loop for database logging with batching support.
        /// Continuously processes log entries from the queue in batches and saves them to the database for improved performance.
        /// Tracks processing metrics including queue depth and processing times.
        /// </summary>
        /// <param name="stoppingToken">Cancellation token to stop the background processing.</param>
        /// <returns>A task representing the background operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("DatabaseLoggingQueue starting at {0}", DateTime.UtcNow);
            var batch = new List<LogEntryDTO>();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Collect batch of log entries
                while (batch.Count < BatchSize && _logQueue.TryDequeue(out var logEntry))
                {
                    batch.Add(logEntry);
                }

                if (batch.Count > 0)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                        try
                        {
                            // Save batch to the appropriate database table based on context
                            foreach (var logEntry in batch)
                            {
                                if (_isOverseer)
                                {
                                    await context.AddOverseerLogEntry(logEntry);
                                }
                                else
                                {
                                    await context.AddLogEntry(logEntry);
                                }
                            }

                            // Update metrics
                            Interlocked.Add(ref _totalProcessedLogs, batch.Count);
                            Interlocked.Add(ref _totalProcessingTimeMs, stopwatch.ElapsedMilliseconds);
                        }
                        catch (Exception ex)
                        {
                            // Log database failure to console with structured format
                            var errorMessage = $"DatabaseLoggingQueue: Failed to save batch of {batch.Count} log entries - {ex.Message}";
                            if (ex.InnerException != null)
                            {
                                errorMessage += $" | Inner: {ex.InnerException.Message}";
                            }
                            Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [ERROR] {errorMessage}");

                            // Log details of failed entries
                            foreach (var logEntry in batch)
                            {
                                Console.WriteLine($"{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss} [{logEntry.Level}] {logEntry.Source}: {logEntry.Message}");
                            }
                        }
                    }

                    batch.Clear();
                }
                else
                {
                    await Task.Delay(100, stoppingToken); // Wait briefly if queue is empty
                }
            }
            Console.WriteLine("DatabaseLoggingQueue stopping at {0}", DateTime.UtcNow);
        }

        /// <summary>
        /// Starts the background service and logs the startup event.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the startup operation.</param>
        /// <returns>A task representing the startup operation.</returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("DatabaseLoggingQueue StartAsync called at {0}", DateTime.UtcNow);
            return base.StartAsync(cancellationToken);
        }
    }
}