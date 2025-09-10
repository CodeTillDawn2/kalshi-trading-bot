using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimulatorWinForms
{
    public enum FontSize
    {
        ExtraSmall = 6,
        Small = 7,
        Medium = 8,
        Large = 9,
        ExtraLarge = 10,
        Header = 12,
        Title = 15
    }

    public enum FontWeight
    {
        Regular,
        Bold,
        Italic,
        BoldItalic
    }

    public class TypographyManager
    {
        private static TypographyManager _instance;
        public static TypographyManager Instance => _instance ??= new TypographyManager();

        // Safe, universally available fonts (no copyright issues)
        private readonly string[] _safeFonts = {
            "Microsoft Sans Serif",  // Most compatible
            "Arial",                 // Very common
            "Tahoma",                // Good alternative
            "Verdana"                // Clean and readable
        };

        private string _primaryFont;
        private string _monospaceFont;

        public TypographyManager()
        {
            // Find the best available font
            _primaryFont = GetBestAvailableFont(_safeFonts);
            _monospaceFont = "Consolas"; // Consolas is generally safe for monospace

            // Fallback if Consolas isn't available
            if (!IsFontAvailable(_monospaceFont))
            {
                _monospaceFont = "Courier New";
                if (!IsFontAvailable(_monospaceFont))
                {
                    _monospaceFont = _primaryFont; // Ultimate fallback
                }
            }
        }

        private string GetBestAvailableFont(string[] fontNames)
        {
            foreach (var fontName in fontNames)
            {
                if (IsFontAvailable(fontName))
                {
                    return fontName;
                }
            }
            return "Microsoft Sans Serif"; // Ultimate fallback
        }

        private bool IsFontAvailable(string fontName)
        {
            try
            {
                using (var font = new Font(fontName, 8f))
                {
                    return font.Name == fontName;
                }
            }
            catch
            {
                return false;
            }
        }

        public Font GetFont(FontSize size, FontWeight weight = FontWeight.Regular)
        {
            float fontSize = (float)size;
            FontStyle style = GetFontStyle(weight);

            return new Font(_primaryFont, fontSize, style);
        }

        public Font GetMonospaceFont(FontSize size, FontWeight weight = FontWeight.Regular)
        {
            float fontSize = (float)size;
            FontStyle style = GetFontStyle(weight);

            return new Font(_monospaceFont, fontSize, style);
        }

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

        public Font GetScaledFont(FontSize baseSize, float scaleFactor, FontWeight weight = FontWeight.Regular)
        {
            float scaledSize = (float)baseSize * scaleFactor;
            FontStyle style = GetFontStyle(weight);

            return new Font(_primaryFont, scaledSize, style);
        }

        public Font GetScaledMonospaceFont(FontSize baseSize, float scaleFactor, FontWeight weight = FontWeight.Regular)
        {
            float scaledSize = (float)baseSize * scaleFactor;
            FontStyle style = GetFontStyle(weight);

            return new Font(_monospaceFont, scaledSize, style);
        }

        // Typography scale for consistent sizing
        public float GetTypographyScale()
        {
            // Get DPI scaling factor
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = graphics.DpiX;
                float dpiY = graphics.DpiY;
                float scaleFactor = Math.Min(dpiX, dpiY) / 96f; // 96 DPI is standard

                // Clamp between reasonable bounds
                return Math.Max(0.8f, Math.Min(2.0f, scaleFactor));
            }
        }

        // Apply typography to controls recursively
        public void ApplyTypography(Control control, float scaleFactor = 1.0f)
        {
            if (control == null) return;

            // Apply to the control itself
            ApplyTypographyToControl(control, scaleFactor);

            // Apply to child controls recursively
            foreach (Control child in control.Controls)
            {
                ApplyTypography(child, scaleFactor);
            }
        }

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

        // Get typography information
        public string PrimaryFont => _primaryFont;
        public string MonospaceFont => _monospaceFont;
        public bool IsHighDPI => GetTypographyScale() > 1.25f;
    }
}