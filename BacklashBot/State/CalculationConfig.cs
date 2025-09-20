using System;
using System.Text.Json.Serialization;

namespace BacklashBot.State
{
    /// <summary>
    /// Configuration class for calculation parameters used in MarketData.
    /// This replaces the eliminated CalculationConfig from TradingStrategies.
    /// </summary>
    public class CalculationConfig
    {
        public required double TolerancePercentage { get; set; }
        public required int RecentCandlestickDays { get; set; }
        public required int SlopeShortMinutes { get; set; }
        public required int SlopeMediumMinutes { get; set; }
        public required int RSI_Short_Periods { get; set; }
        public required int RSI_Medium_Periods { get; set; }
        public required int RSI_Long_Periods { get; set; }
        public required int MACD_Medium_FastPeriod { get; set; }
        public required int MACD_Medium_SlowPeriod { get; set; }
        public required int MACD_Medium_SignalPeriod { get; set; }
        public required int MACD_Long_FastPeriod { get; set; }
        public required int MACD_Long_SlowPeriod { get; set; }
        public required int MACD_Long_SignalPeriod { get; set; }
        public required int EMA_Medium_Periods { get; set; }
        public required int EMA_Long_Periods { get; set; }
        public required int BollingerBands_Medium_Periods { get; set; }
        public required double BollingerBands_Medium_StdDev { get; set; }
        public required int BollingerBands_Long_Periods { get; set; }
        public required double BollingerBands_Long_StdDev { get; set; }
        public required int ATR_Medium_Periods { get; set; }
        public required int ATR_Long_Periods { get; set; }
        public required int VWAP_Short_Periods { get; set; }
        public required int VWAP_Medium_Periods { get; set; }
        public required int Stochastic_Short_Periods { get; set; }
        public required int Stochastic_Short_DPeriods { get; set; }
        public required int Stochastic_Medium_Periods { get; set; }
        public required int Stochastic_Medium_DPeriods { get; set; }
        public required int Stochastic_Long_Periods { get; set; }
        public required int Stochastic_Long_DPeriods { get; set; }
        public required double TradingFeeRate { get; set; }
        public required int PseudoCandlestickLookbackPeriods { get; set; }
        public required int RecentCandlesticksCount { get; set; }
        public required double PSAR_InitialAF { get; set; }
        public required double PSAR_MaxAF { get; set; }
        public required double PSAR_AFStep { get; set; }
        public required int ADX_Periods { get; set; }
        public required double ResistanceLevels_ExponentialMultiplier { get; set; }
        public required double ResistanceLevels_MinCandlestickPercentage { get; set; }
        public required int ResistanceLevels_MaxLevels { get; set; }
        public required double ResistanceLevels_Sigma { get; set; }
        public required int ResistanceLevels_MinDistance { get; set; }
    }
}
