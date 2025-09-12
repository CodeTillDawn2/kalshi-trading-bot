using BacklashBot.State.Interfaces;

namespace KalshiBotOverseer.State
{
    /// <summary>
    /// Manages the readiness status of different components within the Kalshi overseer system.
    /// This class provides TaskCompletionSource objects to signal when various parts of the overseer
    /// have completed their initialization or are ready for operation.
    /// </summary>
    /// <remarks>
    /// The readiness status system allows different components to signal their completion state:
    /// - InitializationCompleted: Signals when the overseer's core initialization is finished
    /// - BrowserReady: Signals when browser-related components are ready (if applicable)
    ///
    /// Components can await these TaskCompletionSource objects to coordinate startup sequences,
    /// and can set the results to signal completion to waiting components.
    /// </remarks>
    public class OverseerReadyStatus : IBotReadyStatus
    {
        /// <summary>
        /// Gets or sets the TaskCompletionSource that signals when the overseer's core initialization is complete.
        /// Components can await this task to wait for initialization to finish, or set its result to signal completion.
        /// </summary>
        /// <value>The TaskCompletionSource for initialization completion signaling.</value>
        public TaskCompletionSource<bool> InitializationCompleted { get; set; } = new TaskCompletionSource<bool>();

        /// <summary>
        /// Gets or sets the TaskCompletionSource that signals when browser-related components are ready.
        /// This is used for components that depend on browser initialization or UI readiness.
        /// </summary>
        /// <value>The TaskCompletionSource for browser readiness signaling.</value>
        public TaskCompletionSource<bool> BrowserReady { get; set; } = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes a new instance of the OverseerReadyStatus class.
        /// Creates the initial TaskCompletionSource objects and sets up the default state.
        /// </summary>
        public OverseerReadyStatus()
        {
            ResetAll();
        }

        /// <summary>
        /// Resets all readiness status indicators to their initial state.
        /// This creates new TaskCompletionSource objects and sets the initialization status to false.
        /// </summary>
        /// <remarks>
        /// This method is typically called when restarting the overseer or when a reset is needed
        /// to clear all previous readiness signals and start fresh.
        /// </remarks>
        public void ResetAll()
        {
            InitializationCompleted = new TaskCompletionSource<bool>();
            InitializationCompleted.SetResult(false);
        }
    }
}
