using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration class containing all technical indicator parameters used throughout the trading bot.
    /// These settings control the calculation periods and thresholds for various technical analysis indicators
    /// that drive trading decisions and market analysis. Values are injected via dependency injection
    /// and used by MarketData and TradingCalculator classes for real-time indicator computation.
    /// Includes comprehensive data validation with Range attributes and IValidatableObject implementation
    /// to prevent invalid parameter combinations and catch misconfigurations early during application startup.
    /// Validation ensures logical relationships (e.g., fast periods < slow periods) and valid ranges.
    /// </summary>
    public class CalculationConfig : IValidatableObject
    {
        /// <summary>
        /// Number of periods to use for short-term RSI (Relative Strength Index) calculations.
        /// RSI measures price momentum on a scale of 0-100, commonly used for overbought/oversold signals.
        /// Shorter periods (e.g., 14) provide more responsive but potentially noisier signals.
        /// Validation ensures: 1 &le; Short &lt; Medium &lt; Long.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "RSI_Short_Periods must be greater than 0.")]
        public int RSI_Short_Periods { get; set; }

        /// <summary>
        /// Number of periods to use for medium-term RSI calculations.
        /// Used for intermediate trend analysis, balancing responsiveness with signal reliability.
        /// Validation ensures: 1 &le; Short &lt; Medium &lt; Long.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "RSI_Medium_Periods must be greater than 0.")]
        public int RSI_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods to use for long-term RSI calculations.
        /// Provides broader trend context, less sensitive to short-term price fluctuations.
        /// Validation ensures: 1 &le; Short &lt; Medium &lt; Long.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "RSI_Long_Periods must be greater than 0.")]
        public int RSI_Long_Periods { get; set; }

        /// <summary>
        /// Fast EMA period for medium-term MACD (Moving Average Convergence Divergence) calculations.
        /// MACD shows the relationship between two moving averages of price, used for momentum analysis.
        /// The fast period must be shorter than the slow period for proper convergence/divergence signals.
        /// Validation ensures: 1 &le; FastPeriod &lt; SlowPeriod.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "MACD_Medium_FastPeriod must be greater than 0.")]
        public int MACD_Medium_FastPeriod { get; set; }

        /// <summary>
        /// Slow EMA period for medium-term MACD calculations.
        /// Longer period provides the baseline trend against which the fast period converges/diverges.
        /// Validation ensures: 1 &le; SlowPeriod, and FastPeriod &lt; SlowPeriod.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "MACD_Medium_SlowPeriod must be greater than 0.")]
        public int MACD_Medium_SlowPeriod { get; set; }

        /// <summary>
        /// Signal line EMA period for medium-term MACD calculations.
        /// Applied to the MACD line to generate trading signals when crossed.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "MACD_Medium_SignalPeriod must be greater than 0.")]
        public int MACD_Medium_SignalPeriod { get; set; }

        /// <summary>
        /// Fast EMA period for long-term MACD calculations.
        /// Used for broader market trend analysis over extended timeframes.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "MACD_Long_FastPeriod must be greater than 0.")]
        public int MACD_Long_FastPeriod { get; set; }

        /// <summary>
        /// Slow EMA period for long-term MACD calculations.
        /// Provides long-term trend baseline for extended analysis periods.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "MACD_Long_SlowPeriod must be greater than 0.")]
        public int MACD_Long_SlowPeriod { get; set; }

        /// <summary>
        /// Signal line EMA period for long-term MACD calculations.
        /// Generates signals for longer-term trading strategies.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "MACD_Long_SignalPeriod must be greater than 0.")]
        public int MACD_Long_SignalPeriod { get; set; }

        /// <summary>
        /// Number of periods for medium-term EMA (Exponential Moving Average) calculations.
        /// EMA gives more weight to recent prices, used for trend identification and support/resistance levels.
        /// Medium period balances short-term responsiveness with trend stability.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "EMA_Medium_Periods must be greater than 0.")]
        public int EMA_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods for long-term EMA calculations.
        /// Used for major trend analysis and long-term market direction assessment.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "EMA_Long_Periods must be greater than 0.")]
        public int EMA_Long_Periods { get; set; }

        /// <summary>
        /// Number of periods for medium-term Bollinger Bands calculations.
        /// Bollinger Bands plot standard deviation bands around a moving average to identify volatility and price levels.
        /// Medium period provides intermediate volatility analysis.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "BollingerBands_Medium_Periods must be greater than 0.")]
        public int BollingerBands_Medium_Periods { get; set; }

        /// <summary>
        /// Standard deviation multiplier for medium-term Bollinger Bands.
        /// Controls band width - higher values create wider bands, lower values create tighter bands.
        /// Typically 2.0 for standard Bollinger Bands.
        /// Validation ensures: StdDev &ge; 0.1.
        /// </summary>
        [Range(0.1, double.MaxValue, ErrorMessage = "BollingerBands_Medium_StdDev must be greater than 0.")]
        public double BollingerBands_Medium_StdDev { get; set; }

        /// <summary>
        /// Number of periods for long-term Bollinger Bands calculations.
        /// Used for extended volatility analysis and major price level identification.
        /// Validation ensures: 1 &le; Long_Periods, and Medium_Periods &lt; Long_Periods.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "BollingerBands_Long_Periods must be greater than 0.")]
        public int BollingerBands_Long_Periods { get; set; }

        /// <summary>
        /// Standard deviation multiplier for long-term Bollinger Bands.
        /// Configures band sensitivity for long-term trend analysis.
        /// Validation ensures: StdDev &ge; 0.1.
        /// </summary>
        [Range(0.1, double.MaxValue, ErrorMessage = "BollingerBands_Long_StdDev must be greater than 0.")]
        public double BollingerBands_Long_StdDev { get; set; }

        /// <summary>
        /// Number of periods for short-term VWAP (Volume Weighted Average Price) calculations.
        /// VWAP calculates the average price weighted by volume, used as dynamic support/resistance.
        /// Short period provides more responsive VWAP levels for intraday trading.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "VWAP_Short_Periods must be greater than 0.")]
        public int VWAP_Short_Periods { get; set; }

        /// <summary>
        /// Number of periods for medium-term VWAP calculations.
        /// Balances responsiveness with trend stability for intermediate analysis.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "VWAP_Medium_Periods must be greater than 0.")]
        public int VWAP_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods for medium-term ATR (Average True Range) calculations.
        /// ATR measures volatility by calculating the average range of price movement.
        /// Used for position sizing, stop loss placement, and volatility assessment.
        /// Medium period provides intermediate volatility context.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ATR_Medium_Periods must be greater than 0.")]
        public int ATR_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods for long-term ATR calculations.
        /// Used for broader volatility trends and long-term risk assessment.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ATR_Long_Periods must be greater than 0.")]
        public int ATR_Long_Periods { get; set; }

        /// <summary>
        /// K period (lookback) for short-term Stochastic Oscillator calculations.
        /// Stochastic Oscillator compares closing price to price range over a period, indicating momentum.
        /// Short period provides responsive overbought/oversold signals.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Stochastic_Short_Periods must be greater than 0.")]
        public int Stochastic_Short_Periods { get; set; }

        /// <summary>
        /// D period (smoothing) for short-term Stochastic Oscillator %D line.
        /// Applied as moving average to %K line to reduce noise and generate signals.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Stochastic_Short_DPeriods must be greater than 0.")]
        public int Stochastic_Short_DPeriods { get; set; }

        /// <summary>
        /// K period for medium-term Stochastic Oscillator calculations.
        /// Provides intermediate momentum analysis with balanced responsiveness.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Stochastic_Medium_Periods must be greater than 0.")]
        public int Stochastic_Medium_Periods { get; set; }

        /// <summary>
        /// D period for medium-term Stochastic Oscillator calculations.
        /// Smooths the medium-term %K line for more reliable signals.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Stochastic_Medium_DPeriods must be greater than 0.")]
        public int Stochastic_Medium_DPeriods { get; set; }

        /// <summary>
        /// K period for long-term Stochastic Oscillator calculations.
        /// Used for extended trend momentum analysis.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Stochastic_Long_Periods must be greater than 0.")]
        public int Stochastic_Long_Periods { get; set; }

        /// <summary>
        /// D period for long-term Stochastic Oscillator calculations.
        /// Provides smoothing for long-term momentum signals.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Stochastic_Long_DPeriods must be greater than 0.")]
        public int Stochastic_Long_DPeriods { get; set; }

        /// <summary>
        /// Minimum percentage of candlesticks required for resistance/support level identification.
        /// Filters out levels that don't represent significant price action.
        /// Expressed as decimal (e.g., 0.05 for 5%).
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "ResistanceLevels_MinCandlestickPercentage must be between 0 and 1.")]
        public double ResistanceLevels_MinCandlestickPercentage { get; set; }

        /// <summary>
        /// Maximum number of resistance/support levels to identify.
        /// Limits computational complexity and focuses on most significant levels.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ResistanceLevels_MaxLevels must be greater than 0.")]
        public int ResistanceLevels_MaxLevels { get; set; }

        /// <summary>
        /// Sigma threshold for resistance/support level statistical significance.
        /// Higher values require stronger statistical evidence for level identification.
        /// </summary>
        [Range(0.1, double.MaxValue, ErrorMessage = "ResistanceLevels_Sigma must be greater than 0.")]
        public double ResistanceLevels_Sigma { get; set; }

        /// <summary>
        /// Minimum distance (in price points) between identified resistance/support levels.
        /// Prevents clustering of levels in narrow price ranges.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ResistanceLevels_MinDistance must be greater than 0.")]
        public int ResistanceLevels_MinDistance { get; set; }

        /// <summary>
        /// Number of periods for ADX (Average Directional Index) calculations.
        /// ADX measures trend strength on a scale of 0-100, used to identify trending vs. ranging markets.
        /// Longer periods provide more stable trend strength readings.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "ADX_Periods must be greater than 0.")]
        public int ADX_Periods { get; set; }

        /// <summary>
        /// Initial acceleration factor for PSAR (Parabolic Stop and Reverse) calculations.
        /// Controls the starting sensitivity of the PSAR indicator to price movements.
        /// Typical value is 0.02.
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "PSAR_InitialAF must be greater than 0.")]
        public double PSAR_InitialAF { get; set; }

        /// <summary>
        /// Maximum acceleration factor for PSAR calculations.
        /// Limits the maximum sensitivity of the PSAR indicator.
        /// Typical value is 0.2.
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "PSAR_MaxAF must be greater than 0.")]
        public double PSAR_MaxAF { get; set; }

        /// <summary>
        /// Acceleration factor step increment for PSAR calculations.
        /// Determines how much the acceleration factor increases on each favorable move.
        /// Typical value is 0.02.
        /// </summary>
        [Range(0.001, double.MaxValue, ErrorMessage = "PSAR_AFStep must be greater than 0.")]
        public double PSAR_AFStep { get; set; }

        /// <summary>
        /// Exponential multiplier for resistance levels calculations.
        /// Controls the weighting decay in time-based exponential smoothing for support/resistance analysis.
        /// Typical value is 2.0.
        /// </summary>
        [Range(0.1, double.MaxValue, ErrorMessage = "ResistanceLevels_ExponentialMultiplier must be greater than 0.")]
        public double ResistanceLevels_ExponentialMultiplier { get; set; }

        /// <summary>
        /// Tolerance percentage used for depth calculations within a price range.
        /// Determines the percentage range around the best bid to include in depth calculations.
        /// Expressed as percentage (e.g., 10.0 for 10%).
        /// Validation ensures: 0.1 &le; TolerancePercentage &le; 100.0.
        /// </summary>
        [Range(0.1, 100.0, ErrorMessage = "TolerancePercentage must be between 0.1 and 100.")]
        public double TolerancePercentage { get; set; } = 10.0;

        /// <summary>
        /// Trading fee rate applied to calculate expected fees.
        /// The fee is calculated as: roundup(fee_rate * contracts * price * (1 - price)).
        /// Expressed as decimal (e.g., 0.07 for 7%).
        /// Validation ensures: 0.0 &le; TradingFeeRate &le; 1.0.
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "TradingFeeRate must be between 0 and 1.")]
        public double TradingFeeRate { get; set; } = 0.07;

        /// <summary>
        /// Number of minutes for short-term slope calculations in price movement analysis.
        /// Used to calculate bid slope over a short timeframe for responsive signals.
        /// Validation ensures: 1 &le; ShortMinutes &lt; MediumMinutes.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "SlopeShortMinutes must be greater than 0.")]
        public int SlopeShortMinutes { get; set; } = 5;

        /// <summary>
        /// Number of minutes for medium-term slope calculations in price movement analysis.
        /// Used to calculate bid slope over a medium timeframe for balanced signals.
        /// Validation ensures: 1 &le; MediumMinutes, and ShortMinutes &lt; MediumMinutes.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "SlopeMediumMinutes must be greater than 0.")]
        public int SlopeMediumMinutes { get; set; } = 15;

        /// <summary>
        /// Number of days to look back for recent candlestick metadata.
        /// Determines the timeframe for identifying recent high/low bid levels.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "RecentCandlestickDays must be greater than 0.")]
        public int RecentCandlestickDays { get; set; } = 1;

        /// <summary>
        /// Default number of lookback periods for building pseudo candlesticks.
        /// Controls how many historical periods are included in pseudo candlestick generation.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "PseudoCandlestickLookbackPeriods must be greater than 0.")]
        public int PseudoCandlestickLookbackPeriods { get; set; } = 34;

        /// <summary>
        /// Number of recent candlesticks to retain for analysis and display.
        /// Controls how many of the most recent minute-level pseudo candlesticks are kept in memory.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "RecentCandlesticksCount must be greater than 0.")]
        public int RecentCandlesticksCount { get; set; } = 15;

        /// <summary>
        /// Validates the configuration parameters to ensure they are within valid ranges and logical constraints.
        /// This method is called during dependency injection to catch misconfigurations early.
        /// For performance, validation is only performed once on startup, not during runtime indicator calculations.
        /// </summary>
        /// <param name="validationContext">The validation context.</param>
        /// <returns>A collection of validation results.</returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // MACD validations
            if (MACD_Medium_FastPeriod >= MACD_Medium_SlowPeriod)
            {
                results.Add(new ValidationResult("MACD_Medium_FastPeriod must be less than MACD_Medium_SlowPeriod.", new[] { nameof(MACD_Medium_FastPeriod), nameof(MACD_Medium_SlowPeriod) }));
            }
            if (MACD_Long_FastPeriod >= MACD_Long_SlowPeriod)
            {
                results.Add(new ValidationResult("MACD_Long_FastPeriod must be less than MACD_Long_SlowPeriod.", new[] { nameof(MACD_Long_FastPeriod), nameof(MACD_Long_SlowPeriod) }));
            }

            // RSI period validations
            if (RSI_Short_Periods >= RSI_Medium_Periods)
            {
                results.Add(new ValidationResult("RSI_Short_Periods must be less than RSI_Medium_Periods.", new[] { nameof(RSI_Short_Periods), nameof(RSI_Medium_Periods) }));
            }
            if (RSI_Medium_Periods >= RSI_Long_Periods)
            {
                results.Add(new ValidationResult("RSI_Medium_Periods must be less than RSI_Long_Periods.", new[] { nameof(RSI_Medium_Periods), nameof(RSI_Long_Periods) }));
            }

            // EMA period validations
            if (EMA_Medium_Periods >= EMA_Long_Periods)
            {
                results.Add(new ValidationResult("EMA_Medium_Periods must be less than EMA_Long_Periods.", new[] { nameof(EMA_Medium_Periods), nameof(EMA_Long_Periods) }));
            }

            // Bollinger Bands period validations
            if (BollingerBands_Medium_Periods >= BollingerBands_Long_Periods)
            {
                results.Add(new ValidationResult("BollingerBands_Medium_Periods must be less than BollingerBands_Long_Periods.", new[] { nameof(BollingerBands_Medium_Periods), nameof(BollingerBands_Long_Periods) }));
            }

            // VWAP period validations
            if (VWAP_Short_Periods >= VWAP_Medium_Periods)
            {
                results.Add(new ValidationResult("VWAP_Short_Periods must be less than VWAP_Medium_Periods.", new[] { nameof(VWAP_Short_Periods), nameof(VWAP_Medium_Periods) }));
            }

            // ATR period validations
            if (ATR_Medium_Periods >= ATR_Long_Periods)
            {
                results.Add(new ValidationResult("ATR_Medium_Periods must be less than ATR_Long_Periods.", new[] { nameof(ATR_Medium_Periods), nameof(ATR_Long_Periods) }));
            }

            // Stochastic validations
            if (Stochastic_Short_Periods >= Stochastic_Medium_Periods)
            {
                results.Add(new ValidationResult("Stochastic_Short_Periods must be less than Stochastic_Medium_Periods.", new[] { nameof(Stochastic_Short_Periods), nameof(Stochastic_Medium_Periods) }));
            }
            if (Stochastic_Medium_Periods >= Stochastic_Long_Periods)
            {
                results.Add(new ValidationResult("Stochastic_Medium_Periods must be less than Stochastic_Long_Periods.", new[] { nameof(Stochastic_Medium_Periods), nameof(Stochastic_Long_Periods) }));
            }
            if (Stochastic_Short_DPeriods >= Stochastic_Short_Periods)
            {
                results.Add(new ValidationResult("Stochastic_Short_DPeriods must be less than Stochastic_Short_Periods.", new[] { nameof(Stochastic_Short_DPeriods), nameof(Stochastic_Short_Periods) }));
            }
            if (Stochastic_Medium_DPeriods >= Stochastic_Medium_Periods)
            {
                results.Add(new ValidationResult("Stochastic_Medium_DPeriods must be less than Stochastic_Medium_Periods.", new[] { nameof(Stochastic_Medium_DPeriods), nameof(Stochastic_Medium_Periods) }));
            }
            if (Stochastic_Long_DPeriods >= Stochastic_Long_Periods)
            {
                results.Add(new ValidationResult("Stochastic_Long_DPeriods must be less than Stochastic_Long_Periods.", new[] { nameof(Stochastic_Long_DPeriods), nameof(Stochastic_Long_Periods) }));
            }

            // PSAR validations
            if (PSAR_InitialAF <= 0)
            {
                results.Add(new ValidationResult("PSAR_InitialAF must be greater than 0.", new[] { nameof(PSAR_InitialAF) }));
            }
            if (PSAR_MaxAF <= PSAR_InitialAF)
            {
                results.Add(new ValidationResult("PSAR_MaxAF must be greater than PSAR_InitialAF.", new[] { nameof(PSAR_MaxAF), nameof(PSAR_InitialAF) }));
            }
            if (PSAR_AFStep <= 0)
            {
                results.Add(new ValidationResult("PSAR_AFStep must be greater than 0.", new[] { nameof(PSAR_AFStep) }));
            }

            // Slope validations
            if (SlopeShortMinutes >= SlopeMediumMinutes)
            {
                results.Add(new ValidationResult("SlopeShortMinutes must be less than SlopeMediumMinutes.", new[] { nameof(SlopeShortMinutes), nameof(SlopeMediumMinutes) }));
            }

            // Resistance levels validations
            if (ResistanceLevels_MinCandlestickPercentage <= 0 || ResistanceLevels_MinCandlestickPercentage > 1)
            {
                results.Add(new ValidationResult("ResistanceLevels_MinCandlestickPercentage must be between 0 and 1.", new[] { nameof(ResistanceLevels_MinCandlestickPercentage) }));
            }
            if (ResistanceLevels_MaxLevels <= 0)
            {
                results.Add(new ValidationResult("ResistanceLevels_MaxLevels must be greater than 0.", new[] { nameof(ResistanceLevels_MaxLevels) }));
            }
            if (ResistanceLevels_Sigma <= 0)
            {
                results.Add(new ValidationResult("ResistanceLevels_Sigma must be greater than 0.", new[] { nameof(ResistanceLevels_Sigma) }));
            }
            if (ResistanceLevels_MinDistance <= 0)
            {
                results.Add(new ValidationResult("ResistanceLevels_MinDistance must be greater than 0.", new[] { nameof(ResistanceLevels_MinDistance) }));
            }

            return results;
        }
    }
}
