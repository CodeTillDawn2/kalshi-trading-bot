namespace KalshiBotOverseer.State
{
    /// <summary>
    /// Configuration class for OverseerReadyStatus default states.
    /// </summary>
    public class OverseerReadyConfig
    {
        /// <summary>
        /// Gets or sets the default state for initialization completion.
        /// </summary>
        public bool DefaultInitializationState { get; set; } = false;

        /// <summary>
        /// Gets or sets the default state for browser readiness.
        /// </summary>
        public bool DefaultBrowserReadyState { get; set; } = false;
    }
}