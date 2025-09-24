using System.Text.Json;

namespace TradingGUI.Configuration
{
    /// <summary>
    /// Configuration class for TypographyManager settings.
    /// </summary>
    public class TypographyConfig
    {
        /// <summary>
        /// The configuration section name for TypographyConfig.
        /// </summary>
        public const string SectionName = "GUI:Typography";

        /// <summary>
        /// Preferred fonts for primary text, in order of preference.
        /// Includes a wide range for better cross-platform compatibility.
        /// </summary>
        public string[] PreferredFonts { get; set; } = {
            "Segoe UI",
            "Microsoft Sans Serif",
            "Arial",
            "Helvetica",
            "Tahoma",
            "Verdana",
            "Calibri",
            "System"
        };

        /// <summary>
        /// Preferred monospace fonts, in order of preference.
        /// Includes common monospace fonts for better compatibility.
        /// </summary>
        public string[] MonospaceFonts { get; set; } = {
            "Consolas",
            "Source Code Pro",
            "Fira Code",
            "Courier New",
            "Monaco",
            "Lucida Console",
            "DejaVu Sans Mono"
        };

        /// <summary>
        /// Minimum allowed scale factor to prevent too small fonts.
        /// </summary>
        public float MinScaleFactor { get; set; } = 0.5f;

        /// <summary>
        /// Maximum allowed scale factor to prevent too large fonts.
        /// </summary>
        public float MaxScaleFactor { get; set; } = 3.0f;

        /// <summary>
        /// Default scale factor for normal displays.
        /// </summary>
        public float DefaultScaleFactor { get; set; } = 1.0f;
    }
}