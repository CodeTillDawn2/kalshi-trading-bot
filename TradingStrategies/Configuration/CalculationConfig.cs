namespace TradingStrategies.Configuration
{
    public class CalculationConfig
    {
        public int RSI_Short_Periods { get; set; }
        public int RSI_Medium_Periods { get; set; }
        public int RSI_Long_Periods { get; set; }

        public int MACD_Medium_FastPeriod { get; set; }
        public int MACD_Medium_SlowPeriod { get; set; }
        public int MACD_Medium_SignalPeriod { get; set; }

        public int MACD_Long_FastPeriod { get; set; }
        public int MACD_Long_SlowPeriod { get; set; }
        public int MACD_Long_SignalPeriod { get; set; }

        public int EMA_Medium_Periods { get; set; }
        public int EMA_Long_Periods { get; set; }

        public int BollingerBands_Medium_Periods { get; set; }
        public int BollingerBands_Medium_StdDev { get; set; }
        public int BollingerBands_Long_Periods { get; set; }
        public int BollingerBands_Long_StdDev { get; set; }

        public int VWAP_Short_Periods { get; set; }
        public int VWAP_Medium_Periods { get; set; }

        public int ATR_Medium_Periods { get; set; }
        public int ATR_Long_Periods { get; set; }

        public int Stochastic_Short_Periods { get; set; }
        public int Stochastic_Short_DPeriods { get; set; }
        public int Stochastic_Medium_Periods { get; set; }
        public int Stochastic_Medium_DPeriods { get; set; }
        public int Stochastic_Long_Periods { get; set; }
        public int Stochastic_Long_DPeriods { get; set; }

        public double ResistanceLevels_MinCandlestickPercentage { get; set; }
        public int ResistanceLevels_MaxLevels { get; set; }
        public double ResistanceLevels_Sigma { get; set; }
        public int ResistanceLevels_MinDistance { get; set; }
        public int ADX_Periods { get; set; }

    }
}