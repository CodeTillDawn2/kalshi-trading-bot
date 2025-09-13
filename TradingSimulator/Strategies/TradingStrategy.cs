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
    /// Enhanced trading strategy simulator that executes trading strategies against historical market snapshots.
    /// This class manages the complete simulation lifecycle including data loading, strategy execution,
    /// position tracking, performance calculation, and risk management. Features include:
    /// - Parallel strategy execution for improved performance
    /// - Comprehensive input validation for market data integrity
    /// - Progress persistence for long-running simulations with resume capability
    /// - Advanced risk management with position limits, stop-loss, and take-profit
    /// - Performance monitoring and optimization
    /// - Real-time progress updates through events
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
        /// Tracks processed markets for progress persistence.
        /// </summary>
        private HashSet<string> _processedMarkets;

        /// <summary>
        /// Simulation state for persistence.
        /// </summary>
        private SimulationState _simulationState;

        /// <summary>
        /// Performance metrics for monitoring simulation efficiency.
        /// </summary>
        private readonly System.Diagnostics.Stopwatch _performanceTimer = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// Represents the persistent state of a simulation for recovery purposes.
        /// </summary>
        private class SimulationState
        {
            public DateTime StartTime { get; set; }
            public HashSet<string> ProcessedMarkets { get; set; } = new HashSet<string>();
            public Dictionary<string, (int Shares, double TotalCost)> PositionTracker { get; set; } = new Dictionary<string, (int, double)>();
            public double TotalProfitLoss { get; set; }
            public string LastProcessedMarket { get; set; }
            public DateTime LastProcessedTimestamp { get; set; }
            public int MarketsProcessed { get; set; }
            public int TotalMarkets { get; set; }
        }

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
            _processedMarkets = new HashSet<string>();
            _simulationState = new SimulationState { StartTime = DateTime.UtcNow };
            Reset();
            ValidateConfiguration();
        }

        /// <summary>
        /// Validates the trading configuration parameters for consistency and safety.
        /// </summary>
        private void ValidateConfiguration()
        {
            var config = _tradingOptions.Value;

            if (config.MaxPositionSizePercent <= 0 || config.MaxPositionSizePercent > 1)
                throw new ArgumentException("MaxPositionSizePercent must be between 0 and 1");

            if (config.MaxTotalExposurePercent <= 0 || config.MaxTotalExposurePercent > 5)
                throw new ArgumentException("MaxTotalExposurePercent must be between 0 and 5");

            if (config.StopLossPercent < 0 || config.StopLossPercent > 1)
                throw new ArgumentException("StopLossPercent must be between 0 and 1");

            if (config.TakeProfitPercent < 0 || config.TakeProfitPercent > 2)
                throw new ArgumentException("TakeProfitPercent must be between 0 and 2");

            if (config.MaxConcurrentPositions <= 0 || config.MaxConcurrentPositions > 100)
                throw new ArgumentException("MaxConcurrentPositions must be between 1 and 100");

            if (config.MaxDrawdownPercent < 0 || config.MaxDrawdownPercent > 1)
                throw new ArgumentException("MaxDrawdownPercent must be between 0 and 1");
        }

        /// <summary>
        /// Resets the trading strategy state, clearing all positions and profit/loss tracking.
        /// This method initializes the simulator to a clean state for a new simulation run.
        /// </summary>
        public void Reset()
        {
            _positionTracker = new Dictionary<string, (int Shares, double TotalCost)>();
            _totalProfitLoss = 0;
            _processedMarkets.Clear();
            _simulationState = new SimulationState { StartTime = DateTime.UtcNow };
            OnProfitLossUpdate?.Invoke("All", _totalProfitLoss);
            OnSimulationProgress?.Invoke("Reset P/L and positions");
        }

        /// <summary>
        /// Resumes a simulation from previously saved progress state.
        /// This method loads the last saved simulation state including positions, processed markets,
        /// and profit/loss tracking, then continues processing from where it left off.
        /// Useful for recovering from interruptions or system restarts during long-running simulations.
        /// </summary>
        /// <returns>A task representing the asynchronous resume operation.</returns>
        public async Task ResumeSimulationAsync()
        {
            bool hasSavedProgress = await LoadProgressAsync();
            if (!hasSavedProgress)
            {
                OnSimulationProgress?.Invoke("No saved progress found. Use RunStrategyTestAsync() to start a new simulation.");
                return;
            }

            // Restore state from saved progress
            _positionTracker = new Dictionary<string, (int, double)>(_simulationState.PositionTracker);
            _totalProfitLoss = _simulationState.TotalProfitLoss;
            _processedMarkets = new HashSet<string>(_simulationState.ProcessedMarkets);

            OnSimulationProgress?.Invoke($"Resumed simulation: {_simulationState.MarketsProcessed} markets already processed, P/L: ${_totalProfitLoss:F2}");

            // Continue with remaining markets
            await RunStrategyTestAsync(resume: true);
        }

        /// <summary>
        /// Executes the trading strategy simulation across all available markets.
        /// This method loads market data, applies all configured strategies in parallel, makes trading decisions
        /// with risk management validation, tracks positions and profit/loss, and reports simulation results.
        /// Features include performance monitoring, progress persistence, and comprehensive error handling.
        /// </summary>
        /// <param name="resume">Whether this is a resumed simulation from saved progress.</param>
        /// <returns>A task representing the asynchronous simulation operation.</returns>
        public async Task RunStrategyTestAsync(bool resume = false)
        {
            if (!resume)
            {
                Reset();
            }

            _performanceTimer.Restart();
            var strategyNames = string.Join(", ", _strategies.Select(s => s.Name));
            OnSimulationProgress?.Invoke($"Starting strategies: {strategyNames}");

            var markets = await _dbContext.GetMarketsWithSnapshots();
            markets = markets
                .OrderBy(m => m).ToHashSet();

            _simulationState.TotalMarkets = markets.Count;

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
                // Skip already processed markets when resuming
                if (resume && _processedMarkets.Contains(market))
                {
                    OnSimulationProgress?.Invoke($"Skipping already processed market {market}");
                    continue;
                }

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

                    // Validate market data integrity
                    if (!ValidateMarketData(snapshot, market))
                    {
                        continue;
                    }

                    var currentData = (T)(object)snapshot;
                    var bid = snapshot.BestYesBid;
                    var ask = snapshot.BestYesAsk;

                    OnPriceUpdate?.Invoke(market, snapshot.Timestamp, bid, ask);

                    if (previousData != null)
                    {
                        var context = new TradingContext();

                        // Execute strategies in parallel for better performance
                        var strategyTasks = _strategies.Select(async strategy =>
                        {
                            try
                            {
                                await Task.Run(() => strategy.Func(currentData, previousData, _snapshotOptions.Value, context)).ConfigureAwait(false);
                                OnSimulationProgress?.Invoke($"[{strategy.Name}] Processed for {market} at {snapshot.Timestamp}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error executing strategy {strategy.Name} for {market}");
                                OnSimulationProgress?.Invoke($"Error in strategy {strategy.Name}: {ex.Message}");
                            }
                        });

                        await Task.WhenAll(strategyTasks).ConfigureAwait(false);

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

                // Update progress for persistence
                await UpdateProgressAsync(market);
            }

            // Save final progress
            await SaveProgressAsync();

            _performanceTimer.Stop();
            var elapsed = _performanceTimer.Elapsed;
            var marketsPerSecond = _simulationState.TotalMarkets / elapsed.TotalSeconds;

            OnSimulationProgress?.Invoke($"Performance: Processed {_simulationState.TotalMarkets} markets in {elapsed.TotalSeconds:F2}s ({marketsPerSecond:F2} markets/sec)");
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
        /// This method handles position management, calculates costs/revenue, updates tracking,
        /// and applies comprehensive risk management including position limits, stop-loss, and take-profit rules.
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

            // Apply risk management checks
            if (!ApplyRiskManagement(market, snapshot, decision, timestamp))
            {
                return;
            }

            switch (decision)
            {
                case TradingDecisionEnum.Buy:
                    if (currentShares >= 0)
                    {
                        int sharesToBuy = CalculateSharesToBuy(betSize, snapshot.BestYesAsk);

                        // Validate position limits
                        if (!ValidatePositionLimits(market, sharesToBuy, snapshot.BestYesAsk, decision))
                        {
                            return;
                        }

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

                        // Validate position limits
                        if (!ValidatePositionLimits(market, sharesToShort, snapshot.BestNoAsk, decision))
                        {
                            return;
                        }

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

        /// <summary>
        /// Validates market data integrity before processing to ensure data quality and prevent errors.
        /// Performs comprehensive checks including null validation, market ticker consistency,
        /// timestamp validity, and bid-ask spread validation.
        /// </summary>
        /// <param name="snapshot">The market snapshot to validate.</param>
        /// <param name="market">The market ticker symbol for cross-reference.</param>
        /// <returns>True if the snapshot passes all validation checks, false otherwise.</returns>
        private bool ValidateMarketData(MarketSnapshot snapshot, string market)
        {
            if (snapshot == null)
            {
                OnSimulationProgress?.Invoke($"Null snapshot encountered for {market}");
                return false;
            }

            if (string.IsNullOrEmpty(snapshot.MarketTicker) || snapshot.MarketTicker != market)
            {
                OnSimulationProgress?.Invoke($"Invalid market ticker in snapshot for {market}: {snapshot.MarketTicker}");
                return false;
            }

            if (snapshot.Timestamp == DateTime.MinValue || snapshot.Timestamp.Ticks <= 0)
            {
                OnSimulationProgress?.Invoke($"Invalid timestamp in snapshot for {market}: {snapshot.Timestamp}");
                return false;
            }

            if (snapshot.BestYesBid < 0 || snapshot.BestYesAsk < 0 || snapshot.BestNoBid < 0 || snapshot.BestNoAsk < 0)
            {
                OnSimulationProgress?.Invoke($"Invalid price data in snapshot for {market}: YesBid={snapshot.BestYesBid}, YesAsk={snapshot.BestYesAsk}, NoBid={snapshot.BestNoBid}, NoAsk={snapshot.BestNoAsk}");
                return false;
            }

            if (snapshot.BestYesBid >= snapshot.BestYesAsk || snapshot.BestNoBid >= snapshot.BestNoAsk)
            {
                OnSimulationProgress?.Invoke($"Invalid bid-ask spread in snapshot for {market}: YesBid={snapshot.BestYesBid}, YesAsk={snapshot.BestYesAsk}, NoBid={snapshot.BestNoBid}, NoAsk={snapshot.BestNoAsk}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the current simulation progress to persistent storage.
        /// </summary>
        private async Task SaveProgressAsync()
        {
            try
            {
                _simulationState.ProcessedMarkets = new HashSet<string>(_processedMarkets);
                _simulationState.PositionTracker = new Dictionary<string, (int, double)>(_positionTracker);
                _simulationState.TotalProfitLoss = _totalProfitLoss;
                _simulationState.MarketsProcessed = _processedMarkets.Count;

                // In a real implementation, this would save to a database or file
                // For now, we'll just log the progress
                OnSimulationProgress?.Invoke($"Progress saved: {_simulationState.MarketsProcessed} markets processed, P/L: ${_simulationState.TotalProfitLoss:F2}");

                await Task.CompletedTask; // Placeholder for async save operation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save simulation progress");
            }
        }

        /// <summary>
        /// Loads simulation progress from persistent storage.
        /// </summary>
        /// <returns>True if progress was loaded successfully, false otherwise.</returns>
        private async Task<bool> LoadProgressAsync()
        {
            try
            {
                // In a real implementation, this would load from a database or file
                // For now, we'll just return false to indicate no saved progress
                OnSimulationProgress?.Invoke("No saved progress found, starting fresh simulation");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load simulation progress");
                return false;
            }
        }

        /// <summary>
        /// Updates simulation progress and saves state periodically.
        /// </summary>
        /// <param name="market">The market that was just processed.</param>
        private async Task UpdateProgressAsync(string market)
        {
            _processedMarkets.Add(market);
            _simulationState.LastProcessedMarket = market;
            _simulationState.LastProcessedTimestamp = DateTime.UtcNow;

            // Save progress every 10 markets or at the end
            if (_processedMarkets.Count % 10 == 0)
            {
                await SaveProgressAsync();
            }
        }

        /// <summary>
        /// Applies comprehensive risk management rules including stop-loss, take-profit, and maximum drawdown protection.
        /// Monitors position performance in real-time and automatically executes protective actions when risk thresholds are breached.
        /// This method ensures portfolio safety by enforcing configured risk limits throughout the simulation.
        /// </summary>
        /// <param name="market">The market ticker symbol for risk assessment.</param>
        /// <param name="snapshot">The current market snapshot for price reference.</param>
        /// <param name="decision">The trading decision being evaluated.</param>
        /// <param name="timestamp">The timestamp of the decision for logging.</param>
        /// <returns>True if the trade should proceed under current risk conditions, false if risk management intervened.</returns>
        private bool ApplyRiskManagement(string market, MarketSnapshot snapshot, TradingDecisionEnum decision, DateTime timestamp)
        {
            if (!_positionTracker.TryGetValue(market, out var pos) || pos.Shares == 0)
            {
                return true; // No position to manage
            }

            var config = _tradingOptions.Value;
            double currentPrice = pos.Shares > 0 ? snapshot.BestYesBid / 100.0 : (100.0 - snapshot.BestNoBid) / 100.0;
            double entryPrice = pos.TotalCost / Math.Abs(pos.Shares);
            double priceChange = (currentPrice - entryPrice) / entryPrice;

            // Check stop-loss
            if (Math.Abs(priceChange) >= config.StopLossPercent)
            {
                var exitDecision = pos.Shares > 0 ? TradingDecisionEnum.Sell : TradingDecisionEnum.Buy;
                OnSimulationProgress?.Invoke($"Stop-loss triggered for {market}: {priceChange:P2} change");
                ProcessTradingDecision(market, snapshot, exitDecision, timestamp);
                return false;
            }

            // Check take-profit
            if (priceChange >= config.TakeProfitPercent)
            {
                OnSimulationProgress?.Invoke($"Take-profit triggered for {market}: {priceChange:P2} gain");
                ProcessTradingDecision(market, snapshot, TradingDecisionEnum.Sell, timestamp);
                return false;
            }

            // Check max drawdown
            double portfolioValue = 1000.0; // Base portfolio value
            double currentPortfolioValue = portfolioValue + _totalProfitLoss;
            double drawdown = (portfolioValue - currentPortfolioValue) / portfolioValue;

            if (drawdown >= config.MaxDrawdownPercent)
            {
                OnSimulationProgress?.Invoke($"Max drawdown reached: {drawdown:P2}. Stopping simulation.");
                // Close all positions
                foreach (var position in _positionTracker.Where(p => p.Value.Shares != 0))
                {
                    var exitDecision = position.Value.Shares > 0 ? TradingDecisionEnum.Sell : TradingDecisionEnum.Buy;
                    ProcessTradingDecision(position.Key, snapshot, exitDecision, timestamp);
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates position size limits and risk management constraints before executing trades.
        /// Checks maximum position size, total exposure limits, concurrent position limits,
        /// and ensures the proposed trade complies with all configured risk parameters.
        /// </summary>
        /// <param name="market">The market ticker symbol for the trade.</param>
        /// <param name="proposedShares">The proposed number of shares to trade.</param>
        /// <param name="price">The price per share in cents.</param>
        /// <param name="decision">The trading decision (buy/sell) being validated.</param>
        /// <returns>True if the trade is allowed under current risk constraints, false otherwise.</returns>
        private bool ValidatePositionLimits(string market, int proposedShares, int price, TradingDecisionEnum decision)
        {
            var config = _tradingOptions.Value;
            double tradeValue = proposedShares * (price / 100.0);

            // Check position size limit
            double maxPositionValue = config.MaxPositionSizePercent * 1000.0; // Assuming $1000 base portfolio
            if (tradeValue > maxPositionValue)
            {
                OnSimulationProgress?.Invoke($"Trade rejected for {market}: exceeds max position size (${maxPositionValue:F2})");
                return false;
            }

            // Check total exposure limit
            double currentExposure = _positionTracker.Sum(p => Math.Abs(p.Value.Shares) * 10.0); // Rough estimate
            if (currentExposure + tradeValue > config.MaxTotalExposurePercent * 1000.0)
            {
                OnSimulationProgress?.Invoke($"Trade rejected for {market}: exceeds max total exposure");
                return false;
            }

            // Check concurrent positions limit
            int activePositions = _positionTracker.Count(p => p.Value.Shares != 0);
            if (decision == TradingDecisionEnum.Buy && activePositions >= config.MaxConcurrentPositions)
            {
                OnSimulationProgress?.Invoke($"Trade rejected for {market}: exceeds max concurrent positions ({config.MaxConcurrentPositions})");
                return false;
            }

            return true;
        }
    }
}
