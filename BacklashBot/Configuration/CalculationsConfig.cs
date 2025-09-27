using System.ComponentModel.DataAnnotations;

namespace BacklashBot.Configuration
{
    /// <summary>
    /// Configuration class for calculation parameters used in MarketData.
    /// This replaces the eliminated CalculationConfig from TradingStrategies.
    /// </summary>
    public class CalculationsConfig
    {
        /// <summary>
        /// Configuration section name for accessing these settings in configuration files.
        /// </summary>
        public const string SectionName = "WatchedMarkets:MarketData:Calculations";

        /// <summary>
        /// Gets or sets the tolerance percentage used for price calculations and comparisons.
        /// </summary>
        [Required(ErrorMessage = "The 'TolerancePercentage' is missing in the configuration.")]
        public double TolerancePercentage { get; set; }

        /// <summary>
        /// Gets or sets the number of recent days to consider for candlestick data analysis.
        /// </summary>
        [Required(ErrorMessage = "The 'RecentCandlestickDays' is missing in the configuration.")]
        public int RecentCandlestickDays { get; set; }

        /// <summary>
        /// Gets or sets the number of minutes for short-term slope calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'SlopeShortMinutes' is missing in the configuration.")]
        public int SlopeShortMinutes { get; set; }

        /// <summary>
        /// Gets or sets the number of minutes for medium-term slope calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'SlopeMediumMinutes' is missing in the configuration.")]
        public int SlopeMediumMinutes { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for short-term RSI calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'RSI_Short_Periods' is missing in the configuration.")]
        public int RSI_Short_Periods { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for medium-term RSI calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'RSI_Medium_Periods' is missing in the configuration.")]
        public int RSI_Medium_Periods { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for long-term RSI calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'RSI_Long_Periods' is missing in the configuration.")]
        public int RSI_Long_Periods { get; set; }

        /// <summary>
        /// Gets or sets the fast period for medium-term MACD calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'MACD_Medium_FastPeriod' is missing in the configuration.")]
        public int MACD_Medium_FastPeriod { get; set; }

        /// <summary>
        /// Gets or sets the slow period for medium-term MACD calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'MACD_Medium_SlowPeriod' is missing in the configuration.")]
        public int MACD_Medium_SlowPeriod { get; set; }

        /// <summary>
        /// Gets or sets the signal period for medium-term MACD calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'MACD_Medium_SignalPeriod' is missing in the configuration.")]
        public int MACD_Medium_SignalPeriod { get; set; }

        /// <summary>
        /// Gets or sets the fast period for long-term MACD calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'MACD_Long_FastPeriod' is missing in the configuration.")]
        public int MACD_Long_FastPeriod { get; set; }

        /// <summary>
        /// Gets or sets the slow period for long-term MACD calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'MACD_Long_SlowPeriod' is missing in the configuration.")]
        public int MACD_Long_SlowPeriod { get; set; }

        /// <summary>
        /// Gets or sets the signal period for long-term MACD calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'MACD_Long_SignalPeriod' is missing in the configuration.")]
        public int MACD_Long_SignalPeriod { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for medium-term EMA calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'EMA_Medium_Periods' is missing in the configuration.")]
        public int EMA_Medium_Periods { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for long-term EMA calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'EMA_Long_Periods' is missing in the configuration.")]
        public int EMA_Long_Periods { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for medium-term Bollinger Bands calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'BollingerBands_Medium_Periods' is missing in the configuration.")]
        public int BollingerBands_Medium_Periods { get; set; }

        /// <summary>
        /// Gets or sets the standard deviation multiplier for medium-term Bollinger Bands.
        /// </summary>
        [Required(ErrorMessage = "The 'BollingerBands_Medium_StdDev' is missing in the configuration.")]
        public double BollingerBands_Medium_StdDev { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for long-term Bollinger Bands calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'BollingerBands_Long_Periods' is missing in the configuration.")]
        public int BollingerBands_Long_Periods { get; set; }

        /// <summary>
        /// Gets or sets the standard deviation multiplier for long-term Bollinger Bands.
        /// </summary>
        [Required(ErrorMessage = "The 'BollingerBands_Long_StdDev' is missing in the configuration.")]
        public double BollingerBands_Long_StdDev { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for medium-term ATR calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'ATR_Medium_Periods' is missing in the configuration.")]
        public int ATR_Medium_Periods { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for long-term ATR calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'ATR_Long_Periods' is missing in the configuration.")]
        public int ATR_Long_Periods { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for short-term VWAP calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'VWAP_Short_Periods' is missing in the configuration.")]
        public int VWAP_Short_Periods { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for medium-term VWAP calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'VWAP_Medium_Periods' is missing in the configuration.")]
        public int VWAP_Medium_Periods { get; set; }

        /// <summary>
        /// Gets or sets the K period for short-term Stochastic Oscillator calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'Stochastic_Short_Periods' is missing in the configuration.")]
        public int Stochastic_Short_Periods { get; set; }

        /// <summary>
        /// Gets or sets the D period for short-term Stochastic Oscillator calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'Stochastic_Short_DPeriods' is missing in the configuration.")]
        public int Stochastic_Short_DPeriods { get; set; }

        /// <summary>
        /// Gets or sets the K period for medium-term Stochastic Oscillator calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'Stochastic_Medium_Periods' is missing in the configuration.")]
        public int Stochastic_Medium_Periods { get; set; }

        /// <summary>
        /// Gets or sets the D period for medium-term Stochastic Oscillator calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'Stochastic_Medium_DPeriods' is missing in the configuration.")]
        public int Stochastic_Medium_DPeriods { get; set; }

        /// <summary>
        /// Gets or sets the K period for long-term Stochastic Oscillator calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'Stochastic_Long_Periods' is missing in the configuration.")]
        public int Stochastic_Long_Periods { get; set; }

        /// <summary>
        /// Gets or sets the D period for long-term Stochastic Oscillator calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'Stochastic_Long_DPeriods' is missing in the configuration.")]
        public int Stochastic_Long_DPeriods { get; set; }

        /// <summary>
        /// Gets or sets the trading fee rate used in profit/loss calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'TradingFeeRate' is missing in the configuration.")]
        public double TradingFeeRate { get; set; }

        /// <summary>
        /// Gets or sets the number of lookback periods for pseudo-candlestick calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'PseudoCandlestickLookbackPeriods' is missing in the configuration.")]
        public int PseudoCandlestickLookbackPeriods { get; set; }

        /// <summary>
        /// Gets or sets the number of recent candlesticks to include in calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'RecentCandlesticksCount' is missing in the configuration.")]
        public int RecentCandlesticksCount { get; set; }

        /// <summary>
        /// Gets or sets the initial acceleration factor for Parabolic SAR calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'PSAR_InitialAF' is missing in the configuration.")]
        public double PSAR_InitialAF { get; set; }

        /// <summary>
        /// Gets or sets the maximum acceleration factor for Parabolic SAR calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'PSAR_MaxAF' is missing in the configuration.")]
        public double PSAR_MaxAF { get; set; }

        /// <summary>
        /// Gets or sets the acceleration factor step for Parabolic SAR calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'PSAR_AFStep' is missing in the configuration.")]
        public double PSAR_AFStep { get; set; }

        /// <summary>
        /// Gets or sets the number of periods for ADX calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'ADX_Periods' is missing in the configuration.")]
        public int ADX_Periods { get; set; }

        /// <summary>
        /// Gets or sets the exponential multiplier for resistance level calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'ResistanceLevels_ExponentialMultiplier' is missing in the configuration.")]
        public double ResistanceLevels_ExponentialMultiplier { get; set; }

        /// <summary>
        /// Gets or sets the minimum candlestick percentage for resistance level detection.
        /// </summary>
        [Required(ErrorMessage = "The 'ResistanceLevels_MinCandlestickPercentage' is missing in the configuration.")]
        public double ResistanceLevels_MinCandlestickPercentage { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of resistance levels to calculate.
        /// </summary>
        [Required(ErrorMessage = "The 'ResistanceLevels_MaxLevels' is missing in the configuration.")]
        public int ResistanceLevels_MaxLevels { get; set; }

        /// <summary>
        /// Gets or sets the sigma value for resistance level statistical calculations.
        /// </summary>
        [Required(ErrorMessage = "The 'ResistanceLevels_Sigma' is missing in the configuration.")]
        public double ResistanceLevels_Sigma { get; set; }

        /// <summary>
        /// Gets or sets the minimum distance between resistance levels.
        /// </summary>
        [Required(ErrorMessage = "The 'ResistanceLevels_MinDistance' is missing in the configuration.")]
        public int ResistanceLevels_MinDistance { get; set; }
    }
}
