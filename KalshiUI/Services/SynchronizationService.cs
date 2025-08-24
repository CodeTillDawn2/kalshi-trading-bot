using KalshiUI.Constants;
using KalshiUI.Constructs;
using KalshiUI.Data;
using KalshiUI.Extensions;
using KalshiUI.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using SmokehousePatterns;

namespace KalshiUI.Services
{
    public class SynchronizationService
    {

        public bool IsRunning { get; set; }
        public bool SynchStopped = true;

        Dictionary<string, int> OrderbookSeq { get; set; }

        PythonService _py = new PythonService();

        public PythonProcess OrderbookProcess { get { return _py.OrderbookProcess; } }
        public PythonProcess LifecycleProcess { get { return _py.LifecycleProcess; } }
        public PythonProcess FillProcess { get { return _py.FillProcess; } }
        public PythonProcess TradeProcess { get { return _py.TradeProcess; } }
        public PythonProcess TickerProcess { get { return _py.TickerProcess; } }
        public PythonProcess EventsProcess { get { return _py.EventsProcess; } }
        public PythonProcess CandlestickProcess { get { return _py.CandlestickProcess; } }
        public PythonProcess SeriesProcess { get { return _py.SeriesProcess; } }
        public PythonProcess MarketsProcess { get { return _py.MarketsProcess; } }

        private List<Market> _marketList = new List<Market>();
        private List<MarketWatch> _watchList = new List<MarketWatch>();

        public List<Market> MarketList { get { return _marketList; } set { _marketList = value; } }
        public List<MarketWatch> WatchList { get { return _watchList; } set { _watchList = value; } }

        private string _automationStatusText = "N/A";

        public string AutomationStatusText { get { return _automationStatusText; } }

        public SynchronizationService()
        {
            OrderbookSeq = new Dictionary<string, int>();
        }


        private void SetAutomationStatusText(string newText)
        {
            _automationStatusText = newText;
        }

        public async Task InitialSetup()
        {
            IsRunning = true;
            //await RefreshOpenEvents();
            if (SynchStopped) return;
            //await FillMarketTitles();
            //if (ExitEarly) return;
            //await FillSeriesGaps();
            if (SynchStopped) return;
            //MarkOldLogsProcessed();
            SynchStopped = false;
            IsRunning = false;
        }

        public async Task Synch()
        {
            IsRunning = true;
            if (SynchStopped)
            {
                return;
            }
            await FillCandlesticks();
            //await ProcessLifecycle();
            //ProcessOrderbook();
            //ProcessTicker();

            IsRunning = false;
        }


        private void MarkOldLogsProcessed()
        {
            using (var kalshiBotContext = new KalshiBotContext())
            {
                kalshiBotContext.Database.ExecuteSqlRaw("UPDATE t_feed_fill SET ProcessedDate = GETDATE() where ProcessedDate is null");
                kalshiBotContext.Database.ExecuteSqlRaw("UPDATE t_feed_lifecycle SET ProcessedDate = GETDATE() where ProcessedDate is null");
                kalshiBotContext.Database.ExecuteSqlRaw("UPDATE t_feed_orderbook SET ProcessedDate = GETDATE() where ProcessedDate is null");
                kalshiBotContext.Database.ExecuteSqlRaw("UPDATE t_feed_ticker SET ProcessedDate = GETDATE() where ProcessedDate is null");
                kalshiBotContext.Database.ExecuteSqlRaw("UPDATE t_feed_trade SET ProcessedDate = GETDATE() where ProcessedDate is null");
            }
        }

