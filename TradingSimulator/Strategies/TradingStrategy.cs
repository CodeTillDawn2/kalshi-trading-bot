using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using TradingSimulator.Simulator;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Configuration;

namespace TradingSimulator.Strategies
{
    /// <summary>
    /// Generic trading strategy simulator that executes trading strategies against historical market snapshots.
    /// This class manages the complete simulation lifecycle including data loading, strategy execution,
    /// position tracking, and performance calculation. It supports multiple strategies running simultaneously
    /// and provides real-time progress updates through events.
    /// </summary>
    /// <typeparam name="T">The type of market data snapshot to process (typically MarketSnapshot).</typeparam>
    public class TradingStrategy<T>
    {
        /// <summary>
        /// Service for managing trading snapshot data loading and validation.
        /// </summary>
        private readonly ITradingSnapshotService _snapshotService;

        /// <summary>
        /// Helper for processing snapshot periods and grouping market data.
        /// </summary>
        private readonly ISnapshotPeriodHelper _snapshotPeriodHelper;

        /// <summary>
        /// Logger for recording simulation progress, errors, and important events.
        /// </summary>
        private readonly ILogger<TradingStrategy<T>> _logger;

        /// <summary>
        /// Configuration options for snapshot processing and validation.
        /// </summary>
        private readonly IOptions<SnapshotConfig> _snapshotOptions;

        /// <summary>
        /// Configuration options for trading parameters and behavior.
        /// </summary>
        private readonly IOptions<TradingConfig> _tradingOptions;

        /// <summary>
        /// Factory for creating service scopes for dependency injection.
        /// </summary>
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Database context for accessing market data and snapshots.
        /// </summary>
        private readonly IKalshiBotContext _dbContext;

        /// <summary>
        /// Collection of trading strategies to execute, each with a name and function delegate.
        /// </summary>
        private readonly List<(string Name, TradingStrategyFunc<T> Func)> _strategies;

        /// <summary>
        /// Tracks current positions for each market, including shares held and total cost basis.
        /// </summary>
        private Dictionary<string, (int Shares, double TotalCost)> _positionTracker;

        /// <summary>
        /// Accumulates total profit/loss across all markets and trades.
        /// </summary>
        private double _totalProfitLoss;

        /// <summary>
        /// Event raised to report simulation progress and status updates.
        /// </summary>
        public event Action<string> OnSimulationProgress;

        /// <summary>
        /// Event raised when price data is updated for a market.
        /// </summary>
        public event Action<string, DateTime, int, int> OnPriceUpdate;

        /// <summary>
        /// Event raised when profit/loss is updated for a market or overall.
        /// </summary>
        public event Action<string, double> OnProfitLossUpdate;

        /// <summary>
        /// Event raised when a trading decision is made (buy/sell/hold).
        /// </summary>
        public event Action<string, DateTime, double, TradingDecisionEnum> OnTradeDecision;

        /// <summary>
        /// Event raised when processing of a market is completed.
        /// </summary>
        public event Action<string> OnMarketProcessed;

        /// <summary>
        /// Initializes a new instance of the TradingStrategy class with required dependencies.
        /// </summary>
        /// <param name="snapshotService">Service for managing trading snapshot data.</param>
        /// <param name="snapshotPeriodHelper">Helper for processing snapshot periods.</param>
        /// <param name="logger">Logger for recording simulation events.</param>
        /// <param name="snapshotOptions">Configuration for snapshot processing.</param>
        /// <param name="tradingOptions">Configuration for trading parameters.</param>
        /// <param name="scopeFactory">Factory for creating service scopes.</param>
        /// <param name="dbContext">Database context for data access.</param>
        /// <param name="strategies">List of strategies to execute, each with name and function.</param>
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

        /// <summary>
        /// Resets the trading strategy state, clearing all positions and profit/loss tracking.
        /// This method initializes the simulator to a clean state for a new simulation run.
        /// </summary>
        public void Reset()
        {
            _positionTracker = new Dictionary<string, (int Shares, double TotalCost)>();
            _totalProfitLoss = 0;
            OnProfitLossUpdate?.Invoke("All", _totalProfitLoss);
            OnSimulationProgress?.Invoke("Reset P/L and positions");
        }

