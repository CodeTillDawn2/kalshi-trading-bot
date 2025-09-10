using System.Collections.Generic;
using System.Linq;
using TradingStrategies.Strategies;
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
        /// Represents a trading strategy with its name and factory method
        /// </summary>
        public class StrategyDefinition
        {
            public string Name { get; set; }
            public Func<string, Dictionary<MarketType, List<Strategy>>> Factory { get; set; }

            public StrategyDefinition(string name, Func<string, Dictionary<MarketType, List<Strategy>>> factory)
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
        /// Gets the factory method for a given strategy name
        /// </summary>
        public static Func<string, Dictionary<MarketType, List<Strategy>>> GetStrategyFactory(string strategyName)
        {
            return GetStrategyDefinition(strategyName).Factory;
        }

        /// <summary>
        /// Gets all strategy names that have parameter sets
        /// </summary>
        public static IEnumerable<string> GetStrategiesWithParameterSets()
        {
            return _strategies.Keys;
        }

        // Factory methods for each strategy
        private static Dictionary<MarketType, List<Strategy>> GetBollingerBreakoutStrategy(string weightName)
        {
            var defaultParams = StrategySelectionHelper.BollingerParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new BollingerBreakout(mlParams: defaultParams.Parameters) }
            );
            return CreateMarketStrategyMapping(strat);
        }

        private static Dictionary<MarketType, List<Strategy>> GetBreakoutStrategy(string weightName)
        {
            var defaultParams = StrategySelectionHelper.BreakoutParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new Breakout2(mlParams: defaultParams.Parameters) }
            );
            return CreateMarketStrategyMapping(strat);
        }

        private static Dictionary<MarketType, List<Strategy>> GetNothingEverHappensStrategy(string weightName)
        {
            var defaultParams = StrategySelectionHelper.NothingEverHappensParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new NothingEverHappensStrat(mlParams: defaultParams.Parameters) }
            );
            return CreateMarketStrategyMapping(strat);
        }

        private static Dictionary<MarketType, List<Strategy>> GetFlowMomentumStrategy(string weightName)
        {
            var defaultParams = StrategySelectionHelper.FlowMomentumParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new FlowMomentumStrat(mlParams: defaultParams.Parameters) }
            );
            return CreateMarketStrategyMapping(strat);
        }

        private static Dictionary<MarketType, List<Strategy>> GetMLSharedStrategy(string weightName)
        {
            var selectedParams = MLEntrySeekerShared.MLSharedParameterSets
                .FirstOrDefault(x => x.Name == weightName);

            var mlStrat = new MLEntrySeekerShared(
                name: $"MLShared_{selectedParams.Name}",
                evaluationOnly: false,
                weight: 1.0,
                p: selectedParams.Parameters);

            var strat = new Strategy(selectedParams.Name, new List<Strat> { mlStrat });
            return CreateMarketStrategyMapping(strat);
        }

        private static Dictionary<MarketType, List<Strategy>> GetTryAgainStrategy(string weightName)
        {
            var defaultParams = TryAgainStrat.TryAgainStratParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new TryAgainStrat(mlParams: defaultParams.Parameters) }
            );
            return CreateMarketStrategyMapping(strat);
        }

        private static Dictionary<MarketType, List<Strategy>> GetSlopeMomentumStrategy(string weightName)
        {
            var defaultParams = SlopeMomentumStrat.SlopeMomentumParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new SlopeMomentumStrat(mlParams: defaultParams.Parameters) }
            );
            return CreateMarketStrategyMapping(strat);
        }

        private static Dictionary<MarketType, List<Strategy>> GetMomentumTradingStrategy(string weightName)
        {
            var defaultParams = StrategySelectionHelper.MomentumTradingParameterSets
                .Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new MomentumTrading(mlParams: defaultParams.Parameters) }
            );
            return CreateMarketStrategyMapping(strat);
        }

        // Define the Low Liquidity strategy as a static field
        private static readonly Strategy LowLiquidityStrategy = new Strategy(
            "LowLiquidity",
            new List<Strat> { new LowLiquidityExitExec() }
        );

        // Define the market-to-strategy mapping as a static method
        private static Dictionary<MarketType, List<Strategy>> CreateMarketStrategyMapping(Strategy bollingerStrategy)
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
                strategiesDict.Add(marketType, new List<Strategy> { bollingerStrategy });
            }

            return strategiesDict;
        }
    }
}