        private async Task FillCandlesticks()
        {
            SetAutomationStatusText("Filling Candlesticks...");

            List<SlimMarket> marketsToAnalyze = SchedulerService.GetScheduledJob();


            int count = 0;
            foreach (SlimMarket market in marketsToAnalyze)
            {
                try
                {
                    using (var kalshiBotContext = new KalshiBotContext())
                    {
                        count++;
                        int marketsWithCandlesticksCount = kalshiBotContext.Markets.Count(x => x.LastCandlestick != null);
                        SetAutomationStatusText($"Filling Candlesticks... {market.MarketTicker}. {count} of {marketsToAnalyze.Count()}. Total Markets w Candles: {marketsWithCandlesticksCount}");
                        if (SynchStopped || SchedulerService.ExitEarly) break;

                        // Determine start time: LastCandlestick if exists, otherwise open_time
                        DateTime startTime_Minute;
                        DateTime startTime_Hour;
                        DateTime startTime_Day;

                        var candlesticks = kalshiBotContext.Candlesticks
                            .Where(x => x.market_ticker == market.MarketTicker)
                            .Select(x => new
                            {
                                MarketTicker = x.market_ticker,
                                IntervalType = x.interval_type,
                                EndPeriodTs = x.end_period_ts
                            })
                            .ToList();

                        if (market.LastCandlestick.HasValue && candlesticks.Any())
                        {
                            startTime_Minute = UnixService.ConvertFromUnixTimestamp(
                                candlesticks.Where(x => x.IntervalType == 1).OrderBy(x => x.EndPeriodTs).Last().EndPeriodTs);
                            startTime_Hour = UnixService.ConvertFromUnixTimestamp(
                                candlesticks.Where(x => x.IntervalType == 2).OrderBy(x => x.EndPeriodTs).Last().EndPeriodTs);
                            startTime_Day = UnixService.ConvertFromUnixTimestamp(
                                candlesticks.Where(x => x.IntervalType == 3).OrderBy(x => x.EndPeriodTs).Last().EndPeriodTs);
                        }
                        else
                        {
                            startTime_Minute = market.OpenTime.AddHours(-2);
                            startTime_Hour = market.OpenTime.AddHours(-12);
                            startTime_Day = market.OpenTime.AddDays(-2);
                        }




                        // Add cushion to endTime (1 hour), capped at current time

                        DateTime endTime = DateTime.UtcNow.AddHours(1);
                        if (market.CloseTime.HasValue && market.CloseTime.Value < DateTime.UtcNow)
                        {
                            endTime = market.CloseTime.Value.AddDays(2);
                        }

                        if (endTime > DateTime.UtcNow)
                            endTime = DateTime.UtcNow;

                        // Generate JSON files via Python script
                        string seriesTicker = kalshiBotContext.Events
                            .Where(e => e.event_ticker == market.EventTicker)
                            .Select(e => e.series_ticker)
                            .First();
                        await _py.RunCandlesticksAsync(seriesTicker, market.MarketTicker, startTime_Minute,
                            startTime_Hour, startTime_Day, endTime);

                        await kalshiBotContext.ImportJsonCandlesticks();

                        // Update LastCandlestick for this market to the MAXIMUM across all interval types
                        var lastCandlestickTs = kalshiBotContext.Candlesticks
                            .Where(x => x.market_ticker == market.MarketTicker)
                            .Max(x => x.end_period_ts);

                        if (lastCandlestickTs > 0)  // Check if any candlesticks exist
                        {
                            var marketEntity = kalshiBotContext.Markets.First(m => m.market_ticker == market.MarketTicker);
                            marketEntity.LastCandlestick = UnixService.ConvertFromUnixTimestamp(lastCandlestickTs);

                            // Get the last minute candlestick
                            var lastMinuteCandle = kalshiBotContext.Candlesticks
                                .Where(x => x.market_ticker == market.MarketTicker && x.interval_type == 1)
                                .OrderByDescending(x => x.end_period_ts)
                                .FirstOrDefault();

                            // Calculate the previous minute based on current UTC time
                            DateTime nowUtc = DateTime.UtcNow;
                            DateTime previousMinuteStart = nowUtc.AddMinutes(-1).TruncateToMinute();

                            if (lastMinuteCandle != null)
                            {
                                // Check if we need to fill a gap to the previous minute
                                DateTime lastCandleTime = UnixService.ConvertFromUnixTimestamp(lastMinuteCandle.end_period_ts);
                                if (lastCandleTime <= DateTime.UtcNow.AddMinutes(-30))
                                    marketEntity.LastCandlestick = previousMinuteStart.AddMinutes(-30);
                                else
                                    marketEntity.LastCandlestick = lastCandleTime;


                            }
                        }
                        else
                        {
                            await RefreshMarket(market.MarketTicker);

                            Market mkt = kalshiBotContext.Markets.Where(x => x.market_ticker == market.MarketTicker).FirstOrDefault();
                            if (mkt != null)
                            {
                                long StartPeriodts = UnixService.ConvertToUnixTimestamp(mkt.open_time);
                                long MinuteEndPeriodts = UnixService.ConvertToUnixTimestamp(mkt.open_time.AddMinutes(1));
                                long HourEndPeriodts = UnixService.ConvertToUnixTimestamp(mkt.open_time.AddHours(1));
                                long DayEndPeriodts = UnixService.ConvertToUnixTimestamp(mkt.open_time.AddDays(1));

                                int year = mkt.open_time.Year;
                                int month = mkt.open_time.Month;
                                int day = mkt.open_time.Day;
                                int hour = mkt.open_time.Hour;
                                int minute = mkt.open_time.Minute;


                                Candlestick minuteCandlestick = new Candlestick(mkt.market_ticker, 1, MinuteEndPeriodts, year, month, day, hour, minute, mkt.open_interest,
                                    mkt.last_price, mkt.last_price, mkt.last_price, mkt.last_price, mkt.last_price, mkt.last_price, 0, mkt.last_price, mkt.last_price,
                                    mkt.last_price, mkt.last_price, mkt.last_price, mkt.last_price, mkt.last_price, mkt.last_price);


                                kalshiBotContext.Candlesticks.Add(minuteCandlestick);
                                mkt.LastCandlestick = mkt.open_time.AddMinutes(1);
                                if (mkt.status != KalshiConstants.Status_Active)
                                {
                                    mkt.status = KalshiConstants.Status_Bad;
                                }
                            }

                        }
                        kalshiBotContext.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    // Log the error to a file
                    string logFilePath = "error_log.txt"; // You can customize the path
                    string logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC} | Market: {market.MarketTicker} | Error: {ex.Message}\nStack Trace: {ex.StackTrace}\n";
                    try
                    {
                        File.AppendAllText(logFilePath, logEntry);
                    }
                    catch (Exception logEx)
                    {
                        throw new Exception("Failed to log");
                    }

                    using (var kalshiBotContext = new KalshiBotContext())
                    {
                        Market failedMarket = kalshiBotContext.Markets.Where(x => x.market_ticker == market.MarketTicker).FirstOrDefault();
                        failedMarket.LastCandlestick = null;
                        kalshiBotContext.SaveChanges();
                    }

                    continue;
                }

                SetAutomationStatusText("Candlestick filling and import completed.");
            }
        }

