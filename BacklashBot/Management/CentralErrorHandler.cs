using BacklashBot.Logging;
using BacklashBot.Management.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Exceptions;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.RegularExpressions;

namespace BacklashBot.Management
{
    public class CentralErrorHandler : ICentralErrorHandler
    {
        private readonly IMarketManagerService _marketManagerService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly DatabaseLoggingQueue _loggingQueue;
        private readonly ConcurrentQueue<(DateTime Timestamp, ErrorHandlerTaskInfo Error)> _nonCatastrophicErrors = new();
        private readonly TimeSpan _errorWindow = TimeSpan.FromMinutes(5); // Adjustable window
        private readonly int _errorThreshold = 10; // Adjustable threshold
        private ILogger<ICentralErrorHandler> _logger;
        public long WarningCount { get; set; } = 0;
        public long ErrorCount { get; set; } = 0;
        public int NonCatastrophicErrorCount => _nonCatastrophicErrors.Count;
        public bool CatastrophicErrorAlreadyDetected { get; set; } = false;
        public ConcurrentQueue<ErrorHandlerTaskInfo> Warnings { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();
        public ConcurrentQueue<ErrorHandlerTaskInfo> Errors { get; } = new ConcurrentQueue<ErrorHandlerTaskInfo>();

        public DateTime LastSuccessfulSnapshot { get; set; } = DateTime.MinValue;
        public DateTime LastErrorDate { get; set; } = DateTime.MinValue;

        public CentralErrorHandler(
            IMarketManagerService marketManagerService,
            IServiceFactory serviceFactory,
            DatabaseLoggingQueue loggingQueue,
            ILogger<ICentralErrorHandler> logger)
        {
            _logger = logger;
            _marketManagerService = marketManagerService;
            _serviceFactory = serviceFactory;
            _loggingQueue = loggingQueue;
            LastSuccessfulSnapshot = DateTime.MinValue;
        }

        public async Task<bool> HandleErrors()
        {
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
                _logger.LogDebug("BRAIN: Processing warnings. Warnings to handle: {WarningCount}", Warnings.Count);
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

            if (!Errors.IsEmpty)
            {
                _logger.LogDebug("BRAIN: Checking for catastrophic failure. Errors to handle: {ErrorCount}", Errors.Count);
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
                                await _serviceFactory.GetKalshiWebSocketClient().ResetConnectionAsync();
                                _logger.LogInformation("HANDLED ERROR: (ConnectionDisruptionException): {ErrorMessage} ... not catastrophic.", condis.Message);
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
                            _logger.LogInformation("Overseer connection failed: {Message}. This is optional and not catastrophic.", exception.Message);
                            // Don't increment error count or mark as catastrophic
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

            Errors.Clear();
            Warnings.Clear();
            return isCatastrophic;
        }

        public void AddWarning(Exception ex, string identifier, string? message = null)
        {
            var cts = new CancellationTokenSource();
            Warnings.Enqueue(new ErrorHandlerTaskInfo
            {
                OriginalException = ex,
                FormattedMessage = message ?? ex?.Message ?? "Warning added directly.",
                LogSourceCategory = identifier,
                Severity = LogLevel.Warning,
                Timestamp = DateTime.Now
            });
            WarningCount++;
        }

        public void AddError(Exception ex, string identifier, string? message = null)
        {
            var cts = new CancellationTokenSource();
            var timestamp = DateTime.Now;
            Errors.Enqueue(new ErrorHandlerTaskInfo
            {
                OriginalException = ex ?? new UnhandledSmokehouseException(message ?? "Error added directly with no original exception."),
                FormattedMessage = message ?? ex?.Message ?? "Error added directly.",
                LogSourceCategory = identifier,
                Severity = LogLevel.Error,
                Timestamp = timestamp
            });
            ErrorCount++;
            LastErrorDate = timestamp;
        }

        private string ExtractValue(string logMessage, string variableName)
        {
            string pattern = $@"{variableName}\s*:\s*""([^""]+)""";
            var match = Regex.Match(logMessage, pattern);
            if (match.Success && match.Groups.Count > 1) return match.Groups[1].Value;
            _logger.LogTrace("ExtractValue called for '{VariableName}' in message '{LogMessage}', returning empty. Consider using custom exceptions.", variableName, logMessage);
            return string.Empty;
        }

        private bool MatchesTemplate(string logMessage, string template)
        {
            string pattern = Regex.Escape(template).Replace(@"\{\w+\}", ".*?");
            return Regex.IsMatch(logMessage, $"^{pattern}$");
        }

        public async Task<bool> CheckInternetConnection()
        {
            int maxAttempts = 100;
            int attempt = 0;
            int delayMs = 1000;
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
                if (delayMs > 60000)
                {
                    delayMs = 60000;
                }
            }

            return true;
        }

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
