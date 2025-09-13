using Microsoft.Extensions.Options;
using TradingStrategies.Configuration;
using System;
using System.Text.Json;
using System.IO;

namespace KalshiBotTests
{
    /// <summary>
    /// Provides utility methods for creating test configurations used in unit tests within the KalshiBotTests project.
    /// This class centralizes the creation of standardized configuration objects with typical values for testing trading and calculation logic.
    /// </summary>
    public static class TestHelper
    {
        public enum TestScenario
        {
            Default,
            Fast,
            Slow
        }

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
            ValidateTradingConfig(tradingConfig);
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
            ValidateCalculationConfig(calculationConfig);
            return Options.Create(calculationConfig);
        }

        private static void ValidateTradingConfig(TradingConfig config)
        {
            if (config.DecisionFrequencySeconds <= 0) throw new ArgumentException("DecisionFrequencySeconds must be positive");
            if (config.ChangeWindowDurationMinutes <= 0) throw new ArgumentException("ChangeWindowDurationMinutes must be positive");
            if (config.TradeMatchingWindowSeconds < 0) throw new ArgumentException("TradeMatchingWindowSeconds must be non-negative");
            if (config.OrderbookCancelWindowSeconds < 0) throw new ArgumentException("OrderbookCancelWindowSeconds must be non-negative");
            if (config.RefreshIntervalMinutes <= 0) throw new ArgumentException("RefreshIntervalMinutes must be positive");
            if (config.RefreshThresholdRatio < 0 || config.RefreshThresholdRatio > 1) throw new ArgumentException("RefreshThresholdRatio must be between 0 and 1");
            if (config.TimeBudgetRatio < 0 || config.TimeBudgetRatio > 1) throw new ArgumentException("TimeBudgetRatio must be between 0 and 1");
            if (config.MaxPositionSizePercent < 0 || config.MaxPositionSizePercent > 1) throw new ArgumentException("MaxPositionSizePercent must be between 0 and 1");
            if (config.MaxTotalExposurePercent < 0) throw new ArgumentException("MaxTotalExposurePercent must be non-negative");
            if (config.StopLossPercent < 0 || config.StopLossPercent > 1) throw new ArgumentException("StopLossPercent must be between 0 and 1");
            if (config.TakeProfitPercent < 0) throw new ArgumentException("TakeProfitPercent must be non-negative");
            if (config.MaxConcurrentPositions <= 0) throw new ArgumentException("MaxConcurrentPositions must be positive");
            if (config.MaxDrawdownPercent < 0 || config.MaxDrawdownPercent > 1) throw new ArgumentException("MaxDrawdownPercent must be between 0 and 1");
        }

        private static void ValidateCalculationConfig(CalculationConfig config)
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

        /// <summary>
        /// Creates and returns an IOptions<TradingConfig> instance with scenario-based test values.
        /// </summary>
        /// <param name="scenario">The test scenario to use for configuration values.</param>
        /// <returns>An IOptions<TradingConfig> instance configured for the specified scenario.</returns>
        public static IOptions<TradingConfig> GetTradingConfig(TestScenario scenario)
        {
            var config = new TradingConfig();
            switch (scenario)
            {
                case TestScenario.Fast:
                    config.DecisionFrequencySeconds = 30;
                    config.ChangeWindowDurationMinutes = 2;
                    config.TradeMatchingWindowSeconds = 2;
                    config.OrderbookCancelWindowSeconds = 5;
                    config.RefreshIntervalMinutes = 1;
                    break;
                case TestScenario.Slow:
                    config.DecisionFrequencySeconds = 300;
                    config.ChangeWindowDurationMinutes = 10;
                    config.TradeMatchingWindowSeconds = 10;
                    config.OrderbookCancelWindowSeconds = 30;
                    config.RefreshIntervalMinutes = 15;
                    break;
                default:
                    config.DecisionFrequencySeconds = 60;
                    config.ChangeWindowDurationMinutes = 5;
                    config.TradeMatchingWindowSeconds = 5;
                    config.OrderbookCancelWindowSeconds = 10;
                    config.RefreshIntervalMinutes = 5;
                    break;
            }
            ValidateTradingConfig(config);
            return Options.Create(config);
        }

