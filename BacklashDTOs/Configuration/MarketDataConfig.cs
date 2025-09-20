using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration;

/// <summary>
/// Configuration class for market data settings.
/// </summary>
public class MarketDataConfig
{
    /// <summary>
    /// The configuration section name for MarketDataConfig.
    /// </summary>
    public const string SectionName = "WatchedMarkets:MarketData";

    /// <summary>
    /// Gets or sets the semaphore timeout in milliseconds. Default is 5000.
    /// </summary>
    [Required(ErrorMessage = "The 'SemaphoreTimeoutMs' is missing in the configuration.")]
    public int SemaphoreTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the ticker batch size. Default is 100.
    /// </summary>
    [Required(ErrorMessage = "The 'TickerBatchSize' is missing in the configuration.")]
    public int TickerBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the API retry timeout in milliseconds. Default is 30000.
    /// </summary>
    [Required(ErrorMessage = "The 'ApiRetryTimeoutMs' is missing in the configuration.")]
    public int ApiRetryTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether performance metrics logging is enabled for MarketDataService. Default is false.
    /// </summary>
    [Required(ErrorMessage = "The 'EnablePerformanceMetrics' is missing in the configuration.")]
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the technical analysis configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'TechnicalAnalysis' is missing in the configuration.")]
    public TechnicalAnalysisConfig TechnicalAnalysis { get; set; } = null!;
}

/// <summary>
/// Configuration class for technical analysis settings.
/// </summary>
public class TechnicalAnalysisConfig
{
    /// <summary>
    /// Gets or sets the RSI configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'RSI' is missing in the configuration.")]
    public RSIConfig RSI { get; set; } = null!;

    /// <summary>
    /// Gets or sets the MACD configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'MACD' is missing in the configuration.")]
    public MACDConfig MACD { get; set; } = null!;

