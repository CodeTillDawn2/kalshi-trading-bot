using System.ComponentModel.DataAnnotations;

namespace TradingStrategies.Configuration;

/// <summary>
/// Configuration class for candlestick-related parameters.
/// Controls precision and formatting for candlestick data processing.
/// </summary>
public class PseudoCandlestickExtensionsConfig
{
    /// <summary>
    /// Number of decimal places to round volume values when converting to double.
    /// This controls precision handling for volume data in candlestick conversions.
    /// Typical values: 0-4 depending on required precision and data source.
    /// Used by PseudoCandlestickExtensions for volume precision in ToCandleMids method.
    /// </summary>
    public int VolumePrecisionDigits { get; set; } = 2;
}