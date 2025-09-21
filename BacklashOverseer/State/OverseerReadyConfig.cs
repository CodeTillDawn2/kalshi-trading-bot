using System.ComponentModel.DataAnnotations;

namespace BacklashOverseer.State
{
    /// <summary>
    /// Configuration class for OverseerReadyStatus default states.
    /// </summary>
    public class OverseerReadyConfig
    {
        /// <summary>
        /// The configuration section name for OverseerReadyConfig.
        /// </summary>
        public const string SectionName = "OverseerReadyConfig";

        /// <summary>
        /// Gets or sets the default state for initialization completion.
        /// </summary>
        [Required(ErrorMessage = "The 'DefaultInitializationState' is missing in the configuration.")]
        public bool DefaultInitializationState { get; set; }

        /// <summary>
        /// Gets or sets the default state for browser readiness.
        /// </summary>
        [Required(ErrorMessage = "The 'DefaultBrowserReadyState' is missing in the configuration.")]
        public bool DefaultBrowserReadyState { get; set; }
    }
}