        private async Task FillMarketTitles()
        {
            SetAutomationStatusText("Filling Market Titles...");
            using (var kalshiBotContext = new KalshiBotContext())
            {
                List<string> marketsWithoutTitles = kalshiBotContext.Markets.Where(x => x.title == "").Select(x => x.market_ticker).ToList();

                for (int i = 0; i < marketsWithoutTitles.Count; i++)
                {
                    SetAutomationStatusText($"Filling Market Titles... {i + 1} of {marketsWithoutTitles.Count}");
                    await RefreshMarket(marketsWithoutTitles[i]);
                }
            }
        }

        private async Task FillSeriesGaps()
        {

            List<string> seriesGaps;
            SetAutomationStatusText("Filling Series Gap...");
            using (var kalshiBotContext = new KalshiBotContext())
            {
                //Find series which don't exist in our series table
                seriesGaps = (
                    from e in kalshiBotContext.Events
                    join s in kalshiBotContext.Series
                    on e.series_ticker equals s.series_ticker into seriesJoin
                    from sj in seriesJoin.DefaultIfEmpty()
                    where sj == null
                    select e.series_ticker
                    ).Distinct().ToList();

            }

            foreach (string series_ticker in seriesGaps)
            {
                SetAutomationStatusText($"Filling Series Gap... {series_ticker}");
                await RefreshSeries(series_ticker);
            }
        }


        private async Task RefreshOpenEvents()
        {
            using (var context = new KalshiBotContext())
            {
                // Single optimized query with Join and Distinct
                var twoYearsAgo = DateTime.Now.AddYears(-2);
                var seriesToCheck = await context.Markets
                    .Where(
                    (m =>
                    (m.open_time >= twoYearsAgo
                    ||
                    m.status == KalshiConstants.Status_Active)
                    && m.LastCandlestick == null
                    ))
                    .Join(context.Events.Include(e => e.Series),
                        m => m.event_ticker,
                        e => e.event_ticker,
                        (m, e) => e.Series.series_ticker)
                    .Distinct()
                    .ToListAsync();

                // Sequential processing with minimal overhead
                foreach (var series in seriesToCheck)
                {
                    SetAutomationStatusText($"Refreshing Open Events for Series {series}...");
                    await _py.RunEventsAsync(
                        seriesTicker: series,
                        status: KalshiConstants.Status_Open,
                        withNestedMarkets: true);
                }
            }
        }


        /// <summary>
        /// Refresh market data by market. Can use a comma delimited list
        /// </summary>
        /// <param name="marketTicker"></param>
        /// <returns></returns>
        private async Task RefreshMarket(string marketTicker)
        {
            await _py.RunMarketsAsync(marketTicker);
        }

