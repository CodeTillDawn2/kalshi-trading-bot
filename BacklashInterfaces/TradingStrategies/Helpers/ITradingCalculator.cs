using BacklashDTOs;

namespace TradingStrategies.Helpers.Interfaces
{
/// <summary>ITradingCalculator</summary>
/// <summary>ITradingCalculator</summary>
    public interface ITradingCalculator
/// <summary>CalculateMACD</summary>
/// <summary>CalculateRSI</summary>
    {
/// <summary>CalculateATR</summary>
/// <summary>CalculateBollingerBands</summary>
        double? CalculateRSI(List<PseudoCandlestick> pseudoCandles, int periods);
/// <summary>CalculateHistoricalSupportResistance</summary>
/// <summary>CalculateATR</summary>
        (double? MACD, double? Signal, double? Histogram) CalculateMACD(List<PseudoCandlestick> pseudoCandlesticks, int shortPeriod, int longPeriod, int signalPeriod);
/// <summary>CalculateOBV</summary>
        (double? Lower, double? Middle, double? Upper) CalculateBollingerBands(List<PseudoCandlestick> pseudoCandles, int period, double stdDevMultiplier);
        (double? K, double? D) CalculateStochastic(List<PseudoCandlestick> pseudoCandles, int kPeriod, int dPeriod);
        double? CalculateATR(List<PseudoCandlestick> pseudoCandles, int period);
        decimal? CalculateVWAP(List<PseudoCandlestick> pseudoCandles, int periods);
/// <summary>CalculateADX</summary>
        decimal CalculateOBV(List<PseudoCandlestick> pseudoCandles);
        List<SupportResistanceLevel> CalculateHistoricalSupportResistance(
            string marketTicker,
            List<CandlestickData> candlesticks,
/// <summary>CalculateADX</summary>
            double minCandlestickPercentage,
            int maxLevels,
            double sigma,
            int minDistance);

        (double? ADX, double? PlusDI, double? MinusDI) CalculateADX(List<PseudoCandlestick> pseudoCandles);
        double? CalculatePSAR(List<PseudoCandlestick> pseudoCandles);
    }
}