    /// <summary>
    /// Gets or sets the EMA configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'EMA' is missing in the configuration.")]
    public EMAConfig EMA { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Bollinger Bands configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'BollingerBands' is missing in the configuration.")]
    public BollingerBandsConfig BollingerBands { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ATR configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'ATR' is missing in the configuration.")]
    public ATRConfig ATR { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Stochastic configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'Stochastic' is missing in the configuration.")]
    public StochasticConfig Stochastic { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ADX periods.
    /// </summary>
    [Required(ErrorMessage = "The 'ADX_Periods' is missing in the configuration.")]
    public int ADX_Periods { get; set; }

    /// <summary>
    /// Gets or sets the volume indicators configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'VolumeIndicators' is missing in the configuration.")]
    public VolumeIndicatorsConfig VolumeIndicators { get; set; } = null!;

    /// <summary>
    /// Gets or sets the support resistance configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'SupportResistance' is missing in the configuration.")]
    public SupportResistanceConfig SupportResistance { get; set; } = null!;

    /// <summary>
    /// Gets or sets the momentum indicators configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'MomentumIndicators' is missing in the configuration.")]
    public MomentumIndicatorsConfig MomentumIndicators { get; set; } = null!;

    /// <summary>
    /// Gets or sets the candlestick analysis configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'CandlestickAnalysis' is missing in the configuration.")]
    public CandlestickAnalysisConfig CandlestickAnalysis { get; set; } = null!;

    /// <summary>
    /// Gets or sets the trading parameters configuration.
    /// </summary>
    [Required(ErrorMessage = "The 'TradingParameters' is missing in the configuration.")]
    public TradingParametersConfig TradingParameters { get; set; } = null!;
}

/// <summary>
/// Configuration class for RSI settings.
/// </summary>
public class RSIConfig
{
    [Required(ErrorMessage = "The 'Short_Periods' is missing in the configuration.")]
    public int Short_Periods { get; set; }

    [Required(ErrorMessage = "The 'Medium_Periods' is missing in the configuration.")]
    public int Medium_Periods { get; set; }

    [Required(ErrorMessage = "The 'Long_Periods' is missing in the configuration.")]
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for MACD settings.
/// </summary>
public class MACDConfig
{
    [Required(ErrorMessage = "The 'Medium_FastPeriod' is missing in the configuration.")]
    public int Medium_FastPeriod { get; set; }

    [Required(ErrorMessage = "The 'Medium_SlowPeriod' is missing in the configuration.")]
    public int Medium_SlowPeriod { get; set; }

    [Required(ErrorMessage = "The 'Medium_SignalPeriod' is missing in the configuration.")]
    public int Medium_SignalPeriod { get; set; }

    [Required(ErrorMessage = "The 'Long_FastPeriod' is missing in the configuration.")]
    public int Long_FastPeriod { get; set; }

    [Required(ErrorMessage = "The 'Long_SlowPeriod' is missing in the configuration.")]
    public int Long_SlowPeriod { get; set; }

    [Required(ErrorMessage = "The 'Long_SignalPeriod' is missing in the configuration.")]
    public int Long_SignalPeriod { get; set; }
}

/// <summary>
/// Configuration class for EMA settings.
/// </summary>
public class EMAConfig
{
    [Required(ErrorMessage = "The 'Medium_Periods' is missing in the configuration.")]
    public int Medium_Periods { get; set; }

    [Required(ErrorMessage = "The 'Long_Periods' is missing in the configuration.")]
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for Bollinger Bands settings.
/// </summary>
public class BollingerBandsConfig
{
    [Required(ErrorMessage = "The 'Medium_Periods' is missing in the configuration.")]
    public int Medium_Periods { get; set; }

    [Required(ErrorMessage = "The 'Medium_StdDev' is missing in the configuration.")]
    public double Medium_StdDev { get; set; }

    [Required(ErrorMessage = "The 'Long_Periods' is missing in the configuration.")]
    public int Long_Periods { get; set; }

    [Required(ErrorMessage = "The 'Long_StdDev' is missing in the configuration.")]
    public double Long_StdDev { get; set; }
}

/// <summary>
/// Configuration class for ATR settings.
/// </summary>
public class ATRConfig
{
    required
    public int Medium_Periods { get; set; }
    required
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for Stochastic settings.
/// </summary>
public class StochasticConfig
{
    required
    public int Short_Periods { get; set; }
    required
    public int Short_DPeriods { get; set; }
    required
    public int Medium_Periods { get; set; }
    required
    public int Medium_DPeriods { get; set; }
    required
    public int Long_Periods { get; set; }
    required
    public int Long_DPeriods { get; set; }
}

/// <summary>
/// Configuration class for volume indicators settings.
/// </summary>
public class VolumeIndicatorsConfig
{
    required
    public int VWAP_Short_Periods { get; set; }
    required
    public int VWAP_Medium_Periods { get; set; }
}

/// <summary>
/// Configuration class for support resistance settings.
/// </summary>
public class SupportResistanceConfig
{
    required
    public double MinCandlestickPercentage { get; set; }
    required
    public int MaxLevels { get; set; }
    required
    public double Sigma { get; set; }
    required
    public int MinDistance { get; set; }
    required
    public double ExponentialMultiplier { get; set; }
}

/// <summary>
/// Configuration class for momentum indicators settings.
/// </summary>
public class MomentumIndicatorsConfig
{
    required
    public double PSAR_InitialAF { get; set; }
    required
    public double PSAR_MaxAF { get; set; }
    required
    public double PSAR_AFStep { get; set; }
}

/// <summary>
/// Configuration class for candlestick analysis settings.
/// </summary>
public class CandlestickAnalysisConfig
{
    required
    public int SlopeShortMinutes { get; set; }
    required
    public int SlopeMediumMinutes { get; set; }
    required
    public int RecentCandlestickDays { get; set; }
    required
    public int PseudoCandlestickLookbackPeriods { get; set; }
    required
    public int RecentCandlesticksCount { get; set; }
}

/// <summary>
/// Configuration class for trading parameters settings.
/// </summary>
public class TradingParametersConfig
{
    required
    public double TolerancePercentage { get; set; }
    required
    public double TradingFeeRate { get; set; }
}
