using BacklashBot.Configuration;
using BacklashBot.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace KalshiBotTests
{
    /// <summary>
    /// Provides utility methods for creating test configurations used in unit tests within the KalshiBotTests project.
    /// This class centralizes the creation of standardized configuration objects with typical values for testing trading and calculation logic.
    /// </summary>
    public static class TestHelper
    {


        /// <summary>
        /// Gets the configuration from the appsettings.json file in the BacklashBot directory.
        /// </summary>
        /// <returns>An IConfiguration instance loaded from the appsettings.json file.</returns>
        private static IConfiguration GetConfiguration()
        {
            var currentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..", "..", "..",  "BacklashBot");
            return BacklashCommon.Configuration.ConfigurationHelper.CreateConfigurationBuilder(currentDir, Array.Empty<string>()).Build();
        }

        /// <summary>
        /// Creates and returns an IOptions&lt;TradingConfig&gt; instance with predefined test values for trading-related settings.
        /// These values represent common configuration parameters used in testing scenarios, such as decision frequency and window durations.
        /// The returned options object can be injected into services that depend on TradingConfig for isolated testing.
        /// </summary>
        /// <returns>An IOptions&lt;TradingConfig&gt; instance configured with standard test values.</returns>
        public static IOptions<GeneralExecutionConfig> GetGeneralExecutionConfig()
        {
            var config = GetConfiguration().GetSection(GeneralExecutionConfig.SectionName).Get<GeneralExecutionConfig>();
            return Options.Create(config);
        }


        /// <summary>
        /// Creates and returns an IOptions&lt;CalculationConfig&gt; instance with predefined test values for technical indicator and calculation settings.
        /// These values represent standard periods and parameters for various trading indicators (RSI, MACD, EMA, Bollinger Bands, etc.) used in testing.
        /// The configuration includes settings for short, medium, and long-term calculations across multiple technical analysis tools.
        /// The returned options object enables consistent testing of calculation logic without external configuration dependencies.
        /// </summary>
        /// <returns>An IOptions&lt;CalculationConfig&gt; instance configured with comprehensive test values for technical indicators.</returns>
        public static IOptions<CalculationsConfig> GetCalculationConfig()
        {
            var config = GetConfiguration().GetSection(CalculationsConfig.SectionName).Get<CalculationsConfig>();
            ValidateCalculationConfig(config);
            return Options.Create(config);
        }
        /// <summary>
        /// Creates and returns an IOptions&lt;TradingSnapshotServiceConfig&gt; instance loaded from the appsettings.json file.
        /// </summary>
        /// <returns>An IOptions&lt;TradingSnapshotServiceConfig&gt; instance configured from the configuration file.</returns>
        public static IOptions<BacklashCommon.Configuration.TradingSnapshotServiceConfig> GetTradingSnapshotServiceConfig()
        {
            var config = GetConfiguration().GetSection(BacklashCommon.Configuration.TradingSnapshotServiceConfig.SectionName).Get<BacklashCommon.Configuration.TradingSnapshotServiceConfig>();
            return Options.Create(config);
        }



        /// <summary>
        /// Validates all properties of a CalculationConfig instance to ensure they contain valid values.
        /// Throws ArgumentException if any property has an invalid value.
        /// </summary>
        /// <param name="config">The CalculationConfig instance to validate.</param>
        /// <exception cref="ArgumentException">Thrown when any configuration property has an invalid value.</exception>
        private static void ValidateCalculationConfig(CalculationsConfig config)
        {
            if (config.RSI_Short_Periods <= 0) throw new ArgumentException("RSI_Short_Periods must be positive");
            if (config.RSI_Medium_Periods <= 0) throw new ArgumentException("RSI_Medium_Periods must be positive");
            if (config.RSI_Long_Periods <= 0) throw new ArgumentException("RSI_Long_Periods must be positive");
            if (config.MACD_Medium_FastPeriod <= 0) throw new ArgumentException("MACD_Medium_FastPeriod must be positive");
            if (config.MACD_Medium_SlowPeriod <= 0) throw new ArgumentException("MACD_Medium_SlowPeriod must be positive");
            if (config.MACD_Medium_SignalPeriod <= 0) throw new ArgumentException("MACD_Medium_SignalPeriod must be positive");
            if (config.MACD_Long_FastPeriod <= 0) throw new ArgumentException("MACD_Long_FastPeriod must be positive");
            if (config.MACD_Long_SlowPeriod <= 0) throw new ArgumentException("MACD_Long_SlowPeriod must be positive");
            if (config.MACD_Long_SignalPeriod <= 0) throw new ArgumentException("MACD_Long_SignalPeriod must be positive");
            if (config.EMA_Medium_Periods <= 0) throw new ArgumentException("EMA_Medium_Periods must be positive");
            if (config.EMA_Long_Periods <= 0) throw new ArgumentException("EMA_Long_Periods must be positive");
            if (config.BollingerBands_Medium_Periods <= 0) throw new ArgumentException("BollingerBands_Medium_Periods must be positive");
            if (config.BollingerBands_Medium_StdDev <= 0) throw new ArgumentException("BollingerBands_Medium_StdDev must be positive");
            if (config.BollingerBands_Long_Periods <= 0) throw new ArgumentException("BollingerBands_Long_Periods must be positive");
            if (config.BollingerBands_Long_StdDev <= 0) throw new ArgumentException("BollingerBands_Long_StdDev must be positive");
            if (config.VWAP_Short_Periods <= 0) throw new ArgumentException("VWAP_Short_Periods must be positive");
            if (config.VWAP_Medium_Periods <= 0) throw new ArgumentException("VWAP_Medium_Periods must be positive");
            if (config.ATR_Medium_Periods <= 0) throw new ArgumentException("ATR_Medium_Periods must be positive");
            if (config.ATR_Long_Periods <= 0) throw new ArgumentException("ATR_Long_Periods must be positive");
            if (config.Stochastic_Short_Periods <= 0) throw new ArgumentException("Stochastic_Short_Periods must be positive");
            if (config.Stochastic_Short_DPeriods <= 0) throw new ArgumentException("Stochastic_Short_DPeriods must be positive");
            if (config.Stochastic_Medium_Periods <= 0) throw new ArgumentException("Stochastic_Medium_Periods must be positive");
            if (config.Stochastic_Medium_DPeriods <= 0) throw new ArgumentException("Stochastic_Medium_DPeriods must be positive");
            if (config.Stochastic_Long_Periods <= 0) throw new ArgumentException("Stochastic_Long_Periods must be positive");
            if (config.Stochastic_Long_DPeriods <= 0) throw new ArgumentException("Stochastic_Long_DPeriods must be positive");
            if (config.ResistanceLevels_MaxLevels <= 0) throw new ArgumentException("ResistanceLevels_MaxLevels must be positive");
            if (config.ResistanceLevels_MinDistance <= 0) throw new ArgumentException("ResistanceLevels_MinDistance must be positive");
            if (config.ADX_Periods <= 0) throw new ArgumentException("ADX_Periods must be positive");
            if (config.ResistanceLevels_ExponentialMultiplier <= 0) throw new ArgumentException("ResistanceLevels_ExponentialMultiplier must be positive");
            if (config.TolerancePercentage <= 0) throw new ArgumentException("TolerancePercentage must be positive");
            if (config.TradingFeeRate < 0) throw new ArgumentException("TradingFeeRate must be non-negative");
            if (config.SlopeShortMinutes <= 0) throw new ArgumentException("SlopeShortMinutes must be positive");
            if (config.SlopeMediumMinutes <= 0) throw new ArgumentException("SlopeMediumMinutes must be positive");
            if (config.RecentCandlestickDays <= 0) throw new ArgumentException("RecentCandlestickDays must be positive");
            if (config.PseudoCandlestickLookbackPeriods <= 0) throw new ArgumentException("PseudoCandlestickLookbackPeriods must be positive");
            if (config.RecentCandlesticksCount <= 0) throw new ArgumentException("RecentCandlesticksCount must be positive");
        }

    }
}
