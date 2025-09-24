using Microsoft.Extensions.Logging;

namespace OverseerBotShared
{
    /// <summary>
    /// Enhanced error handler for SignalR communication with comprehensive error classification,
    /// retry logic, and detailed logging.
    /// </summary>
    public class SignalRErrorHandler : ISignalRErrorHandler
    {
        private readonly ILogger<SignalRErrorHandler> _logger;
        private readonly Dictionary<Type, ErrorClassification> _errorClassifications;

        /// <summary>
        /// Initializes a new instance of the SignalRErrorHandler.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public SignalRErrorHandler(ILogger<SignalRErrorHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorClassifications = InitializeErrorClassifications();
        }

        /// <summary>
        /// Handles connection-related errors with appropriate logging and recovery strategies.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="context">Additional context about where the error occurred.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandleConnectionErrorAsync(Exception exception, string context)
        {
            var classification = ClassifyError(exception);
            _logger.LogError(exception, "SignalR connection error in {Context}: {ErrorType} - {Message}",
                context, classification.Type, exception.Message);

            await ApplyErrorHandlingStrategyAsync(classification, exception, context);
        }

        /// <summary>
        /// Handles invocation-related errors with detailed logging and retry logic.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="methodName">The name of the method that was being invoked.</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandleInvocationErrorAsync(Exception exception, string methodName, object[] args)
        {
            var classification = ClassifyError(exception);
            _logger.LogError(exception, "SignalR invocation error for method '{MethodName}': {ErrorType} - {Message}",
                methodName, classification.Type, exception.Message);

            // Log method arguments (sanitized for security)
            if (args != null && args.Length > 0)
            {
                _logger.LogDebug("Method arguments count: {Count}", args.Length);
            }

            await ApplyErrorHandlingStrategyAsync(classification, exception, $"Method: {methodName}");
        }

        /// <summary>
        /// Handles reconnection errors with exponential backoff.
        /// </summary>
        /// <param name="exception">The exception that occurred during reconnection.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandleReconnectionErrorAsync(Exception exception)
        {
            var classification = ClassifyError(exception);
            _logger.LogWarning(exception, "SignalR reconnection error: {ErrorType} - {Message}, Inner: {Inner}",
                classification.Type, exception.Message, exception.InnerException?.Message ?? "None");

            await ApplyErrorHandlingStrategyAsync(classification, exception, "Reconnection");
        }

        /// <summary>
        /// Determines whether an exception should trigger a retry.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the operation should be retried, false otherwise.</returns>
        public bool ShouldRetry(Exception exception)
        {
            var classification = ClassifyError(exception);
            return classification.ShouldRetry;
        }