        private async Task RefreshSeriesEvents(string seriesTicker)
        {
            await _py.RunEventsAsync(seriesTicker);
        }
        private async Task RefreshSeries(string seriesTicker)
        {
            await _py.RunSeriesAsync(seriesTicker);
        }

        private async Task ProcessLifecycle()
        {
            using (var kalshiBotContext = new KalshiBotContext())
            {
                //Need to get only the most recent result
                foreach (Lifecycle lifeCycle in kalshiBotContext.Lifecycles.Where(x => x.ProcessedDate == null).ToList())
                {
                    Market? market = kalshiBotContext.Markets.Where(x => x.market_ticker == lifeCycle.market_ticker).FirstOrDefault();

                    if (market == null)
                    {
                        await RefreshMarket(lifeCycle.market_ticker);
                    }
                    else
                    {
                        market.close_time = UnixService.ConvertFromUnixTimestamp(lifeCycle.close_ts);
                        market.result = lifeCycle.result;


                        //TODO: more fields need updated
                    }
                }
            }
        }

        private void ProcessOrderbook()
        {
            using (var kalshiBotContext = new KalshiBotContext())
            {
                // Retrieve orders where ProcessedDate is null
                var orders = kalshiBotContext.OrderbookSnapshots
                    .Where(x => x.ProcessedDate == null)
                    .AsEnumerable();

                // Group orders by market_id, side, and price
                var groupedOrders = orders.GroupBy(x => new { x.market_id, x.side, x.price });

                foreach (var group in groupedOrders)
                {
                    var marketId = group.Key.market_id.ToString();
                    var side = group.Key.side;
                    var price = group.Key.price;

                    // Retrieve the last processed sequence for this market_id, default to 0 if not found
                    int lastSeq = OrderbookSeq.TryGetValue(marketId, out var storedSeq) ? storedSeq : 0;

                    // Sort orders by table_seq within this group
                    var sortedOrders = group.OrderBy(x => x.table_seq).ToList();
                    List<OrderbookSnapshot> validOrders = new List<OrderbookSnapshot>();
                    int expectedSeq = lastSeq + 1;
                    bool inValidSequence = lastSeq > 0;

                    foreach (var orderbook in sortedOrders)
                    {
                        if (orderbook.kalshi_seq == 1)
                        {
                            // New sequence starts, discard previous
                            validOrders.Clear();
                            expectedSeq = 1;
                            inValidSequence = true;
                        }

                        if (inValidSequence && orderbook.kalshi_seq == expectedSeq)
                        {
                            validOrders.Add(orderbook);
                            expectedSeq++;
                        }
                        else if (inValidSequence && orderbook.kalshi_seq != expectedSeq)
                        {
                            validOrders.Clear();
                            inValidSequence = false;
                        }

                        // Mark this order as processed regardless of whether it's in a valid sequence or not
                        orderbook.ProcessedDate = DateTime.UtcNow;
                    }

                    // If valid orders exist, process them based on whether they are snapshots or deltas
                    if (validOrders.Any())
                    {

                        foreach (var validOrder in validOrders)
                        {
                            if (validOrder.kalshi_seq == 1) // Snapshot
                            {
                                // Reset current orderbook for this specific market_ticker, price, and side
                                kalshiBotContext.Database.ExecuteSqlRaw(
                                    $"DELETE FROM t_Orderbook where market_ticker = '{validOrder.market_ticker}' and price = {validOrder.price} and side = '{validOrder.side}'");

                                // Add new snapshot entry
                                Orderbook newEntry = new Orderbook
                                {
                                    market_ticker = validOrder.market_ticker,
                                    price = validOrder.price,
                                    side = validOrder.side,
                                    resting_contracts = validOrder.resting_contracts
                                };
                                kalshiBotContext.Orderbooks.Add(newEntry);
                            }
                            else // Delta
                            {
                                // Find existing entry or assume it hasn't been processed yet
                                Orderbook existingEntry = kalshiBotContext.Orderbooks.FirstOrDefault(x =>
                                    x.market_ticker == validOrder.market_ticker &&
                                    x.price == validOrder.price &&
                                    x.side == validOrder.side);

                                if (existingEntry != null)
                                {
                                    // Update existing entry with delta
                                    existingEntry.resting_contracts += validOrder.delta.Value;
                                }
                                else
                                {
                                    // If no existing entry, we might want to log this as an error or handle it differently
                                    System.Diagnostics.Debug.WriteLine($"No matching orderbook entry for delta update: {validOrder.market_ticker}, {validOrder.price}, {validOrder.side}");
                                }
                            }

                            System.Diagnostics.Debug.WriteLine($"Processing valid order: kalshi_seq = {validOrder.kalshi_seq}, table_seq = {validOrder.table_seq}");
                        }

                        // Update or add the last sequence ID for the market
                        OrderbookSeq[marketId] = validOrders.Last().kalshi_seq;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"No valid sequence found for market {marketId}, side {side}, price {price}. Disregarding.");
                    }

                    // Save all changes, including ProcessedDate updates
                    kalshiBotContext.SaveChanges();
                }
            }
        }

