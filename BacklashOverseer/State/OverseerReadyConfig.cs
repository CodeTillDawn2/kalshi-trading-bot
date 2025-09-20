using System.Text.Json.Serialization;

namespace BacklashOverseer.State
{
    /// <summary>
    /// Configuration class for OverseerReadyStatus default states.
    /// </summary>
    public class OverseerReadyConfig
    {
        /// <summary>
        /// Gets or sets the default state for initialization completion.
        /// </summary>
        required
        public bool DefaultInitializationState { get; set; }

        /// <summary>
        /// Gets or sets the default state for browser readiness.
        /// </summary>
        required
        public bool DefaultBrowserReadyState { get; set; }
    }
}
