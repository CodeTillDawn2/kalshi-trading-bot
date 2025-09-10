using Microsoft.AspNetCore.SignalR;
using BacklashBot.Hubs;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;
using BacklashBot.Configuration;
using Microsoft.Extensions.Options;

namespace BacklashBot.Services
{
    public class BroadcastService : IBroadcastService
    {
        private readonly IHubContext<ChartHub> _hubContext;
        private readonly IServiceFactory _serviceFactory;
        private readonly ILogger<IBroadcastService> _logger;
        private readonly ConcurrentDictionary<string, (int Ask, int Bid)> _lastBroadcastedPrices = new();
        private readonly ConcurrentDictionary<string, long> _broadcastCounts = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _executionTimes = new();
        private Task? _realTimeBroadcastTask;
        private Task? _performanceBroadcastTask;
        private Task? _checkInBroadcastTask;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IScopeManagerService _scopeManagerService;
        private readonly IStatusTrackerService _statusTracker;
        private readonly ExecutionConfig _executionConfig;
        private ConcurrentDictionary<string, long> BroadcastCounts => _broadcastCounts;

        public BroadcastService(
            IHubContext<ChartHub> hubContext,
            IServiceFactory serviceFactory,
            IStatusTrackerService statusTracker,
            IServiceScopeFactory scopeFactory,
            ILogger<IBroadcastService> logger,
            IScopeManagerService scopeManagerService,
            IOptions<ExecutionConfig> executionConfig)
        {
            _scopeManagerService = scopeManagerService;
            _hubContext = hubContext;
            _statusTracker = statusTracker;
            _serviceFactory = serviceFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _executionConfig = executionConfig.Value;
            SubscribeToEvents();
        }

