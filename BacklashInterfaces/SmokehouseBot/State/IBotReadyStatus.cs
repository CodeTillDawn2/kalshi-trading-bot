namespace BacklashBot.State.Interfaces
{
    /// <summary>
    /// Defines the contract for tracking the readiness status of the trading bot,
    /// including initialization completion and browser readiness states.
    /// </summary>
    public interface IBotReadyStatus
    {
        /// <summary>
        /// Gets or sets the task completion source that signals when initialization is completed.
        /// </summary>
        /// <value>The task completion source for initialization status.</value>
        TaskCompletionSource<bool> InitializationCompleted { get; set; }

        /// <summary>
        /// Gets or sets the task completion source that signals when the browser is ready.
        /// </summary>
        /// <value>The task completion source for browser readiness status.</value>
        TaskCompletionSource<bool> BrowserReady { get; set; }

        /// <summary>
        /// Resets all readiness status indicators to their initial state.
        /// </summary>
        void ResetAll();
    }
}
