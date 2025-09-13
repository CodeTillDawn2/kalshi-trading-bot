using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using TradingStrategies.Strategies;
using TradingStrategies.Strategies.Strategies.Strats;
using TradingStrategies.Strategies.Strats;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Helpers
{
    /// <summary>
    /// Centralized configuration for all trading strategies and their parameter sets.
    /// This class serves as the single source of truth for strategy definitions.
    /// </summary>
    public static class StrategyConfiguration
    {
        /// <summary>
        /// Represents a trading strategy with its name and async factory method
        /// </summary>
        public class StrategyDefinition
        {
            public string Name { get; set; }
            public Func<string, Task<Dictionary<MarketType, List<Strategy>>>> Factory { get; set; }

            public StrategyDefinition(string name, Func<string, Task<Dictionary<MarketType, List<Strategy>>>> factory)
            {
                Name = name;
                Factory = factory;
            }
        }

        /// <summary>
        /// Dictionary mapping strategy names to their definitions
        /// </summary>
        private static readonly Dictionary<string, StrategyDefinition> _strategies = new()
        {
            ["Bollinger"] = new StrategyDefinition("Bollinger", GetBollingerBreakoutStrategy),
            ["Breakout2"] = new StrategyDefinition("Breakout2", GetBreakoutStrategy),
            ["Nothing"] = new StrategyDefinition("Nothing", GetNothingEverHappensStrategy),
            ["FlowMo"] = new StrategyDefinition("FlowMo", GetFlowMomentumStrategy),
            ["MLShared"] = new StrategyDefinition("MLShared", GetMLSharedStrategy),
            ["TryAgain"] = new StrategyDefinition("TryAgain", GetTryAgainStrategy),
            ["SloMo"] = new StrategyDefinition("SloMo", GetSlopeMomentumStrategy),
            ["Momentum"] = new StrategyDefinition("Momentum", GetMomentumTradingStrategy)
        };

        /// <summary>
        /// Performance metrics for strategy instantiation
        /// </summary>
        private static readonly ConcurrentDictionary<string, List<TimeSpan>> _performanceMetrics = new();

        /// <summary>
        /// Gets all available strategy names
        /// </summary>
        public static IEnumerable<string> GetStrategyNames()
        {
            return _strategies.Keys.OrderBy(name => name);
        }

        /// <summary>
        /// Gets the strategy definition for a given strategy name
        /// </summary>
        public static StrategyDefinition GetStrategyDefinition(string strategyName)
        {
            if (_strategies.TryGetValue(strategyName, out var definition))
            {
                return definition;
            }
            throw new ArgumentException($"Unknown strategy '{strategyName}'", nameof(strategyName));
        }

        /// <summary>
        /// Gets the async factory method for a given strategy name
        /// </summary>
        public static Func<string, Task<Dictionary<MarketType, List<Strategy>>>> GetStrategyFactory(string strategyName)
        {
            return GetStrategyDefinition(strategyName).Factory;
        }

        /// <summary>
        /// Gets the performance metrics for strategy instantiation
        /// </summary>
        public static IReadOnlyDictionary<string, List<TimeSpan>> GetPerformanceMetrics()
        {
            return _performanceMetrics;
        }

        /// <summary>
        /// Gets all strategy names that have parameter sets
        /// </summary>
        public static IEnumerable<string> GetStrategiesWithParameterSets()
        {
            return _strategies.Keys;
        }

        /// <summary>
        /// Creates a strategy configuration for the Bollinger Breakout strategy with the specified weight parameters.
        /// This method retrieves the parameter set for the given weight name from the centralized parameter storage
        /// and constructs a Strategy instance containing a BollingerBreakout strategy implementation.
        /// The strategy is then mapped to appropriate market types for execution.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the Bollinger Breakout strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetBollingerBreakoutStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var defaultParams = StrategySelectionHelper.BollingerParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new BollingerBreakout(mlParams: defaultParams.Parameters) }
            );
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("Bollinger", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Creates a strategy configuration for the Breakout strategy with the specified weight parameters.
        /// This method retrieves the parameter set for the given weight name and constructs a Strategy instance
        /// containing a Breakout2 strategy implementation for detecting price breakouts with velocity confirmation.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the Breakout strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetBreakoutStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var defaultParams = StrategySelectionHelper.BreakoutParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new Breakout2(mlParams: defaultParams.Parameters) }
            );
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("Breakout2", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Creates a strategy configuration for the Nothing Ever Happens strategy with the specified weight parameters.
        /// This conservative strategy employs high thresholds to minimize trading frequency and emphasizes stability
        /// over aggressive trading in volatile conditions.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the Nothing Ever Happens strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetNothingEverHappensStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var defaultParams = StrategySelectionHelper.NothingEverHappensParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new NothingEverHappensStrat(mlParams: defaultParams.Parameters) }
            );
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("Nothing", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Creates a strategy configuration for the Flow Momentum strategy with the specified weight parameters.
        /// This strategy focuses on sustained flow patterns with technical confirmations, gating decisions
        /// on normalized flow, consecutive bars, trade shares, and RSI flattening for position exits.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the Flow Momentum strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetFlowMomentumStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var defaultParams = StrategySelectionHelper.FlowMomentumParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new FlowMomentumStrat(mlParams: defaultParams.Parameters) }
            );
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("FlowMo", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Creates a strategy configuration for the ML Shared strategy with the specified weight parameters.
        /// This machine learning-based strategy uses shared parameters for Long/Short signals and incorporates
        /// an online logistic regression model for entry prediction in trading scenarios.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the ML Shared strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetMLSharedStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var selectedParams = MLEntrySeekerShared.MLSharedParameterSets
                .FirstOrDefault(x => x.Name == weightName);

            var mlStrat = new MLEntrySeekerShared(
                name: $"MLShared_{selectedParams.Name}",
                evaluationOnly: false,
                weight: 1.0,
                p: selectedParams.Parameters);

            var strat = new Strategy(selectedParams.Name, new List<Strat> { mlStrat });
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("MLShared", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Creates a strategy configuration for the Try Again strategy with the specified weight parameters.
        /// This adaptive retry-based strategy adjusts parameters based on previous failures to improve
        /// entry timing through learning from unsuccessful attempts.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the Try Again strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetTryAgainStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var defaultParams = TryAgainStrat.TryAgainStratParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new TryAgainStrat(mlParams: defaultParams.Parameters) }
            );
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("TryAgain", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Creates a strategy configuration for the Slope Momentum strategy with the specified weight parameters.
        /// This strategy analyzes EMA and velocity slopes for trend confirmation, focusing on detecting
        /// trending conditions through slope analysis of key indicators.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the Slope Momentum strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetSlopeMomentumStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var defaultParams = SlopeMomentumStrat.SlopeMomentumParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new SlopeMomentumStrat(mlParams: defaultParams.Parameters) }
            );
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("SloMo", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Creates a strategy configuration for the Momentum Trading strategy with the specified weight parameters.
        /// This strategy uses RSI and velocity indicators for entry and exit signals, detecting momentum
        /// through RSI divergence and velocity changes to capitalize on trending market conditions.
        /// </summary>
        /// <param name="weightName">The name of the parameter set to use for configuring the Momentum Trading strategy.</param>
        /// <returns>A dictionary mapping market types to lists of strategies configured for those market conditions.</returns>
        private static async Task<Dictionary<MarketType, List<Strategy>>> GetMomentumTradingStrategy(string weightName)
        {
            var stopwatch = Stopwatch.StartNew();
            var defaultParams = StrategySelectionHelper.MomentumTradingParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new MomentumTrading(mlParams: defaultParams.Parameters) }
            );
            var result = CreateMarketStrategyMapping(strat);
            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("Momentum", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);
            return result;
        }

        /// <summary>
        /// Defines the Low Liquidity strategy as a static field for use in low liquidity market conditions.
        /// This strategy always returns an Exit action while calculating comprehensive liquidity metrics
        /// including spread, depth, volume, imbalance, and slippage scores for risk assessment and decision logging.
        /// </summary>
        private static readonly Strategy LowLiquidityStrategy = new Strategy(
            "LowLiquidity",
            new List<Strat> { new LowLiquidityExitExec() }
        );

        /// <summary>
        /// Creates a standardized market-to-strategy mapping dictionary for the provided strategy.
        /// This method establishes a consistent pattern where the LowLiquidity strategy is applied to
        /// low liquidity market conditions, while the provided strategy is applied to all other market types.
        /// This ensures appropriate strategy selection based on market characteristics for optimal trading performance.
        /// </summary>
        /// <param name="strategy">The primary strategy to be applied to most market types.</param>
        /// <returns>A dictionary mapping each market type to its appropriate strategy configuration.</returns>
        private static Dictionary<MarketType, List<Strategy>> CreateMarketStrategyMapping(Strategy strategy)
        {
            var strategiesDict = new Dictionary<MarketType, List<Strategy>>();

            // LowLiquidity: Strategies for thin markets with wide spreads
            strategiesDict.Add(MarketType.LowLiquidity, new List<Strategy> { LowLiquidityStrategy });

            // Apply the provided strategy to all other market types
            var strategyMarkets = new[]
            {
                MarketType.Bouncing,
                MarketType.Trending,
                MarketType.Volatile,
                MarketType.Stagnant,
                MarketType.HighUncertainty,
                MarketType.TrendingActive,
                MarketType.VolatileEvents,
                MarketType.StableMacro,
                MarketType.SeasonalVolatile,
                MarketType.NewsDriven,
                MarketType.TechTrend,
                MarketType.FinancialMomentum,
                MarketType.ImminentCloseVolatile,
                MarketType.FarStable,
                MarketType.EventDriven
            };

            foreach (var marketType in strategyMarkets)
            {
                strategiesDict.Add(marketType, new List<Strategy> { strategy });
            }

            return strategiesDict;
        }
    }
}