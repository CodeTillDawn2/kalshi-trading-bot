using KalshiUI.Data;
using SmokehousePatterns;

namespace KalshiUI.Services
{
    public class MarketStateService
    {
        public static bool Running = false;

        public static async Task StartExportingParquets(Action<string> updateStatus, int batchSize = 10)
        {
            Running = true;
            MarketProcessor processor = null;
            int offset = 0;

            try
            {
                using (var kalshiBotContext = new KalshiBotContext())
                {
                    processor = kalshiBotContext.ReturnMarketProcessor();
                }

                if (processor == null)
                {
                    updateStatus?.Invoke("Error: Failed to initialize MarketProcessor.");
                    return;
                }

                using (var kalshiBotContext = new KalshiBotContext())
                {
                    int totalMarkets = kalshiBotContext.Markets
                        .Where(x => x.LastCandlestick != null && x.status.ToLower() != "active")
                        .Count();
                    int totalBatches = (int)Math.Ceiling(totalMarkets / (double)batchSize);

                    updateStatus?.Invoke($"Starting to process {totalMarkets} markets in {totalBatches} batches of {batchSize}.");

                    for (int batch = 0; batch < totalBatches; batch++)
                    {
                        offset = batch * batchSize;
                        updateStatus?.Invoke($"Batch: {batch + 1} of {totalBatches}.");

                        // Get only the unique market tickers for this batch
                        List<string> marketTickers = await Task.Run(() => processor.FetchUniqueMarkets(
                            startTime: null,
                            endTime: null,
                            marketTickers: null,
                            topX: batchSize,
                            offset: offset
                        ));

                        foreach (var interval in new[] { 1, 2, 3 })
                        {
                            string intervalName = interval == 1 ? "Minute" : interval == 2 ? "Hourly" : "Daily";
                            string parquetPathSuffix = interval == 1 ? "" : interval == 2 ? "_Hourly" : "_Daily";

                            foreach (string marketName in marketTickers)
                            {
                                updateStatus?.Invoke($"Starting - Batch: {batch + 1} of {totalBatches}. Market: {marketName}. Interval: {intervalName}.");
                                string parquetPath = $"../../../../TestingOutput/CachedMarketData/{marketName}{parquetPathSuffix}_MarketStates.parquet";

                                if (!File.Exists(parquetPath))
                                {
                                    // Fetch candlesticks only for this specific market and interval
                                    List<CandlestickData> marketCandlesticks = await Task.Run(() => processor.FetchCandlesticks(
                                        intervalType: interval,
                                        startTime: null,
                                        endTime: null,
                                        marketTickers: new List<string> { marketName },
                                        topX: null,
                                        offset: 0
                                    ));

                                    if (marketCandlesticks.Any())
                                    {
                                        marketCandlesticks = marketCandlesticks.OrderBy(c => c.Date).ToList();
                                        DateTime firstDate = marketCandlesticks.First().Date;
                                        DateTime lastDate = marketCandlesticks.Last().Date;
                                        int timeSpan = interval == 1 ? (int)Math.Ceiling((lastDate - firstDate).TotalMinutes) + 1 :
                                                      interval == 2 ? (int)Math.Ceiling((lastDate - firstDate).TotalHours) + 1 :
                                                                      (lastDate - firstDate).Days + 1;
                                        string spanUnit = interval == 1 ? "Minutes" : interval == 2 ? "Hours" : "Days";

                                        updateStatus?.Invoke($"Processing - Batch: {batch + 1} of {totalBatches}. Market: {marketName}. Interval: {intervalName}. {spanUnit}: {timeSpan}.");

                                        List<MarketState> marketStates = await Task.Run(() => processor.ComputeMarketStates(marketName, marketCandlesticks));
                                        if (marketStates.Count == timeSpan)
                                        {
                                            await Task.Run(() => MarketState.SaveToParquet(marketStates, parquetPath));
                                        }
                                        else
                                        {
                                            throw new Exception($"Problem with market {marketName} ({intervalName}). Counts do not match. Expected: {timeSpan}. Found: {marketStates.Count}");
                                        }

                                        updateStatus?.Invoke($"Done - Batch: {batch + 1} of {totalBatches}. Market: {marketName}. Interval: {intervalName}. {spanUnit}: {timeSpan}.");
                                    }
                                    else
                                    {
                                        updateStatus?.Invoke($"Warning - Batch: {batch + 1} of {totalBatches}. Market: {marketName}. Interval: {intervalName}. No candlestick data found.");
                                    }
                                }
                                else
                                {
                                    updateStatus?.Invoke($"Batch: {batch + 1} of {totalBatches}. Market: {marketName}. Interval: {intervalName}. Skipped!");
                                }
                            }
                        }
                    }

                    updateStatus?.Invoke("Done.");
                }
            }
            catch (Exception ex)
            {
                Running = false;
                updateStatus?.Invoke($"Error: {ex.Message}");
                Console.WriteLine($"Error in StartExportingParquets: {ex}");
            }
            Running = false;
        }
    }
}