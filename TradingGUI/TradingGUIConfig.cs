using System.ComponentModel.DataAnnotations;

namespace TradingGUI
{
    /// <summary>
    /// Configuration class for persisting user selections in the Trading GUI.
    /// Stores the last selected strategy and weight set to maintain user preferences
    /// across application sessions.
    /// </summary>
    public class TradingGUIConfig
    {
        /// <summary>
        /// The configuration section name for TradingGUIConfig.
        /// </summary>
        public const string SectionName = "TradingGUI";

        /// <summary>
        /// Gets or sets the name of the last selected trading strategy.
        /// </summary>
        [Required(ErrorMessage = "The 'LastSelectedStrategy' is missing in the configuration.")]
        public string LastSelectedStrategy { get; set; } = null!;

        /// <summary>
        /// Gets or sets the name of the last selected weight set for the strategy.
        /// </summary>
        [Required(ErrorMessage = "The 'LastSelectedWeightSet' is missing in the configuration.")]
        public string LastSelectedWeightSet { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether pattern image generation is enabled.
        /// When enabled, the system will generate and save visualization images for detected patterns.
        /// </summary>
        [Required(ErrorMessage = "The 'EnablePatternImageGeneration' is missing in the configuration.")]
        public bool EnablePatternImageGeneration { get; set; }
    }
}
