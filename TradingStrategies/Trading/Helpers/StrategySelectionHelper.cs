using System;
using System.Collections.Generic;
using TradingStrategies.Strategies;
using TradingStrategies.Strategies.Strats;
using static SmokehouseInterfaces.Enums.StrategyEnums;
using static TradingStrategies.Strategies.Strats.BollingerBreakout;

namespace TradingStrategies.Trading.Helpers
{
    public class StrategySelectionHelper
    {
        // add at class scope
        private readonly Dictionary<string, Func<string, Dictionary<MarketType, List<Strategy>>>> _mappingFactories;


        public StrategySelectionHelper()
        {
            _mappingFactories = new()
            {
                ["Bollinger"] = GetBollingerBreakoutStrategy,
                ["Breakout2"] = GetBreakoutStrategy,
                ["Nothing"] = GetNothingEverHappensStrategy,
                ["FlowMo"] = GetFlowMomentumStrategy
                
            };
        }

        public IEnumerable<string> GetSetKeys() => _mappingFactories.Keys;
        public IEnumerable<string> GetWeightNames(string setKey) =>
            setKey switch
            {
                "Bollinger" => BollingerParameterSets.Select(x => x.Name),
                "Breakout2" => BreakoutParameterSets.Select(x => x.Name),
                "FlowMo" => FlowMomentumParameterSets.Select(x => x.Name),
                "Nothing" => NothingEverHappensParameterSets.Select(x => x.Name),
                _ => Enumerable.Empty<string>()
            };
        public Dictionary<MarketType, List<Strategy>> GetMapping(string setKey, string weightName)
        {
            if (!_mappingFactories.TryGetValue(setKey, out var factory))
                throw new ArgumentException($"Unknown set '{setKey}'", nameof(setKey));
            return factory(weightName);
        }
        public List<Dictionary<MarketType, List<Strategy>>> GetTrainingMappings(string setKey) =>
            setKey switch
            {
                "Bollinger" => GetBollingerBreakoutStrategiesForTraining(),
                "Breakout2" => GetBreakoutStrategiesForTraining(),
                "FlowMo" => GetFlowMomentumStrategiesForTraining(),
                "Nothing" => GetNothingEverHappensStrategiesForTraining(),
                _ => throw new ArgumentException($"Unknown set '{setKey}'", nameof(setKey))
            };



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

            // Apply the provided Bollinger Breakout strategy to all other market types
            var bollingerMarkets = new[]
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

            foreach (var marketType in bollingerMarkets)
            {
                strategiesDict.Add(marketType, new List<Strategy> { bollingerStrategy });
            }

            return strategiesDict;
        }