        private void ProcessTicker()
        {

            using (var kalshiBotContext = new KalshiBotContext())
            {

                // Load data into memory and group in memory
                var tickers = kalshiBotContext.Tickers
                    .Where(x => x.ProcessedDate == null)
                    .ToList() // Materialize data into memory
                    .GroupBy(t => t.market_id)
                    .Select(g => g.OrderByDescending(t => t.ts).FirstOrDefault())
                    .Where(t => t != null)
                    .AsEnumerable();

                // Get distinct market_ticker values
                var distinctMarketTickers = tickers
                    .Where(t => t != null && t.market_ticker != null)
                    .Select(t => t.market_ticker!)
                    .AsEnumerable();

                // Process each distinct market_ticker
                foreach (string marketTicker in distinctMarketTickers)
                {
                    System.Diagnostics.Debug.WriteLine($"Processing Market Ticker: {marketTicker}");
                    Ticker? latestTicker = kalshiBotContext.Tickers
                        .Where(x => x.market_ticker == marketTicker)
                        .Include(x => x.Market)
                        .OrderByDescending(x => x.LoggedDate)
                        .FirstOrDefault();
                    if (latestTicker != null)
                    {
                        Market relevantMarket = latestTicker.Market;
                        if (relevantMarket != null)
                        {
                            relevantMarket.previous_price = relevantMarket.last_price;
                            relevantMarket.last_price = latestTicker.price;
                            relevantMarket.yes_bid = latestTicker.yes_bid;
                            relevantMarket.yes_ask = latestTicker.yes_ask;
                            relevantMarket.no_bid = 100 - latestTicker.yes_ask;
                            relevantMarket.no_ask = 100 - latestTicker.yes_bid;
                            relevantMarket.volume = latestTicker.volume;
                            relevantMarket.open_interest = latestTicker.open_interest;
                            relevantMarket.volume = latestTicker.dollar_volume;
                            //relevantMarket.liquidity = latestTicker.dollar_open_interest;
                        }
                        else
                        {
                            RefreshMarket(latestTicker.market_ticker);
                        }
                    }
                }

                kalshiBotContext.SaveChanges();
            }
        }


        public async Task StartAllProcesses()
        {
            //await StartFillProcess();
            //await StartLifecycleProcess();
            //await StartOrderbookProcess();
            //await StartTickerProcess();
            //await StartTradeProcess();
        }

        public async Task StartOrderbookProcess()
        {
            string marketsToRun = "";
            foreach (string watchedMarket in _watchList.Select(x => x.market_ticker))
            {
                marketsToRun += $"{watchedMarket} ";
            }

            if (marketsToRun != "")
            {
                marketsToRun = marketsToRun.Substring(0, marketsToRun.Length - 1);
            }

            await _py.RunOrderbookAsync(marketsToRun);
        }
        public async Task StartTickerProcess()
        {

            await _py.RunTickerAsync();
        }
        public async Task StartFillProcess()
        {
            await _py.RunFillAsync();
        }
        public async Task StartLifecycleProcess()
        {
            await _py.RunLifecycleAsync();
        }

        public async Task ShowCandlesticks(string marketTicker)
        {
            await _py.RunChartCandlesticksAsync(marketTicker);
        }
        public async Task StartTradeProcess()
        {
            await _py.RunTradeAsync();
        }

        public void StopOrderbookProcess()
        {
            _py.StopOrderbookProcess();
        }
        public void StopTickerProcess()
        {

            _py.StopTickerProcess();
        }
        public void StopFillProcess()
        {
            _py.StopFillProcess();
        }
        public void StopLifecycleProcess()
        {
            _py.StopLifecycleProcess();
        }
        public void StopTradeProcess()
        {
            _py.StopTradeProcess();
        }

        public static void TerminatePythonProcesses()
        {
            //PythonService.TerminatePythonProcesses(); Turned off for now
        }


    }
}
