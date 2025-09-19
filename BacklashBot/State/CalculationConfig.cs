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
        [JsonRequired]
        public double TolerancePercentage { get; set; }
        [JsonRequired]
        public int RecentCandlestickDays { get; set; }
        [JsonRequired]
        public int SlopeShortMinutes { get; set; }
        [JsonRequired]
        public int SlopeMediumMinutes { get; set; }
        [JsonRequired]
        public int RSI_Short_Periods { get; set; }
        [JsonRequired]
        public int RSI_Medium_Periods { get; set; }
        [JsonRequired]
        public int RSI_Long_Periods { get; set; }
        [JsonRequired]
        public int MACD_Medium_FastPeriod { get; set; }
        [JsonRequired]
        public int MACD_Medium_SlowPeriod { get; set; }
        [JsonRequired]
        public int MACD_Medium_SignalPeriod { get; set; }
        [JsonRequired]
        public int MACD_Long_FastPeriod { get; set; }
        [JsonRequired]
        public int MACD_Long_SlowPeriod { get; set; }
        [JsonRequired]
        public int MACD_Long_SignalPeriod { get; set; }
        [JsonRequired]
        public int EMA_Medium_Periods { get; set; }
        [JsonRequired]
        public int EMA_Long_Periods { get; set; }
        [JsonRequired]
        public int BollingerBands_Medium_Periods { get; set; }
        [JsonRequired]
        public double BollingerBands_Medium_StdDev { get; set; }
        [JsonRequired]
        public int BollingerBands_Long_Periods { get; set; }
        [JsonRequired]
        public double BollingerBands_Long_StdDev { get; set; }
        [JsonRequired]
        public int ATR_Medium_Periods { get; set; }
        [JsonRequired]
        public int ATR_Long_Periods { get; set; }
        [JsonRequired]
        public int VWAP_Short_Periods { get; set; }
        [JsonRequired]
        public int VWAP_Medium_Periods { get; set; }
        [JsonRequired]
        public int Stochastic_Short_Periods { get; set; }
        [JsonRequired]
        public int Stochastic_Short_DPeriods { get; set; }
        [JsonRequired]
        public int Stochastic_Medium_Periods { get; set; }
        [JsonRequired]
        public int Stochastic_Medium_DPeriods { get; set; }
        [JsonRequired]
        public int Stochastic_Long_Periods { get; set; }
        [JsonRequired]
        public int Stochastic_Long_DPeriods { get; set; }
        [JsonRequired]
        public double TradingFeeRate { get; set; }
        [JsonRequired]
        public int PseudoCandlestickLookbackPeriods { get; set; }
        [JsonRequired]
        public int RecentCandlesticksCount { get; set; }
        [JsonRequired]
        public double PSAR_InitialAF { get; set; }
        [JsonRequired]
        public double PSAR_MaxAF { get; set; }
        [JsonRequired]
        public double PSAR_AFStep { get; set; }
        [JsonRequired]
        public int ADX_Periods { get; set; }
        [JsonRequired]
        public double ResistanceLevels_ExponentialMultiplier { get; set; }
        [JsonRequired]
        public double ResistanceLevels_MinCandlestickPercentage { get; set; }
        [JsonRequired]
        public int ResistanceLevels_MaxLevels { get; set; }
        [JsonRequired]
        public double ResistanceLevels_Sigma { get; set; }
        [JsonRequired]
        public int ResistanceLevels_MinDistance { get; set; }
    }
}