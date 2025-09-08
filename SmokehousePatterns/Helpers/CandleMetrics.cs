namespace SmokehousePatterns.Helpers
{
    public struct CandleMetrics
    {
        public double BodySize { get; set; }
        public double UpperWick { get; set; }
        public double LowerWick { get; set; }
        public double TotalRange { get; set; }
        public bool IsBullish { get; set; }
        public bool IsBearish { get; set; }

        public double BodyToRangeRatio { get; set; }
        public double MidPoint { get; set; }
        public bool HasNoUpperWick { get; set; }
        public bool HasNoLowerWick { get; set; }
        public double BodyMidPoint { get; set; }
        /// <summary>
        /// Mean Trend for the candle at this index if the pattern is index - 1 long.. e.g. Single candlestick pattern mean is at index 0
        /// </summary>
        public double[] LookbackMeanTrend { get; set; }
        public double[] LookbackTrendConsistency { get; set; }
        public double[] AvgVoumeVsLookback { get; set; }
        public double[] LookbackAvgRange { get; set; }
        public double[] BullishRatio { get; set; }
        public double[] BearishRatio { get; set; }
        public int IntervalType { get; set; }

        public double GetLookbackMeanTrend(int patternLength)
        {
            if (LookbackMeanTrend[patternLength - 1] != null)
                return LookbackMeanTrend[patternLength - 1];
            return 0;
        }

        public double GetLookbackTrendConsistency(int patternLength)
        {
            if (LookbackTrendConsistency[patternLength - 1] != null)
                return LookbackTrendConsistency[patternLength - 1];
            return 0;
        }

        public double GetAvgVoumeVsLookback(int patternLength)
        {
            if (AvgVoumeVsLookback[patternLength - 1] != null)
                return AvgVoumeVsLookback[patternLength - 1];
            return 0;
        }

        public double GetLookbackAvgRange(int patternLength)
        {
            if (LookbackAvgRange[patternLength - 1] != null)
                return LookbackAvgRange[patternLength - 1];
            return 0;
        }
      
    }
}