using BacklashBot.Configuration;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Exceptions;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotLogging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;

namespace BacklashBot.Management
{
    /// <summary>
    /// Central error handling service that processes logged errors and warnings from the system,
    /// determines catastrophic failure conditions, and triggers appropriate recovery actions.
    /// This class acts as the main error coordinator for the Kalshi trading bot, handling various
    /// exception types with specific recovery strategies including market resets, connection
    /// recovery, and system restarts.
    /// </summary>
    /// <remarks>
    /// The error handler processes errors from a database logging queue and categorizes them
    /// as either catastrophic (requiring system restart) or non-catastrophic (handled with
    /// market resets or connection recovery). It maintains counters for error tracking and
    /// implements threshold-based catastrophic detection based on configurable error frequency parameters.
    ///
    /// Configuration options include:
    /// - Error window duration for frequency monitoring
    /// - Error threshold for catastrophic detection
    /// - Internet connectivity check parameters (max attempts, delays)
    ///
    /// Key responsibilities:
    /// - Process warnings and errors from the logging queue
    /// - Handle specific exception types with targeted recovery actions
    /// - Monitor error frequency and detect catastrophic conditions
    /// - Maintain error statistics and timestamps
    /// - Trigger market resets for transient failures
    /// - Manage internet connectivity checks for connection-related errors
    /// - Validate input parameters to prevent null reference exceptions
    /// </remarks>
    public class CentralErrorHandler : ICentralErrorHandler
    {
        private readonly IMarketManagerService _marketManagerService;
        private readonly IServiceFactory _serviceFactory;
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly CentralErrorHandlerConfig _errorHandlerConfig;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly ConcurrentQueue<(DateTime Timestamp, ErrorHandlerTaskInfo Error)> _nonCatastrophicErrors = new();
        private readonly TimeSpan _errorWindow; // Configurable window
        private readonly int _errorThreshold; // Configurable threshold
        private ILogger<ICentralErrorHandler> _logger;

        /// <summary>
        /// Gets or sets the total count of warnings processed by the error handler.
        /// </summary>
        public long WarningCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total count of errors processed by the error handler.
        /// </summary>
        public long ErrorCount { get; set; } = 0;

        /// <summary>
        /// Gets the current count of non-catastrophic errors within the monitoring window.
        /// </summary>
        /// <remarks>
        /// This count is used to determine if the error threshold has been exceeded,
        /// which would trigger a catastrophic failure condition.
        /// </remarks>
        public int NonCatastrophicErrorCount => _nonCatastrophicErrors.Count;

        /// <summary>
        /// Gets or sets a flag indicating whether a catastrophic error has already been detected.
        /// </summary>
        /// <remarks>
        /// When set to true, prevents further error processing to avoid redundant handling
        /// and focuses on system recovery procedures.
        /// </remarks>
        public bool CatastrophicErrorAlreadyDetected { get; set; } = false;

