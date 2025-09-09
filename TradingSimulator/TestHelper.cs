using Microsoft.Extensions.Options;
using TradingStrategies.Configuration;

namespace TradingSimulator.Tests
{
    public static class TestHelper
    {

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
