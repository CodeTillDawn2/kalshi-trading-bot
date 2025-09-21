using System.ComponentModel.DataAnnotations;

namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Configuration class for secrets management settings.
    /// </summary>
    public class SecretsConfig
    {
        /// <summary>
        /// The configuration section name for SecretsConfig.
        /// </summary>
        public const string SectionName = "Secrets";

        /// <summary>
        /// Gets or sets the path to the secrets folder relative to the application directory.
        /// Default is "Secrets".
        /// </summary>
        [Required(ErrorMessage = "The 'SecretsPath' is missing in the configuration.")]
        public string SecretsPath { get; set; } = null!;
    }
}
