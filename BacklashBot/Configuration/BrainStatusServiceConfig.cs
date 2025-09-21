using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for BrainStatusService-specific settings.
    /// </summary>
    public class BrainStatusServiceConfig
    {
        /// <summary>
        /// The configuration section name for BrainStatusServiceConfig.
        /// </summary>
        public const string SectionName = "Central:BrainStatusService";

        /// <summary>
        /// Gets or sets the length of the session identifier string.
        /// </summary>
        [Required(ErrorMessage = "The 'SessionIdLength' is missing in the configuration.")]
        public int SessionIdLength { get; set; }
    }
}
