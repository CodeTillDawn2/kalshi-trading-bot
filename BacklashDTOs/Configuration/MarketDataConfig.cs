using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace BacklashDTOs.Configuration;

/// <summary>
/// Configuration class for market data settings.
/// </summary>
public class MarketDataConfig : IValidateOptions<MarketDataConfig>
{
    /// <summary>
    /// Gets or sets the semaphore timeout in milliseconds. Default is 5000.
    /// </summary>
    public int SemaphoreTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the ticker batch size. Default is 100.
    /// </summary>
    public int TickerBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the API retry timeout in milliseconds. Default is 30000.
    /// </summary>
    public int ApiRetryTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets a value indicating whether performance metrics logging is enabled for MarketDataService. Default is false.
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets the technical analysis configuration.
    /// </summary>
    public TechnicalAnalysisConfig TechnicalAnalysis { get; set; } = new();

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
    public ValidateOptionsResult Validate(string? name, MarketDataConfig options)
    {
        var failures = new List<string>();

        if (options.SemaphoreTimeoutMs <= 0)
        {
            failures.Add($"{nameof(SemaphoreTimeoutMs)} must be greater than 0.");
        }

        if (options.TickerBatchSize <= 0)
        {
            failures.Add($"{nameof(TickerBatchSize)} must be greater than 0.");
        }

        if (options.ApiRetryTimeoutMs <= 0)
        {
            failures.Add($"{nameof(ApiRetryTimeoutMs)} must be greater than 0.");
        }

        return failures.Any() ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Configuration class for technical analysis settings.
/// </summary>
public class TechnicalAnalysisConfig
{
    /// <summary>
    /// Gets or sets the RSI configuration.
    /// </summary>
    public RSIConfig RSI { get; set; } = new();

    /// <summary>
    /// Gets or sets the MACD configuration.
    /// </summary>
    public MACDConfig MACD { get; set; } = new();

    /// <summary>
    /// Gets or sets the EMA configuration.
    /// </summary>
    public EMAConfig EMA { get; set; } = new();

    /// <summary>
    /// Gets or sets the Bollinger Bands configuration.
    /// </summary>
    public BollingerBandsConfig BollingerBands { get; set; } = new();

    /// <summary>
    /// Gets or sets the ATR configuration.
    /// </summary>
    public ATRConfig ATR { get; set; } = new();

    /// <summary>
    /// Gets or sets the Stochastic configuration.
    /// </summary>
    public StochasticConfig Stochastic { get; set; } = new();

    /// <summary>
    /// Gets or sets the ADX periods.
    /// </summary>
    public int ADX_Periods { get; set; } = 14;

    /// <summary>
    /// Gets or sets the volume indicators configuration.
    /// </summary>
    public VolumeIndicatorsConfig VolumeIndicators { get; set; } = new();

    /// <summary>
    /// Gets or sets the support resistance configuration.
    /// </summary>
    public SupportResistanceConfig SupportResistance { get; set; } = new();

    /// <summary>
    /// Gets or sets the momentum indicators configuration.
    /// </summary>
    public MomentumIndicatorsConfig MomentumIndicators { get; set; } = new();

    /// <summary>
    /// Gets or sets the candlestick analysis configuration.
    /// </summary>
    public CandlestickAnalysisConfig CandlestickAnalysis { get; set; } = new();

    /// <summary>
    /// Gets or sets the trading parameters configuration.
    /// </summary>
    public TradingParametersConfig TradingParameters { get; set; } = new();
}

/// <summary>
/// Configuration class for RSI settings.
/// </summary>
public class RSIConfig
{
    public int Short_Periods { get; set; } = 14;
    public int Medium_Periods { get; set; } = 14;
    public int Long_Periods { get; set; } = 14;
}

/// <summary>
/// Configuration class for MACD settings.
/// </summary>
public class MACDConfig
{
    public int Medium_FastPeriod { get; set; } = 12;
    public int Medium_SlowPeriod { get; set; } = 26;
    public int Medium_SignalPeriod { get; set; } = 9;
    public int Long_FastPeriod { get; set; } = 12;
    public int Long_SlowPeriod { get; set; } = 26;
    public int Long_SignalPeriod { get; set; } = 9;
}

/// <summary>
/// Configuration class for EMA settings.
/// </summary>
public class EMAConfig
{
    public int Medium_Periods { get; set; } = 14;
    public int Long_Periods { get; set; } = 14;
}

/// <summary>
/// Configuration class for Bollinger Bands settings.
/// </summary>
public class BollingerBandsConfig
{
    public int Medium_Periods { get; set; } = 20;
    public double Medium_StdDev { get; set; } = 2.0;
    public int Long_Periods { get; set; } = 20;
    public double Long_StdDev { get; set; } = 2.0;
}

/// <summary>
/// Configuration class for ATR settings.
/// </summary>
public class ATRConfig
{
    public int Medium_Periods { get; set; } = 14;
    public int Long_Periods { get; set; } = 14;
}

/// <summary>
/// Configuration class for Stochastic settings.
/// </summary>
public class StochasticConfig
{
    public int Short_Periods { get; set; } = 14;
    public int Short_DPeriods { get; set; } = 3;
    public int Medium_Periods { get; set; } = 14;
    public int Medium_DPeriods { get; set; } = 3;
    public int Long_Periods { get; set; } = 14;
    public int Long_DPeriods { get; set; } = 3;
}

/// <summary>
/// Configuration class for volume indicators settings.
/// </summary>
public class VolumeIndicatorsConfig
{
    public int VWAP_Short_Periods { get; set; } = 15;
    public int VWAP_Medium_Periods { get; set; } = 15;
}

/// <summary>
/// Configuration class for support resistance settings.
/// </summary>
public class SupportResistanceConfig
{
    public double MinCandlestickPercentage { get; set; } = 0.1;
    public int MaxLevels { get; set; } = 6;
    public double Sigma { get; set; } = 2.0;
    public int MinDistance { get; set; } = 3;
    public double ExponentialMultiplier { get; set; } = 2.0;
}

/// <summary>
/// Configuration class for momentum indicators settings.
/// </summary>
public class MomentumIndicatorsConfig
{
    public double PSAR_InitialAF { get; set; } = 0.02;
    public double PSAR_MaxAF { get; set; } = 0.2;
    public double PSAR_AFStep { get; set; } = 0.02;
}

/// <summary>
/// Configuration class for candlestick analysis settings.
/// </summary>
public class CandlestickAnalysisConfig
{
    public int SlopeShortMinutes { get; set; } = 5;
    public int SlopeMediumMinutes { get; set; } = 15;
    public int RecentCandlestickDays { get; set; } = 1;
    public int PseudoCandlestickLookbackPeriods { get; set; } = 34;
    public int RecentCandlesticksCount { get; set; } = 15;
}

/// <summary>
/// Configuration class for trading parameters settings.
/// </summary>
public class TradingParametersConfig
{
    public double TolerancePercentage { get; set; } = 10.0;
    public double TradingFeeRate { get; set; } = 0.07;
}