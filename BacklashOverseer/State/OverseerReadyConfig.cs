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
        [JsonRequired]
        public bool DefaultInitializationState { get; set; }

        /// <summary>
        /// Gets or sets the default state for browser readiness.
        /// </summary>
        [JsonRequired]
        public bool DefaultBrowserReadyState { get; set; }
    }
}