        public Dictionary<MarketType, List<Strategy>> GetBollingerBreakoutStrategy(string weightName)
        {
            // Use the first (default) parameter set for the standard strategy
            var defaultParams = BollingerParameterSets.Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new BollingerBreakout(mlParams: defaultParams.Parameters) }
            );

            return CreateMarketStrategyMapping(strat);
        }

        public Dictionary<MarketType, List<Strategy>> GetBreakoutStrategy(string weightName)
        {
            // Use the first (default) parameter set for the standard strategy
            var defaultParams = BreakoutParameterSets.Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new Breakout2(mlParams: defaultParams.Parameters) }
            );

            return CreateMarketStrategyMapping(strat);
        }

        public Dictionary<MarketType, List<Strategy>> GetNothingEverHappensStrategy(string weightName)
        {
            // Use the first (default) parameter set for the standard strategy
            var defaultParams = NothingEverHappensParameterSets.Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new NothingEverHappensStrat(mlParams: defaultParams.Parameters) }
            );

            return CreateMarketStrategyMapping(strat);
        }

        public Dictionary<MarketType, List<Strategy>> GetFlowMomentumStrategy(string weightName)
        {
            // Use the first (default) parameter set for the standard strategy
            var defaultParams = FlowMomentumParameterSets.Where(x => x.Name == weightName).First();
            var strat = new Strategy(
                defaultParams.Name,
                new List<Strat> { new FlowMomentumStrat(mlParams: defaultParams.Parameters) }
            );

            return CreateMarketStrategyMapping(strat);
        }

        public List<Dictionary<MarketType, List<Strategy>>> GetFlowMomentumStrategiesForTraining()
        {
            var returnList = new List<Dictionary<MarketType, List<Strategy>>>();

            // Create a strategy set for each parameter configuration
            foreach (var (name, parameters) in FlowMomentumParameterSets)
            {
                // Create a new NothingEverHappensStrat with the current parameter set
                var flowStrat = new FlowMomentumStrat(name: name, mlParams: parameters);
                var flowStrategy = new Strategy(name, new List<Strat> { flowStrat });

                // Create the market-to-strategy mapping for this parameter set
                var strategiesDict = CreateMarketStrategyMapping(flowStrategy);

                returnList.Add(strategiesDict);
            }

            return returnList;
        }

        public List<Dictionary<MarketType, List<Strategy>>> GetBollingerBreakoutStrategiesForTraining()
        {
            var returnList = new List<Dictionary<MarketType, List<Strategy>>>();

            // Create a strategy set for each parameter configuration
            foreach (var (name, parameters) in BollingerParameterSets)
            {
                // Create a new Bollinger Breakout strategy with the current parameter set
                var bollingerStrat = new BollingerBreakout(mlParams: parameters);
                var bollingerStrategy = new Strategy(name, new List<Strat> { bollingerStrat });

                // Create the market-to-strategy mapping for this parameter set
                var strategiesDict = CreateMarketStrategyMapping(bollingerStrategy);

                returnList.Add(strategiesDict);
            }

            return returnList;
        }




        public static readonly List<(string Name, Dictionary<BollingerBreakout.ParamKey, double> Parameters)> BollingerParameterSets = new List<(string, Dictionary<ParamKey, double>)>
        {
            (
                "BollingerBands_Default",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_TightSqueeze_1",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.03 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_LooseSqueeze_2",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.07 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_LowAbsorption_3",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 8.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_HighAbsorption_4",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 12.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_StrongSignal_5",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.5 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_WeakSignal_6",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 1.5 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_HighVelocityBB_7",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.08 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_LowVelocityBB_8",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.03 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_HighVelocityStandalone_9",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.15 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_LowVelocityStandalone_10",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.08 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_HighVolume_11",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.07 },
                    { ParamKey.VolumePercentageNo, 0.07 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_LowVolume_12",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.03 },
                    { ParamKey.VolumePercentageNo, 0.03 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_NearResolved_13",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 3 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_FarResolved_14",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 7 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_HighImbalance_15",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1500.0 }
                }
            ),
            (
                "BollingerBands_LowImbalance_16",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 500.0 }
                }
            ),
            (
                "BollingerBands_Aggressive_17",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.03 },
                    { ParamKey.AbsorptionThreshold, 8.0 },
                    { ParamKey.MinSignalStrength, 1.5 },
                    { ParamKey.VelocityPercentageBollinger, 0.07 },
                    { ParamKey.VelocityPercentageStandalone, 0.12 },
                    { ParamKey.VolumePercentageYes, 0.06 },
                    { ParamKey.VolumePercentageNo, 0.06 },
                    { ParamKey.MinNumberOfPointsFromResolved, 4 },
                    { ParamKey.MaxBidImbalance, 800.0 }
                }
            ),
            (
                "BollingerBands_Conservative_18",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.07 },
                    { ParamKey.AbsorptionThreshold, 12.0 },
                    { ParamKey.MinSignalStrength, 2.5 },
                    { ParamKey.VelocityPercentageBollinger, 0.03 },
                    { ParamKey.VelocityPercentageStandalone, 0.08 },
                    { ParamKey.VolumePercentageYes, 0.04 },
                    { ParamKey.VolumePercentageNo, 0.04 },
                    { ParamKey.MinNumberOfPointsFromResolved, 6 },
                    { ParamKey.MaxBidImbalance, 1200.0 }
                }
            ),
            (
                "BollingerBands_HighVelocityCombo_19",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.07 },
                    { ParamKey.VelocityPercentageStandalone, 0.13 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_LowVelocityCombo_20",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.04 },
                    { ParamKey.VelocityPercentageStandalone, 0.09 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_Strict_21",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.06 },
                    { ParamKey.AbsorptionThreshold, 11.0 },
                    { ParamKey.MinSignalStrength, 2.3 },
                    { ParamKey.VelocityPercentageBollinger, 0.06 },
                    { ParamKey.VelocityPercentageStandalone, 0.11 },
                    { ParamKey.VolumePercentageYes, 0.06 },
                    { ParamKey.VolumePercentageNo, 0.06 },
                    { ParamKey.MinNumberOfPointsFromResolved, 6 },
                    { ParamKey.MaxBidImbalance, 1100.0 }
                }
            ),
            (
                "BollingerBands_Relaxed_22",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.04 },
                    { ParamKey.AbsorptionThreshold, 9.0 },
                    { ParamKey.MinSignalStrength, 1.7 },
                    { ParamKey.VelocityPercentageBollinger, 0.04 },
                    { ParamKey.VelocityPercentageStandalone, 0.09 },
                    { ParamKey.VolumePercentageYes, 0.04 },
                    { ParamKey.VolumePercentageNo, 0.04 },
                    { ParamKey.MinNumberOfPointsFromResolved, 4 },
                    { ParamKey.MaxBidImbalance, 900.0 }
                }
            ),
            (
                "BollingerBands_Balanced_23",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.06 },
                    { ParamKey.VelocityPercentageStandalone, 0.11 },
                    { ParamKey.VolumePercentageYes, 0.06 },
                    { ParamKey.VolumePercentageNo, 0.06 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_VolumeSensitive_24",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.08 },
                    { ParamKey.VolumePercentageNo, 0.08 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "BollingerBands_ImbalanceSensitive_25",
                new Dictionary<ParamKey, double>
                {
                    { ParamKey.SqueezeThreshold, 0.05 },
                    { ParamKey.AbsorptionThreshold, 10.0 },
                    { ParamKey.MinSignalStrength, 2.0 },
                    { ParamKey.VelocityPercentageBollinger, 0.05 },
                    { ParamKey.VelocityPercentageStandalone, 0.10 },
                    { ParamKey.VolumePercentageYes, 0.05 },
                    { ParamKey.VolumePercentageNo, 0.05 },
                    { ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { ParamKey.MaxBidImbalance, 700.0 }
                }
            )
        };

        public static readonly List<(string Name, Dictionary<Breakout2.ParamKey, double> Parameters)>
    BreakoutParameterSets = new List<(string, Dictionary<Breakout2.ParamKey, double>)>
{
    // ==== Controls & Extremes (prefixed B3_) ====
    ("B3_CTRL_BASE", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,17.5},
        {Breakout2.ParamKey.ReversalExtraStrength,3.0},
        {Breakout2.ParamKey.MinSignalStrength,2.8},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.110},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,6.0},
        {Breakout2.ParamKey.MinRatioDifference,0.050},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},
        {Breakout2.ParamKey.SpikeWeightScale,0.35},
        {Breakout2.ParamKey.SpikeWeightCap,5.2},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.55},
        {Breakout2.ParamKey.TradeRateShareMin,0.61},
        {Breakout2.ParamKey.TradeEventShareMin,0.41},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.0},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.40},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.12},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}
    }),
    ("B3_CTRL_TIGHT", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,20.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.2},
        {Breakout2.ParamKey.MinSignalStrength,3.2},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.118},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.5},
        {Breakout2.ParamKey.MinRatioDifference,0.060},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},
        {Breakout2.ParamKey.SpikeWeightScale,0.32},
        {Breakout2.ParamKey.SpikeWeightCap,5.1},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},
        {Breakout2.ParamKey.TradeRateShareMin,0.65},
        {Breakout2.ParamKey.TradeEventShareMin,0.45},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},
        {Breakout2.ParamKey.ExitRsiDevThreshold,5.6},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.48},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.10},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.24}
    }),
    ("B3_CTRL_LOOSE", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,16.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.6},
        {Breakout2.ParamKey.MinSignalStrength,2.5},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.102},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,7.0},
        {Breakout2.ParamKey.MinRatioDifference,0.045},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},
        {Breakout2.ParamKey.SpikeWeightScale,0.38},
        {Breakout2.ParamKey.SpikeWeightCap,5.3},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.50},
        {Breakout2.ParamKey.TradeRateShareMin,0.59},
        {Breakout2.ParamKey.TradeEventShareMin,0.39},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.8},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.15},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.32},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.16},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.40}
    }),
    ("B3_CTRL_FLAT_STRICT", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,18.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.1},
        {Breakout2.ParamKey.MinSignalStrength,3.0},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.112},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,6.0},
        {Breakout2.ParamKey.MinRatioDifference,0.055},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},
        {Breakout2.ParamKey.SpikeWeightScale,0.33},
        {Breakout2.ParamKey.SpikeWeightCap,5.2},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.58},
        {Breakout2.ParamKey.TradeRateShareMin,0.62},
        {Breakout2.ParamKey.TradeEventShareMin,0.42},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},
        {Breakout2.ParamKey.ExitRsiDevThreshold,5.2},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.85},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.50},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.10},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.22}
    }),
    ("B3_CTRL_FLAT_LENIENT", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,18.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.8},
        {Breakout2.ParamKey.MinSignalStrength,2.6},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.108},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,6.5},
        {Breakout2.ParamKey.MinRatioDifference,0.045},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},
        {Breakout2.ParamKey.SpikeWeightScale,0.36},
        {Breakout2.ParamKey.SpikeWeightCap,5.4},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.62},
        {Breakout2.ParamKey.TradeRateShareMin,0.60},
        {Breakout2.ParamKey.TradeEventShareMin,0.40},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.9},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.20},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.30},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.18},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.44}
    }),
    ("B3_EDGE_ABSORB_MIN", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,10.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.4},
        {Breakout2.ParamKey.MinSignalStrength,2.6},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.106},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,7.5},
        {Breakout2.ParamKey.MinRatioDifference,0.040},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,1.7},
        {Breakout2.ParamKey.SpikeWeightScale,0.40},
        {Breakout2.ParamKey.SpikeWeightCap,5.5},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.50},
        {Breakout2.ParamKey.TradeRateShareMin,0.58},
        {Breakout2.ParamKey.TradeEventShareMin,0.38},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,1.8},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.6},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.10},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.34},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.16},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.42}
    }),
    ("B3_EDGE_ABSORB_MAX", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,30.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.8},
        {Breakout2.ParamKey.MinSignalStrength,3.2},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.112},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.0},
        {Breakout2.ParamKey.MinRatioDifference,0.065},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.8},
        {Breakout2.ParamKey.SpikeWeightScale,0.28},
        {Breakout2.ParamKey.SpikeWeightCap,4.9},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.70},
        {Breakout2.ParamKey.TradeRateShareMin,0.66},
        {Breakout2.ParamKey.TradeEventShareMin,0.46},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},
        {Breakout2.ParamKey.ExitRsiDevThreshold,5.0},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.46},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.10},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.24}
    }),
    ("B3_EDGE_VDR_MIN", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,18.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.8},
        {Breakout2.ParamKey.MinSignalStrength,2.7},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.090},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,6.5},
        {Breakout2.ParamKey.MinRatioDifference,0.050},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},
        {Breakout2.ParamKey.SpikeWeightScale,0.34},
        {Breakout2.ParamKey.SpikeWeightCap,5.2},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.58},
        {Breakout2.ParamKey.TradeRateShareMin,0.60},
        {Breakout2.ParamKey.TradeEventShareMin,0.42},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.4},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.10},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.36},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.14},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.36}
    }),
    ("B3_EDGE_VDR_MAX", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,19.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.3},
        {Breakout2.ParamKey.MinSignalStrength,3.3},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.140},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.5},
        {Breakout2.ParamKey.MinRatioDifference,0.060},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},
        {Breakout2.ParamKey.SpikeWeightScale,0.30},
        {Breakout2.ParamKey.SpikeWeightCap,5.0},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},
        {Breakout2.ParamKey.TradeRateShareMin,0.64},
        {Breakout2.ParamKey.TradeEventShareMin,0.45},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},
        {Breakout2.ParamKey.ExitRsiDevThreshold,5.4},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.85},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.48},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.10},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.24}
    }),
    ("B3_EDGE_CONFIRM_MIN", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,17.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.7},
        {Breakout2.ParamKey.MinSignalStrength,2.6},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.106},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,7.0},
        {Breakout2.ParamKey.MinRatioDifference,0.045},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},
        {Breakout2.ParamKey.SpikeWeightScale,0.36},
        {Breakout2.ParamKey.SpikeWeightCap,5.4},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.52},
        {Breakout2.ParamKey.TradeRateShareMin,0.55},
        {Breakout2.ParamKey.TradeEventShareMin,0.35},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.8},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.15},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.34},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.18},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.44}
    }),
    ("B3_EDGE_CONFIRM_MAX", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,21.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.4},
        {Breakout2.ParamKey.MinSignalStrength,3.1},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.116},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.0},
        {Breakout2.ParamKey.MinRatioDifference,0.065},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},
        {Breakout2.ParamKey.SpikeWeightScale,0.28},
        {Breakout2.ParamKey.SpikeWeightCap,5.5},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.68},
        {Breakout2.ParamKey.TradeRateShareMin,0.68},
        {Breakout2.ParamKey.TradeEventShareMin,0.50},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},
        {Breakout2.ParamKey.ExitRsiDevThreshold,5.2},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.88},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.50},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.10},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.22}
    }),
    ("B3_EDGE_RSIFLAT_NARROW", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,18.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.0},
        {Breakout2.ParamKey.MinSignalStrength,2.9},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.110},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,6.0},
        {Breakout2.ParamKey.MinRatioDifference,0.050},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},
        {Breakout2.ParamKey.SpikeWeightScale,0.33},
        {Breakout2.ParamKey.SpikeWeightCap,5.3},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},
        {Breakout2.ParamKey.TradeRateShareMin,0.61},
        {Breakout2.ParamKey.TradeEventShareMin,0.41},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},
        {Breakout2.ParamKey.ExitRsiDevThreshold,3.5},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.95},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.44},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.12},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}
    }),
    ("B3_EDGE_RSIFLAT_WIDE", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,18.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.9},
        {Breakout2.ParamKey.MinSignalStrength,2.7},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.108},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,6.5},
        {Breakout2.ParamKey.MinRatioDifference,0.048},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},
        {Breakout2.ParamKey.SpikeWeightScale,0.36},
        {Breakout2.ParamKey.SpikeWeightCap,5.4},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.58},
        {Breakout2.ParamKey.TradeRateShareMin,0.60},
        {Breakout2.ParamKey.TradeEventShareMin,0.40},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},
        {Breakout2.ParamKey.ExitRsiDevThreshold,8.0},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.15},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.32},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.18},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.44}
    }),
    ("B3_EDGE_FLIP_LOW", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,17.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.7},
        {Breakout2.ParamKey.MinSignalStrength,2.6},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.112},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,6.5},
        {Breakout2.ParamKey.MinRatioDifference,0.048},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},
        {Breakout2.ParamKey.SpikeWeightScale,0.34},
        {Breakout2.ParamKey.SpikeWeightCap,5.2},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},
        {Breakout2.ParamKey.TradeRateShareMin,0.60},
        {Breakout2.ParamKey.TradeEventShareMin,0.40},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,1.6},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.4},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.10},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.36},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.14},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.36}
    }),
    ("B3_EDGE_FLIP_HIGH", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,19.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.3},
        {Breakout2.ParamKey.MinSignalStrength,3.2},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.114},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.5},
        {Breakout2.ParamKey.MinRatioDifference,0.060},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},
        {Breakout2.ParamKey.SpikeWeightScale,0.30},
        {Breakout2.ParamKey.SpikeWeightCap,5.1},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},
        {Breakout2.ParamKey.TradeRateShareMin,0.64},
        {Breakout2.ParamKey.TradeEventShareMin,0.45},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},
        {Breakout2.ParamKey.ExitRsiDevThreshold,5.4},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.48},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.10},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.24}
    }),

    // ==== A-series (100+ intelligently varied sets, B3_A01..B3_A100) ====
    ("B3_A01", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,17.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.8},
        {Breakout2.ParamKey.MinSignalStrength,2.7},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.105},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,6.0},
        {Breakout2.ParamKey.MinRatioDifference,0.048},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},
        {Breakout2.ParamKey.SpikeWeightScale,0.34},
        {Breakout2.ParamKey.SpikeWeightCap,5.2},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.55},
        {Breakout2.ParamKey.TradeRateShareMin,0.60},
        {Breakout2.ParamKey.TradeEventShareMin,0.40},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.2},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.95},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.40},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.12},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}
    }),
    ("B3_A02", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,18.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.0},
        {Breakout2.ParamKey.MinSignalStrength,2.9},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.110},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,6.5},
        {Breakout2.ParamKey.MinRatioDifference,0.050},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},
        {Breakout2.ParamKey.SpikeWeightScale,0.32},
        {Breakout2.ParamKey.SpikeWeightCap,5.0},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.58},
        {Breakout2.ParamKey.TradeRateShareMin,0.62},
        {Breakout2.ParamKey.TradeEventShareMin,0.42},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.4},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.05},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.38},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.13},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}
    }),
    ("B3_A03", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,19.0},
        {Breakout2.ParamKey.ReversalExtraStrength,2.7},
        {Breakout2.ParamKey.MinSignalStrength,3.0},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.115},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,6.5},
        {Breakout2.ParamKey.MinRatioDifference,0.055},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},
        {Breakout2.ParamKey.SpikeWeightScale,0.37},
        {Breakout2.ParamKey.SpikeWeightCap,5.3},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},
        {Breakout2.ParamKey.TradeRateShareMin,0.63},
        {Breakout2.ParamKey.TradeEventShareMin,0.39},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.0},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.42},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.14},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.32}
    }),
    ("B3_A04", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,20.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.2},
        {Breakout2.ParamKey.MinSignalStrength,2.8},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.100},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.5},
        {Breakout2.ParamKey.MinRatioDifference,0.050},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},
        {Breakout2.ParamKey.SpikeWeightScale,0.34},
        {Breakout2.ParamKey.SpikeWeightCap,5.1},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.62},
        {Breakout2.ParamKey.TradeRateShareMin,0.61},
        {Breakout2.ParamKey.TradeEventShareMin,0.41},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.8},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.36},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.10},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}
    }),
    ("B3_A05", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,21.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.1},
        {Breakout2.ParamKey.MinSignalStrength,3.1},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.118},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,5.5},
        {Breakout2.ParamKey.MinRatioDifference,0.060},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},
        {Breakout2.ParamKey.SpikeWeightScale,0.30},
        {Breakout2.ParamKey.SpikeWeightCap,5.4},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.65},
        {Breakout2.ParamKey.TradeRateShareMin,0.64},
        {Breakout2.ParamKey.TradeEventShareMin,0.43},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.5},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.10},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.35},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.15},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}
    }),
    ("B3_A06", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,16.5},
        {Breakout2.ParamKey.ReversalExtraStrength,2.7},
        {Breakout2.ParamKey.MinSignalStrength,2.6},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.104},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,6.8},
        {Breakout2.ParamKey.MinRatioDifference,0.045},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},
        {Breakout2.ParamKey.SpikeWeightScale,0.36},
        {Breakout2.ParamKey.SpikeWeightCap,5.3},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.52},
        {Breakout2.ParamKey.TradeRateShareMin,0.59},
        {Breakout2.ParamKey.TradeEventShareMin,0.40},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.6},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.12},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.34},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.16},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.38}
    }),
    ("B3_A07", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,22.0},
        {Breakout2.ParamKey.ReversalExtraStrength,3.3},
        {Breakout2.ParamKey.MinSignalStrength,3.0},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.122},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.0},
        {Breakout2.ParamKey.MinRatioDifference,0.065},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},
        {Breakout2.ParamKey.SpikeWeightScale,0.28},
        {Breakout2.ParamKey.SpikeWeightCap,5.0},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.68},
        {Breakout2.ParamKey.TradeRateShareMin,0.65},
        {Breakout2.ParamKey.TradeEventShareMin,0.45},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},
        {Breakout2.ParamKey.ExitRsiDevThreshold,5.8},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.88},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.46},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.12},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}
    }),
    ("B3_A08", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,18.5},
        {Breakout2.ParamKey.ReversalExtraStrength,2.9},
        {Breakout2.ParamKey.MinSignalStrength,2.8},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.108},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},
        {Breakout2.ParamKey.MaxVolumeRatio,6.2},
        {Breakout2.ParamKey.MinRatioDifference,0.050},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},
        {Breakout2.ParamKey.SpikeWeightScale,0.34},
        {Breakout2.ParamKey.SpikeWeightCap,5.2},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},
        {Breakout2.ParamKey.TradeRateShareMin,0.61},
        {Breakout2.ParamKey.TradeEventShareMin,0.41},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.2},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.97},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.40},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.12},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.32}
    }),
    ("B3_A09", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,20.5},
        {Breakout2.ParamKey.ReversalExtraStrength,3.2},
        {Breakout2.ParamKey.MinSignalStrength,2.9},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.112},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},
        {Breakout2.ParamKey.MaxVolumeRatio,5.8},
        {Breakout2.ParamKey.MinRatioDifference,0.055},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},
        {Breakout2.ParamKey.SpikeWeightScale,0.31},
        {Breakout2.ParamKey.SpikeWeightCap,5.1},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.61},
        {Breakout2.ParamKey.TradeRateShareMin,0.62},
        {Breakout2.ParamKey.TradeEventShareMin,0.42},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.3},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,0.92},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.44},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.11},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}
    }),
    ("B3_A10", new() {
        {Breakout2.ParamKey.AbsorptionThreshold,16.8},
        {Breakout2.ParamKey.ReversalExtraStrength,2.6},
        {Breakout2.ParamKey.MinSignalStrength,2.6},
        {Breakout2.ParamKey.VelocityToDepthRatio,0.100},
        {Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},
        {Breakout2.ParamKey.MaxVolumeRatio,6.9},
        {Breakout2.ParamKey.MinRatioDifference,0.045},
        {Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},
        {Breakout2.ParamKey.SpikeWeightScale,0.38},
        {Breakout2.ParamKey.SpikeWeightCap,5.5},
        {Breakout2.ParamKey.SpikeVolumeWeightScale,0.50},
        {Breakout2.ParamKey.TradeRateShareMin,0.59},
        {Breakout2.ParamKey.TradeEventShareMin,0.39},
        {Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},
        {Breakout2.ParamKey.ExitRsiDevThreshold,6.7},
        {Breakout2.ParamKey.ExitFlatBidRangeMax,1.12},
        {Breakout2.ParamKey.ExitFlatQuietRatio,0.32},
        {Breakout2.ParamKey.ExitFlatVolContextMax,0.16},
        {Breakout2.ParamKey.ExitFlatTradeRateMax,0.42}
    }),

    ("B3_A11", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.2},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.055},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.62},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.41},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
    ("B3_A12", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.2},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.4},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.01},{Breakout2.ParamKey.ExitFlatQuietRatio,0.39},{Breakout2.ParamKey.ExitFlatVolContextMax,0.13},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.33}}),
    ("B3_A13", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.8},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.120},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.46},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
    ("B3_A14", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.4},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.103},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.8},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.52},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.10},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
    ("B3_A15", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.2},{Breakout2.ParamKey.ReversalExtraStrength,3.4},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.124},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.2},{Breakout2.ParamKey.MinRatioDifference,0.065},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.29},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.68},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.88},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
    ("B3_A16", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.8},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.111},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.052},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.98},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
    ("B3_A17", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.2},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.119},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.6},{Breakout2.ParamKey.MinRatioDifference,0.059},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.65},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.92},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
    ("B3_A18", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.2},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.104},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.7},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.36},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.09},{Breakout2.ParamKey.ExitFlatQuietRatio,0.34},{Breakout2.ParamKey.ExitFlatVolContextMax,0.16},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.39}}),
    ("B3_A19", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.5},{Breakout2.ParamKey.ReversalExtraStrength,3.4},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.1},{Breakout2.ParamKey.MinRatioDifference,0.064},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.29},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.69},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.89},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
    ("B3_A20", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.6},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.1},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.99},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),


    ("B3_A21", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.8},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.113},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.9},{Breakout2.ParamKey.MinRatioDifference,0.056},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.61},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.43},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
    ("B3_A22", new() {{Breakout2.ParamKey.AbsorptionThreshold,16.9},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.101},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.9},{Breakout2.ParamKey.MinRatioDifference,0.045},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.SpikeWeightScale,0.39},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.51},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.13},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
    ("B3_A23", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.0},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.3},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
    ("B3_A24", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.9},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.053},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.97},{Breakout2.ParamKey.ExitFlatQuietRatio,0.41},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
    ("B3_A25", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.2},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.8},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
    ("B3_A26", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.6},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.106},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.55},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),
    ("B3_A27", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.4},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.4},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
    ("B3_A28", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.7},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.1},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.99},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
    ("B3_A29", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.6},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
    ("B3_A30", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.3},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.103},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.7},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.09},{Breakout2.ParamKey.ExitFlatQuietRatio,0.35},{Breakout2.ParamKey.ExitFlatVolContextMax,0.16},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.39}}),
    ("B3_A31", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.0},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.054},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.96},{Breakout2.ParamKey.ExitFlatQuietRatio,0.42},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
    ("B3_A32", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.1},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.107},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.3},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.02},{Breakout2.ParamKey.ExitFlatQuietRatio,0.39},{Breakout2.ParamKey.ExitFlatVolContextMax,0.13},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.33}}),
    ("B3_A33", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.9},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.119},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.6},{Breakout2.ParamKey.MinRatioDifference,0.059},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
    ("B3_A34", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.1},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.102},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.8},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.11},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
    ("B3_A35", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.1},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.2},{Breakout2.ParamKey.MinRatioDifference,0.064},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
    ("B3_A36", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.4},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
    ("B3_A37", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.7},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
    ("B3_A38", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.5},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),
    ("B3_A39", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.6},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.4},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
    ("B3_A40", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.3},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
    // Append these entries inside the same BreakoutParameterSets list initializer:

("B3_A41", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.4},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,5.9},{Breakout2.ParamKey.MinRatioDifference,0.054},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.96},{Breakout2.ParamKey.ExitFlatQuietRatio,0.42},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
("B3_A42", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.0},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.107},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.3},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.02},{Breakout2.ParamKey.ExitFlatQuietRatio,0.39},{Breakout2.ParamKey.ExitFlatVolContextMax,0.13},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.33}}),
("B3_A43", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.0},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.119},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.6},{Breakout2.ParamKey.MinRatioDifference,0.059},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
("B3_A44", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.0},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.102},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.8},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.11},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
("B3_A45", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.0},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.2},{Breakout2.ParamKey.MinRatioDifference,0.064},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
("B3_A46", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.2},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A47", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.4},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A48", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.6},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),
("B3_A49", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.7},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.4},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
("B3_A50", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.4},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A51", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.1},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.053},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.97},{Breakout2.ParamKey.ExitFlatQuietRatio,0.41},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
("B3_A52", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.1},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.107},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.3},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.02},{Breakout2.ParamKey.ExitFlatQuietRatio,0.39},{Breakout2.ParamKey.ExitFlatVolContextMax,0.13},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.33}}),
("B3_A53", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.9},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.119},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.6},{Breakout2.ParamKey.MinRatioDifference,0.059},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
("B3_A54", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.2},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.103},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.7},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.09},{Breakout2.ParamKey.ExitFlatQuietRatio,0.35},{Breakout2.ParamKey.ExitFlatVolContextMax,0.16},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.39}}),
("B3_A55", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.2},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.2},{Breakout2.ParamKey.MinRatioDifference,0.064},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
("B3_A56", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.3},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A57", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.8},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A58", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.4},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),
("B3_A59", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.4},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.4},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
("B3_A60", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.5},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A61", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.6},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,5.9},{Breakout2.ParamKey.MinRatioDifference,0.054},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.96},{Breakout2.ParamKey.ExitFlatQuietRatio,0.42},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
("B3_A62", new() {{Breakout2.ParamKey.AbsorptionThreshold,16.8},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.101},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.9},{Breakout2.ParamKey.MinRatioDifference,0.045},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.SpikeWeightScale,0.39},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.51},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.13},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
("B3_A63", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.3},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.3},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
("B3_A64", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.6},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.1},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.99},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A65", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.5},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
("B3_A66", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.3},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.104},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.7},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.09},{Breakout2.ParamKey.ExitFlatQuietRatio,0.35},{Breakout2.ParamKey.ExitFlatVolContextMax,0.16},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.39}}),
("B3_A67", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.4},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.3},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
("B3_A68", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.7},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.1},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.99},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A69", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.6},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
("B3_A70", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.5},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),
("B3_A71", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.5},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.4},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
("B3_A72", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.2},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A73", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.3},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.053},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.97},{Breakout2.ParamKey.ExitFlatQuietRatio,0.41},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
("B3_A74", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.0},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.107},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.3},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.02},{Breakout2.ParamKey.ExitFlatQuietRatio,0.39},{Breakout2.ParamKey.ExitFlatVolContextMax,0.13},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.33}}),
("B3_A75", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.0},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.119},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.6},{Breakout2.ParamKey.MinRatioDifference,0.059},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
("B3_A76", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.1},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.103},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.7},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.11},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
("B3_A77", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.1},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.3},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
("B3_A78", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.4},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A79", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.8},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A80", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.6},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),
("B3_A81", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.3},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.4},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
("B3_A82", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.3},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A83", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.2},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.053},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.97},{Breakout2.ParamKey.ExitFlatQuietRatio,0.41},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
("B3_A84", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.9},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.107},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.3},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.02},{Breakout2.ParamKey.ExitFlatQuietRatio,0.39},{Breakout2.ParamKey.ExitFlatVolContextMax,0.13},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.33}}),
("B3_A85", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.7},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A86", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.4},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.104},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.7},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.09},{Breakout2.ParamKey.ExitFlatQuietRatio,0.35},{Breakout2.ParamKey.ExitFlatVolContextMax,0.16},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.39}}),
("B3_A87", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.0},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.3},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
("B3_A88", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.5},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A89", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.9},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A90", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.7},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),
("B3_A91", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.8},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,5.9},{Breakout2.ParamKey.MinRatioDifference,0.054},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.SpikeWeightScale,0.33},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.60},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.96},{Breakout2.ParamKey.ExitFlatQuietRatio,0.42},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.30}}),
("B3_A92", new() {{Breakout2.ParamKey.AbsorptionThreshold,16.9},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.101},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.9},{Breakout2.ParamKey.MinRatioDifference,0.045},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.SpikeWeightScale,0.39},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.51},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.13},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
("B3_A93", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.4},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.3},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
("B3_A94", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.6},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.1},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.99},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A95", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.6},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.64},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.93},{Breakout2.ParamKey.ExitFlatQuietRatio,0.45},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.28}}),
("B3_A96", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.2},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.104},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.7},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.09},{Breakout2.ParamKey.ExitFlatQuietRatio,0.35},{Breakout2.ParamKey.ExitFlatVolContextMax,0.16},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.39}}),
("B3_A97", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.3},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.3},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.67},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.90},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
("B3_A98", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.7},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.1},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.99},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A99", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.7},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A100", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.5},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}}),

