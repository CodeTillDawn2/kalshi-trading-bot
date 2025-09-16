namespace BacklashCommon.Configuration
{
    /// <summary>
    /// Configuration class for secrets management settings.
    /// </summary>
    public class SecretsConfig
    {
        /// <summary>
        /// Gets or sets the path to the secrets folder relative to the application directory.
        /// Default is "Secrets".
        /// </summary>
        public string SecretsPath { get; set; } = "Secrets";
    }
}