        /// <summary>
        /// Creates and returns an IOptions<CalculationConfig> instance with scenario-based test values.
        /// </summary>
        /// <param name="scenario">The test scenario to use for configuration values.</param>
        /// <returns>An IOptions<CalculationConfig> instance configured for the specified scenario.</returns>
        public static IOptions<CalculationConfig> GetCalculationConfig(TestScenario scenario)
        {
            var config = new CalculationConfig();
            switch (scenario)
            {
                case TestScenario.Fast:
                    config.RSI_Short_Periods = 7;
                    config.RSI_Medium_Periods = 7;
                    config.RSI_Long_Periods = 7;
                    config.MACD_Medium_FastPeriod = 6;
                    config.MACD_Medium_SlowPeriod = 13;
                    config.MACD_Medium_SignalPeriod = 5;
                    config.EMA_Medium_Periods = 7;
                    config.EMA_Long_Periods = 7;
                    config.BollingerBands_Medium_Periods = 10;
                    config.BollingerBands_Medium_StdDev = 2;
                    config.ATR_Medium_Periods = 7;
                    config.ATR_Long_Periods = 7;
                    config.Stochastic_Short_Periods = 7;
                    config.Stochastic_Short_DPeriods = 3;
                    config.ResistanceLevels_MaxLevels = 3;
                    config.ResistanceLevels_MinDistance = 1;
                    break;
                case TestScenario.Slow:
                    config.RSI_Short_Periods = 21;
                    config.RSI_Medium_Periods = 21;
                    config.RSI_Long_Periods = 21;
                    config.MACD_Medium_FastPeriod = 18;
                    config.MACD_Medium_SlowPeriod = 39;
                    config.MACD_Medium_SignalPeriod = 13;
                    config.EMA_Medium_Periods = 21;
                    config.EMA_Long_Periods = 21;
                    config.BollingerBands_Medium_Periods = 30;
                    config.BollingerBands_Medium_StdDev = 2;
                    config.ATR_Medium_Periods = 21;
                    config.ATR_Long_Periods = 21;
                    config.Stochastic_Short_Periods = 21;
                    config.Stochastic_Short_DPeriods = 3;
                    config.ResistanceLevels_MaxLevels = 10;
                    config.ResistanceLevels_MinDistance = 5;
                    break;
                default:
                    config.RSI_Short_Periods = 14;
                    config.RSI_Medium_Periods = 14;
                    config.RSI_Long_Periods = 14;
                    config.MACD_Medium_FastPeriod = 12;
                    config.MACD_Medium_SlowPeriod = 26;
                    config.MACD_Medium_SignalPeriod = 9;
                    config.EMA_Medium_Periods = 14;
                    config.EMA_Long_Periods = 14;
                    config.BollingerBands_Medium_Periods = 20;
                    config.BollingerBands_Medium_StdDev = 2;
                    config.ATR_Medium_Periods = 14;
                    config.ATR_Long_Periods = 14;
                    config.Stochastic_Short_Periods = 14;
                    config.Stochastic_Short_DPeriods = 3;
                    config.ResistanceLevels_MaxLevels = 6;
                    config.ResistanceLevels_MinDistance = 3;
                    break;
            }
            ValidateCalculationConfig(config);
            return Options.Create(config);
        }

        /// <summary>
        /// Loads TradingConfig from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON configuration file.</param>
        /// <returns>An IOptions<TradingConfig> instance loaded from the file.</returns>
        public static IOptions<TradingConfig> LoadTradingConfigFromFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Configuration file not found", filePath);
            var json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<TradingConfig>(json);
            ValidateTradingConfig(config);
            return Options.Create(config);
        }

        /// <summary>
        /// Loads CalculationConfig from a JSON file.
        /// </summary>
        /// <param name="filePath">Path to the JSON configuration file.</param>
        /// <returns>An IOptions<CalculationConfig> instance loaded from the file.</returns>
        public static IOptions<CalculationConfig> LoadCalculationConfigFromFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("Configuration file not found", filePath);
            var json = File.ReadAllText(filePath);
            var config = JsonSerializer.Deserialize<CalculationConfig>(json);
            ValidateCalculationConfig(config);
            return Options.Create(config);
        }

    }
}
