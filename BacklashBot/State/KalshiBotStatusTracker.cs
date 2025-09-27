using BacklashBot.Configuration;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Options;

namespace BacklashBot.State
{
    /// <summary>
    /// Manages the global cancellation state for the Kalshi trading bot system.
    /// This class provides a centralized CancellationTokenSource that can be used to coordinate
    /// graceful shutdown and cancellation across all components of the bot.
    /// </summary>
    /// <remarks>
    /// The status tracker ensures thread-safe access to the global cancellation token and provides:
    /// - A single CancellationToken that all components can monitor for shutdown signals
    /// - Thread-safe cancellation operations
    /// - Proper resource cleanup through IDisposable and IAsyncDisposable implementation
    /// - Reset capability for restarting the cancellation state
    /// - Logging for cancellation operations and metrics collection
    ///
    /// This is registered as a singleton service to ensure all components share the same cancellation state.
    /// </remarks>
    public class KalshiBotStatusTracker : IStatusTrackerService, IAsyncDisposable
    {
        /// <summary>
        /// The global CancellationTokenSource used to coordinate cancellation across all bot components.
        /// </summary>
        private CancellationTokenSource _globalCancellationTokenSource = new();

        /// <summary>
        /// Lock object to ensure thread-safe access to the CancellationTokenSource.
        /// </summary>
        private readonly object _lock = new();

        private readonly ILogger<KalshiBotStatusTracker> _logger;
        private readonly IPerformanceMonitor _performanceMonitor;
        private readonly IOptions<KalshiBotStatusTrackerConfig> _config;
        private readonly bool _enablePerformanceMetrics;

        private DateTime _lastCancellationTime = DateTime.MinValue;
        private int _cancellationCount = 0;

        /// <summary>
        /// Initializes a new instance of the KalshiBotStatusTracker class.
        /// Creates the initial CancellationTokenSource and sets up the cancellation state.
        /// </summary>
        /// <param name="logger">The logger for tracking cancellation operations.</param>
        /// <param name="performanceMonitor">The performance monitor for recording metrics.</param>
        /// <param name="config">Configuration options for the KalshiBotStatusTracker.</param>
        public KalshiBotStatusTracker(
            ILogger<KalshiBotStatusTracker> logger,
            IPerformanceMonitor performanceMonitor,
            IOptions<KalshiBotStatusTrackerConfig> config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _enablePerformanceMetrics = _config.Value.EnablePerformanceMetrics;

            _logger.LogInformation("KalshiBotStatusTracker initialized with EnablePerformanceMetrics={EnablePerformanceMetrics}", _enablePerformanceMetrics);
        }

        /// <summary>
        /// Gets the global CancellationToken that all components should monitor for shutdown signals.
        /// </summary>
        /// <returns>The CancellationToken that can be used to monitor for cancellation requests.</returns>
        /// <remarks>
        /// This method provides thread-safe access to the cancellation token.
        /// Components should regularly check this token or use it in async operations
        /// to respond to shutdown requests gracefully.
        /// </remarks>
        public CancellationToken GetCancellationToken()
        {
            lock (_lock)
            {
                return _globalCancellationTokenSource.Token;
            }
        }

        /// <summary>
        /// Cancels all operations by triggering the global cancellation token.
        /// This signals all components that are monitoring the cancellation token to stop their operations.
        /// </summary>
        /// <remarks>
        /// This method is thread-safe and should be called when the bot needs to shut down.
        /// After calling this method, the cancellation token will be in a cancelled state.
        /// </remarks>
        public void CancelAll()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Initiating cancellation of all operations");

            lock (_lock)
            {
                if (_globalCancellationTokenSource.IsCancellationRequested)
                {
                    _logger.LogWarning("Cancellation already requested, skipping duplicate cancel operation");
                    return;
                }

                try
                {
                    _globalCancellationTokenSource.Cancel();
                    _lastCancellationTime = DateTime.UtcNow;
                    _cancellationCount++;

                    var timing = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Record cancellation timing metric
                    if (!_enablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordDisabledMetric("KalshiBotStatusTracker", "CancellationTiming", "Cancellation Timing", "Time taken for cancellation operation", timing, "ms", "StatusTracker", _enablePerformanceMetrics);
                    }
                    else
                    {
                        _performanceMonitor.RecordSpeedDialMetric("KalshiBotStatusTracker", "CancellationTiming", "Cancellation Timing", "Time taken for cancellation operation", timing, "ms", "StatusTracker", null, 1000, 5000, _enablePerformanceMetrics);
                    }

                    // Record cancellation operations metric
                    if (!_enablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordDisabledMetric("KalshiBotStatusTracker", "CancellationOperations", "Cancellation Operations", "Number of cancellation operations", 1, "count", "StatusTracker", _enablePerformanceMetrics);
                    }
                    else
                    {
                        _performanceMonitor.RecordCounterMetric("KalshiBotStatusTracker", "CancellationOperations", "Cancellation Operations", "Number of cancellation operations", 1, "count", "StatusTracker", _enablePerformanceMetrics);
                    }

                    _logger.LogInformation("All operations cancelled successfully. Total cancellations: {Count}, timing: {Timing}ms",
                        _cancellationCount, timing);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cancelling operations");
                    throw;
                }
            }
        }

