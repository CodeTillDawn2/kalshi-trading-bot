using BacklashDTOs;

namespace TradingStrategies.Helpers.Interfaces
{
    /// <summary>
    /// Defines the contract for a trading calculator that performs various technical analysis
    /// calculations including indicators, oscillators, and support/resistance levels.
    /// </summary>
    public interface ITradingCalculator
    {
        /// <summary>
        /// Calculates the Relative Strength Index (RSI) for the given pseudo candlesticks.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate RSI for.</param>
        /// <param name="periods">The number of periods to use for the RSI calculation.</param>
        /// <returns>The RSI value, or null if calculation is not possible.</returns>
        double? CalculateRSI(List<PseudoCandlestick> pseudoCandles, int periods);

        /// <summary>
        /// Calculates the Moving Average Convergence Divergence (MACD) indicator.
        /// </summary>
        /// <param name="pseudoCandlesticks">The list of pseudo candlesticks to calculate MACD for.</param>
        /// <param name="shortPeriod">The short period for the MACD calculation.</param>
        /// <param name="longPeriod">The long period for the MACD calculation.</param>
        /// <param name="signalPeriod">The signal period for the MACD calculation.</param>
        /// <returns>A tuple containing MACD, Signal, and Histogram values.</returns>
        (double? MACD, double? Signal, double? Histogram) CalculateMACD(List<PseudoCandlestick> pseudoCandlesticks, int shortPeriod, int longPeriod, int signalPeriod);

        /// <summary>
        /// Calculates Bollinger Bands for the given pseudo candlesticks.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate Bollinger Bands for.</param>
        /// <param name="period">The period for the moving average calculation.</param>
        /// <param name="stdDevMultiplier">The standard deviation multiplier for the bands.</param>
        /// <returns>A tuple containing Lower, Middle, and Upper band values.</returns>
        (double? Lower, double? Middle, double? Upper) CalculateBollingerBands(List<PseudoCandlestick> pseudoCandles, int period, double stdDevMultiplier);

        /// <summary>
        /// Calculates the Stochastic Oscillator for the given pseudo candlesticks.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate Stochastic for.</param>
        /// <param name="kPeriod">The K period for the Stochastic calculation.</param>
        /// <param name="dPeriod">The D period for the Stochastic calculation.</param>
        /// <returns>A tuple containing K and D values.</returns>
        (double? K, double? D) CalculateStochastic(List<PseudoCandlestick> pseudoCandles, int kPeriod, int dPeriod);

        /// <summary>
        /// Calculates the Average True Range (ATR) for the given pseudo candlesticks.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate ATR for.</param>
        /// <param name="period">The period for the ATR calculation.</param>
        /// <returns>The ATR value, or null if calculation is not possible.</returns>
        double? CalculateATR(List<PseudoCandlestick> pseudoCandles, int period);

        /// <summary>
        /// Calculates the Volume Weighted Average Price (VWAP) for the given pseudo candlesticks.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate VWAP for.</param>
        /// <param name="periods">The number of periods to use for the VWAP calculation.</param>
        /// <returns>The VWAP value, or null if calculation is not possible.</returns>
        decimal? CalculateVWAP(List<PseudoCandlestick> pseudoCandles, int periods);

        /// <summary>
        /// Calculates the On Balance Volume (OBV) for the given pseudo candlesticks.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate OBV for.</param>
        /// <returns>The OBV value.</returns>
        decimal CalculateOBV(List<PseudoCandlestick> pseudoCandles);

        /// <summary>
        /// Calculates historical support and resistance levels for the specified market.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol.</param>
        /// <param name="candlesticks">The list of candlestick data to analyze.</param>
        /// <param name="minCandlestickPercentage">The minimum candlestick percentage for level detection.</param>
        /// <param name="maxLevels">The maximum number of levels to return.</param>
        /// <param name="sigma">The sigma value for statistical analysis.</param>
        /// <param name="minDistance">The minimum distance between levels.</param>
        /// <returns>A list of support and resistance levels.</returns>
        List<SupportResistanceLevel> CalculateHistoricalSupportResistance(
            string marketTicker,
            List<CandlestickData> candlesticks,
            double minCandlestickPercentage,
            int maxLevels,
            double sigma,
            int minDistance);

        /// <summary>
        /// Calculates the Average Directional Index (ADX) and directional indicators.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate ADX for.</param>
        /// <returns>A tuple containing ADX, PlusDI, and MinusDI values.</returns>
        (double? ADX, double? PlusDI, double? MinusDI) CalculateADX(List<PseudoCandlestick> pseudoCandles);

        /// <summary>
        /// Calculates the Parabolic Stop and Reverse (PSAR) for the given pseudo candlesticks.
        /// </summary>
        /// <param name="pseudoCandles">The list of pseudo candlesticks to calculate PSAR for.</param>
        /// <returns>The PSAR value, or null if calculation is not possible.</returns>
        double? CalculatePSAR(List<PseudoCandlestick> pseudoCandles);
    }
}