        /// <summary>
        /// Executes the trading strategy simulation across all available markets.
        /// This method loads market data, applies all configured strategies, makes trading decisions,
        /// tracks positions and profit/loss, and reports simulation results.
        /// </summary>
        /// <returns>A task representing the asynchronous simulation operation.</returns>
        public async Task RunStrategyTestAsync()
        {
            Reset();
            var strategyNames = string.Join(", ", _strategies.Select(s => s.Name));
            OnSimulationProgress?.Invoke($"Starting strategies: {strategyNames}");

            var markets = await _dbContext.GetMarketsWithSnapshots();
            markets = markets
                .OrderBy(m => m).ToHashSet();

            OnSimulationProgress?.Invoke($"Found {markets.Count} market tickers");

            if (!markets.Any())
            {
                OnSimulationProgress?.Invoke("No market tickers found.");
                _logger.LogWarning("No market tickers found.");
                return;
            }

            bool decisionMade = false;

            foreach (var market in markets)
            {
                OnSimulationProgress?.Invoke($"Processing market {market}");

                var snapshotGroups = await _dbContext.GetSnapshotGroups();


                var snapshotData = await _dbContext.GetSnapshots(market);
                var snapshots = snapshotData.ToList();

                if (snapshots.Count < 60)
                {
                    OnSimulationProgress?.Invoke($"Skipping {market}: only {snapshots.Count} snapshots (<60)");
                    continue;
                }
                var cacheSnapshotsDict = await _snapshotService.LoadManySnapshots(snapshots);
                if (!cacheSnapshotsDict.TryGetValue(market, out var cacheSnapshots) || cacheSnapshots?.Count < 2)
                {
                    OnSimulationProgress?.Invoke($"Failed to load snapshots for {market}");
                    continue;
                }

                var sortedSnapshots = cacheSnapshots.OrderBy(s => s.Timestamp).ToList();
                T? previousData = default;

                for (int i = 0; i < sortedSnapshots.Count(); i++)
                {
                    var snapshot = sortedSnapshots[i];
                    bool isLastSnapshot = i == sortedSnapshots.Count() - 1;

                    if (snapshot.MarketTicker != market)
                    {
                        OnSimulationProgress?.Invoke($"Invalid snapshot for {market} at {snapshot?.Timestamp}");
                        continue;
                    }

                    if (snapshot.Timestamp == DateTime.MinValue || snapshot.Timestamp.Ticks <= 0)
                    {
                        OnSimulationProgress?.Invoke($"Skipping invalid snapshot date for {market}");
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
                            OnSimulationProgress?.Invoke($"[{strategy.Name}] Processed for {market} at {snapshot.Timestamp}");
                        }

                        if (context.Decision.Signals.Any())
                        {
                            decisionMade = true;
                            var signals = string.Join(", ", context.Decision.Signals.Select(s => $"{s.Key}={s.Value}"));
                            OnSimulationProgress?.Invoke($"Decision signals for {market} at {snapshot.Timestamp}: {signals}");

                            var decision = DetermineTradingDecision(context.Decision);
                            ProcessTradingDecision(market, snapshot, decision, snapshot.Timestamp);
                        }
                    }

                    if (isLastSnapshot)
                    {
                        if (!_positionTracker.TryGetValue(market, out var pos))
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

                OnSimulationProgress?.Invoke($"Completed market {market}");
                OnMarketProcessed?.Invoke(market); // Single chart save per market
            }

            OnSimulationProgress?.Invoke(decisionMade
                ? $"Simulation completed: Decisions made. Final P/L: ${_totalProfitLoss:F2}"
                : $"Simulation completed: No decisions made. Final P/L: ${_totalProfitLoss:F2}");
            if (!decisionMade)
            {
                _logger.LogWarning("No decisions made during simulation.");
            }
        }

        /// <summary>
        /// Determines the trading decision based on strategy signals.
        /// This method interprets the signals from the trading decision to make a buy, sell, or hold decision.
        /// </summary>
        /// <param name="decision">The trading decision containing signals from strategies.</param>
        /// <returns>The determined trading decision enum value.</returns>
        private TradingDecisionEnum DetermineTradingDecision(TradingDecision decision)
        {
            if (decision.Signals.ContainsKey("PriceRise") && decision.Signals["PriceRise"] == 1.0)
                return TradingDecisionEnum.Buy;
            if (decision.Signals.ContainsKey("PriceDrop") && decision.Signals["PriceDrop"] == 1.0)
                return TradingDecisionEnum.Sell;
            return TradingDecisionEnum.Hold;
        }

        /// <summary>
        /// Processes a trading decision by executing the appropriate buy, sell, or hold action.
        /// This method handles position management, calculates costs/revenue, and updates tracking.
        /// </summary>
        /// <param name="market">The market ticker symbol.</param>
        /// <param name="snapshot">The current market snapshot data.</param>
        /// <param name="decision">The trading decision to execute.</param>
        /// <param name="timestamp">The timestamp of the decision.</param>
        /// <param name="worstPrice">Optional worst price for liquidation calculations.</param>
        private void ProcessTradingDecision(string market, MarketSnapshot snapshot, TradingDecisionEnum decision, DateTime timestamp, int? worstPrice = null)
        {
            if (!_positionTracker.TryGetValue(market, out var pos))
            {
                pos = (0, 0.0);
                _positionTracker[market] = pos;
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
                        _positionTracker[market] = (currentShares + sharesToBuy, currentTotalCost + cost);
                        OnSimulationProgress?.Invoke($"Bought {sharesToBuy} shares (long) for {market} at ${cost:F2}");
                        OnTradeDecision?.Invoke(market, timestamp, snapshot.BestYesAsk / 100.0, TradingDecisionEnum.Buy);
                    }
                    else
                    {
                        int sharesToClose = Math.Abs(currentShares);
                        double liquidationValue = CalculateLiquidationValue(sharesToClose, snapshot.GetNoBids(), worstPrice.HasValue ? 100 - worstPrice.Value : (int?)null);
                        double revenue = liquidationValue - currentTotalCost;
                        _totalProfitLoss += revenue;
                        _positionTracker[market] = (0, 0.0);
                        OnSimulationProgress?.Invoke($"Closed short position for {market}, revenue: ${revenue:F2}");
                        OnProfitLossUpdate?.Invoke(market, _totalProfitLoss);
                        double averagePrice = liquidationValue / sharesToClose;
                        OnTradeDecision?.Invoke(market, timestamp, averagePrice, TradingDecisionEnum.Buy);
                    }
                    break;

                case TradingDecisionEnum.Sell:
                    if (currentShares > 0)
                    {
                        double liquidationValue = CalculateLiquidationValue(currentShares, snapshot.GetYesBids(), worstPrice);
                        double revenue = liquidationValue - currentTotalCost;
                        _totalProfitLoss += revenue;
                        _positionTracker[market] = (0, 0.0);
                        OnSimulationProgress?.Invoke($"Closed long position for {market}, revenue: ${revenue:F2}");
                        OnProfitLossUpdate?.Invoke(market, _totalProfitLoss);
                        double averagePrice = liquidationValue / currentShares;
                        OnTradeDecision?.Invoke(market, timestamp, averagePrice, TradingDecisionEnum.Sell);
                    }
                    else
                    {
                        int sharesToShort = CalculateSharesToBuy(betSize, snapshot.BestNoAsk);
                        double cost = sharesToShort * (snapshot.BestNoAsk / 100.0);
                        _positionTracker[market] = (currentShares - sharesToShort, currentTotalCost + cost);
                        OnSimulationProgress?.Invoke($"Shorted {sharesToShort} shares for {market} at ${cost:F2}");
                        OnTradeDecision?.Invoke(market, timestamp, snapshot.BestNoAsk / 100.0, TradingDecisionEnum.Sell);
                    }
                    break;

                case TradingDecisionEnum.Hold:
                    break;
            }
        }

        /// <summary>
        /// Calculates the liquidation value for selling shares based on order book bids.
        /// This method simulates selling shares by walking through the bid order book
        /// and calculating the total value received at available prices.
        /// </summary>
        /// <param name="sharesToSell">The number of shares to sell.</param>
        /// <param name="bids">The order book bids dictionary (price -> depth).</param>
        /// <param name="minPrice">Optional minimum price threshold for selling.</param>
        /// <returns>The total liquidation value in dollars.</returns>
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

        /// <summary>
        /// Calculates the number of shares to buy based on bet size and price.
        /// This method determines how many shares can be purchased with the specified bet size
        /// at the given price, ensuring integer share quantities.
        /// </summary>
        /// <param name="betSize">The dollar amount to invest.</param>
        /// <param name="priceInCents">The price per share in cents.</param>
        /// <returns>The number of shares to buy.</returns>
        private int CalculateSharesToBuy(double betSize, int priceInCents)
        {
            if (priceInCents <= 0) return 0;
            double priceInDollars = priceInCents / 100.0;
            return (int)(betSize / priceInDollars);
        }
    }
}
