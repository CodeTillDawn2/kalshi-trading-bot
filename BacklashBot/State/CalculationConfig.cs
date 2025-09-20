using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BacklashBot.State
{
    /// <summary>
    /// Configuration class for calculation parameters used in MarketData.
    /// This replaces the eliminated CalculationConfig from TradingStrategies.
    /// </summary>
    public class CalculationConfig
    {
        public const string SectionName = "WatchedMarkets:CalculationConfig";

        [Required(ErrorMessage = "The 'TolerancePercentage' is missing in the configuration.")]
        public double TolerancePercentage { get; set; }

        [Required(ErrorMessage = "The 'RecentCandlestickDays' is missing in the configuration.")]
        public int RecentCandlestickDays { get; set; }

        [Required(ErrorMessage = "The 'SlopeShortMinutes' is missing in the configuration.")]
        public int SlopeShortMinutes { get; set; }

        [Required(ErrorMessage = "The 'SlopeMediumMinutes' is missing in the configuration.")]
        public int SlopeMediumMinutes { get; set; }

        [Required(ErrorMessage = "The 'RSI_Short_Periods' is missing in the configuration.")]
        public int RSI_Short_Periods { get; set; }

        [Required(ErrorMessage = "The 'RSI_Medium_Periods' is missing in the configuration.")]
        public int RSI_Medium_Periods { get; set; }

        [Required(ErrorMessage = "The 'RSI_Long_Periods' is missing in the configuration.")]
        public int RSI_Long_Periods { get; set; }

        [Required(ErrorMessage = "The 'MACD_Medium_FastPeriod' is missing in the configuration.")]
        public int MACD_Medium_FastPeriod { get; set; }

        [Required(ErrorMessage = "The 'MACD_Medium_SlowPeriod' is missing in the configuration.")]
        public int MACD_Medium_SlowPeriod { get; set; }

        [Required(ErrorMessage = "The 'MACD_Medium_SignalPeriod' is missing in the configuration.")]
        public int MACD_Medium_SignalPeriod { get; set; }

        [Required(ErrorMessage = "The 'MACD_Long_FastPeriod' is missing in the configuration.")]
        public int MACD_Long_FastPeriod { get; set; }

        [Required(ErrorMessage = "The 'MACD_Long_SlowPeriod' is missing in the configuration.")]
        public int MACD_Long_SlowPeriod { get; set; }

        [Required(ErrorMessage = "The 'MACD_Long_SignalPeriod' is missing in the configuration.")]
        public int MACD_Long_SignalPeriod { get; set; }

        [Required(ErrorMessage = "The 'EMA_Medium_Periods' is missing in the configuration.")]
        public int EMA_Medium_Periods { get; set; }

        [Required(ErrorMessage = "The 'EMA_Long_Periods' is missing in the configuration.")]
        public int EMA_Long_Periods { get; set; }

        [Required(ErrorMessage = "The 'BollingerBands_Medium_Periods' is missing in the configuration.")]
        public int BollingerBands_Medium_Periods { get; set; }

        [Required(ErrorMessage = "The 'BollingerBands_Medium_StdDev' is missing in the configuration.")]
        public double BollingerBands_Medium_StdDev { get; set; }

        [Required(ErrorMessage = "The 'BollingerBands_Long_Periods' is missing in the configuration.")]
        public int BollingerBands_Long_Periods { get; set; }

        [Required(ErrorMessage = "The 'BollingerBands_Long_StdDev' is missing in the configuration.")]
        public double BollingerBands_Long_StdDev { get; set; }

        [Required(ErrorMessage = "The 'ATR_Medium_Periods' is missing in the configuration.")]
        public int ATR_Medium_Periods { get; set; }

        [Required(ErrorMessage = "The 'ATR_Long_Periods' is missing in the configuration.")]
        public int ATR_Long_Periods { get; set; }

        [Required(ErrorMessage = "The 'VWAP_Short_Periods' is missing in the configuration.")]
        public int VWAP_Short_Periods { get; set; }

        [Required(ErrorMessage = "The 'VWAP_Medium_Periods' is missing in the configuration.")]
        public int VWAP_Medium_Periods { get; set; }

        [Required(ErrorMessage = "The 'Stochastic_Short_Periods' is missing in the configuration.")]
        public int Stochastic_Short_Periods { get; set; }

        [Required(ErrorMessage = "The 'Stochastic_Short_DPeriods' is missing in the configuration.")]
        public int Stochastic_Short_DPeriods { get; set; }

        [Required(ErrorMessage = "The 'Stochastic_Medium_Periods' is missing in the configuration.")]
        public int Stochastic_Medium_Periods { get; set; }

        [Required(ErrorMessage = "The 'Stochastic_Medium_DPeriods' is missing in the configuration.")]
        public int Stochastic_Medium_DPeriods { get; set; }

        [Required(ErrorMessage = "The 'Stochastic_Long_Periods' is missing in the configuration.")]
        public int Stochastic_Long_Periods { get; set; }

        [Required(ErrorMessage = "The 'Stochastic_Long_DPeriods' is missing in the configuration.")]
        public int Stochastic_Long_DPeriods { get; set; }

        [Required(ErrorMessage = "The 'TradingFeeRate' is missing in the configuration.")]
        public double TradingFeeRate { get; set; }

        [Required(ErrorMessage = "The 'PseudoCandlestickLookbackPeriods' is missing in the configuration.")]
        public int PseudoCandlestickLookbackPeriods { get; set; }

        [Required(ErrorMessage = "The 'RecentCandlesticksCount' is missing in the configuration.")]
        public int RecentCandlesticksCount { get; set; }

        [Required(ErrorMessage = "The 'PSAR_InitialAF' is missing in the configuration.")]
        public double PSAR_InitialAF { get; set; }

        [Required(ErrorMessage = "The 'PSAR_MaxAF' is missing in the configuration.")]
        public double PSAR_MaxAF { get; set; }

        [Required(ErrorMessage = "The 'PSAR_AFStep' is missing in the configuration.")]
        public double PSAR_AFStep { get; set; }

        [Required(ErrorMessage = "The 'ADX_Periods' is missing in the configuration.")]
        public int ADX_Periods { get; set; }

        [Required(ErrorMessage = "The 'ResistanceLevels_ExponentialMultiplier' is missing in the configuration.")]
        public double ResistanceLevels_ExponentialMultiplier { get; set; }

        [Required(ErrorMessage = "The 'ResistanceLevels_MinCandlestickPercentage' is missing in the configuration.")]
        public double ResistanceLevels_MinCandlestickPercentage { get; set; }

        [Required(ErrorMessage = "The 'ResistanceLevels_MaxLevels' is missing in the configuration.")]
        public int ResistanceLevels_MaxLevels { get; set; }

        [Required(ErrorMessage = "The 'ResistanceLevels_Sigma' is missing in the configuration.")]
        public double ResistanceLevels_Sigma { get; set; }

        [Required(ErrorMessage = "The 'ResistanceLevels_MinDistance' is missing in the configuration.")]
        public int ResistanceLevels_MinDistance { get; set; }
    }
}
