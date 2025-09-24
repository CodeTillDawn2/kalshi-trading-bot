using System.Text.Json;
using TradingGUI.Configuration;

namespace TradingGUI
{
    /// <summary>
    /// Defines standard font sizes used throughout the application for consistent typography.
    /// Values represent font sizes in points.
    /// </summary>
    public enum FontSize
    {
        /// <summary>
        /// Extra small font size (6pt), suitable for fine details or compact displays.
        /// </summary>
        ExtraSmall = 6,

        /// <summary>
        /// Small font size (7pt), used for secondary information and labels.
        /// </summary>
        Small = 7,

        /// <summary>
        /// Medium font size (8pt), the default size for most UI elements.
        /// </summary>
        Medium = 8,

        /// <summary>
        /// Large font size (9pt), for emphasized content.
        /// </summary>
        Large = 9,

        /// <summary>
        /// Extra large font size (10pt), for prominent display elements.
        /// </summary>
        ExtraLarge = 10,

        /// <summary>
        /// Header font size (12pt), used for section headers and important labels.
        /// </summary>
        Header = 12,

        /// <summary>
        /// Title font size (15pt), for main titles and headings.
        /// </summary>
        Title = 15
    }

    /// <summary>
    /// Defines font weight styles available for typography management.
    /// </summary>
    public enum FontWeight
    {
        /// <summary>
        /// Regular font weight, the standard weight for most text.
        /// </summary>
        Regular,

        /// <summary>
        /// Bold font weight, for emphasis and headers.
        /// </summary>
        Bold,

        /// <summary>
        /// Italic font weight, for stylistic emphasis.
        /// </summary>
        Italic,

        /// <summary>
        /// Bold italic font weight, combining bold and italic styles.
        /// </summary>
        BoldItalic
    }

    /// <summary>
    /// Manages typography for the Windows Forms GUI application, providing consistent font selection,
    /// sizing, and scaling across different display configurations. This singleton class ensures
    /// that all UI elements use appropriate fonts that are universally available and handle DPI scaling
    /// for optimal readability on various screens.
    /// </summary>
    /// <remarks>
    /// The TypographyManager automatically selects the best available font from a list of safe,
    /// copyright-free fonts. It provides methods for creating fonts with specific sizes and weights,
    /// scaling fonts for high-DPI displays, and applying consistent typography to controls recursively.
    /// The class is designed as a singleton to ensure consistent font management across the application.
    /// </remarks>
    public class TypographyManager
    {
        /// <summary>
        /// The singleton instance of the TypographyManager.
        /// </summary>
        private static TypographyManager _instance;

