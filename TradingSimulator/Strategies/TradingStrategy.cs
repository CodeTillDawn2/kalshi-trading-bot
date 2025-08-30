using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using TradingSimulator.Simulator;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;

namespace TradingSimulator.Strategies
{
    public class TradingStrategy<T>
    {
        private readonly ITradingSnapshotService _snapshotService;
        private readonly ISnapshotPeriodHelper _snapshotPeriodHelper;
        private readonly ILogger<TradingStrategy<T>> _logger;
        private readonly IOptions<SnapshotConfig> _snapshotOptions;
        private readonly IOptions<TradingConfig> _tradingOptions;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IKalshiBotContext _dbContext;
        private readonly List<(string Name, TradingStrategyFunc<T> Func)> _strategies;
        private Dictionary<string, (int Shares, double TotalCost)> _marketPositions;
        private double _totalRevenue;

        public event Action<string> OnTestProgress;
        public event Action<string, DateTime, int, int> OnPriceUpdate;
        public event Action<string, double> OnProfitLossUpdate;
        public event Action<string, DateTime, double, TradingDecisionEnum> OnTradeDecision;
        public event Action<string> OnMarketProcessed;

        public TradingStrategy(
            ITradingSnapshotService snapshotService,
            ISnapshotPeriodHelper snapshotPeriodHelper,
            ILogger<TradingStrategy<T>> logger,
            IOptions<SnapshotConfig> snapshotOptions,
            IOptions<TradingConfig> tradingOptions,
            IServiceScopeFactory scopeFactory,
            IKalshiBotContext dbContext,
            List<(string Name, TradingStrategyFunc<T> Func)> strategies)
        {
            _snapshotService = snapshotService;
            _logger = logger;
            _snapshotPeriodHelper = snapshotPeriodHelper;
            _snapshotOptions = snapshotOptions;
            _tradingOptions = tradingOptions;
            _scopeFactory = scopeFactory;
            _dbContext = dbContext;
            _strategies = strategies;
            Reset();
        }

        public void Reset()
        {
            _marketPositions = new Dictionary<string, (int Shares, double TotalCost)>();
            _totalRevenue = 0;
            OnProfitLossUpdate?.Invoke("All", _totalRevenue);
            OnTestProgress?.Invoke("Reset P/L and positions");
        }

        public async Task RunStrategyTestAsync()
        {
            Reset();
            var strategyNames = string.Join(", ", _strategies.Select(s => s.Name));
            OnTestProgress?.Invoke($"Starting strategies: {strategyNames}");

            var markets = await _dbContext.GetMarketsWithSnapshots();
            markets = markets
                .OrderBy(m => m).ToHashSet();

            OnTestProgress?.Invoke($"Found {markets.Count} market tickers");

            if (!markets.Any())
            {
                OnTestProgress?.Invoke("No market tickers found.");
                Assert.Inconclusive("No market tickers found.");
                return;
            }

            bool decisionMade = false;

            foreach (var market in markets)
            {
                OnTestProgress?.Invoke($"Processing market {market}");

                var snapshotGroups = await _dbContext.GetSnapshotGroups();


                var snapshotData = await _dbContext.GetSnapshots(market);
                var snapshots = snapshotData.ToList();

                if (snapshots.Count < 60)
                {
                    OnTestProgress?.Invoke($"Skipping {market}: only {snapshots.Count} snapshots (<60)");
                    continue;
                }
                var cacheSnapshotsDict = await _snapshotService.LoadManySnapshots(snapshots);
                if (!cacheSnapshotsDict.TryGetValue(market, out var cacheSnapshots) || cacheSnapshots?.Count < 2)
                {
                    OnTestProgress?.Invoke($"Failed to load snapshots for {market}");
                    continue;
                }

                var sortedSnapshots = cacheSnapshots.OrderBy(s => s.Timestamp).ToList();
                T previousData = default;

                for (int i = 0; i < sortedSnapshots.Count(); i++)
                {
                    var snapshot = sortedSnapshots[i];
                    bool isLastSnapshot = i == sortedSnapshots.Count() - 1;

                    if (snapshot.MarketTicker != market)
                    {
                        OnTestProgress?.Invoke($"Invalid snapshot for {market} at {snapshot?.Timestamp}");
                        continue;
                    }

                    if (snapshot.Timestamp == DateTime.MinValue || snapshot.Timestamp.Ticks <= 0)
                    {
                        OnTestProgress?.Invoke($"Skipping invalid snapshot date for {market}");
                        continue;
                    }

                    var currentData = (T)(object)snapshot;
                    var bid = snapshot.BestYesBid;
                    var ask = snapshot.BestYesAsk;

                    OnPriceUpdate?.Invoke(market, snapshot.Timestamp, bid, ask);

                    if (previousData != null)
                    {
                        var context = new TradingContext();
                        foreach (var strategy in _strategies)
                        {
                            strategy.Func(currentData, previousData, _snapshotOptions.Value, context);
                            OnTestProgress?.Invoke($"[{strategy.Name}] Processed for {market} at {snapshot.Timestamp}");
                        }

                        if (context.Decision.Signals.Any())
                        {
                            decisionMade = true;
                            var signals = string.Join(", ", context.Decision.Signals.Select(s => $"{s.Key}={s.Value}"));
                            OnTestProgress?.Invoke($"Decision signals for {market} at {snapshot.Timestamp}: {signals}");

                            var decision = DetermineTradingDecision(context.Decision);
                            ProcessTradingDecision(market, snapshot, decision, snapshot.Timestamp);
                        }
                    }

                    if (isLastSnapshot)
                    {
                        if (!_marketPositions.TryGetValue(market, out var pos))
                        {
                            pos = (0, 0.0);
                        }
                        if (pos.Shares != 0)
                        {
                            var closeDecision = pos.Shares > 0 ? TradingDecisionEnum.Sell : TradingDecisionEnum.Buy;
                            ProcessTradingDecision(market, snapshot, closeDecision, snapshot.Timestamp);
                        }
                    }

                    previousData = currentData;
                }

                OnTestProgress?.Invoke($"Completed market {market}");
                OnMarketProcessed?.Invoke(market); // Single chart save per market
            }

            OnTestProgress?.Invoke(decisionMade
                ? $"Test passed: Decisions made. Final P/L: ${_totalRevenue:F2}"
                : $"Test failed: No decisions made. Final P/L: ${_totalRevenue:F2}");
            Assert.That(decisionMade, Is.True, "No decisions made.");
        }