// Extra beyond 100 for headroom
("B3_A101", new() {{Breakout2.ParamKey.AbsorptionThreshold,19.9},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,5.9},{Breakout2.ParamKey.MinRatioDifference,0.055},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.61},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.95},{Breakout2.ParamKey.ExitFlatQuietRatio,0.43},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A102", new() {{Breakout2.ParamKey.AbsorptionThreshold,16.7},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.100},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,7.0},{Breakout2.ParamKey.MinRatioDifference,0.045},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.SpikeWeightScale,0.39},{Breakout2.ParamKey.SpikeWeightCap,5.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.51},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},{Breakout2.ParamKey.ExitRsiDevThreshold,6.9},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.14},{Breakout2.ParamKey.ExitFlatQuietRatio,0.32},{Breakout2.ParamKey.ExitFlatVolContextMax,0.18},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.42}}),
("B3_A103", new() {{Breakout2.ParamKey.AbsorptionThreshold,22.5},{Breakout2.ParamKey.ReversalExtraStrength,3.4},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.2},{Breakout2.ParamKey.MinRatioDifference,0.064},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.7},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.68},{Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.89},{Breakout2.ParamKey.ExitFlatQuietRatio,0.48},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.26}}),
("B3_A104", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.8},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.1},{Breakout2.ParamKey.MinRatioDifference,0.052},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.57},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.99},{Breakout2.ParamKey.ExitFlatQuietRatio,0.41},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A105", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.4},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.8},{Breakout2.ParamKey.MinRatioDifference,0.057},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.62},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A106", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.0},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.103},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.8},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.SpikeWeightScale,0.37},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.53},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.11},{Breakout2.ParamKey.ExitFlatQuietRatio,0.33},{Breakout2.ParamKey.ExitFlatVolContextMax,0.17},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.41}}),
("B3_A107", new() {{Breakout2.ParamKey.AbsorptionThreshold,21.9},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.4},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.1},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.66},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.91},{Breakout2.ParamKey.ExitFlatQuietRatio,0.47},{Breakout2.ParamKey.ExitFlatVolContextMax,0.10},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.27}}),
("B3_A108", new() {{Breakout2.ParamKey.AbsorptionThreshold,18.4},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.56},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.00},{Breakout2.ParamKey.ExitFlatQuietRatio,0.40},{Breakout2.ParamKey.ExitFlatVolContextMax,0.12},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.31}}),
("B3_A109", new() {{Breakout2.ParamKey.AbsorptionThreshold,20.8},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,5.7},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.SpikeWeightScale,0.31},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.63},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.ExitFlatBidRangeMax,0.94},{Breakout2.ParamKey.ExitFlatQuietRatio,0.44},{Breakout2.ParamKey.ExitFlatVolContextMax,0.11},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.29}}),
("B3_A110", new() {{Breakout2.ParamKey.AbsorptionThreshold,17.6},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.6},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.3},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.54},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.ExitFlatBidRangeMax,1.06},{Breakout2.ParamKey.ExitFlatQuietRatio,0.38},{Breakout2.ParamKey.ExitFlatVolContextMax,0.14},{Breakout2.ParamKey.ExitFlatTradeRateMax,0.35}})

};

        public static readonly List<(string Name, Dictionary<FlowMomentumStrat.ParamKey, double> Parameters)>
