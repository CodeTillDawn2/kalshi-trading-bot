using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Gets or sets the name of the last selected trading strategy.
        /// </summary>
        public string? LastSelectedStrategy { get; set; }

        /// <summary>
        /// Gets or sets the name of the last selected weight set for the strategy.
        /// </summary>
        public string? LastSelectedWeightSet { get; set; }

        /// <summary>
        /// Gets or sets whether pattern image generation is enabled.
        /// When enabled, the system will generate and save visualization images for detected patterns.
        /// </summary>
        public bool EnablePatternImageGeneration { get; set; } = true;
    }
}
