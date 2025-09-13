using Microsoft.Extensions.Options;
using TradingStrategies.Configuration;

namespace TradingSimulator.Tests
{
    /// <summary>
    /// Provides utility methods for creating test configurations used in unit tests within the TradingSimulator project.
    /// This class centralizes the creation of standardized configuration objects with typical values for testing trading and calculation logic.
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// Creates and returns an IOptions<TradingConfig> instance with predefined test values for trading-related settings.
        /// These values represent common configuration parameters used in testing scenarios, such as decision frequency and window durations.
        /// The returned options object can be injected into services that depend on TradingConfig for isolated testing.
        /// </summary>
        /// <returns>An IOptions<TradingConfig> instance configured with standard test values.</returns>
        public static IOptions<TradingConfig> GetTradingConfig()
        {
            var tradingConfig = new TradingConfig
            {
                DecisionFrequencySeconds = 60,
                ChangeWindowDurationMinutes = 5,
                TradeMatchingWindowSeconds = 5,
            };
            return Options.Create(tradingConfig);
        }

        /// <summary>
        /// Creates and returns an IOptions<CalculationConfig> instance with predefined test values for technical indicator and calculation settings.
        /// These values represent standard periods and parameters for various trading indicators (RSI, MACD, EMA, Bollinger Bands, etc.) used in testing.
        /// The configuration includes settings for short, medium, and long-term calculations across multiple technical analysis tools.
        /// The returned options object enables consistent testing of calculation logic without external configuration dependencies.
        /// </summary>
        /// <returns>An IOptions<CalculationConfig> instance configured with comprehensive test values for technical indicators.</returns>
        public static IOptions<CalculationConfig> GetCalculationConfig()
        {
            var calculationConfig = new CalculationConfig
            {
                RSI_Short_Periods = 14,
                RSI_Medium_Periods = 14,
                RSI_Long_Periods = 14,
                MACD_Medium_FastPeriod = 12,
                MACD_Medium_SlowPeriod = 26,
                MACD_Medium_SignalPeriod = 9,
                MACD_Long_FastPeriod = 12,
                MACD_Long_SlowPeriod = 26,
                MACD_Long_SignalPeriod = 9,
                EMA_Medium_Periods = 14,
                EMA_Long_Periods = 14,
                BollingerBands_Medium_Periods = 20,
                BollingerBands_Medium_StdDev = 2,
                BollingerBands_Long_Periods = 20,
                BollingerBands_Long_StdDev = 2,
                ATR_Medium_Periods = 14,
                ATR_Long_Periods = 14,
                Stochastic_Short_Periods = 14,
                Stochastic_Medium_Periods = 14,
                Stochastic_Long_Periods = 14,
                ResistanceLevels_MinCandlestickPercentage = 0.1,
                ResistanceLevels_MaxLevels = 6,
                ResistanceLevels_Sigma = 2.0,
                ResistanceLevels_MinDistance = 3
            };
            return Options.Create(calculationConfig);
        }

    }
}
