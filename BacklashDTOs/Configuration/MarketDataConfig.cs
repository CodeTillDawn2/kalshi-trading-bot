using System.Text.Json.Serialization;

namespace BacklashDTOs.Configuration;

/// <summary>
/// Configuration class for market data settings.
/// </summary>
public class MarketDataConfig
{
    /// <summary>
    /// Gets or sets the semaphore timeout in milliseconds. Default is 5000.
    /// </summary>
    [JsonRequired]
    public int SemaphoreTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the ticker batch size. Default is 100.
    /// </summary>
    [JsonRequired]
    public int TickerBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the API retry timeout in milliseconds. Default is 30000.
    /// </summary>
    [JsonRequired]
    public int ApiRetryTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether performance metrics logging is enabled for MarketDataService. Default is false.
    /// </summary>
    [JsonRequired]
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the technical analysis configuration.
    /// </summary>
    [JsonRequired]
    public TechnicalAnalysisConfig TechnicalAnalysis { get; set; }
}

/// <summary>
/// Configuration class for technical analysis settings.
/// </summary>
public class TechnicalAnalysisConfig
{
    /// <summary>
    /// Gets or sets the RSI configuration.
    /// </summary>
    [JsonRequired]
    public RSIConfig RSI { get; set; }

    /// <summary>
    /// Gets or sets the MACD configuration.
    /// </summary>
    [JsonRequired]
    public MACDConfig MACD { get; set; }

    /// <summary>
    /// Gets or sets the EMA configuration.
    /// </summary>
    [JsonRequired]
    public EMAConfig EMA { get; set; }

    /// <summary>
    /// Gets or sets the Bollinger Bands configuration.
    /// </summary>
    [JsonRequired]
    public BollingerBandsConfig BollingerBands { get; set; }

    /// <summary>
    /// Gets or sets the ATR configuration.
    /// </summary>
    [JsonRequired]
    public ATRConfig ATR { get; set; }

    /// <summary>
    /// Gets or sets the Stochastic configuration.
    /// </summary>
    [JsonRequired]
    public StochasticConfig Stochastic { get; set; }

    /// <summary>
    /// Gets or sets the ADX periods.
    /// </summary>
    [JsonRequired]
    public int ADX_Periods { get; set; }

    /// <summary>
    /// Gets or sets the volume indicators configuration.
    /// </summary>
    [JsonRequired]
    public VolumeIndicatorsConfig VolumeIndicators { get; set; }

    /// <summary>
    /// Gets or sets the support resistance configuration.
    /// </summary>
    [JsonRequired]
    public SupportResistanceConfig SupportResistance { get; set; }

    /// <summary>
    /// Gets or sets the momentum indicators configuration.
    /// </summary>
    [JsonRequired]
    public MomentumIndicatorsConfig MomentumIndicators { get; set; }

    /// <summary>
    /// Gets or sets the candlestick analysis configuration.
    /// </summary>
    [JsonRequired]
    public CandlestickAnalysisConfig CandlestickAnalysis { get; set; }

    /// <summary>
    /// Gets or sets the trading parameters configuration.
    /// </summary>
    [JsonRequired]
    public TradingParametersConfig TradingParameters { get; set; }
}

/// <summary>
/// Configuration class for RSI settings.
/// </summary>
public class RSIConfig
{
    [JsonRequired]
    public int Short_Periods { get; set; }
    [JsonRequired]
    public int Medium_Periods { get; set; }
    [JsonRequired]
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for MACD settings.
/// </summary>
public class MACDConfig
{
    [JsonRequired]
    public int Medium_FastPeriod { get; set; }
    [JsonRequired]
    public int Medium_SlowPeriod { get; set; }
    [JsonRequired]
    public int Medium_SignalPeriod { get; set; }
    [JsonRequired]
    public int Long_FastPeriod { get; set; }
    [JsonRequired]
    public int Long_SlowPeriod { get; set; }
    [JsonRequired]
    public int Long_SignalPeriod { get; set; }
}

/// <summary>
/// Configuration class for EMA settings.
/// </summary>
public class EMAConfig
{
    [JsonRequired]
    public int Medium_Periods { get; set; }
    [JsonRequired]
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for Bollinger Bands settings.
/// </summary>
public class BollingerBandsConfig
{
    [JsonRequired]
    public int Medium_Periods { get; set; }
    [JsonRequired]
    public double Medium_StdDev { get; set; }
    [JsonRequired]
    public int Long_Periods { get; set; }
    [JsonRequired]
    public double Long_StdDev { get; set; }
}

/// <summary>
/// Configuration class for ATR settings.
/// </summary>
public class ATRConfig
{
    [JsonRequired]
    public int Medium_Periods { get; set; }
    [JsonRequired]
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for Stochastic settings.
/// </summary>
public class StochasticConfig
{
    [JsonRequired]
    public int Short_Periods { get; set; }
    [JsonRequired]
    public int Short_DPeriods { get; set; }
    [JsonRequired]
    public int Medium_Periods { get; set; }
    [JsonRequired]
    public int Medium_DPeriods { get; set; }
    [JsonRequired]
    public int Long_Periods { get; set; }
    [JsonRequired]
    public int Long_DPeriods { get; set; }
}

/// <summary>
/// Configuration class for volume indicators settings.
/// </summary>
public class VolumeIndicatorsConfig
{
    [JsonRequired]
    public int VWAP_Short_Periods { get; set; }
    [JsonRequired]
    public int VWAP_Medium_Periods { get; set; }
}

/// <summary>
/// Configuration class for support resistance settings.
/// </summary>
public class SupportResistanceConfig
{
    [JsonRequired]
    public double MinCandlestickPercentage { get; set; }
    [JsonRequired]
    public int MaxLevels { get; set; }
    [JsonRequired]
    public double Sigma { get; set; }
    [JsonRequired]
    public int MinDistance { get; set; }
    [JsonRequired]
    public double ExponentialMultiplier { get; set; }
}

/// <summary>
/// Configuration class for momentum indicators settings.
/// </summary>
public class MomentumIndicatorsConfig
{
    [JsonRequired]
    public double PSAR_InitialAF { get; set; }
    [JsonRequired]
    public double PSAR_MaxAF { get; set; }
    [JsonRequired]
    public double PSAR_AFStep { get; set; }
}

/// <summary>
/// Configuration class for candlestick analysis settings.
/// </summary>
public class CandlestickAnalysisConfig
{
    [JsonRequired]
    public int SlopeShortMinutes { get; set; }
    [JsonRequired]
    public int SlopeMediumMinutes { get; set; }
    [JsonRequired]
    public int RecentCandlestickDays { get; set; }
    [JsonRequired]
    public int PseudoCandlestickLookbackPeriods { get; set; }
    [JsonRequired]
    public int RecentCandlesticksCount { get; set; }
}

/// <summary>
/// Configuration class for trading parameters settings.
/// </summary>
public class TradingParametersConfig
{
    [JsonRequired]
    public double TolerancePercentage { get; set; }
    [JsonRequired]
    public double TradingFeeRate { get; set; }
}