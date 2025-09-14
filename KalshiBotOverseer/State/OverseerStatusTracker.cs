using BacklashBot.State.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace KalshiBotOverseer.State
{
    /// <summary>
    /// Manages the global cancellation state for the Kalshi overseer system.
    /// This class provides a centralized CancellationTokenSource that can be used to coordinate
    /// graceful shutdown and cancellation across all components of the overseer.
    /// </summary>
    /// <remarks>
    /// The status tracker ensures thread-safe access to the global cancellation token and provides:
    /// - A single CancellationToken that all components can monitor for shutdown signals
    /// - Thread-safe cancellation operations with comprehensive logging
    /// - Proper resource cleanup through IDisposable and IAsyncDisposable implementations
    /// - Reset capability for restarting the cancellation state with timing metrics
    /// - Metrics collection for cancellation frequency and operation timing
    ///
    /// This is registered as a singleton service to ensure all components share the same cancellation state.
    /// </remarks>
    public class OverseerStatusTracker : IStatusTrackerService, IAsyncDisposable
    {
        private readonly ILogger<OverseerStatusTracker> _logger;
        private readonly Meter _meter;
        private readonly Counter<long> _cancellationCounter;
        private readonly Histogram<double> _resetTimingHistogram;

        /// <summary>
        /// The global CancellationTokenSource used to coordinate cancellation across all overseer components.
        /// </summary>
        private CancellationTokenSource _globalCancellationTokenSource = new();

        /// <summary>
        /// Lock object to ensure thread-safe access to the CancellationTokenSource.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the OverseerStatusTracker class.
        /// Creates the initial CancellationTokenSource and sets up the cancellation state with logging and metrics.
        /// </summary>
        /// <param name="logger">The logger for tracking cancellation operations and state changes.</param>
        public OverseerStatusTracker(ILogger<OverseerStatusTracker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _meter = new Meter("KalshiBotOverseer.StatusTracker");
            _cancellationCounter = _meter.CreateCounter<long>("cancellation_count", description: "Number of cancellation operations");
            _resetTimingHistogram = _meter.CreateHistogram<double>("reset_timing", unit: "ms", description: "Timing for reset operations");
            ResetAll();
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
        /// This method is thread-safe and should be called when the overseer needs to shut down.
        /// After calling this method, the cancellation token will be in a cancelled state.
        /// </remarks>
        public void CancelAll()
        {
            _logger.LogInformation("Initiating cancellation of all operations.");
            lock (_lock)
            {
                _globalCancellationTokenSource.Cancel();
                _cancellationCounter.Add(1);
            }
            _logger.LogInformation("Cancellation of all operations completed.");
        }

        /// <summary>
        /// Resets the cancellation state by creating a new CancellationTokenSource.
        /// This clears any previous cancellation state and allows the overseer to restart operations.
        /// </summary>
        /// <remarks>
        /// This method properly disposes of the old CancellationTokenSource before creating a new one.
        /// It should be called when restarting the overseer or when a fresh cancellation state is needed.
        /// </remarks>
        public void ResetAll()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Resetting cancellation state.");
            lock (_lock)
            {
                if (_globalCancellationTokenSource != null)
                {
                    _globalCancellationTokenSource.Dispose();
                    _logger.LogDebug("Disposed old CancellationTokenSource.");
                }
                _globalCancellationTokenSource = new CancellationTokenSource();
                _logger.LogDebug("Created new CancellationTokenSource.");
            }
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _resetTimingHistogram.Record(duration);
            _logger.LogInformation("Cancellation state reset completed in {Duration}ms.", duration);
        }

        /// <summary>
        /// Disposes of the resources used by the status tracker synchronously.
        /// This cancels any pending operations and disposes of the CancellationTokenSource and metrics.
        /// </summary>
        /// <remarks>
        /// This method should be called when the status tracker is no longer needed,
        /// typically during application shutdown. It ensures proper cleanup of unmanaged resources
        /// and logs the disposal operation for monitoring purposes.
        /// </remarks>
        public void Dispose()
        {
            _logger.LogInformation("Disposing status tracker synchronously.");
            lock (_lock)
            {
                CancelAll();
                _globalCancellationTokenSource?.Dispose();
                _meter?.Dispose();
            }
            _logger.LogInformation("Status tracker disposed synchronously.");
        }

        /// <summary>
        /// Asynchronously disposes of the resources used by the status tracker.
        /// This cancels any pending operations and disposes of the CancellationTokenSource.
        /// </summary>
        /// <returns>A ValueTask representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Disposing status tracker asynchronously.");
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    CancelAll();
                    _globalCancellationTokenSource?.Dispose();
                    _meter?.Dispose();
                }
            });
            _logger.LogInformation("Status tracker disposed asynchronously.");
        }
    }
}
