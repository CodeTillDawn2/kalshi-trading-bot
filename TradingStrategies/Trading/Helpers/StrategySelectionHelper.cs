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


        // Breakout2.cs — inside class Breakout2 (new members only)

        public static List<(string Name, Dictionary<Breakout2.ParamKey, double> Parameters)>
            GenerateParameterSetsStructured(int maxCount = 192)
        {
            // Structured discrete grids, deterministic.
            double[] AbsorptionThreshold = { 16.0, 18.0, 20.0, 22.0 };
            double[] ReversalExtraStrength = { 2.6, 3.0, 3.4 };
            double[] MinSignalStrength = { 2.6, 2.9, 3.2 };
            double[] VelocityToDepthRatio = { 0.100, 0.108, 0.116, 0.124 };
            double[] MinNumberOfPointsResolved = { 5, 6, 7 };
            double[] MaxVolumeRatio = { 5.5, 6.5 };
            double[] MinRatioDifference = { 0.045, 0.055, 0.065 };

            double[] SpikeMinRelativeIncrease = { 1.8, 2.2, 2.6 };
            double[] SpikeWeightScale = { 0.25, 0.35, 0.45 };
            double[] SpikeWeightCap = { 4.9, 5.2 };
            double[] SpikeVolumeWeightScale = { 0.50, 0.60, 0.70 };

            double[] TradeRateShareMin = { 0.59, 0.62, 0.65 };
            double[] TradeEventShareMin = { 0.39, 0.42, 0.45 };

            double[] ExitOppositeSignalStrength = { 1.9, 2.2, 2.5 };
            double[] ExitRsiDevThreshold = { 5.5, 6.2, 6.9 };

            double[] ExitFlatBidRangeMax = { 0.85, 1.00, 1.15 };
            double[] ExitFlatQuietRatio = { 0.32, 0.40, 0.48 };
            double[] ExitFlatVolContextMax = { 0.10, 0.14, 0.18 };
            double[] ExitFlatTradeRateMax = { 0.24, 0.34, 0.44 };

            var list = new List<(string, Dictionary<Breakout2.ParamKey, double>)>(maxCount);
            int id = 0;

            foreach (var a in AbsorptionThreshold)
                foreach (var r in ReversalExtraStrength)
                    foreach (var m in MinSignalStrength)
                        foreach (var v in VelocityToDepthRatio)
                            foreach (var p in MinNumberOfPointsResolved)
                                foreach (var mvr in MaxVolumeRatio)
                                    foreach (var mrd in MinRatioDifference)
                                        foreach (var smri in SpikeMinRelativeIncrease)
                                            foreach (var sws in SpikeWeightScale)
                                                foreach (var swc in SpikeWeightCap)
                                                    foreach (var svws in SpikeVolumeWeightScale)
                                                        foreach (var tr in TradeRateShareMin)
                                                            foreach (var te in TradeEventShareMin)
                                                                foreach (var eos in ExitOppositeSignalStrength)
                                                                    foreach (var erd in ExitRsiDevThreshold)
                                                                        foreach (var ebr in ExitFlatBidRangeMax)
                                                                            foreach (var eqr in ExitFlatQuietRatio)
                                                                                foreach (var evc in ExitFlatVolContextMax)
                                                                                    foreach (var etr in ExitFlatTradeRateMax)
                                                                                    {
                                                                                        id++;
                                                                                        if (id > maxCount) goto Done;

                                                                                        var pset = new Dictionary<Breakout2.ParamKey, double>
        {
            { Breakout2.ParamKey.AbsorptionThreshold, a },
            { Breakout2.ParamKey.ReversalExtraStrength, r },
            { Breakout2.ParamKey.MinSignalStrength, m },
            { Breakout2.ParamKey.VelocityToDepthRatio, v },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, p },
            { Breakout2.ParamKey.MaxVolumeRatio, mvr },
            { Breakout2.ParamKey.MinRatioDifference, mrd },

            { Breakout2.ParamKey.SpikeMinRelativeIncrease, smri },
            { Breakout2.ParamKey.SpikeWeightScale, sws },
            { Breakout2.ParamKey.SpikeWeightCap, swc },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, svws },

            { Breakout2.ParamKey.TradeRateShareMin, tr },
            { Breakout2.ParamKey.TradeEventShareMin, te },

            { Breakout2.ParamKey.ExitOppositeSignalStrength, eos },
            { Breakout2.ParamKey.ExitRsiDevThreshold, erd },

            { Breakout2.ParamKey.ExitFlatBidRangeMax, ebr },
            { Breakout2.ParamKey.ExitFlatQuietRatio, eqr },
            { Breakout2.ParamKey.ExitFlatVolContextMax, evc },
            { Breakout2.ParamKey.ExitFlatTradeRateMax, etr }
        };

                                                                                        string name = $"B2_MRB5_G{id.ToString().PadLeft(3, '0')}";
                                                                                        list.Add((name, pset));
                                                                                    }