        public async Task StartServicesAsync()
        {
            try
            {
                _logger.LogDebug("BroadcastService starting...");
                UnsubscribeFromEvents();
                SubscribeToEvents();

                var cancellationToken = _statusTracker.GetCancellationToken();

                // Only keep the 30-second CheckIn broadcast loop - no automatic market data broadcasting
                _checkInBroadcastTask = Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (ChartHub.HasConnectedClients())
                            {
                                await BroadcastCheckInAsync();
                            }
                            else
                            {
                                _logger.LogDebug("No clients connected, skipping CheckIn broadcast.");
                            }
                            await Task.Delay(30000, cancellationToken); // 30 seconds
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("CheckIn broadcast task canceled.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in CheckIn broadcast cycle.");
                        }
                    }
                }, cancellationToken);

                _logger.LogDebug("BroadcastService started with automatic check-ins only.");
                await Task.CompletedTask;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastService.StartAsync stopped due to cancellation");
                return;
            }
        }

        public async Task StopServicesAsync()
        {
            _logger.LogDebug("BroadcastService stopping...");
            try
            {
                var tasksToWait = new List<Task>();
                if (_performanceBroadcastTask != null) tasksToWait.Add(_performanceBroadcastTask);
                if (_checkInBroadcastTask != null) tasksToWait.Add(_checkInBroadcastTask);
                if (tasksToWait.Any())
                {
                    await Task.WhenAll(tasksToWait).ConfigureAwait(false);
                }
                ChartHub.ClearConnectedClients();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastService tasks canceled as expected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping BroadcastService.");
            }
            UnsubscribeFromEvents();
            _logger.LogDebug("BroadcastService stopped.");
        }



        public async Task BroadcastAllDataToClientAsync(string connectionId)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastAllDataToClientAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogDebug("Broadcasting All data to {connectionId})", connectionId);
            try
            {
                await BroadcastMarketListAsync(connectionId);
                var markets = await GetWatchedMarketsAsync();
                foreach (var marketTicker in markets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _hubContext.Clients.Client(connectionId).SendAsync("StartBatchUpdate", cancellationToken: cancellationToken);
                    try
                    {
                        await BroadcastTickerUpdateAsync(marketTicker, connectionId);
                        await BroadcastOrderbookAsync(marketTicker, connectionId);
                        await BroadcastPositionsAsync(marketTicker, connectionId);
                        await BroadcastHistoricalDataAsync(marketTicker, connectionId);
                        await BroadcastRealTimeMetrics(marketTicker, connectionId);
                        await BroadcastExchangeStatusAsync(connectionId);
                    }
                    finally
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync("EndBatchUpdate", cancellationToken: cancellationToken);
                    }
                }
                await BroadcastBalanceAsync(connectionId);
                await BroadcastPortfolioValueAsync(connectionId);
                _logger.LogDebug("Broadcasted all data to client {ConnectionId}", connectionId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastAllDataToClientAsync was cancelled for {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting all data to client {ConnectionId}", connectionId);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastAllDataToClientAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Broadcasts all market-related data on demand (called when overseer requests refresh)
        /// </summary>
        public async Task BroadcastAllMarketDataOnDemandAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastAllMarketDataOnDemandAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();

            if (!ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping on-demand market data broadcast.");
                return;
            }

            _logger.LogDebug("Broadcasting all market data on demand to all clients");
            try
            {
                await BroadcastMarketListAsync();
                await BroadcastExchangeStatusAsync();
                await BroadcastLastWebSocketUpdateAsync();
                await BroadcastBalanceAsync();
                await BroadcastPortfolioValueAsync();

                var markets = await GetWatchedMarketsAsync();
                foreach (var marketTicker in markets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await _hubContext.Clients.All.SendAsync("StartBatchUpdate", cancellationToken: cancellationToken);
                    try
                    {
                        await BroadcastTickerUpdateAsync(marketTicker);
                        await BroadcastOrderbookAsync(marketTicker);
                        await BroadcastPositionsAsync(marketTicker);
                        await BroadcastHistoricalDataAsync(marketTicker);
                        await BroadcastRealTimeMetrics(marketTicker);
                    }
                    finally
                    {
                        await _hubContext.Clients.All.SendAsync("EndBatchUpdate", cancellationToken: cancellationToken);
                    }
                }
                _logger.LogDebug("Broadcasted all market data on demand to all clients");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastAllMarketDataOnDemandAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting all market data on demand");
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastAllMarketDataOnDemandAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        public void Dispose()
        {
            _performanceBroadcastTask?.Dispose();
            _checkInBroadcastTask?.Dispose();
        }

        private void SubscribeToEvents()
        {
            var marketDataService = _serviceFactory.GetMarketDataService();
            var orderBookService = _serviceFactory.GetOrderBookService();
            marketDataService.MarketDataUpdated += OnMarketDataUpdated;
            marketDataService.PositionDataUpdated += OnPositionDataUpdated;
            marketDataService.WatchListChanged += BroadcastMarketList;
            marketDataService.TickerAdded += OnTickerAdded;
            marketDataService.AccountBalanceUpdated += OnAccountBalanceUpdated;
            orderBookService.OrderBookUpdated += OnOrderbookDataUpdated;
        }

        public void UnsubscribeFromEvents()
        {
            var marketDataService = _serviceFactory.GetMarketDataService();
            var orderBookService = _serviceFactory.GetOrderBookService();
            marketDataService.MarketDataUpdated -= OnMarketDataUpdated;
            marketDataService.PositionDataUpdated -= OnPositionDataUpdated;
            marketDataService.WatchListChanged -= BroadcastMarketList;
            marketDataService.TickerAdded -= OnTickerAdded;
            marketDataService.AccountBalanceUpdated -= OnAccountBalanceUpdated;
            orderBookService.OrderBookUpdated -= OnOrderbookDataUpdated;
        }

        private void IncrementBroadcastCount(string broadcastType)
        {
            _broadcastCounts.AddOrUpdate(broadcastType, 1, (key, count) => count + 1);
        }

        private void RecordExecutionTime(string broadcastType, long elapsedMs)
        {
            var bag = _executionTimes.GetOrAdd(broadcastType, _ => new ConcurrentBag<long>());
            bag.Add(elapsedMs);
        }



        private async Task BroadcastTickerUpdateAsync(string marketTicker, string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastTickerUpdateAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping ticker broadcast for {MarketTicker}", marketTicker);
                return;
            }

            _logger.LogDebug("Broadcasting Ticker Data to {connectionId} for {marketTicker}", connectionId, marketTicker);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                var candlestickService = _serviceFactory.GetCandlestickService();
                var marketData = marketDataService.GetMarketDetails(marketTicker);
                if (marketData == null || !marketData.Tickers.Any() && !marketData.Candlesticks.ContainsKey("minute") || marketData.OrderbookData.Count == 0)
                {
                    _logger.LogDebug("No market data or tickers available yet for {MarketTicker}, skipping broadcast", marketTicker);
                    return;
                }


                var tickerList = marketData.Tickers
                    .OrderByDescending(t => t.LoggedDate)
                    .Select(t => $"{{Ask={t.yes_ask}, Bid={t.yes_bid}, LoggedDate={t.LoggedDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")}}}")
                    .ToList();
                _logger.LogDebug("Tickers for {marketTicker}: Count={Count}, List=[{Tickers}]",
                    marketTicker, tickerList.Count, string.Join(", ", tickerList));

                var lastCandlestick = marketData.Candlesticks["minute"]
                    .OrderByDescending(c => c.Date)
                    .FirstOrDefault();
                var lastCandlestickDate = lastCandlestick?.Date ?? DateTime.UnixEpoch;
                _logger.LogDebug("Last candlestick for {marketTicker}: Date={Date}, AskClose={Ask}, BidClose={Bid}",
                    marketTicker, lastCandlestickDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    lastCandlestick?.AskClose ?? 0, lastCandlestick?.BidClose ?? 0);

                var tickersToBroadcast = marketData.Tickers
                    .Where(t => t.LoggedDate > lastCandlestickDate)
                    .OrderBy(t => t.LoggedDate)
                    .Select(t => new
                    {
                        yesAsk = t.yes_ask,
                        yesBid = t.yes_bid,
                        timestamp = t.LoggedDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        source = marketData.CurrentPriceSource ?? "Unknown",
                        yesSpread = marketData.YesSpread,
                        noSpread = marketData.NoSpread
                    })
                    .ToList();

                if (!tickersToBroadcast.Any())
                {
                    _logger.LogDebug("No recent tickers to broadcast for market {marketTicker}", marketTicker);
                    if (lastCandlestick != null)
                    {
                        tickersToBroadcast.Add(new
                        {
                            yesAsk = lastCandlestick.AskClose,
                            yesBid = lastCandlestick.BidClose,
                            timestamp = lastCandlestick.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            source = "Candlestick",
                            yesSpread = ((100 - lastCandlestick.AskClose) + lastCandlestick.BidClose) / 2,
                            noSpread = ((100 - lastCandlestick.BidClose) + lastCandlestick.AskClose) / 2
                        });
                        _logger.LogDebug("Falling back to candlestick for {marketTicker}: Ask={Ask}, Bid={Bid}, Date={Date}",
                            marketTicker, lastCandlestick.AskClose, lastCandlestick.BidClose,
                            lastCandlestick.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                    }
                    else
                    {
                        _logger.LogDebug("No tickers or candlestick available for {marketTicker}, skipping broadcast", marketTicker);
                        return;
                    }
                }

                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                _logger.LogDebug("Broadcasting Market Data Batch to {connectionId} for {marketTicker}. Tickers: {tickers}",
                    connectionId, marketTicker, tickersToBroadcast);
                await target.SendAsync("ReceiveMarketDataBatch", new { marketTicker, tickers = tickersToBroadcast }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting ticker updates for {MarketTicker}", marketTicker);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastTickerUpdateAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastRealTimeMetrics(string marketTicker, string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastRealTimeMetrics));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping real-time metrics broadcast for {MarketTicker}", marketTicker);
                return;
            }

            _logger.LogDebug("Broadcasting Real Time Data to {connectionId} for {marketTicker}", connectionId, marketTicker);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();
                var dataCache = _serviceFactory.GetDataCache();
                var marketData = marketDataService.GetMarketDetails(marketTicker);
                if (marketData == null)
                {
                    _logger.LogDebug("No market data available for {MarketTicker}, skipping real-time metrics broadcast", marketTicker);
                    return;
                }

                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("RealTimeData", new
                {
                    marketTicker,
                    VelocityPerMinute_Top_YesBid = marketData.VelocityPerMinute_Top_Yes_Bid,
                    VelocityPerMinute_Top_NoBid = marketData.VelocityPerMinute_Top_No_Bid,
                    VelocityPerMinute_Bottom_YesBid = marketData.VelocityPerMinute_Bottom_Yes_Bid,
                    VelocityPerMinute_Bottom_NoBid = marketData.VelocityPerMinute_Bottom_No_Bid,
                    LevelCount_Top_YesBid = marketData.LevelCount_Top_Yes_Bid,
                    LevelCount_Top_NoBid = marketData.LevelCount_Top_No_Bid,
                    LevelCount_Bottom_YesBid = marketData.LevelCount_Bottom_Yes_Bid,
                    LevelCount_Bottom_NoBid = marketData.LevelCount_Bottom_No_Bid,
                    OrderRatePerMinute_YesBid = marketData.OrderVolumePerMinute_YesBid,
                    OrderRatePerMinute_NoBid = marketData.OrderVolumePerMinute_NoBid,
                    NonTradeRelatedOrderCount_Yes = marketData.NonTradeRelatedOrderCount_Yes,
                    NonTradeRelatedOrderCount_No = marketData.NonTradeRelatedOrderCount_No,
                    tradeVolumePerMinute_Yes = marketData.TradeVolumePerMinute_Yes,
                    tradeVolumePerMinute_No = marketData.TradeVolumePerMinute_No,
                    tradeRatePerMinute_Yes = marketData.TradeRatePerMinute_Yes,
                    tradeRatePerMinute_No = marketData.TradeRatePerMinute_No,
                    TradeCount_Yes = marketData.TradeCount_Yes,
                    TradeCount_No = marketData.TradeCount_No,
                    AverageTradeSize_Yes = marketData.AverageTradeSize_Yes,
                    AverageTradeSize_No = marketData.AverageTradeSize_No,
                    rsi = marketData.RSI_Medium,
                    macd = marketData.MACD_Medium,
                    ema = marketData.EMA_Medium,
                    bollingerBands = marketData.BollingerBands_Medium,
                    atr = marketData.ATR_Medium,
                    vwap = marketData.VWAP_Medium,
                    stochasticOscillator = marketData.StochasticOscillator_Medium,
                    obv = marketData.OBV_Medium,
                    warningCount = errorHandler.WarningCount,
                    errorCount = errorHandler.ErrorCount
                }, cancellationToken);
                _logger.LogDebug("Successfully broadcasted real-time metrics for {MarketTicker}", marketTicker);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting real-time metrics for {MarketTicker}", marketTicker);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastRealTimeMetrics), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastOrderbookAsync(string marketTicker, string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastOrderbookAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping orderbook broadcast for {MarketTicker}", marketTicker);
                return;
            }

            _logger.LogDebug("Broadcasting Orderbook Data to {connectionId} for {marketTicker}", connectionId, marketTicker);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();

                var marketData = marketDataService.GetMarketDetails(marketTicker);

                if (marketData == null)
                {
                    _logger.LogDebug("No market data available for {MarketTicker}, skipping orderbook broadcast", marketTicker);
                    return;
                }

                var orderbookData = marketDataService.GetCurrentOrderBook(marketTicker);
                var formattedOrderbook = orderbookData.Select(o => new
                {
                    o.Price,
                    o.Side,
                    size = o.RestingContracts,
                    value = o.Price * o.RestingContracts / 100.0
                }).OrderByDescending(o => o.Price).ToList();

                _logger.LogInformation("Broadcasting Orderbook for {MarketTicker}: Orders=[{Orders}]",
                    marketTicker,
                    string.Join("; ", formattedOrderbook.Select(o => $"Price={o.Price},Side={o.Side},Size={o.size}")));

                var lastUpdated = marketDataService.GetLatestOrderbookTimestamp(marketTicker)?.ToString("yyyy-MM-ddTHH:mm:ss") ?? null;

                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("OrderbookData", new
                {
                    marketTicker,
                    orders = formattedOrderbook,
                    cumulativeYesBidDepth = marketData.TopTenPercentLevelDepth_Yes,
                    cumulativeNoBidDepth = marketData.TopTenPercentLevelDepth_No,
                    yesBidRange = marketData.BidRange_Yes,
                    noBidRange = marketData.BidRange_No,
                    depthAtBestYesBid = marketData.DepthAtBestYesBid,
                    depthAtBestNoBid = marketData.DepthAtBestNoBid,
                    totalYesBidContracts = marketData.TotalBidContracts_Yes,
                    totalNoBidContracts = marketData.TotalBidContracts_No,
                    bidImbalance = marketData.BidCountImbalance,
                    depthAtTop4YesBids = marketData.DepthAtTop4YesBids,
                    depthAtTop4NoBids = marketData.DepthAtTop4NoBids,
                    yesBidCenterOfMass = marketData.YesBidCenterOfMass,
                    noBidCenterOfMass = marketData.NoBidCenterOfMass,
                    lastUpdated
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting orderbook for {MarketTicker}", marketTicker);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastOrderbookAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task<List<string>> GetWatchedMarketsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(GetWatchedMarketsAsync));
            try
            {
                var cancellationToken = _statusTracker.GetCancellationToken();
                cancellationToken.ThrowIfCancellationRequested();
                var marketDataService = _serviceFactory.GetMarketDataService();
                var markets = await marketDataService.FetchWatchedMarketsAsync();
                return markets;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetWatchedMarketsAsync was cancelled");
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get watched markets");
                return new List<string>();
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(GetWatchedMarketsAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastPositionsAsync(string marketTicker, string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastPositionsAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping positions broadcast for {MarketTicker}", marketTicker);
                return;
            }

            _logger.LogDebug("Broadcasting Position Data to {connectionId} for {marketTicker}", connectionId, marketTicker);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                var marketData = marketDataService.GetMarketDetails(marketTicker);
                if (marketData == null) return;

                string ReadableHoldTime = "--";
                if (marketData.HoldTime != null)
                {
                    ReadableHoldTime = $"{Math.Round(marketData.HoldTime.Value.TotalDays, 0)} days, {marketData.HoldTime.Value.Hours} hours";
                }

                var positionData = marketData.Positions.Select(p => new
                {
                    ticker = p.Ticker,
                    totalTraded = p.TotalTraded,
                    position = p.Position,
                    marketExposure = p.MarketExposure,
                    realizedPnl = p.RealizedPnl,
                    restingOrdersCount = p.RestingOrdersCount,
                    feesPaid = p.FeesPaid,
                    lastUpdatedTs = p.LastUpdatedUTC.ToString("yyyy-MM-ddTHH:mm:ss"),
                    lastModified = p.LastModified?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    positionROI = marketData.PositionROI,
                    positionROIAmt = marketData.PositionROIAmt,
                    holdTime = ReadableHoldTime,
                    positionUpside = marketData.PositionUpside,
                    positionDownside = marketData.PositionDownside,
                    buyinPrice = p.Position != 0 ? p.MarketExposure / p.Position : 0
                }).ToList();

                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("UpdatePositions", new { marketTicker, positions = positionData }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting positions for {MarketTicker}", marketTicker);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastPositionsAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastPositionPriceMetadataAsync(string marketTicker, string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastPositionPriceMetadataAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping position price metadata broadcast for {MarketTicker}", marketTicker);
                return;
            }

            _logger.LogDebug("Broadcasting Position Price Metadata to {connectionId} for {marketTicker}", connectionId, marketTicker);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                var marketData = marketDataService.GetMarketDetails(marketTicker);
                if (marketData == null) return;

                var positionData = marketData.Positions.Select(p => new
                {
                    ticker = p.Ticker,
                    positionROI = marketData.PositionROI,
                    positionROIAmt = marketData.PositionROIAmt,
                    positionUpside = marketData.PositionUpside,
                    positionDownside = marketData.PositionDownside
                }).ToList();

                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("UpdatePositionPriceMetadata", new { marketTicker, positions = positionData }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting position price metadata for {MarketTicker}", marketTicker);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastPositionPriceMetadataAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastHistoricalDataAsync(string marketTicker, string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastHistoricalDataAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping historical data broadcast for {MarketTicker}", marketTicker);
                return;
            }

            _logger.LogDebug("Broadcasting Historical Data to {connectionId} for {marketTicker}", connectionId, marketTicker);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                var candlestickService = _serviceFactory.GetCandlestickService();
                var marketData = marketDataService.GetMarketDetails(marketTicker);
                if (marketData == null) return;

                double MarketAge = marketData.MarketAge?.TotalSeconds ?? 0;
                double TimeLeft = marketData.MarketInfo.status != "closed" && marketData.TimeLeft.HasValue ? marketData.TimeLeft.Value.TotalSeconds : 0;

                var lastUpdated = marketDataService.GetLatestWebSocketTimestamp().ToString("yyyy-MM-ddTHH:mm:ss") ?? null;

                var marketInfo = new
                {
                    marketData.MarketInfo?.title,
                    yesSubtitle = marketData.MarketInfo?.yes_sub_title,
                    noSubtitle = marketData.MarketInfo?.no_sub_title,
                    marketType = marketData.MarketInfo?.market_type,
                    marketStatus = marketData.MarketInfo?.status,
                    marketAgeSeconds = MarketAge,
                    timeLeftSeconds = TimeLeft,
                    open_time = marketData.MarketInfo?.open_time.ToString("yyyy-MM-ddTHH:mm:ss"),
                    close_time = marketData.MarketInfo?.close_time.ToString("yyyy-MM-ddTHH:mm:ss"),
                    canCloseEarly = marketData.CanCloseEarly
                };

                var priceData = new
                {
                    currentPrice = new { ask = marketData.TickerPriceYes.Ask, bid = marketData.TickerPriceYes.Bid, when = marketData.TickerPriceYes.When.ToString("yyyy-MM-ddTHH:mm:ss"), source = marketData.CurrentPriceSource },
                    currentPriceNo = new { ask = marketData.TickerPriceNo.Ask, bid = marketData.TickerPriceNo.Bid, when = marketData.TickerPriceNo.When.ToString("yyyy-MM-ddTHH:mm:ss"), source = marketData.CurrentPriceSource },
                    allTimeHighYes_Bid = new { bid = marketData.AllTimeHighYes_Bid.Bid, when = marketData.AllTimeHighYes_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    allTimeLowYes_Bid = new { bid = marketData.AllTimeLowYes_Bid.Bid, when = marketData.AllTimeLowYes_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    recentHighYes_Bid = new { bid = marketData.RecentHighYes_Bid.Bid, when = marketData.RecentHighYes_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    recentLowYes_Bid = new { bid = marketData.RecentLowYes_Bid.Bid, when = marketData.RecentLowYes_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    allTimeHighNo_Bid = new { ultrafast = marketData.AllTimeHighNo_Bid.Bid, when = marketData.AllTimeHighNo_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    allTimeLowNo_Bid = new { bid = marketData.AllTimeLowNo_Bid.Bid, when = marketData.AllTimeLowNo_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    recentHighNo_Bid = new { bid = marketData.RecentHighNo_Bid.Bid, when = marketData.RecentHighNo_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    recentLowNo_Bid = new { bid = marketData.RecentLowNo_Bid.Bid, when = marketData.RecentLowNo_Bid.When.ToString("yyyy-MM-ddTHH:mm:ss") },
                    goodBadPriceYes = marketData.GoodBadPriceYes,
                    goodBadPriceNo = marketData.GoodBadPriceNo,
                    marketBehaviorYes = marketData.MarketBehaviorYes,
                    marketBehaviorNo = marketData.MarketBehaviorNo,
                    highestVolume_Day = marketData.HighestVolume_Day,
                    highestVolume_Hour = marketData.HighestVolume_Hour,
                    highestVolume_Minute = marketData.HighestVolume_Minute,
                    recentVolume_LastHour = marketData.RecentVolume_LastHour,
                    recentVolume_LastThreeHours = marketData.RecentVolume_LastThreeHours,
                    recentVolume_LastMonth = marketData.RecentVolume_LastMonth,
                    supportResistanceLevels = marketData.GetFilteredSupportResistanceLevels().Select(srl => new
                    {
                        price = srl.Price,
                        testCount = srl.TestCount,
                        totalVolume = srl.TotalVolume
                    }).ToList(),
                    lastWebSocketUpdate = lastUpdated
                };

                var candlesticksData = new
                {
                    minute = new
                    {
                        data = candlestickService.RetrieveHistoricalCandlesticksAsync(marketTicker, "minute")
                            .Where(x => x.Date >= DateTime.UtcNow.AddHours(-3))
                            .Select(c => new
                            {
                                x = ((DateTimeOffset)c.Date).ToUnixTimeMilliseconds(),
                                o = (c.BidOpen + c.AskOpen) / 2.0,
                                h = (c.BidHigh + c.AskHigh) / 2.0,
                                l = (c.BidLow + c.AskLow) / 2.0,
                                c = (c.BidClose + c.AskClose) / 2.0,
                                v = c.Volume
                            }).ToList()
                    },
                    hour = new
                    {
                        data = candlestickService.RetrieveHistoricalCandlesticksAsync(marketTicker, "hour")
                            .Where(x => x.Date >= DateTime.UtcNow.AddMonths(-1))
                            .Select(c => new
                            {
                                x = ((DateTimeOffset)c.Date).ToUnixTimeMilliseconds(),
                                o = (c.BidOpen + c.AskOpen) / 2.0,
                                h = (c.BidHigh + c.AskHigh) / 2.0,
                                l = (c.BidLow + c.AskLow) / 2.0,
                                c = (c.BidClose + c.AskClose) / 2.0,
                                v = c.Volume
                            }).ToList()
                    },
                    day = new
                    {
                        data = candlestickService.RetrieveHistoricalCandlesticksAsync(marketTicker, "day")
                            .Select(c => new
                            {
                                x = ((DateTimeOffset)c.Date).ToUnixTimeMilliseconds(),
                                o = (c.BidOpen + c.AskOpen) / 2.0,
                                h = (c.BidHigh + c.AskHigh) / 2.0,
                                l = (c.BidLow + c.AskLow) / 2.0,
                                c = (c.BidClose + c.AskClose) / 2.0,
                                v = c.Volume
                            }).ToList()
                    }
                };

                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("HistoricalData", new
                {
                    candlesticks = candlesticksData,
                    marketTicker,
                    marketInfo,
                    priceData
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting historical data for {MarketTicker}", marketTicker);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastHistoricalDataAsync), stopwatch.ElapsedMilliseconds);
            }
        }


        private async Task BroadcastExchangeStatusAsync(string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastExchangeStatusAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping exchange status broadcast.");
                return;
            }

            _logger.LogDebug("Broadcasting Exchange Status Data to {connectionId}", connectionId);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                bool exchangeStatus = marketDataService.GetExchangeStatus();
                bool tradingStatus = marketDataService.GetTradingStatus();
                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("UpdateExchangeStatus", new { exchangeStatus, tradingStatus }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting exchange and trading status");
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastExchangeStatusAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private void BroadcastMarketList(object sender, EventArgs e)
        {
            Task.Run(() => BroadcastMarketListAsync()).GetAwaiter().GetResult();
        }

        private async Task BroadcastMarketListAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastMarketListAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!ChartHub.HasConnectedClients())
                {
                    _logger.LogDebug("No clients connected, skipping market list broadcast.");
                    return;
                }
                var markets = await GetWatchedMarketsAsync();
                _logger.LogDebug("Broadcasting Market List");
                await _hubContext.Clients.All.SendAsync("UpdateMarketList", markets, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastMarketListAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting market list");
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastMarketListAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        public async Task BroadcastMarketListAsync(string connectionId)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount($"{nameof(BroadcastMarketListAsync)}_WithConnectionId");
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var markets = await GetWatchedMarketsAsync();
                _logger.LogDebug("Broadcasting Market List to {ConnectionId}", connectionId);
                await _hubContext.Clients.Client(connectionId).SendAsync("UpdateMarketList", markets, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("BroadcastMarketListAsync was cancelled for {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting market list to {ConnectionId}", connectionId);
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime($"{nameof(BroadcastMarketListAsync)}_WithConnectionId", stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastBalanceAsync(string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastBalanceAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping balance broadcast.");
                return;
            }

            _logger.LogDebug("Broadcasting Balance to {connectionId}", connectionId);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                double balance = marketDataService.GetAccountBalance();
                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("UpdateBalance", balance, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting balance");
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastBalanceAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastPortfolioValueAsync(string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastPortfolioValueAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping portfolio value broadcast.");
                return;
            }

            _logger.LogDebug("Broadcasting Positions Data to {connectionId}", connectionId);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                double positionsValue = marketDataService.GetPortfolioValue();
                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("UpdatePositionsValue", positionsValue, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting positions value");
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastPortfolioValueAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastLastWebSocketUpdateAsync(string? connectionId = null)
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastLastWebSocketUpdateAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (connectionId == null && !ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping last WebSocket update broadcast.");
                return;
            }

            _logger.LogDebug("Broadcasting Last Update to {connectionId}", connectionId);
            try
            {
                var marketDataService = _serviceFactory.GetMarketDataService();
                var lastUpdate = marketDataService.GetLatestWebSocketTimestamp();
                string timestampString = lastUpdate == DateTime.MinValue ? null : lastUpdate.ToString("yyyy-MM-ddTHH:mm:ss");
                _logger.LogDebug("Last WebSocket update timestamp: {Timestamp}", timestampString ?? "null");

                var target = connectionId != null ? _hubContext.Clients.Client(connectionId) : _hubContext.Clients.All;
                await target.SendAsync("UpdateLastWebSocketUpdate", timestampString, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting last WebSocket update");
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastLastWebSocketUpdateAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task BroadcastCheckInAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            IncrementBroadcastCount(nameof(BroadcastCheckInAsync));
            var cancellationToken = _statusTracker.GetCancellationToken();
            cancellationToken.ThrowIfCancellationRequested();
            if (!ChartHub.HasConnectedClients())
            {
                _logger.LogDebug("No clients connected, skipping CheckIn broadcast.");
                return;
            }

            _logger.LogDebug("Broadcasting CheckIn");
            try
            {
                var markets = await GetWatchedMarketsAsync();
                var errorHandler = _serviceFactory.GetBacklashErrorHandler();
                var lastSnapshot = errorHandler.LastSuccessfulSnapshot;
                var lastErrorDate = errorHandler.LastErrorDate;

                // Get brain instance name from configuration
                var brainInstanceName = _executionConfig.BrainInstance ?? "Unknown";

                var checkInData = new
                {
                    BrainInstanceName = brainInstanceName,
                    Markets = markets,
                    ErrorCount = errorHandler.ErrorCount,
                    LastSnapshot = lastSnapshot == DateTime.MinValue ? (DateTime?)null : lastSnapshot,
                    LastErrorDate = lastErrorDate == DateTime.MinValue ? (DateTime?)null : lastErrorDate,
                    Timestamp = DateTime.UtcNow
                };

                await _hubContext.Clients.All.SendAsync("CheckIn", checkInData, cancellationToken);
                _logger.LogDebug("CheckIn broadcasted from {BrainInstanceName} with {MarketCount} markets, ErrorCount: {ErrorCount}, LastSnapshot: {LastSnapshot}, LastErrorDate: {LastErrorDate}",
                    brainInstanceName, markets.Count, errorHandler.ErrorCount, lastSnapshot, lastErrorDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting CheckIn");
            }
            finally
            {
                stopwatch.Stop();
                RecordExecutionTime(nameof(BroadcastCheckInAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        private void OnOrderbookDataUpdated(object sender, string marketTicker)
        {
            // No longer automatically broadcasting on data updates
            // Data will only be sent when overseer requests refresh
            _logger.LogDebug("Orderbook data updated for {MarketTicker}, but not broadcasting automatically", marketTicker);
        }

        private void OnAccountBalanceUpdated(object sender, string marketTicker)
        {
            // No longer automatically broadcasting on data updates
            // Data will only be sent when overseer requests refresh
            _logger.LogDebug("Account balance updated for {MarketTicker}, but not broadcasting automatically", marketTicker);
        }

        private void OnPositionDataUpdated(object sender, string marketTicker)
        {
            // No longer automatically broadcasting on data updates
            // Data will only be sent when overseer requests refresh
            _logger.LogDebug("Position data updated for {MarketTicker}, but not broadcasting automatically", marketTicker);
        }

        private void OnMarketDataUpdated(object sender, string marketTicker)
        {
            // No longer automatically broadcasting on data updates
            // Data will only be sent when overseer requests refresh
            _logger.LogDebug("Market data updated for {MarketTicker}, but not broadcasting automatically", marketTicker);
        }

        private void OnTickerAdded(object sender, string marketTicker)
        {
            // No longer automatically broadcasting on data updates
            // Data will only be sent when overseer requests refresh
            _logger.LogDebug("Ticker added for {MarketTicker}, but not broadcasting automatically", marketTicker);
        }



    }
}
