using BacklashBot.State.Interfaces;

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
    /// - Proper resource cleanup through IDisposable implementation
    /// - Reset capability for restarting the cancellation state
    ///
    /// This is registered as a singleton service to ensure all components share the same cancellation state.
    /// </remarks>
    public class KalshiBotStatusTracker : IStatusTrackerService
    {
        /// <summary>
        /// The global CancellationTokenSource used to coordinate cancellation across all bot components.
        /// </summary>
        private CancellationTokenSource _globalCancellationTokenSource;

        /// <summary>
        /// Lock object to ensure thread-safe access to the CancellationTokenSource.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// Initializes a new instance of the KalshiBotStatusTracker class.
        /// Creates the initial CancellationTokenSource and sets up the cancellation state.
        /// </summary>
        public KalshiBotStatusTracker()
        {
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
        /// This method is thread-safe and should be called when the bot needs to shut down.
        /// After calling this method, the cancellation token will be in a cancelled state.
        /// </remarks>
        public void CancelAll()
        {
            lock (_lock)
            {
                _globalCancellationTokenSource.Cancel();
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
            lock (_lock)
            {
                if (_globalCancellationTokenSource != null)
                {
                    _globalCancellationTokenSource.Dispose();
                }
                _globalCancellationTokenSource = new CancellationTokenSource();
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
            lock (_lock)
            {
                CancelAll();
                _globalCancellationTokenSource?.Dispose();
            }
        }

    }
}