Done:
            return list;
        }

        // Ready-to-use: 192 deterministic sets
        public static readonly List<(string Name, Dictionary<Breakout2.ParamKey, double> Parameters)>
            BreakoutParameterSets = GenerateParameterSetsStructured(192);



        //    public static readonly List<(string Name, Dictionary<Breakout2.ParamKey, double> Parameters)> BreakoutParameterSets = new List<(string, Dictionary<Breakout2.ParamKey, double>)>
        //    {
        //           // ===== Cluster A: MRB_Focus_05 anchor (42 MP, 8 profit) =====
        //    // Anchor cues: Absorption 17.2, Reversal 3.0, MinSig ~2.9, ExitOpp ~2.1, ExitRSI ~6.4, TR 0.60, TE 0.40, SpikeW ~0.3, Cap ~5.0, Vol 0.5
        //    ("B2_MRB5_A01", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.0},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.35},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB5_A02", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.6},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB5_A03", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.8},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.100},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.SpikeWeightScale,0.40},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB5_A04", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.2},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8},{Breakout2.ParamKey.SpikeWeightScale,0.28},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB5_A05", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.4},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB5_A06", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.9},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.112},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB5_A07", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.8},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.106},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.7} }),
        //    ("B2_MRB5_A08", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.6},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.098},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.SpikeWeightScale,0.45},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB5_A09", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.4},{Breakout2.ParamKey.ReversalExtraStrength,3.4},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.SpikeWeightScale,0.28},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB5_A10", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.1},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.104},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB5_A11", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.9},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.113},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB5_A12", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.7},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.101},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.42},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB5_A13", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.0},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.7} }),
        //    ("B2_MRB5_A14", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.5},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.099},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.SpikeWeightScale,0.48},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),

        //    // ===== Cluster B: MRB_Focus_09 anchor (40 MP, 7 profit; higher thresholds) =====
        //    ("B2_MRB9_B01", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.8},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB9_B02", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.2},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.112},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB9_B03", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.4},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.SpikeWeightScale,0.28},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB9_B04", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.6},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.106},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB9_B05", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.0},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB9_B06", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.4},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.104},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB9_B07", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.6},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.9},{Breakout2.ParamKey.SpikeWeightScale,0.28},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB9_B08", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.2},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.102},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.SpikeWeightScale,0.44},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB9_B09", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.1},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.7},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB9_B10", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.0},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.107},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.SpikeWeightScale,0.36},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB9_B11", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.3},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.SpikeWeightScale,0.30},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB9_B12", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.5},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB9_B13", new() { {Breakout2.ParamKey.AbsorptionThreshold,18.5},{Breakout2.ParamKey.ReversalExtraStrength,3.3},{Breakout2.ParamKey.MinSignalStrength,3.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8},{Breakout2.ParamKey.SpikeWeightScale,0.28},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB9_B14", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.3},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.40},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),

        //    // ===== Cluster C: MRB_Focus_03 anchor (42 MP, 7 profit; slightly softer than _09) =====
        //    ("B2_MRB3_C01", new() { {Breakout2.ParamKey.AbsorptionThreshold,15.6},{Breakout2.ParamKey.ReversalExtraStrength,2.6},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.102},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.SpikeWeightScale,0.42},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB3_C02", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.2},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.105},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB3_C03", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.6},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.SpikeWeightScale,0.36},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB3_C04", new() { {Breakout2.ParamKey.AbsorptionThreshold,15.8},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.100},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.44},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB3_C05", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.8},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB3_C06", new() { {Breakout2.ParamKey.AbsorptionThreshold,15.5},{Breakout2.ParamKey.ReversalExtraStrength,2.5},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.099},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.57},{Breakout2.ParamKey.TradeEventShareMin,0.37},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.SpikeWeightScale,0.48},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB3_C07", new() { {Breakout2.ParamKey.AbsorptionThreshold,17.0},{Breakout2.ParamKey.ReversalExtraStrength,3.2},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.112},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB3_C08", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.1},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.104},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3},{Breakout2.ParamKey.SpikeWeightScale,0.40},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB3_C09", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.9},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.111},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB3_C10", new() { {Breakout2.ParamKey.AbsorptionThreshold,15.7},{Breakout2.ParamKey.ReversalExtraStrength,2.7},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.101},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2},{Breakout2.ParamKey.SpikeWeightScale,0.46},{Breakout2.ParamKey.SpikeWeightCap,5.6},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4} }),
        //    ("B2_MRB3_C11", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.3},{Breakout2.ParamKey.ReversalExtraStrength,2.9},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.106},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.4},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB3_C12", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.5},{Breakout2.ParamKey.ReversalExtraStrength,3.0},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.109},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),
        //    ("B2_MRB3_C13", new() { {Breakout2.ParamKey.AbsorptionThreshold,15.9},{Breakout2.ParamKey.ReversalExtraStrength,2.8},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.103},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.1},{Breakout2.ParamKey.SpikeWeightScale,0.44},{Breakout2.ParamKey.SpikeWeightCap,5.4},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5} }),
        //    ("B2_MRB3_C14", new() { {Breakout2.ParamKey.AbsorptionThreshold,16.7},{Breakout2.ParamKey.ReversalExtraStrength,3.1},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.113},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6},{Breakout2.ParamKey.SpikeWeightScale,0.32},{Breakout2.ParamKey.SpikeWeightCap,5.2},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6} }),

        //    // ===== Cluster D: MaxVThrMin_Focus_04 anchor (65 MP, 12 profit) =====
        //    // Note: velocity cap frozen in code; we vary base V/D and neighbors.
        //    ("B2_MV4_D01", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.TradeRateShareMin,0.57},{Breakout2.ParamKey.TradeEventShareMin,0.37},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.1},{Breakout2.ParamKey.ExitRsiDevThreshold,5.3},{Breakout2.ParamKey.SpikeWeightScale,0.42},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV4_D02", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.120},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.TradeRateShareMin,0.57},{Breakout2.ParamKey.TradeEventShareMin,0.37},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.4},{Breakout2.ParamKey.SpikeWeightScale,0.40},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV4_D03", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.5},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV4_D04", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.TradeRateShareMin,0.56},{Breakout2.ParamKey.TradeEventShareMin,0.36},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.1},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2},{Breakout2.ParamKey.SpikeWeightScale,0.44},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV4_D05", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.119},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.4},{Breakout2.ParamKey.SpikeWeightScale,0.40},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV4_D06", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.6},{Breakout2.ParamKey.SpikeWeightScale,0.36},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV4_D07", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.TradeRateShareMin,0.56},{Breakout2.ParamKey.TradeEventShareMin,0.36},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.3},{Breakout2.ParamKey.SpikeWeightScale,0.46},{Breakout2.ParamKey.SpikeWeightCap,5.4} }),
        //    ("B2_MV4_D08", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.5},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV4_D09", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.TradeRateShareMin,0.57},{Breakout2.ParamKey.TradeEventShareMin,0.37},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.1},{Breakout2.ParamKey.ExitRsiDevThreshold,5.3},{Breakout2.ParamKey.SpikeWeightScale,0.42},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV4_D10", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.124},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.6},{Breakout2.ParamKey.SpikeWeightScale,0.36},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV4_D11", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinSignalStrength,2.1},{Breakout2.ParamKey.TradeRateShareMin,0.56},{Breakout2.ParamKey.TradeEventShareMin,0.36},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,5.1},{Breakout2.ParamKey.SpikeWeightScale,0.48},{Breakout2.ParamKey.SpikeWeightCap,5.4} }),
        //    ("B2_MV4_D12", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.125},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,5.7},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV4_D13", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.113},{Breakout2.ParamKey.MinSignalStrength,2.1},{Breakout2.ParamKey.TradeRateShareMin,0.55},{Breakout2.ParamKey.TradeEventShareMin,0.35},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0},{Breakout2.ParamKey.SpikeWeightScale,0.50},{Breakout2.ParamKey.SpikeWeightCap,5.6} }),
        //    ("B2_MV4_D14", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.4},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.4} }),

        //    // ===== Cluster E: MaxVThrMin_Focus_05 anchor (65 MP, 12 profit; slightly tighter exits) =====
        //    ("B2_MV5_E01", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.121},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0},{Breakout2.ParamKey.SpikeWeightScale,0.40},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV5_E02", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.1},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV5_E03", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2},{Breakout2.ParamKey.SpikeWeightScale,0.36},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV5_E04", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.119},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.TradeRateShareMin,0.57},{Breakout2.ParamKey.TradeEventShareMin,0.37},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0},{Breakout2.ParamKey.SpikeWeightScale,0.42},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV5_E05", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.124},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,5.3},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV5_E06", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.TradeRateShareMin,0.56},{Breakout2.ParamKey.TradeEventShareMin,0.36},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.1},{Breakout2.ParamKey.ExitRsiDevThreshold,5.1},{Breakout2.ParamKey.SpikeWeightScale,0.46},{Breakout2.ParamKey.SpikeWeightCap,5.4} }),
        //    ("B2_MV5_E07", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.120},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.TradeRateShareMin,0.58},{Breakout2.ParamKey.TradeEventShareMin,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2},{Breakout2.ParamKey.SpikeWeightScale,0.40},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV5_E08", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.125},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.4},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV5_E09", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.117},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.TradeRateShareMin,0.57},{Breakout2.ParamKey.TradeEventShareMin,0.37},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.1},{Breakout2.ParamKey.SpikeWeightScale,0.44},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV5_E10", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.3},{Breakout2.ParamKey.SpikeWeightScale,0.38},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),
        //    ("B2_MV5_E11", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.TradeRateShareMin,0.56},{Breakout2.ParamKey.TradeEventShareMin,0.36},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.1},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0},{Breakout2.ParamKey.SpikeWeightScale,0.48},{Breakout2.ParamKey.SpikeWeightCap,5.4} }),
        //    ("B2_MV5_E12", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.126},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.TradeRateShareMin,0.60},{Breakout2.ParamKey.TradeEventShareMin,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.5},{Breakout2.ParamKey.SpikeWeightScale,0.34},{Breakout2.ParamKey.SpikeWeightCap,5.0} }),
        //    ("B2_MV5_E13", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinSignalStrength,2.1},{Breakout2.ParamKey.TradeRateShareMin,0.55},{Breakout2.ParamKey.TradeEventShareMin,0.35},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,4.9},{Breakout2.ParamKey.SpikeWeightScale,0.50},{Breakout2.ParamKey.SpikeWeightCap,5.6} }),
        //    ("B2_MV5_E14", new() { {Breakout2.ParamKey.VelocityToDepthRatio,0.123},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.TradeRateShareMin,0.59},{Breakout2.ParamKey.TradeEventShareMin,0.39},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2},{Breakout2.ParamKey.SpikeWeightScale,0.36},{Breakout2.ParamKey.SpikeWeightCap,5.2} }),

        //    // ===== Cluster F: Confirm sensitivity anchors (Confirm_07/09/10; 68–71 MP, 9 profit) =====
        //    ("B2_CONF_F01", new() { {Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,4.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.105} }),
        //    ("B2_CONF_F02", new() { {Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.7},{Breakout2.ParamKey.ExitRsiDevThreshold,4.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.106} }),
        //    ("B2_CONF_F03", new() { {Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.8},{Breakout2.ParamKey.ExitRsiDevThreshold,4.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.108} }),
        //    ("B2_CONF_F04", new() { {Breakout2.ParamKey.TradeRateShareMin,0.66},{Breakout2.ParamKey.TradeEventShareMin,0.46},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.0},{Breakout2.ParamKey.ExitRsiDevThreshold,4.4},{Breakout2.ParamKey.VelocityToDepthRatio,0.110} }),
        //    ("B2_CONF_F05", new() { {Breakout2.ParamKey.TradeRateShareMin,0.68},{Breakout2.ParamKey.TradeEventShareMin,0.48},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.2},{Breakout2.ParamKey.ExitRsiDevThreshold,4.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.112} }),
        //    ("B2_CONF_F06", new() { {Breakout2.ParamKey.TradeRateShareMin,0.61},{Breakout2.ParamKey.TradeEventShareMin,0.41},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,4.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.104} }),
        //    ("B2_CONF_F07", new() { {Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,4.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.107} }),
        //    ("B2_CONF_F08", new() { {Breakout2.ParamKey.TradeRateShareMin,0.63},{Breakout2.ParamKey.TradeEventShareMin,0.43},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.7},{Breakout2.ParamKey.ExitRsiDevThreshold,4.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.109} }),
        //    ("B2_CONF_F09", new() { {Breakout2.ParamKey.TradeRateShareMin,0.65},{Breakout2.ParamKey.TradeEventShareMin,0.45},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.9},{Breakout2.ParamKey.ExitRsiDevThreshold,4.5},{Breakout2.ParamKey.VelocityToDepthRatio,0.111} }),
        //    ("B2_CONF_F10", new() { {Breakout2.ParamKey.TradeRateShareMin,0.67},{Breakout2.ParamKey.TradeEventShareMin,0.47},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.1},{Breakout2.ParamKey.ExitRsiDevThreshold,4.3},{Breakout2.ParamKey.VelocityToDepthRatio,0.113} }),
        //    ("B2_CONF_F11", new() { {Breakout2.ParamKey.TradeRateShareMin,0.62},{Breakout2.ParamKey.TradeEventShareMin,0.42},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,4.9},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6} }),
        //    ("B2_CONF_F12", new() { {Breakout2.ParamKey.TradeRateShareMin,0.64},{Breakout2.ParamKey.TradeEventShareMin,0.44},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.8},{Breakout2.ParamKey.ExitRsiDevThreshold,4.6},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7} }),
        //    ("B2_CONF_F13", new() { {Breakout2.ParamKey.TradeRateShareMin,0.66},{Breakout2.ParamKey.TradeEventShareMin,0.46},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.0},{Breakout2.ParamKey.ExitRsiDevThreshold,4.4},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7} }),
        //    ("B2_CONF_F14", new() { {Breakout2.ParamKey.TradeRateShareMin,0.68},{Breakout2.ParamKey.TradeEventShareMin,0.48},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.2},{Breakout2.ParamKey.ExitRsiDevThreshold,4.2},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8} }),
        //};

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