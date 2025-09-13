namespace TradingStrategies.Configuration
{
    /// <summary>
    /// Configuration class containing all technical indicator parameters used throughout the trading bot.
    /// These settings control the calculation periods and thresholds for various technical analysis indicators
    /// that drive trading decisions and market analysis. Values are injected via dependency injection
    /// and used by MarketData and TradingCalculator classes for real-time indicator computation.
    /// </summary>
    public class CalculationConfig
    {
        /// <summary>
        /// Number of periods to use for short-term RSI (Relative Strength Index) calculations.
        /// RSI measures price momentum on a scale of 0-100, commonly used for overbought/oversold signals.
        /// Shorter periods (e.g., 14) provide more responsive but potentially noisier signals.
        /// </summary>
        public int RSI_Short_Periods { get; set; }

        /// <summary>
        /// Number of periods to use for medium-term RSI calculations.
        /// Used for intermediate trend analysis, balancing responsiveness with signal reliability.
        /// </summary>
        public int RSI_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods to use for long-term RSI calculations.
        /// Provides broader trend context, less sensitive to short-term price fluctuations.
        /// </summary>
        public int RSI_Long_Periods { get; set; }

        /// <summary>
        /// Fast EMA period for medium-term MACD (Moving Average Convergence Divergence) calculations.
        /// MACD shows the relationship between two moving averages of price, used for momentum analysis.
        /// The fast period should be shorter than the slow period for proper convergence/divergence signals.
        /// </summary>
        public int MACD_Medium_FastPeriod { get; set; }

        /// <summary>
        /// Slow EMA period for medium-term MACD calculations.
        /// Longer period provides the baseline trend against which the fast period converges/diverges.
        /// </summary>
        public int MACD_Medium_SlowPeriod { get; set; }

        /// <summary>
        /// Signal line EMA period for medium-term MACD calculations.
        /// Applied to the MACD line to generate trading signals when crossed.
        /// </summary>
        public int MACD_Medium_SignalPeriod { get; set; }

        /// <summary>
        /// Fast EMA period for long-term MACD calculations.
        /// Used for broader market trend analysis over extended timeframes.
        /// </summary>
        public int MACD_Long_FastPeriod { get; set; }

        /// <summary>
        /// Slow EMA period for long-term MACD calculations.
        /// Provides long-term trend baseline for extended analysis periods.
        /// </summary>
        public int MACD_Long_SlowPeriod { get; set; }

        /// <summary>
        /// Signal line EMA period for long-term MACD calculations.
        /// Generates signals for longer-term trading strategies.
        /// </summary>
        public int MACD_Long_SignalPeriod { get; set; }

        /// <summary>
        /// Number of periods for medium-term EMA (Exponential Moving Average) calculations.
        /// EMA gives more weight to recent prices, used for trend identification and support/resistance levels.
        /// Medium period balances short-term responsiveness with trend stability.
        /// </summary>
        public int EMA_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods for long-term EMA calculations.
        /// Used for major trend analysis and long-term market direction assessment.
        /// </summary>
        public int EMA_Long_Periods { get; set; }

        /// <summary>
        /// Number of periods for medium-term Bollinger Bands calculations.
        /// Bollinger Bands plot standard deviation bands around a moving average to identify volatility and price levels.
        /// Medium period provides intermediate volatility analysis.
        /// </summary>
        public int BollingerBands_Medium_Periods { get; set; }

        /// <summary>
        /// Standard deviation multiplier for medium-term Bollinger Bands.
        /// Controls band width - higher values create wider bands, lower values create tighter bands.
        /// Typically 2.0 for standard Bollinger Bands.
        /// </summary>
        public int BollingerBands_Medium_StdDev { get; set; }

        /// <summary>
        /// Number of periods for long-term Bollinger Bands calculations.
        /// Used for extended volatility analysis and major price level identification.
        /// </summary>
        public int BollingerBands_Long_Periods { get; set; }

        /// <summary>
        /// Standard deviation multiplier for long-term Bollinger Bands.
        /// Configures band sensitivity for long-term trend analysis.
        /// </summary>
        public int BollingerBands_Long_StdDev { get; set; }

        /// <summary>
        /// Number of periods for short-term VWAP (Volume Weighted Average Price) calculations.
        /// VWAP calculates the average price weighted by volume, used as dynamic support/resistance.
        /// Short period provides more responsive VWAP levels for intraday trading.
        /// </summary>
        public int VWAP_Short_Periods { get; set; }

        /// <summary>
        /// Number of periods for medium-term VWAP calculations.
        /// Balances responsiveness with trend stability for intermediate analysis.
        /// </summary>
        public int VWAP_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods for medium-term ATR (Average True Range) calculations.
        /// ATR measures volatility by calculating the average range of price movement.
        /// Used for position sizing, stop loss placement, and volatility assessment.
        /// Medium period provides intermediate volatility context.
        /// </summary>
        public int ATR_Medium_Periods { get; set; }

        /// <summary>
        /// Number of periods for long-term ATR calculations.
        /// Used for broader volatility trends and long-term risk assessment.
        /// </summary>
        public int ATR_Long_Periods { get; set; }

        /// <summary>
        /// K period (lookback) for short-term Stochastic Oscillator calculations.
        /// Stochastic Oscillator compares closing price to price range over a period, indicating momentum.
        /// Short period provides responsive overbought/oversold signals.
        /// </summary>
        public int Stochastic_Short_Periods { get; set; }

        /// <summary>
        /// D period (smoothing) for short-term Stochastic Oscillator %D line.
        /// Applied as moving average to %K line to reduce noise and generate signals.
        /// </summary>
        public int Stochastic_Short_DPeriods { get; set; }

        /// <summary>
        /// K period for medium-term Stochastic Oscillator calculations.
        /// Provides intermediate momentum analysis with balanced responsiveness.
        /// </summary>
        public int Stochastic_Medium_Periods { get; set; }

        /// <summary>
        /// D period for medium-term Stochastic Oscillator calculations.
        /// Smooths the medium-term %K line for more reliable signals.
        /// </summary>
        public int Stochastic_Medium_DPeriods { get; set; }

        /// <summary>
        /// K period for long-term Stochastic Oscillator calculations.
        /// Used for extended trend momentum analysis.
        /// </summary>
        public int Stochastic_Long_Periods { get; set; }

        /// <summary>
        /// D period for long-term Stochastic Oscillator calculations.
        /// Provides smoothing for long-term momentum signals.
        /// </summary>
        public int Stochastic_Long_DPeriods { get; set; }

        /// <summary>
        /// Minimum percentage of candlesticks required for resistance/support level identification.
        /// Filters out levels that don't represent significant price action.
        /// Expressed as decimal (e.g., 0.05 for 5%).
        /// </summary>
        public double ResistanceLevels_MinCandlestickPercentage { get; set; }

        /// <summary>
        /// Maximum number of resistance/support levels to identify.
        /// Limits computational complexity and focuses on most significant levels.
        /// </summary>
        public int ResistanceLevels_MaxLevels { get; set; }

        /// <summary>
        /// Sigma threshold for resistance/support level statistical significance.
        /// Higher values require stronger statistical evidence for level identification.
        /// </summary>
        public double ResistanceLevels_Sigma { get; set; }

        /// <summary>
        /// Minimum distance (in price points) between identified resistance/support levels.
        /// Prevents clustering of levels in narrow price ranges.
        /// </summary>
        public int ResistanceLevels_MinDistance { get; set; }

        /// <summary>
        /// Number of periods for ADX (Average Directional Index) calculations.
        /// ADX measures trend strength on a scale of 0-100, used to identify trending vs. ranging markets.
        /// Longer periods provide more stable trend strength readings.
        /// </summary>
        public int ADX_Periods { get; set; }

        /// <summary>
        /// Initial acceleration factor for PSAR (Parabolic Stop and Reverse) calculations.
        /// Controls the starting sensitivity of the PSAR indicator to price movements.
        /// Typical value is 0.02.
        /// </summary>
        public double PSAR_InitialAF { get; set; }

        /// <summary>
        /// Maximum acceleration factor for PSAR calculations.
        /// Limits the maximum sensitivity of the PSAR indicator.
        /// Typical value is 0.2.
        /// </summary>
        public double PSAR_MaxAF { get; set; }

        /// <summary>
        /// Acceleration factor step increment for PSAR calculations.
        /// Determines how much the acceleration factor increases on each favorable move.
        /// Typical value is 0.02.
        /// </summary>
        public double PSAR_AFStep { get; set; }

        /// <summary>
        /// Exponential multiplier for resistance levels calculations.
        /// Controls the weighting decay in time-based exponential smoothing for support/resistance analysis.
        /// Typical value is 2.0.
        /// </summary>
        public double ResistanceLevels_ExponentialMultiplier { get; set; }

        /// <summary>
        /// Tolerance percentage used for depth calculations within a price range.
        /// Determines the percentage range around the best bid to include in depth calculations.
        /// Expressed as percentage (e.g., 10.0 for 10%).
        /// </summary>
        public double TolerancePercentage { get; set; } = 10.0;

        /// <summary>
        /// Trading fee rate applied to calculate expected fees.
        /// The fee is calculated as: roundup(fee_rate * contracts * price * (1 - price)).
        /// Expressed as decimal (e.g., 0.07 for 7%).
        /// </summary>
        public double TradingFeeRate { get; set; } = 0.07;

        /// <summary>
        /// Number of minutes for short-term slope calculations in price movement analysis.
        /// Used to calculate bid slope over a short timeframe for responsive signals.
        /// </summary>
        public int SlopeShortMinutes { get; set; } = 5;

        /// <summary>
        /// Number of minutes for medium-term slope calculations in price movement analysis.
        /// Used to calculate bid slope over a medium timeframe for balanced signals.
        /// </summary>
        public int SlopeMediumMinutes { get; set; } = 15;

        /// <summary>
        /// Number of days to look back for recent candlestick metadata.
        /// Determines the timeframe for identifying recent high/low bid levels.
        /// </summary>
        public int RecentCandlestickDays { get; set; } = 1;

        /// <summary>
        /// Default number of lookback periods for building pseudo candlesticks.
        /// Controls how many historical periods are included in pseudo candlestick generation.
        /// </summary>
        public int PseudoCandlestickLookbackPeriods { get; set; } = 34;

        /// <summary>
        /// Number of recent candlesticks to retain for analysis and display.
        /// Controls how many of the most recent minute-level pseudo candlesticks are kept in memory.
        /// </summary>
        public int RecentCandlesticksCount { get; set; } = 15;
    }
}