        private TradingDecisionEnum DetermineTradingDecision(TradingDecision decision)
        {
            if (decision.Signals.ContainsKey("PriceRise") && decision.Signals["PriceRise"] == 1.0)
                return TradingDecisionEnum.Buy;
            if (decision.Signals.ContainsKey("PriceDrop") && decision.Signals["PriceDrop"] == 1.0)
                return TradingDecisionEnum.Sell;
            return TradingDecisionEnum.Hold;
        }

        private void ProcessTradingDecision(string market, MarketSnapshot snapshot, TradingDecisionEnum decision, DateTime timestamp, int? worstPrice = null)
        {
            if (!_marketPositions.TryGetValue(market, out var pos))
            {
                pos = (0, 0.0);
                _marketPositions[market] = pos;
            }

            int currentShares = pos.Shares;
            double currentTotalCost = pos.TotalCost;
            const double betSize = 10.0;

            switch (decision)
            {
                case TradingDecisionEnum.Buy:
                    if (currentShares >= 0)
                    {
                        int sharesToBuy = CalculateSharesToBuy(betSize, snapshot.BestYesAsk);
                        double cost = sharesToBuy * (snapshot.BestYesAsk / 100.0);
                        _marketPositions[market] = (currentShares + sharesToBuy, currentTotalCost + cost);
                        OnTestProgress?.Invoke($"Bought {sharesToBuy} shares (long) for {market} at ${cost:F2}");
                        OnTradeDecision?.Invoke(market, timestamp, snapshot.BestYesAsk / 100.0, TradingDecisionEnum.Buy);
                    }
                    else
                    {
                        int sharesToClose = Math.Abs(currentShares);
                        double liquidationValue = CalculateLiquidationValue(sharesToClose, snapshot.GetNoBids(), worstPrice.HasValue ? 100 - worstPrice.Value : (int?)null);
                        double revenue = liquidationValue - currentTotalCost;
                        _totalRevenue += revenue;
                        _marketPositions[market] = (0, 0.0);
                        OnTestProgress?.Invoke($"Closed short position for {market}, revenue: ${revenue:F2}");
                        OnProfitLossUpdate?.Invoke(market, _totalRevenue);
                        double averagePrice = liquidationValue / sharesToClose;
                        OnTradeDecision?.Invoke(market, timestamp, averagePrice, TradingDecisionEnum.Buy);
                    }
                    break;

                case TradingDecisionEnum.Sell:
                    if (currentShares > 0)
                    {
                        double liquidationValue = CalculateLiquidationValue(currentShares, snapshot.GetYesBids(), worstPrice);
                        double revenue = liquidationValue - currentTotalCost;
                        _totalRevenue += revenue;
                        _marketPositions[market] = (0, 0.0);
                        OnTestProgress?.Invoke($"Closed long position for {market}, revenue: ${revenue:F2}");
                        OnProfitLossUpdate?.Invoke(market, _totalRevenue);
                        double averagePrice = liquidationValue / currentShares;
                        OnTradeDecision?.Invoke(market, timestamp, averagePrice, TradingDecisionEnum.Sell);
                    }
                    else
                    {
                        int sharesToShort = CalculateSharesToBuy(betSize, snapshot.BestNoAsk);
                        double cost = sharesToShort * (snapshot.BestNoAsk / 100.0);
                        _marketPositions[market] = (currentShares - sharesToShort, currentTotalCost + cost);
                        OnTestProgress?.Invoke($"Shorted {sharesToShort} shares for {market} at ${cost:F2}");
                        OnTradeDecision?.Invoke(market, timestamp, snapshot.BestNoAsk / 100.0, TradingDecisionEnum.Sell);
                    }
                    break;

                case TradingDecisionEnum.Hold:
                    break;
            }
        }

        private double CalculateLiquidationValue(int sharesToSell, Dictionary<int, int> bids, int? minPrice = null)
        {
            if (sharesToSell == 0) return 0.0;

            var sortedPrices = bids.Keys.Where(p => !minPrice.HasValue || p >= minPrice.Value).OrderByDescending(p => p).ToList();

            double value = 0.0;
            int remaining = sharesToSell;

            foreach (int price in sortedPrices)
            {
                int depth = bids[price];
                int take = Math.Min(remaining, depth);
                value += take * (price / 100.0);
                remaining -= take;
                if (remaining <= 0) break;
            }

            // If remaining > 0, unable to sell all at or above minPrice, value remains partial
            return value;
        }

        private int CalculateSharesToBuy(double betSize, int priceInCents)
        {
            if (priceInCents <= 0) return 0;
            double priceInDollars = priceInCents / 100.0;
            return (int)(betSize / priceInDollars);
        }
    }
}