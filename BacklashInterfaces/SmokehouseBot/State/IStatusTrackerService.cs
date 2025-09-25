namespace BacklashBot.State.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that tracks and manages cancellation tokens
    /// and operation status across the trading bot system.
    /// </summary>
    public interface IStatusTrackerService : IDisposable
    {
        /// <summary>
        /// Gets a cancellation token for coordinating cancellation across operations.
        /// </summary>
        /// <returns>The cancellation token.</returns>
        CancellationToken GetCancellationToken();

        /// <summary>
        /// Cancels all ongoing operations.
        /// </summary>
        void CancelAll();

        /// <summary>
        /// Resets all status tracking to initial state.
        /// </summary>
        void ResetAll();
    }
}
