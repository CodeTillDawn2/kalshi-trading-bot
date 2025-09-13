using BacklashDTOs;

namespace TradingStrategies.Helpers.Interfaces
{
    public interface ITradingCalculator
    {
        double? CalculateRSI(List<PseudoCandlestick> pseudoCandles, int periods);
        (double? MACD, double? Signal, double? Histogram) CalculateMACD(List<PseudoCandlestick> pseudoCandlesticks, int shortPeriod, int longPeriod, int signalPeriod);
        (double? Lower, double? Middle, double? Upper) CalculateBollingerBands(List<PseudoCandlestick> pseudoCandles, int period, double stdDevMultiplier);
        (double? K, double? D) CalculateStochastic(List<PseudoCandlestick> pseudoCandles, int kPeriod, int dPeriod);
        double? CalculateATR(List<PseudoCandlestick> pseudoCandles, int period);
        decimal? CalculateVWAP(List<PseudoCandlestick> pseudoCandles, int periods);
        decimal CalculateOBV(List<PseudoCandlestick> pseudoCandles);
        List<SupportResistanceLevel> CalculateHistoricalSupportResistance(
            string marketTicker,
            List<CandlestickData> candlesticks,
            double minCandlestickPercentage,
            int maxLevels,
            double sigma,
            int minDistance);

        (double? ADX, double? PlusDI, double? MinusDI) CalculateADX(List<PseudoCandlestick> pseudoCandles);
        double? CalculatePSAR(List<PseudoCandlestick> pseudoCandles);
    }
}
