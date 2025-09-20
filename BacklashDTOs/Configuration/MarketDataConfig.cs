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
    required
    public int SemaphoreTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets the ticker batch size. Default is 100.
    /// </summary>
    required
    public int TickerBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the API retry timeout in milliseconds. Default is 30000.
    /// </summary>
    required
    public int ApiRetryTimeoutMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether performance metrics logging is enabled for MarketDataService. Default is false.
    /// </summary>
    required
    public bool EnablePerformanceMetrics { get; set; }

    /// <summary>
    /// Gets or sets the technical analysis configuration.
    /// </summary>
    required
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
    required
    public RSIConfig RSI { get; set; }

    /// <summary>
    /// Gets or sets the MACD configuration.
    /// </summary>
    required
    public MACDConfig MACD { get; set; }

    /// <summary>
    /// Gets or sets the EMA configuration.
    /// </summary>
    required
    public EMAConfig EMA { get; set; }

    /// <summary>
    /// Gets or sets the Bollinger Bands configuration.
    /// </summary>
    required
    public BollingerBandsConfig BollingerBands { get; set; }

    /// <summary>
    /// Gets or sets the ATR configuration.
    /// </summary>
    required
    public ATRConfig ATR { get; set; }

    /// <summary>
    /// Gets or sets the Stochastic configuration.
    /// </summary>
    required
    public StochasticConfig Stochastic { get; set; }

    /// <summary>
    /// Gets or sets the ADX periods.
    /// </summary>
    required
    public int ADX_Periods { get; set; }

    /// <summary>
    /// Gets or sets the volume indicators configuration.
    /// </summary>
    required
    public VolumeIndicatorsConfig VolumeIndicators { get; set; }

    /// <summary>
    /// Gets or sets the support resistance configuration.
    /// </summary>
    required
    public SupportResistanceConfig SupportResistance { get; set; }

    /// <summary>
    /// Gets or sets the momentum indicators configuration.
    /// </summary>
    required
    public MomentumIndicatorsConfig MomentumIndicators { get; set; }

    /// <summary>
    /// Gets or sets the candlestick analysis configuration.
    /// </summary>
    required
    public CandlestickAnalysisConfig CandlestickAnalysis { get; set; }

    /// <summary>
    /// Gets or sets the trading parameters configuration.
    /// </summary>
    required
    public TradingParametersConfig TradingParameters { get; set; }
}

/// <summary>
/// Configuration class for RSI settings.
/// </summary>
public class RSIConfig
{
    required
    public int Short_Periods { get; set; }
    required
    public int Medium_Periods { get; set; }
    required
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for MACD settings.
/// </summary>
public class MACDConfig
{
    required
    public int Medium_FastPeriod { get; set; }
    required
    public int Medium_SlowPeriod { get; set; }
    required
    public int Medium_SignalPeriod { get; set; }
    required
    public int Long_FastPeriod { get; set; }
    required
    public int Long_SlowPeriod { get; set; }
    required
    public int Long_SignalPeriod { get; set; }
}

/// <summary>
/// Configuration class for EMA settings.
/// </summary>
public class EMAConfig
{
    required
    public int Medium_Periods { get; set; }
    required
    public int Long_Periods { get; set; }
}

/// <summary>
/// Configuration class for Bollinger Bands settings.
/// </summary>
public class BollingerBandsConfig
{
    required
    public int Medium_Periods { get; set; }
    required
    public double Medium_StdDev { get; set; }
    required
    public int Long_Periods { get; set; }
    required
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