FlowMomentumParameterSets = new List<(string, Dictionary<FlowMomentumStrat.ParamKey, double>)>
{
    // Baselines
    ("FM_Default", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
    }),
    ("FM_Default_TightEdge", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.07 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.9 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.4 },
    }),
    ("FM_Default_LooseEdge", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.05 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.59 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.39 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.6 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 6.0 },
    }),

    // Momentum length sweep
    ("FM_Momentum_2bars", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.7 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.8 },
    }),
    ("FM_Momentum_3bars_A", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.085 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.07 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.9 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
    }),
    ("FM_Momentum_3bars_B", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.15 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.4 },
    }),
    ("FM_Momentum_4bars", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.1 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.2 },
    }),

    // High-edge (participation ↓)
    ("FM_HighEdge_01", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.10 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.63 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.43 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.2 },
    }),
    ("FM_HighEdge_02", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.12 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.1 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.20 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.0 },
    }),
    ("FM_HighEdge_03", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.20 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.15 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.2 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.22 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 4.8 },
    }),

    // V-cap emphasis
    ("FM_VCap_01", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.085 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.07 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.9 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
    }),
    ("FM_VCap_02", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.4 },
    }),
    ("FM_VCap_03", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.095 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.17 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.09 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.63 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.43 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.1 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.2 },
    }),

    // Balanced mixes
    ("FM_Balanced_01", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.085 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.8 },
    }),
    ("FM_Balanced_02", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.07 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.9 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
    }),
    ("FM_Balanced_03", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.095 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.15 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.4 },
    }),
    ("FM_Balanced_04", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.09 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.1 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.2 },
    }),
    ("FM_Balanced_05", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.105 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.20 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.10 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.2 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.0 },
    }),

    // Spike gate variants (to ensure “unusual” not noise)
    ("FM_SpikeGate_01", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.4 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.8 },
    }),
    ("FM_SpikeGate_02", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.085 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.07 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.6 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
    }),
    ("FM_SpikeGate_03", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.4 },
    }),
    ("FM_SpikeGate_04", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.095 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.09 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.2 },
    }),
    ("FM_SpikeGate_05", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.10 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.20 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.13 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.10 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.63 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.43 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.2 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.20 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.0 },
    }),

    // Loose edge safety net
    ("FM_Loose_01", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.07 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.17 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.08 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.05 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.58 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.38 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.5 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.10 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 6.2 },
    }),
    ("FM_Loose_02", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 5 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.075 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.09 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.055 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.59 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.39 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.6 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.11 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 6.0 },
    }),
    ("FM_Loose_03", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 6 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.08 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.10 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.06 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.60 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.40 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.7 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.12 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.8 },
    }),

    // Conservative distance-from-bounds
    ("FM_Dist_01", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 7 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.085 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.11 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 2 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.07 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.61 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.41 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.8 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.6 },
    }),
    ("FM_Dist_02", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 8 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.09 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.18 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.08 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 2.9 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.16 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.4 },
    }),
    ("FM_Dist_03", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 9 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.095 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.19 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.12 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.09 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.62 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.42 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.0 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.18 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.2 },
    }),

    // Extreme edge (low participation probes)
    ("FM_Extreme_01", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.11 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.14 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 3 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.12 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.64 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.44 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.1 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.20 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 5.0 },
    }),
    ("FM_Extreme_02", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 10 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.12 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.16 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.14 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.66 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.46 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.2 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.22 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 4.8 },
    }),
    ("FM_Extreme_03", new()
    {
        { FlowMomentumStrat.ParamKey.MinDistanceFromBounds, 11 },
        { FlowMomentumStrat.ParamKey.VelocityToDepthRatio, 0.13 },
        { FlowMomentumStrat.ParamKey.MaxVelocityThresholdRatio, 0.16 },
        { FlowMomentumStrat.ParamKey.MinRatioDifference, 0.18 },
        { FlowMomentumStrat.ParamKey.MinConsecutiveBars, 4 },
        { FlowMomentumStrat.ParamKey.MinSignalStrength, 0.16 },
        { FlowMomentumStrat.ParamKey.TradeRateShareMin, 0.68 },
        { FlowMomentumStrat.ParamKey.TradeEventShareMin, 0.48 },
        { FlowMomentumStrat.ParamKey.SpikeMinRelativeIncrease, 3.3 },
        { FlowMomentumStrat.ParamKey.ExitOppositeSignalStrength, 0.24 },
        { FlowMomentumStrat.ParamKey.ExitRsiDevThreshold, 4.6 },
    })
};




        public Dictionary<MarketType, List<Strategy>> GetBreakoutStrategy()
        {
            // Use the first (default) parameter set for the standard strategy
            var defaultParams = BreakoutParameterSets[0];
            var breakoutStrategy = new Strategy(
                defaultParams.Name,
                new List<Strat> { new Breakout2(mlParams: defaultParams.Parameters) }
            );

            return CreateMarketStrategyMapping(breakoutStrategy);
        }

        public List<Dictionary<MarketType, List<Strategy>>> GetBreakoutStrategiesForTraining()
        {
            var returnList = new List<Dictionary<MarketType, List<Strategy>>>();

            // Create a strategy set for each parameter configuration
            foreach (var (name, parameters) in BreakoutParameterSets)
            {
                // Create a new Breakout strategy with the current parameter set
                var breakoutStrat = new Breakout2(mlParams: parameters);
                var breakoutStrategy = new Strategy(name, new List<Strat> { breakoutStrat });

                // Create the market-to-strategy mapping for this parameter set
                var strategiesDict = CreateMarketStrategyMapping(breakoutStrategy);

                returnList.Add(strategiesDict);
            }

            return returnList;
        }


        public static readonly List<(string Name, Dictionary<NothingEverHappensStrat.ParamKey, double> Parameters)> NothingEverHappensParameterSets = new List<(string, Dictionary<NothingEverHappensStrat.ParamKey, double>)>
        {
            (
                "Nothing_Default",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_HighProbNo_1",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.70 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_LowProbNo_2",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.50 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_HighVelYes_3",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.15 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_LowVelYes_4",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_HighVelNo_5",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.15 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_LowVelNo_6",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_HighVolYes_7",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.07 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_LowVolYes_8",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.03 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_HighVolNo_9",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.07 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_LowVolNo_10",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.03 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_HighAbsorption_11",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 12.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_LowAbsorption_12",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 8.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_StrongSignalEntry_13",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.5 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_WeakSignalEntry_14",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 2.5 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_StrongSignalExit_15",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.5 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_WeakSignalExit_16",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 1.5 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_NearResolved_17",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 3 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_FarResolved_18",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 7 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_HighImbalance_19",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1500.0 }
                }
            ),
            (
                "Nothing_LowImbalance_20",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 500.0 }
                }
            ),
            (
                "Nothing_Aggressive_21",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.55 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.12 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.12 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.06 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.06 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 9.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 2.5 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 1.5 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 4 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1200.0 }
                }
            ),
            (
                "Nothing_Conservative_22",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.65 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.08 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.08 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.04 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.04 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 11.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.5 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.5 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 6 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 800.0 }
                }
            ),
            (
                "Nothing_Balanced_23",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_VolumeSensitive_24",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.08 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.08 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1000.0 }
                }
            ),
            (
                "Nothing_ImbalanceSensitive_25",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.60 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.10 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 10.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 3.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 5 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 700.0 }
                }
            ),
            (
                "Nothing_LowBarrierAll_26",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.50 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.05 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.03 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.03 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 12.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 2.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 1.5 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 3 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 1500.0 }
                }
            ),
            (
                "Nothing_VeryLoose_27",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.45 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.03 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.03 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.02 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.02 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 15.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 1.5 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 1.0 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 2 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 2000.0 }
                }
            ),
            (
                "Nothing_ExtremelyLoose_28",
                new Dictionary<NothingEverHappensStrat.ParamKey, double>
                {
                    { NothingEverHappensStrat.ParamKey.ProbNoThreshold, 0.40 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneYes, 0.01 },
                    { NothingEverHappensStrat.ParamKey.VelocityThresholdStandaloneNo, 0.01 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageYes, 0.01 },
                    { NothingEverHappensStrat.ParamKey.VolumePercentageNo, 0.01 },
                    { NothingEverHappensStrat.ParamKey.AbsorptionThreshold, 20.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthEntry, 1.0 },
                    { NothingEverHappensStrat.ParamKey.MinSignalStrengthExit, 0.5 },
                    { NothingEverHappensStrat.ParamKey.MinNumberOfPointsFromResolved, 1 },
                    { NothingEverHappensStrat.ParamKey.MaxBidImbalance, 3000.0 }
                }
            )
        };

        public Dictionary<MarketType, List<Strategy>> GetNothingEverHappensStrategy()
        {
            // Use the first (default) parameter set for the standard strategy
            var defaultParams = NothingEverHappensParameterSets[0];
            var nothingStrat = new NothingEverHappensStrat(name: defaultParams.Name, mlParams: defaultParams.Parameters);
            var nothingStrategy = new Strategy(defaultParams.Name, new List<Strat> { nothingStrat });

            return CreateMarketStrategyMapping(nothingStrategy);
        }

        public List<Dictionary<MarketType, List<Strategy>>> GetNothingEverHappensStrategiesForTraining()
        {
            var returnList = new List<Dictionary<MarketType, List<Strategy>>>();

            // Create a strategy set for each parameter configuration
            foreach (var (name, parameters) in NothingEverHappensParameterSets)
            {
                // Create a new NothingEverHappensStrat with the current parameter set
                var nothingStrat = new NothingEverHappensStrat(name: name, mlParams: parameters);
                var nothingStrategy = new Strategy(name, new List<Strat> { nothingStrat });

                // Create the market-to-strategy mapping for this parameter set
                var strategiesDict = CreateMarketStrategyMapping(nothingStrategy);

                returnList.Add(strategiesDict);
            }

            return returnList;
        }
    }


}