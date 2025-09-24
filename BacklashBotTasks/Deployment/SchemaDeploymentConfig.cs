using System.ComponentModel.DataAnnotations;

namespace BacklashBotTasks
{
    /// <summary>
    /// Configuration options for the SchemaDeployment functionality, including deployment paths and formatting settings.
    /// This configuration is loaded from the "Deployment" section of the application configuration.
    /// </summary>
    public class SchemaDeploymentConfig
    {
        /// <summary>
        /// The configuration section name for SchemaDeploymentConfig.
        /// This constant defines the path in the configuration file where these settings are located.
        /// </summary>
        public const string SectionName = "Deployment";

        /// <summary>
        /// Gets or sets the base path for deployment operations.
        /// This path is used to locate configuration files and other deployment resources.
        /// </summary>
        [Required(ErrorMessage = "The 'BasePath' is missing in the configuration.")]
        public string? BasePath { get; set; }

        /// <summary>
        /// Gets or sets whether JSON files should be written with indentation for readability.
        /// When true, JSON output is formatted with proper indentation; when false, it's minified.
        /// </summary>
        [Required(ErrorMessage = "The 'JsonWriteIndented' is missing in the configuration.")]
        public bool JsonWriteIndented { get; set; }
    }
}