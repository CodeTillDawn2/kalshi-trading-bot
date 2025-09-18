using System;

namespace BacklashBot.State
{
    /// <summary>
    /// Configuration class for calculation parameters used in MarketData.
    /// This replaces the eliminated CalculationConfig from TradingStrategies.
    /// </summary>
    public class CalculationConfig
    {
        public double TolerancePercentage { get; set; } = 10.0;
        public int RecentCandlestickDays { get; set; } = 1;
        public int SlopeShortMinutes { get; set; } = 5;
        public int SlopeMediumMinutes { get; set; } = 15;
        public int RSI_Short_Periods { get; set; } = 14;
        public int RSI_Medium_Periods { get; set; } = 14;
        public int RSI_Long_Periods { get; set; } = 14;
        public int MACD_Medium_FastPeriod { get; set; } = 12;
        public int MACD_Medium_SlowPeriod { get; set; } = 26;
        public int MACD_Medium_SignalPeriod { get; set; } = 9;
        public int MACD_Long_FastPeriod { get; set; } = 12;
        public int MACD_Long_SlowPeriod { get; set; } = 26;
        public int MACD_Long_SignalPeriod { get; set; } = 9;
        public int EMA_Medium_Periods { get; set; } = 14;
        public int EMA_Long_Periods { get; set; } = 14;
        public int BollingerBands_Medium_Periods { get; set; } = 20;
        public double BollingerBands_Medium_StdDev { get; set; } = 2.0;
        public int BollingerBands_Long_Periods { get; set; } = 20;
        public double BollingerBands_Long_StdDev { get; set; } = 2.0;
        public int ATR_Medium_Periods { get; set; } = 14;
        public int ATR_Long_Periods { get; set; } = 14;
        public int VWAP_Short_Periods { get; set; } = 15;
        public int VWAP_Medium_Periods { get; set; } = 15;
        public int Stochastic_Short_Periods { get; set; } = 14;
        public int Stochastic_Short_DPeriods { get; set; } = 3;
        public int Stochastic_Medium_Periods { get; set; } = 14;
        public int Stochastic_Medium_DPeriods { get; set; } = 3;
        public int Stochastic_Long_Periods { get; set; } = 14;
        public int Stochastic_Long_DPeriods { get; set; } = 3;
        public double TradingFeeRate { get; set; } = 0.07;
        public int PseudoCandlestickLookbackPeriods { get; set; } = 34;
        public int RecentCandlesticksCount { get; set; } = 15;
        public double PSAR_InitialAF { get; set; } = 0.02;
        public double PSAR_MaxAF { get; set; } = 0.2;
        public double PSAR_AFStep { get; set; } = 0.02;
        public int ADX_Periods { get; set; } = 14;
        public double ResistanceLevels_ExponentialMultiplier { get; set; } = 2.0;
        public double ResistanceLevels_MinCandlestickPercentage { get; set; } = 0.1;
        public int ResistanceLevels_MaxLevels { get; set; } = 6;
        public double ResistanceLevels_Sigma { get; set; } = 2.0;
        public int ResistanceLevels_MinDistance { get; set; } = 3;
    }
}