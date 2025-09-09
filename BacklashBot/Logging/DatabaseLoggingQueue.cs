using KalshiBotData.Data.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using System.Collections.Concurrent;

namespace BacklashBot.Logging
{
    public class DatabaseLoggingQueue : BackgroundService
    {
        private readonly ConcurrentQueue<LogEntryDTO> _logQueue = new ConcurrentQueue<LogEntryDTO>();
        private readonly IServiceProvider _serviceProvider;

        // Changed from ConcurrentQueue<(Exception Exception, string Identifier)>
        public ConcurrentQueue<ErrorHandlerTaskInfo> Warnings { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();
        public ConcurrentQueue<ErrorHandlerTaskInfo> Errors { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();

        public DatabaseLoggingQueue(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void EnqueueDBLogs(LogEntryDTO logEntry)
        {
            _logQueue.Enqueue(logEntry);
        }

        // Modified signature and logic
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
                // SmokehouseErrorHandler will need to handle cases where OriginalException might be null,
                // though for Error/Critical, an exception is expected.
                Errors.Enqueue(taskInfo);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("DatabaseLoggingQueue starting at {0}", DateTime.UtcNow);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logQueue.TryDequeue(out var logEntry))
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                        // Format the timestamp
                        var formattedTimestamp = logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                        try
                        {
                            //// Construct the main log line
                            //var logLine = $"[{formattedTimestamp}] [{logEntry.Level}] [{logEntry.Source}] [{logEntry.Environment}] [{logEntry.BrainInstance}]: {logEntry.Message}";

                            //// Write to the log file
                            //File.AppendAllText("logs/logging.log", logLine + "\n");
                            //if (!string.IsNullOrEmpty(logEntry.Exception))
                            //{
                            //    File.AppendAllText("logs/logging.log", $"Exception: {logEntry.Exception}\n");
                            //}
                            //File.AppendAllText("logs/logging.log", "-----\n");

                            // Save to the database
                            await context.AddLogEntry(logEntry);
                        }
                        catch (Exception ex)
                        {
                            // Log the database failure to console and file
                            var errorMessage = $"Failed to save log to database: {ex.Message}, Inner: {ex.InnerException?.Message}";
                            Console.WriteLine(errorMessage);
                            try
                            {
                                File.AppendAllText("logs/logging.log", errorMessage + "\n");
                            }
                            catch (IOException ioEx)
                            {
                                Console.WriteLine($"Failed to write error to log file: {ioEx.Message}");
                            }
                            // Log the original entry to console
                            Console.WriteLine($"{formattedTimestamp} [{logEntry.Level}] {logEntry.Source}: {logEntry.Message} {(logEntry.Exception != null ? logEntry.Exception : string.Empty)}");
                        }
                    }
                }
                else
                {
                    await Task.Delay(100, stoppingToken); // Wait briefly if queue is empty
                }
            }
            Console.WriteLine("DatabaseLoggingQueue stopping at {0}", DateTime.UtcNow);
        }

        // StartAsync is part of IHostedService, which BackgroundService implements.
        // The base BackgroundService.StartAsync returns Task.CompletedTask.
        // This explicit implementation might have been for specific logging or initialization.
        // If not doing anything special beyond what BackgroundService.StartAsync does, it could be removed
        // and `ExecuteAsync` would still be called.
        // For now, keeping it as it was in the original file provided.
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("DatabaseLoggingQueue StartAsync called at {0}", DateTime.UtcNow);
            return base.StartAsync(cancellationToken); // Proper way to call base StartAsync if overriding
        }
    }
}