        /// <summary>
        /// Resets the cancellation state by creating a new CancellationTokenSource.
        /// This clears any previous cancellation state and allows the bot to restart operations.
        /// </summary>
        /// <remarks>
        /// This method properly disposes of the old CancellationTokenSource before creating a new one.
        /// It should be called when restarting the bot or when a fresh cancellation state is needed.
        /// </remarks>
        public void ResetAll()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Resetting cancellation state");

            lock (_lock)
            {
                try
                {
                    if (_globalCancellationTokenSource != null)
                    {
                        if (!_globalCancellationTokenSource.IsCancellationRequested)
                        {
                            _logger.LogWarning("Resetting active CancellationTokenSource that was not cancelled");
                        }
                        _globalCancellationTokenSource.Dispose();
                        _logger.LogDebug("Previous CancellationTokenSource disposed");
                    }

                    _globalCancellationTokenSource = new CancellationTokenSource();
                    _lastCancellationTime = DateTime.MinValue;
                    _cancellationCount = 0;

                    var timing = (DateTime.UtcNow - startTime).TotalMilliseconds;

                    // Record reset timing metric
                    if (!_enablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordDisabledMetric("KalshiBotStatusTracker", "ResetTiming", "Reset Timing", "Time taken for reset operation", timing, "ms", "StatusTracker", _enablePerformanceMetrics);
                    }
                    else
                    {
                        _performanceMonitor.RecordSpeedDialMetric("KalshiBotStatusTracker", "ResetTiming", "Reset Timing", "Time taken for reset operation", timing, "ms", "StatusTracker", null, 500, 2000, _enablePerformanceMetrics);
                    }

                    // Record reset operations metric
                    if (!_enablePerformanceMetrics)
                    {
                        _performanceMonitor.RecordDisabledMetric("KalshiBotStatusTracker", "ResetOperations", "Reset Operations", "Number of reset operations", 1, "count", "StatusTracker", _enablePerformanceMetrics);
                    }
                    else
                    {
                        _performanceMonitor.RecordCounterMetric("KalshiBotStatusTracker", "ResetOperations", "Reset Operations", "Number of reset operations", 1, "count", "StatusTracker", _enablePerformanceMetrics);
                    }

                    _logger.LogInformation("Cancellation state reset successfully, timing: {Timing}ms", timing);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while resetting cancellation state");
                    throw;
                }
            }
        }

        /// <summary>
        /// Disposes of the resources used by the status tracker.
        /// This cancels any pending operations and disposes of the CancellationTokenSource.
        /// </summary>
        /// <remarks>
        /// This method should be called when the status tracker is no longer needed,
        /// typically during application shutdown. It ensures proper cleanup of unmanaged resources.
        /// </remarks>
        public void Dispose()
        {
            _logger.LogInformation("Disposing KalshiBotStatusTracker resources");

            lock (_lock)
            {
                try
                {
                    if (_globalCancellationTokenSource != null && !_globalCancellationTokenSource.IsCancellationRequested)
                    {
                        _globalCancellationTokenSource.Cancel();
                        _logger.LogDebug("Cancellation requested during disposal");
                    }

                    _globalCancellationTokenSource?.Dispose();
                    _logger.LogDebug("CancellationTokenSource disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during disposal");
                }
            }

            _logger.LogInformation("KalshiBotStatusTracker disposed successfully");
        }

        /// <summary>
        /// Asynchronously disposes of the resources used by the status tracker.
        /// This provides an async disposal pattern for better resource management in async contexts.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Asynchronously disposing KalshiBotStatusTracker resources");

            // Perform synchronous disposal first
            Dispose();

            // Any additional async cleanup can be added here if needed
            await Task.CompletedTask;

            _logger.LogInformation("KalshiBotStatusTracker disposed asynchronously");
        }

        /// <summary>
        /// Gets metrics information about the status tracker.
        /// </summary>
        /// <returns>A dictionary containing current metrics.</returns>
        public IReadOnlyDictionary<string, object> GetMetrics()
        {
            lock (_lock)
            {
                return new Dictionary<string, object>
                {
                    ["IsCancellationRequested"] = _globalCancellationTokenSource.IsCancellationRequested,
                    ["LastCancellationTime"] = _lastCancellationTime,
                    ["CancellationCount"] = _cancellationCount,
                    ["TokenCanBeCanceled"] = _globalCancellationTokenSource.Token.CanBeCanceled
                };
            }
        }
    }
}