        /// <summary>
        /// Loads typography configuration from appsettings.json.
        /// </summary>
        /// <returns>TypographyConfig instance loaded from configuration file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if appsettings.json is not found.</exception>
        /// <exception cref="KeyNotFoundException">Thrown if the Typography section is not found.</exception>
        /// <exception cref="InvalidDataException">Thrown if deserialization fails.</exception>
        private static TypographyConfig LoadTypographyConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("appsettings.json not found.", configPath);
            }
            string json = File.ReadAllText(configPath);
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                // Navigate to GUI:Typography section
                if (!doc.RootElement.TryGetProperty("GUI", out JsonElement guiElement))
                {
                    throw new KeyNotFoundException($"GUI section not found in appsettings.json.");
                }
                if (!guiElement.TryGetProperty("Typography", out JsonElement typographyElement))
                {
                    throw new KeyNotFoundException($"Typography section 'GUI:Typography' not found in appsettings.json.");
                }
                var config = JsonSerializer.Deserialize<TypographyConfig>(typographyElement.GetRawText());
                if (config == null)
                {
                    throw new InvalidDataException("Failed to deserialize Typography configuration.");
                }
                return config;
            }
        }

        /// <summary>
        /// Gets the singleton instance of the TypographyManager, creating it if necessary.
        /// </summary>
        public static TypographyManager Instance => _instance ??= new TypographyManager();

        /// <summary>
        /// Configuration instance for typography settings.
        /// </summary>
        private static readonly TypographyConfig _config = LoadTypographyConfig();

        /// <summary>
        /// Gets the configuration for typography settings.
        /// </summary>
        public static TypographyConfig Config => _config;

        /// <summary>
        /// The selected primary font for general UI text.
        /// </summary>
        private string _primaryFont;

        /// <summary>
        /// The selected monospace font for code, logs, and tabular data.
        /// </summary>
        private string _monospaceFont;

        /// <summary>
        /// Cache for font availability checks to improve performance.
        /// </summary>
        private readonly Dictionary<string, bool> _fontCache = new Dictionary<string, bool>();

        /// <summary>
        /// Initializes a new instance of the TypographyManager, selecting the best available fonts
        /// for primary and monospace text from the configured font lists.
        /// </summary>
        public TypographyManager()
        {
            // Find the best available font
            _primaryFont = GetBestAvailableFont(_config.PreferredFonts);
            _monospaceFont = GetBestAvailableFont(_config.MonospaceFonts);

            // Ultimate fallback if no monospace font is available
            if (string.IsNullOrEmpty(_monospaceFont))
            {
                _monospaceFont = _primaryFont;
            }
        }

        /// <summary>
        /// Finds the best available font from the provided list of font names.
        /// </summary>
        /// <param name="fontNames">Array of font names to check for availability.</param>
        /// <returns>The name of the first available font.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no font from the list is available.</exception>
        private string GetBestAvailableFont(string[] fontNames)
        {
            foreach (var fontName in fontNames)
            {
                if (IsFontAvailable(fontName))
                {
                    return fontName;
                }
            }
            throw new InvalidOperationException("No available font found from the configured preferred fonts list.");
        }

        /// <summary>
        /// Checks if a specific font is available on the system by attempting to create a Font object.
        /// Uses caching to improve performance for repeated checks.
        /// </summary>
        /// <param name="fontName">The name of the font to check.</param>
        /// <returns>True if the font is available and can be created; otherwise, false.</returns>
        private bool IsFontAvailable(string fontName)
        {
            if (_fontCache.TryGetValue(fontName, out bool isAvailable))
            {
                return isAvailable;
            }

            try
            {
                using (var font = new Font(fontName, 8f))
                {
                    isAvailable = font.Name == fontName;
                }
            }
            catch
            {
                isAvailable = false;
            }

            _fontCache[fontName] = isAvailable;
            return isAvailable;
        }

        /// <summary>
        /// Creates a Font object using the primary font with the specified size and weight.
        /// </summary>
        /// <param name="size">The font size from the FontSize enum.</param>
        /// <param name="weight">The font weight (defaults to Regular).</param>
        /// <returns>A new Font object with the specified characteristics.</returns>
        public Font GetFont(FontSize size, FontWeight weight = FontWeight.Regular)
        {
            float fontSize = (float)size;
            FontStyle style = GetFontStyle(weight);

            return new Font(_primaryFont, fontSize, style);
        }

        /// <summary>
        /// Creates a Font object using the monospace font with the specified size and weight.
        /// Suitable for code, logs, and tabular data display.
        /// </summary>
        /// <param name="size">The font size from the FontSize enum.</param>
        /// <param name="weight">The font weight (defaults to Regular).</param>
        /// <returns>A new Font object with the specified characteristics.</returns>
        public Font GetMonospaceFont(FontSize size, FontWeight weight = FontWeight.Regular)
        {
            float fontSize = (float)size;
            FontStyle style = GetFontStyle(weight);

            return new Font(_monospaceFont, fontSize, style);
        }

        /// <summary>
        /// Converts a FontWeight enum value to the corresponding FontStyle.
        /// </summary>
        /// <param name="weight">The font weight to convert.</param>
        /// <returns>The equivalent FontStyle value.</returns>
        private FontStyle GetFontStyle(FontWeight weight)
        {
            return weight switch
            {
                FontWeight.Regular => FontStyle.Regular,
                FontWeight.Bold => FontStyle.Bold,
                FontWeight.Italic => FontStyle.Italic,
                FontWeight.BoldItalic => FontStyle.Bold | FontStyle.Italic,
                _ => FontStyle.Regular
            };
        }

        /// <summary>
        /// Validates the scale factor to ensure it is within acceptable bounds.
        /// </summary>
        /// <param name="scaleFactor">The scale factor to validate.</param>
        /// <exception cref="ArgumentException">Thrown if scaleFactor is not positive or out of bounds.</exception>
        private void ValidateScaleFactor(float scaleFactor)
        {
            if (scaleFactor <= 0)
            {
                throw new ArgumentException("Scale factor must be positive.", nameof(scaleFactor));
            }
            if (scaleFactor < _config.MinScaleFactor || scaleFactor > _config.MaxScaleFactor)
            {
                throw new ArgumentException($"Scale factor must be between {_config.MinScaleFactor} and {_config.MaxScaleFactor}.", nameof(scaleFactor));
            }
        }

        /// <summary>
        /// Calculates the height of the specified font.
        /// </summary>
        /// <param name="font">The font to measure.</param>
        /// <returns>The height of the font in pixels.</returns>
        public float GetFontHeight(Font font)
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return font.GetHeight(graphics);
            }
        }

        /// <summary>
        /// Calculates the size of the specified text when rendered with the given font.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="font">The font used to render the text.</param>
        /// <returns>The size of the text in pixels.</returns>
        public SizeF GetTextSize(string text, Font font)
        {
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.MeasureString(text, font);
            }
        }

        /// <summary>
        /// Calculates the width of the specified text when rendered with the given font.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="font">The font used to render the text.</param>
        /// <returns>The width of the text in pixels.</returns>
        public float GetTextWidth(string text, Font font)
        {
            return GetTextSize(text, font).Width;
        }

        /// <summary>
        /// Creates a scaled Font object using the primary font, applying a scale factor to the base size.
        /// Useful for DPI scaling and responsive typography.
        /// </summary>
        /// <param name="baseSize">The base font size from the FontSize enum.</param>
        /// <param name="scaleFactor">The scaling factor to apply (e.g., 1.2 for 20% larger).</param>
        /// <param name="weight">The font weight (defaults to Regular).</param>
        /// <returns>A new Font object with the scaled size and specified characteristics.</returns>
        public Font GetScaledFont(FontSize baseSize, float scaleFactor, FontWeight weight = FontWeight.Regular)
        {
            ValidateScaleFactor(scaleFactor);
            float scaledSize = (float)baseSize * scaleFactor;
            FontStyle style = GetFontStyle(weight);

            return new Font(_primaryFont, scaledSize, style);
        }

        /// <summary>
        /// Creates a scaled Font object using the monospace font, applying a scale factor to the base size.
        /// Useful for DPI scaling of code and tabular displays.
        /// </summary>
        /// <param name="baseSize">The base font size from the FontSize enum.</param>
        /// <param name="scaleFactor">The scaling factor to apply (e.g., 1.2 for 20% larger).</param>
        /// <param name="weight">The font weight (defaults to Regular).</param>
        /// <returns>A new Font object with the scaled size and specified characteristics.</returns>
        public Font GetScaledMonospaceFont(FontSize baseSize, float scaleFactor, FontWeight weight = FontWeight.Regular)
        {
            ValidateScaleFactor(scaleFactor);
            float scaledSize = (float)baseSize * scaleFactor;
            FontStyle style = GetFontStyle(weight);

            return new Font(_monospaceFont, scaledSize, style);
        }

        /// <summary>
        /// Calculates the typography scale factor based on the current display's DPI settings.
        /// This ensures that fonts are appropriately sized for high-DPI displays while maintaining readability.
        /// Considers all available screens to determine the most appropriate scaling factor.
        /// </summary>
        /// <returns>A scale factor clamped between configured min and max values, where 1.0 represents standard 96 DPI.</returns>
        public float GetTypographyScale()
        {
            float maxDpi = 96f; // Default standard DPI

            // Check DPI across all screens to find the maximum
            foreach (var screen in Screen.AllScreens)
            {
                using (var graphics = Graphics.FromHwnd(IntPtr.Zero)) // System DPI, as per-monitor DPI requires newer APIs
                {
                    float dpiX = graphics.DpiX;
                    float dpiY = graphics.DpiY;
                    float screenDpi = Math.Min(dpiX, dpiY);
                    if (screenDpi > maxDpi)
                    {
                        maxDpi = screenDpi;
                    }
                }
            }

            float scaleFactor = maxDpi / 96f;

            // Clamp between configured bounds
            return Math.Max(_config.MinScaleFactor, Math.Min(_config.MaxScaleFactor, scaleFactor));
        }

        /// <summary>
        /// Applies consistent typography to a control and all its child controls recursively.
        /// This method ensures that all UI elements use appropriate fonts based on their type and purpose.
        /// </summary>
        /// <param name="control">The root control to apply typography to.</param>
        /// <param name="scaleFactor">The scaling factor to apply for DPI adjustment (defaults to 1.0).</param>
        public void ApplyTypography(Control control, float scaleFactor = 1.0f)
        {
            ValidateScaleFactor(scaleFactor);
            if (control == null) return;

            // Apply to the control itself
            ApplyTypographyToControl(control, scaleFactor);

            // Apply to child controls recursively
            foreach (Control child in control.Controls)
            {
                ApplyTypography(child, scaleFactor);
            }
        }

        /// <summary>
        /// Applies appropriate typography to a single control based on its type and naming conventions.
        /// Certain controls like ScottPlot charts are skipped to preserve their internal font management.
        /// </summary>
        /// <param name="control">The control to apply typography to.</param>
        /// <param name="scaleFactor">The scaling factor to apply for DPI adjustment.</param>
        private void ApplyTypographyToControl(Control control, float scaleFactor)
        {
            // Skip certain controls that shouldn't have their fonts changed
            if (control is ScottPlot.FormsPlot)
                return;

            if (control is Button button)
            {
                button.Font = GetScaledFont(FontSize.Medium, scaleFactor, FontWeight.Bold);
            }
            else if (control is Label label)
            {
                // Determine font size based on label name/content
                if (label.Name?.Contains("Header") == true || label.Name?.Contains("Title") == true)
                {
                    label.Font = GetScaledFont(FontSize.Header, scaleFactor, FontWeight.Bold);
                }
                else if (label.Name?.Contains("Value") == true || label.Font.Size < 8)
                {
                    label.Font = GetScaledFont(FontSize.Small, scaleFactor);
                }
                else
                {
                    label.Font = GetScaledFont(FontSize.Medium, scaleFactor);
                }
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.Font = GetScaledFont(FontSize.Small, scaleFactor);
            }
            else if (control is DataGridView dgv)
            {
                dgv.Font = GetScaledFont(FontSize.Small, scaleFactor);
                dgv.ColumnHeadersDefaultCellStyle.Font = GetScaledFont(FontSize.Small, scaleFactor, FontWeight.Bold);
            }
            else if (control is RichTextBox rtb)
            {
                // Use monospace for log/terminal-like text
                rtb.Font = GetScaledMonospaceFont(FontSize.Medium, scaleFactor);
            }
            else if (control is TextBox tb)
            {
                tb.Font = GetScaledFont(FontSize.Medium, scaleFactor);
            }
            else if (control is ComboBox cb)
            {
                cb.Font = GetScaledFont(FontSize.Medium, scaleFactor);
            }
            else if (control is ListBox lb)
            {
                lb.Font = GetScaledFont(FontSize.Small, scaleFactor);
            }
            else if (control is MenuStrip ms)
            {
                ms.Font = GetScaledFont(FontSize.Medium, scaleFactor);
            }
            else
            {
                // Default font for other controls
                control.Font = GetScaledFont(FontSize.Medium, scaleFactor);
            }
        }

        /// <summary>
        /// Gets the name of the selected primary font used for general UI text.
        /// </summary>
        public string PrimaryFont => _primaryFont;

        /// <summary>
        /// Gets the name of the selected monospace font used for code and tabular data.
        /// </summary>
        public string MonospaceFont => _monospaceFont;

        /// <summary>
        /// Gets a value indicating whether the current display is considered high-DPI (scale factor > 1.25).
        /// </summary>
        public bool IsHighDPI => GetTypographyScale() > 1.25f;
    }
}