        /// <summary>
        /// Gets the queue of pending warnings to be processed.
        /// </summary>
        public ConcurrentQueue<ErrorHandlerTaskInfo> Warnings { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();

        /// <summary>
        /// Gets the queue of pending errors to be processed.
        /// </summary>
        public ConcurrentQueue<ErrorHandlerTaskInfo> Errors { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();

        /// <summary>
        /// Gets or sets the timestamp of the last successful snapshot creation.
        /// </summary>
        /// <remarks>
        /// Used to detect prolonged periods without successful snapshots,
        /// which may indicate a catastrophic system failure.
        /// </remarks>
        public DateTime LastSuccessfulSnapshot { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the timestamp of the last error occurrence.
        /// </summary>
        public DateTime LastErrorDate { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the CentralErrorHandler class.
        /// </summary>
        /// <param name="marketManagerService">Service for managing market operations and resets.</param>
        /// <param name="serviceFactory">Factory for creating and accessing various system services.</param>
        /// <param name="loggingQueue">Queue containing logged errors and warnings to be processed.</param>
        /// <param name="errorHandlerConfig">Configuration settings for error handling.</param>
        /// <param name="performanceMonitor">Monitor for recording performance metrics.</param>
        /// <param name="logger">Logger instance for recording error handler operations.</param>
        public CentralErrorHandler(
            IMarketManagerService marketManagerService,
            IServiceFactory serviceFactory,
            DatabaseLoggingQueue loggingQueue,
            IOptions<CentralErrorHandlerConfig> errorHandlerConfig,
            IPerformanceMonitor performanceMonitor,
            ILogger<ICentralErrorHandler> logger)
        {
            _logger = logger;
            _marketManagerService = marketManagerService;
            _serviceFactory = serviceFactory;
            _loggingQueue = loggingQueue;
            _errorHandlerConfig = errorHandlerConfig.Value;
            _performanceMonitor = performanceMonitor;
            _errorWindow = TimeSpan.FromMinutes(_errorHandlerConfig.ErrorWindowMinutes);
            _errorThreshold = _errorHandlerConfig.ErrorThreshold;
            LastSuccessfulSnapshot = DateTime.MinValue;
        }

        /// <summary>
        /// Processes all pending errors and warnings from the logging queue, determines if any
        /// constitute catastrophic failures, and triggers appropriate recovery actions.
        /// </summary>
        /// <returns>True if a catastrophic error was detected requiring system restart; otherwise false.</returns>
        /// <remarks>
        /// This method performs the following operations:
        /// 1. Dequeues all errors and warnings from the logging queue
        /// 2. Processes warnings, converting those with exceptions to errors if necessary
        /// 3. Handles each error based on its exception type with specific recovery strategies
        /// 4. Monitors error frequency and triggers catastrophic failure if thresholds are exceeded
        /// 5. Cleans up old non-catastrophic errors and clears processed queues
        ///
        /// Specific exception handling includes:
        /// - MarketInterestScoreDeadlockException: Logged as handled
        /// - ConnectionDisruptionException: Attempts WebSocket reconnection
        /// - KnownDuplicateInsertException: Logged as handled
        /// - Various market-related exceptions: Triggers market resets
        /// - WebSocketRetryFailedException: Marked as catastrophic
        /// - Internet connectivity issues: Marked as catastrophic
        /// - Overseer connection failures: Logged as informational (not catastrophic)
        /// - Unhandled exceptions: Marked as catastrophic
        /// </remarks>
        public async Task<bool> HandleErrors()
        {
            var stopwatch = Stopwatch.StartNew();
            bool isCatastrophic = false;

            while (_loggingQueue.Errors.TryDequeue(out var errorTask))
            {
                Errors.Enqueue(errorTask);
            }
            while (_loggingQueue.Warnings.TryDequeue(out var warningTask))
            {
                Warnings.Enqueue(warningTask);
            }
            if (!Warnings.IsEmpty)
            {
                _logger.LogInformation("BRAIN: Processing warnings. Warnings to handle: {WarningCount}", Warnings.Count);
                while (Warnings.TryDequeue(out var warningTaskInfo))
                {
                    var exceptionFromWarning = warningTaskInfo.OriginalException;
                    var logSource = warningTaskInfo.LogSourceCategory;
                    var originalMessage = warningTaskInfo.FormattedMessage;

                    if (exceptionFromWarning != null)
                    {
                        _logger.LogInformation("BRAIN: Processing warning from {LogSource} (Message: '{OriginalMessage}'). Exception type: {ExceptionType}",
                            logSource, originalMessage, exceptionFromWarning.GetType().Name);

                        if (!Errors.Any(x => x.OriginalException == exceptionFromWarning
                            && x.FormattedMessage == warningTaskInfo.FormattedMessage))
                            Errors.Enqueue(new ErrorHandlerTaskInfo
                            {
                                OriginalException = exceptionFromWarning,
                                FormattedMessage = warningTaskInfo.FormattedMessage,
                                Severity = LogLevel.Error,
                                Timestamp = warningTaskInfo.Timestamp,
                                LogSourceCategory = warningTaskInfo.LogSourceCategory
                            });
                        ErrorCount++;
                        LastErrorDate = warningTaskInfo.Timestamp;
                    }
                    else
                    {
                        _logger.LogDebug("BRAIN: Processing warning (message-only) from {LogSource}. Original Message: '{OriginalMessage}'",
                            logSource, originalMessage);
                        WarningCount++;
                    }
                }
            }

            // Record warnings processed metric
            if (_errorHandlerConfig.EnablePerformanceMetrics)
            {
                _performanceMonitor.RecordCounterMetric(nameof(CentralErrorHandler), "WarningsProcessed", "Warnings Processed", "Total warnings processed in this cycle", WarningCount, "count", "ErrorHandling");
            }
            else
            {
                _performanceMonitor.RecordDisabledMetric(nameof(CentralErrorHandler), "WarningsProcessed", "Warnings Processed", "Total warnings processed in this cycle", WarningCount, "count", "ErrorHandling");
            }

            if (!Errors.IsEmpty)
            {
                _logger.LogInformation("BRAIN: Checking for catastrophic failure. Errors to handle: {ErrorCount}", Errors.Count);
                while (Errors.TryDequeue(out var errorTaskInfo) && !CatastrophicErrorAlreadyDetected)
                {
                    var exception = errorTaskInfo.OriginalException;
                    var identifier = errorTaskInfo.LogSourceCategory;
                    var message = errorTaskInfo.FormattedMessage;

                    bool isCatastrophicLocal = false;

                    if (exception == null)
                    {
                        _logger.LogWarning("CATASTROPHIC ERROR: OriginalException was null in ErrorHandlerTaskInfo but was logged as an error. Message: '{FormattedMessage}', Source: '{LogSourceCategory}'", message, identifier);
                        ErrorCount++;
                        LastErrorDate = errorTaskInfo.Timestamp;
                        isCatastrophicLocal = true;
                    }
                    else
                    {
                        _logger.LogDebug(exception, "BRAIN: Handling error from {Identifier}. Original Message: {Message}, Type: {ExceptionType}, Inner Exception: {InnerExceptionMessage}",
                            identifier, message, exception.GetType().FullName, exception.InnerException?.Message ?? "N/A");

                        if (exception is MarketInterestScoreDeadlockException midEx)
                        {
                            _logger.LogInformation("HANDLED ERROR: (MarketInterestScoreDeadlockException): {ErrorMessage} ... not catastrophic.", midEx.Message);
                        }
                        else if (exception is ConnectionDisruptionException condis)
                        {
                            bool InternetUp = await CheckInternetConnection();
                            if (!InternetUp)
                            {
                                _logger.LogCritical("BRAIN: Internet connection is down. Cannot reset connection. Please check your internet connection.");
                                ErrorCount++;
                                LastErrorDate = errorTaskInfo.Timestamp;
                                isCatastrophicLocal = true;
                            }
                            else
                            {
                                var webSocketClient = _serviceFactory.GetKalshiWebSocketClient();
                                if (webSocketClient != null)
                                {
                                    await webSocketClient.ResetConnectionAsync();
                                    _logger.LogInformation("HANDLED ERROR: (ConnectionDisruptionException): {ErrorMessage} ... not catastrophic.", condis.Message);
                                }
                                else
                                {
                                    _logger.LogWarning("HANDLED ERROR: (ConnectionDisruptionException): {ErrorMessage} ... WebSocket client not available.", condis.Message);
                                }
                            }
                        }
                        else if (exception is KnownDuplicateInsertException kdiEx)
                        {
                            _logger.LogInformation("HANDLED ERROR: (KnownDuplicateInsertException): EntityType: {EntityType}, KeyInfo: {KeyInfo}. Another instance likely inserted a lifecycle event. Message: {ErrorMessage}",
                                kdiEx.EntityType, kdiEx.DuplicateKeyInfo, kdiEx.Message);
                        }
                        else if (exception is CandlestickFetchException cfEx)
                        {
                            _marketManagerService.TriggerMarketReset(cfEx.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (CandlestickFetchException): Retrieving candlesticks failed for market {MarketId}. Resetting. Message: {ErrorMessage}", cfEx.MarketId, cfEx.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is SnapshotInvalidException ex)
                        {
                            _marketManagerService.TriggerMarketReset(ex.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (MajorPriceDiscrepancyException): while validating market {market} prices. Resetting. Message: {ErrorMessage}", ex.MarketId, ex.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is NotInCacheException nicEx)
                        {
                            _marketManagerService.TriggerMarketReset(nicEx.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (NotInCacheException): an orderbook update to {market} was applied before a market was fully loaded. Resetting. Message: {ErrorMessage}", nicEx.MarketId, nicEx.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is OrderbookTransientFailureException obtfEx)
                        {
                            _marketManagerService.TriggerMarketReset(obtfEx.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (OrderbookTransientFailureException): processing orderbook delta for market {market}. Resetting. Message: {ErrorMessage}", obtfEx.MarketId, obtfEx.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is CandlestickTransientFailureException cndletrEx)
                        {
                            _marketManagerService.TriggerMarketReset(cndletrEx.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (CandlestickTransientFailureException): processing candlesticks for market {market}. Resetting. Message: {ErrorMessage}", cndletrEx.MarketId, cndletrEx.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is MarketTransientFailureException mkttrEx)
                        {
                            _marketManagerService.TriggerMarketReset(mkttrEx.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (MarketTransientFailureException): processing market {market}. Resetting. Message: {ErrorMessage}", mkttrEx.MarketId, mkttrEx.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is MarketInvalidException mktInv)
                        {
                            _marketManagerService.TriggerMarketReset(mktInv.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (MarketInvalidException): processing market {market}. Resetting. Message: {ErrorMessage}", mktInv.MarketId, mktInv.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is WebSocketRetryFailedException wsrEx)
                        {
                            _logger.LogError("CATASTROPHIC ERROR: (WebSocketRetryFailedException): Connecting to web socket failed after retries. Restarting services. Message: {ErrorMessage}", wsrEx.Message);
                            ErrorCount++;
                            LastErrorDate = errorTaskInfo.Timestamp;
                            isCatastrophicLocal = true;
                        }
                        else if (exception is TradeMissedException tmEx)
                        {
                            _marketManagerService.TriggerMarketReset(tmEx.MarketId);
                            _logger.LogInformation("HANDLED ERROR: (TradeMissedException): Missed trade for market {MarketId}. Resetting. Message: {ErrorMessage}", tmEx.MarketId, tmEx.Message);
                            ErrorCount++;
                            LastErrorDate = errorTaskInfo.Timestamp;
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is DeadLockException dlex)
                        {
                            _logger.LogInformation("HANDLED ERROR: (DeadLockException): processing orderbook delta for market {market}. Resetting. Message: {ErrorMessage}", dlex.MarketId, dlex.Message);
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is ProcessingThresholdExceededException pteEx)
                        {
                            _logger.LogInformation("HANDLED ERROR: (ProcessingThresholdExceededException): {ErrorMessage}", pteEx.Message);
                            ErrorCount++;
                            LastErrorDate = errorTaskInfo.Timestamp;
                            _nonCatastrophicErrors.Enqueue((DateTime.Now, errorTaskInfo));
                        }
                        else if (exception is KalshiKeyFileNotFoundException keyex)
                        {
                            _logger.LogCritical(keyex, "CATASTROPHIC ERROR: Kalshi Key File is missing or there are permissions issues.");
                            ErrorCount++;
                            LastErrorDate = errorTaskInfo.Timestamp;
                            isCatastrophicLocal = true;
                        }
                        else if ((exception is HttpRequestException
                            && exception.Message.Contains("No such host is known")) ||
                            (exception is WebSocketException
                            && exception.Message.Contains("Unable to connect to the remote server")))
                        {
                            _logger.LogCritical(exception, "CATASTROPHIC ERROR: The internet may be down...");
                            ErrorCount++;
                            LastErrorDate = errorTaskInfo.Timestamp;
                            isCatastrophicLocal = true;
                        }
                        else if (identifier.Contains("OverseerClientService") &&
                                (exception is HttpRequestException && exception.Message.Contains("actively refused")))
                        {
                            // Overseer connection failure - this is expected when overseer is not running
                            _logger.LogInformation("OVERSEER- Overseer connection failed: {Message}. This is optional and not catastrophic.", exception.Message);
                            // Don't increment error count or mark as catastrophic
                        }
                        else if (exception is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Message.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("HANDLED ERROR: (SqlException - Duplicate Key): {ErrorMessage} ... not catastrophic.", sqlEx.Message);
                        }
                        else
                        {
                            _logger.LogCritical(exception, "UNHANDLED ERROR: from {Identifier}. Original Message: '{OriginalMessage}', Type: {ExceptionType}, Inner exception: {InnerExceptionMessage}",
                                identifier, message, exception.GetType().FullName, exception.InnerException?.Message ?? "N/A");
                            ErrorCount++;
                            LastErrorDate = errorTaskInfo.Timestamp;
                            isCatastrophicLocal = true;
                        }
                    }
                    if (isCatastrophicLocal) CatastrophicErrorAlreadyDetected = true;

                    // Clean up old non-catastrophic errors
                    while (_nonCatastrophicErrors.TryPeek(out var error) && error.Timestamp < DateTime.Now - _errorWindow)
                    {
                        _nonCatastrophicErrors.TryDequeue(out _);
                    }

                    // Check if non-catastrophic errors exceed threshold
                    if (_nonCatastrophicErrors.Count >= _errorThreshold && LastSuccessfulSnapshot <= DateTime.Now.AddMinutes(-3))
                    {
                        _logger.LogCritical("CATASTROPHIC ERROR: Non-catastrophic error threshold exceeded. {ErrorCount} errors in {Window} minutes.", _nonCatastrophicErrors.Count, _errorWindow.TotalMinutes);
                        CatastrophicErrorAlreadyDetected = true;
                        isCatastrophic = true;
                    }
                    else if (LastSuccessfulSnapshot != DateTime.MinValue && LastSuccessfulSnapshot <= DateTime.Now.AddMinutes(-10))
                    {
                        _logger.LogCritical("CATASTROPHIC ERROR: Snapshots have not been saved in the last 10 minutes. Last snapshot time: {0}", LastSuccessfulSnapshot);
                        CatastrophicErrorAlreadyDetected = true;
                        isCatastrophic = true;
                    }
                    else if (isCatastrophicLocal)
                    {
                        _logger.LogCritical("CATASTROPHIC ERROR: An error was detected that requires a restart.");
                        CatastrophicErrorAlreadyDetected = true;
                        isCatastrophic = true;
                    }
                }
            }

            // Record errors processed metrics
            if (_errorHandlerConfig.EnablePerformanceMetrics)
            {
                _performanceMonitor.RecordCounterMetric(nameof(CentralErrorHandler), "ErrorsProcessed", "Errors Processed", "Total errors processed in this cycle", ErrorCount, "count", "ErrorHandling");
                _performanceMonitor.RecordCounterMetric(nameof(CentralErrorHandler), "NonCatastrophicErrors", "Non-Catastrophic Errors", "Current count of non-catastrophic errors in window", NonCatastrophicErrorCount, "count", "ErrorHandling");
            }
            else
            {
                _performanceMonitor.RecordDisabledMetric(nameof(CentralErrorHandler), "ErrorsProcessed", "Errors Processed", "Total errors processed in this cycle", ErrorCount, "count", "ErrorHandling");
                _performanceMonitor.RecordDisabledMetric(nameof(CentralErrorHandler), "NonCatastrophicErrors", "Non-Catastrophic Errors", "Current count of non-catastrophic errors in window", NonCatastrophicErrorCount, "count", "ErrorHandling");
            }

            Errors.Clear();
            Warnings.Clear();

            stopwatch.Stop();

            // Record processing time metric
            if (_errorHandlerConfig.EnablePerformanceMetrics)
            {
                _performanceMonitor.RecordSpeedDialMetric(nameof(CentralErrorHandler), "HandleErrorsProcessingTime", "Handle Errors Processing Time", "Time taken to process errors and warnings", stopwatch.ElapsedMilliseconds, "ms", "ErrorHandling");
            }
            else
            {
                _performanceMonitor.RecordDisabledMetric(nameof(CentralErrorHandler), "HandleErrorsProcessingTime", "Handle Errors Processing Time", "Time taken to process errors and warnings", stopwatch.ElapsedMilliseconds, "ms", "ErrorHandling");
            }

            return isCatastrophic;
        }

      
        /// <summary>
        /// Performs a series of internet connectivity checks with exponential backoff retry logic.
        /// </summary>
        /// <returns>True if internet connection is confirmed; false if all attempts fail.</returns>
        /// <remarks>
        /// This method attempts to verify internet connectivity with configurable parameters.
        /// Used primarily during system startup to ensure network availability before proceeding
        /// with dashboard initialization.
        /// </remarks>
        public async Task<bool> CheckInternetConnection()
        {
            int maxAttempts = _errorHandlerConfig.InternetCheckMaxAttempts;
            int attempt = 0;
            int delayMs = _errorHandlerConfig.InternetCheckInitialDelayMs;
            while (attempt < maxAttempts)
            {
                if (await IsInternetUpAsync())
                {
                    _logger.LogInformation("BRAIN: Internet connection confirmed.");
                    break;
                }
                attempt++;
                if (attempt == maxAttempts)
                {
                    _logger.LogError("BRAIN: No internet connection after {Attempts} attempts. Aborting dashboard startup.", maxAttempts);
                    return false;
                }
                _logger.LogWarning("BRAIN: Internet down, retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})", delayMs, attempt, maxAttempts);
                await Task.Delay(delayMs);
                delayMs *= 2;
                if (delayMs > _errorHandlerConfig.InternetCheckMaxDelayMs)
                {
                    delayMs = _errorHandlerConfig.InternetCheckMaxDelayMs;
                }
            }

            return true;
        }

        /// <summary>
        /// Performs a single internet connectivity check by pinging Google's DNS server.
        /// </summary>
        /// <returns>True if the ping succeeds; false otherwise.</returns>
        /// <remarks>
        /// Uses ICMP ping to 8.8.8.8 (Google's public DNS) with a 1-second timeout.
        /// This is a simple connectivity test that doesn't guarantee full internet access
        /// but provides a reliable indicator of network availability.
        /// </remarks>
        private async Task<bool> IsInternetUpAsync()
        {
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync("8.8.8.8", 1000);
                bool isUp = reply.Status == System.Net.NetworkInformation.IPStatus.Success;
                _logger.LogDebug("BRAIN: Internet check: {Status}", isUp ? "Up" : "Down");
                return isUp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BRAIN: Error checking internet connectivity");
                return false;
            }
        }
    }
}