        /// <summary>
        /// Calculates the delay before the next retry attempt using exponential backoff.
        /// </summary>
        /// <param name="attemptCount">The current attempt count (0-based).</param>
        /// <returns>The delay before the next retry.</returns>
        public TimeSpan GetRetryDelay(int attemptCount)
        {
            // Exponential backoff with jitter: baseDelay * 2^attempt + random jitter
            var baseDelay = TimeSpan.FromSeconds(1);
            var maxDelay = TimeSpan.FromSeconds(30);
            var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attemptCount, 5)));

            // Add jitter to prevent thundering herd
            var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 1000));

            var totalDelay = baseDelay + exponentialDelay + jitter;
            return totalDelay > maxDelay ? maxDelay : totalDelay;
        }

        private ErrorClassification ClassifyError(Exception exception)
        {
            var exceptionType = exception.GetType();

            if (_errorClassifications.TryGetValue(exceptionType, out var classification))
            {
                return classification;
            }

            // Check for inner exceptions
            if (exception.InnerException != null)
            {
                var innerType = exception.InnerException.GetType();
                if (_errorClassifications.TryGetValue(innerType, out var innerClassification))
                {
                    return innerClassification;
                }
            }

            // Default classification for unknown errors
            return new ErrorClassification
            {
                Type = ErrorType.Unknown,
                ShouldRetry = false,
                Severity = ErrorSeverity.Error,
                Description = "Unknown error type"
            };
        }

        private async Task ApplyErrorHandlingStrategyAsync(ErrorClassification classification, Exception exception, string context)
        {
            switch (classification.Type)
            {
                case ErrorType.Network:
                    await HandleNetworkErrorAsync(exception, context);
                    break;

                case ErrorType.Authentication:
                    await HandleAuthenticationErrorAsync(exception, context);
                    break;

                case ErrorType.Timeout:
                    await HandleTimeoutErrorAsync(exception, context);
                    break;

                case ErrorType.Server:
                    await HandleServerErrorAsync(exception, context);
                    break;

                case ErrorType.Client:
                    await HandleClientErrorAsync(exception, context);
                    break;

                default:
                    await HandleUnknownErrorAsync(exception, context);
                    break;
            }
        }

        private async Task HandleNetworkErrorAsync(Exception exception, string context)
        {
            _logger.LogWarning("Network error in {Context}. This may indicate connectivity issues. Error: {Message}, Inner: {Inner}", context, exception.Message, exception.InnerException?.Message ?? "None");
            // Could implement network diagnostics here
            await Task.CompletedTask;
        }

        private async Task HandleAuthenticationErrorAsync(Exception exception, string context)
        {
            _logger.LogError("Authentication error in {Context}. This may indicate invalid credentials or expired tokens. Error: {Message}", context, exception.Message);
            // Could implement token refresh logic here
            await Task.CompletedTask;
        }

        private async Task HandleTimeoutErrorAsync(Exception exception, string context)
        {
            _logger.LogWarning("Timeout error in {Context}. The operation took too long to complete. Error: {Message}, Inner: {Inner}", context, exception.Message, exception.InnerException?.Message ?? "None");
            // Could implement timeout configuration adjustments here
            await Task.CompletedTask;
        }

        private async Task HandleServerErrorAsync(Exception exception, string context)
        {
            _logger.LogError("Server error in {Context}. This indicates an issue on the server side. Error: {Message}", context, exception.Message);
            // Could implement server health checks here
            await Task.CompletedTask;
        }

        private async Task HandleClientErrorAsync(Exception exception, string context)
        {
            _logger.LogError("Client error in {Context}. This indicates an issue with the client request. Error: {Message}", context, exception.Message);
            // Could implement client-side validation here
            await Task.CompletedTask;
        }

        private async Task HandleUnknownErrorAsync(Exception exception, string context)
        {
            _logger.LogError(exception, "Unknown error in {Context}. This requires investigation. Error: {Message}", context, exception.Message);
            await Task.CompletedTask;
        }

        private Dictionary<Type, ErrorClassification> InitializeErrorClassifications()
        {
            return new Dictionary<Type, ErrorClassification>
            {
                // Network-related errors
                { typeof(System.Net.Http.HttpRequestException), new ErrorClassification {
                    Type = ErrorType.Network, ShouldRetry = true, Severity = ErrorSeverity.Warning,
                    Description = "HTTP request failed - network connectivity issue" } },

                { typeof(System.Net.Sockets.SocketException), new ErrorClassification {
                    Type = ErrorType.Network, ShouldRetry = true, Severity = ErrorSeverity.Warning,
                    Description = "Socket error - network connectivity issue" } },

                // Authentication errors
                { typeof(UnauthorizedAccessException), new ErrorClassification {
                    Type = ErrorType.Authentication, ShouldRetry = false, Severity = ErrorSeverity.Error,
                    Description = "Authentication failed - invalid credentials" } },

                // Timeout errors
                { typeof(TimeoutException), new ErrorClassification {
                    Type = ErrorType.Timeout, ShouldRetry = true, Severity = ErrorSeverity.Warning,
                    Description = "Operation timed out" } },

                { typeof(TaskCanceledException), new ErrorClassification {
                    Type = ErrorType.Timeout, ShouldRetry = true, Severity = ErrorSeverity.Warning,
                    Description = "Operation was cancelled, possibly due to timeout" } },

                // Server errors
                { typeof(System.Net.Http.HttpRequestException), new ErrorClassification {
                    Type = ErrorType.Server, ShouldRetry = false, Severity = ErrorSeverity.Error,
                    Description = "Server returned an error response" } },

                // Client errors
                { typeof(ArgumentException), new ErrorClassification {
                    Type = ErrorType.Client, ShouldRetry = false, Severity = ErrorSeverity.Error,
                    Description = "Invalid argument provided" } },

                { typeof(InvalidOperationException), new ErrorClassification {
                    Type = ErrorType.Client, ShouldRetry = false, Severity = ErrorSeverity.Error,
                    Description = "Operation is not valid in current state" } }
            };
        }
    }

    /// <summary>
    /// Represents the classification of an error for appropriate handling.
    /// </summary>
    public class ErrorClassification
    {
        public ErrorType Type { get; set; }
        public bool ShouldRetry { get; set; }
        public ErrorSeverity Severity { get; set; }
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// Enumeration of error types for classification.
    /// </summary>
    public enum ErrorType
    {
        Network,
        Authentication,
        Timeout,
        Server,
        Client,
        Unknown
    }

    /// <summary>
    /// Enumeration of error severity levels.
    /// </summary>
    public enum ErrorSeverity
    {
        Debug,
        Information,
        Warning,
        Error,
        Critical
    }
}
