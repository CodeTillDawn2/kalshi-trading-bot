using SmokehouseDTOs;

namespace TradingStrategies.Extensions
{
    public static class PseudoCandlestickExtensions
    {
        public static CandleMids[] ToCandleMids(
            this IList<PseudoCandlestick> candles,
            string marketTicker)
        {
            if (candles == null || candles.Count < 2)
                return Array.Empty<CandleMids>();

            var result = new List<CandleMids>(candles.Count - 1);

            for (int i = 1; i < candles.Count; i++)
            {
                var prev = candles[i - 1];
                var curr = candles[i];

                result.Add(new CandleMids
                {
                    MarketTicker = marketTicker,
                    Timestamp = curr.Timestamp,
                    Open = prev.MidClose,
                    Close = curr.MidClose,
                    High = curr.MidHigh,
                    Low = curr.MidLow,
                    Volume = (double)curr.Volume
                });
            }

            return result.ToArray();
        }
    }
}
