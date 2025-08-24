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

        public static readonly List<(string Name, Dictionary<Breakout2.ParamKey, double> Parameters)> BreakoutParameterSets = new List<(string, Dictionary<Breakout2.ParamKey, double>)>
        {
            // --- Control: near-default, for repeatability (5) ---
            ("B2_Control_01", new() { {Breakout2.ParamKey.MinSignalStrength, 2.0}, {Breakout2.ParamKey.ExitOppositeSignalStrength, 3.0}, {Breakout2.ParamKey.ExitRsiDevThreshold, 5.0} }),
            ("B2_Control_02", new() { {Breakout2.ParamKey.MinSignalStrength, 2.2}, {Breakout2.ParamKey.ExitOppositeSignalStrength, 3.2}, {Breakout2.ParamKey.ExitRsiDevThreshold, 5.0} }),
            ("B2_Control_03", new() { {Breakout2.ParamKey.MinSignalStrength, 1.8}, {Breakout2.ParamKey.ExitOppositeSignalStrength, 2.8}, {Breakout2.ParamKey.ExitRsiDevThreshold, 5.5} }),
            ("B2_Control_04", new() { {Breakout2.ParamKey.MinSignalStrength, 2.0}, {Breakout2.ParamKey.ExitOppositeSignalStrength, 3.5}, {Breakout2.ParamKey.ExitRsiDevThreshold, 4.5} }),
            ("B2_Control_05", new() { {Breakout2.ParamKey.MinSignalStrength, 2.0}, {Breakout2.ParamKey.ExitOppositeSignalStrength, 2.5}, {Breakout2.ParamKey.ExitRsiDevThreshold, 6.0} }),

            // --- Conservative cluster (10) ---
            ("B2_Conservative_01", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.0},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.095},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,4.2},{Breakout2.ParamKey.MinRatioDifference,0.065},{Breakout2.ParamKey.ReversalExtraStrength,1.3},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.22},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_Conservative_02", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.5},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.092},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,4.0},{Breakout2.ParamKey.MinRatioDifference,0.07},{Breakout2.ParamKey.ReversalExtraStrength,1.4},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.9},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.21},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,4.5} }),
            ("B2_Conservative_03", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.8},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.089},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.MaxVolumeRatio,3.8},{Breakout2.ParamKey.MinRatioDifference,0.07},{Breakout2.ParamKey.ReversalExtraStrength,1.5},{Breakout2.ParamKey.SpikeMinRelativeIncrease,3.0},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.20},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,4.0} }),
            ("B2_Conservative_04", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.2},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.094},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,4.3},{Breakout2.ParamKey.MinRatioDifference,0.066},{Breakout2.ParamKey.ReversalExtraStrength,1.3},{Breakout2.ParamKey.SpikeMinRelativeIncrease,3.1},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.21},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),
            ("B2_Conservative_05", new() { {Breakout2.ParamKey.AbsorptionThreshold,13.0},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.090},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.MaxVolumeRatio,3.7},{Breakout2.ParamKey.MinRatioDifference,0.072},{Breakout2.ParamKey.ReversalExtraStrength,1.6},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.7},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.20},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.7},{Breakout2.ParamKey.ExitRsiDevThreshold,4.0} }),
            ("B2_Conservative_06", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.4},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.093},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,4.1},{Breakout2.ParamKey.MinRatioDifference,0.066},{Breakout2.ParamKey.ReversalExtraStrength,1.2},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.8},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.22},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.1},{Breakout2.ParamKey.ExitRsiDevThreshold,5.5} }),
            ("B2_Conservative_07", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.9},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.088},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,8},{Breakout2.ParamKey.MaxVolumeRatio,3.9},{Breakout2.ParamKey.MinRatioDifference,0.069},{Breakout2.ParamKey.ReversalExtraStrength,1.5},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.20},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,4.2} }),
            ("B2_Conservative_08", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.1},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.096},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,4.4},{Breakout2.ParamKey.MinRatioDifference,0.065},{Breakout2.ParamKey.ReversalExtraStrength,1.3},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.9},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.22},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8} }),
            ("B2_Conservative_09", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.6},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.091},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,4.0},{Breakout2.ParamKey.MinRatioDifference,0.068},{Breakout2.ParamKey.ReversalExtraStrength,1.4},{Breakout2.ParamKey.SpikeMinRelativeIncrease,3.2},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.21},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,4.8} }),
            ("B2_Conservative_10", new() { {Breakout2.ParamKey.AbsorptionThreshold,12.3},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.094},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,4.2},{Breakout2.ParamKey.MinRatioDifference,0.066},{Breakout2.ParamKey.ReversalExtraStrength,1.2},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.7},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.22},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2} }),

            // --- Balanced cluster (10) ---
            ("B2_Balanced_01", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.8},{Breakout2.ParamKey.MinSignalStrength,2.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.5},{Breakout2.ParamKey.MinRatioDifference,0.050},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.24},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_Balanced_02", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.0},{Breakout2.ParamKey.MinSignalStrength,2.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.055},{Breakout2.ParamKey.ReversalExtraStrength,1.15},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.23},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},{Breakout2.ParamKey.ExitRsiDevThreshold,5.5} }),
            ("B2_Balanced_03", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.6},{Breakout2.ParamKey.MinSignalStrength,2.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.8},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.ReversalExtraStrength,1.05},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.24},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),
            ("B2_Balanced_04", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.9},{Breakout2.ParamKey.MinSignalStrength,2.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.112},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.2},{Breakout2.ParamKey.MinRatioDifference,0.052},{Breakout2.ParamKey.ReversalExtraStrength,1.10},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.23},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.9},{Breakout2.ParamKey.ExitRsiDevThreshold,4.8} }),
            ("B2_Balanced_05", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.7},{Breakout2.ParamKey.MinSignalStrength,2.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.116},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.5},{Breakout2.ParamKey.MinRatioDifference,0.051},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.24},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5} }),
            ("B2_Balanced_06", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.8},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.113},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.3},{Breakout2.ParamKey.MinRatioDifference,0.053},{Breakout2.ParamKey.ReversalExtraStrength,1.12},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.23},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8} }),
            ("B2_Balanced_07", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.2},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.VelocityToDepthRatio,0.108},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,5.8},{Breakout2.ParamKey.MinRatioDifference,0.056},{Breakout2.ParamKey.ReversalExtraStrength,1.2},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.22},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,4.5} }),
            ("B2_Balanced_08", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.5},{Breakout2.ParamKey.MinSignalStrength,1.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.120},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.9},{Breakout2.ParamKey.MinRatioDifference,0.047},{Breakout2.ParamKey.ReversalExtraStrength,1.05},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.25},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8} }),
            ("B2_Balanced_09", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.0},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,6.0},{Breakout2.ParamKey.MinRatioDifference,0.055},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.24},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2} }),
            ("B2_Balanced_10", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.9},{Breakout2.ParamKey.MinSignalStrength,2.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.114},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,6.4},{Breakout2.ParamKey.MinRatioDifference,0.052},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.24},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},{Breakout2.ParamKey.ExitRsiDevThreshold,5.7} }),

            // --- Aggressive cluster (10) ---
            ("B2_Aggressive_01", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.0},{Breakout2.ParamKey.MinSignalStrength,1.4},{Breakout2.ParamKey.VelocityToDepthRatio,0.14},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,9.0},{Breakout2.ParamKey.MinRatioDifference,0.035},{Breakout2.ParamKey.ReversalExtraStrength,0.95},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.28},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,7.0} }),
            ("B2_Aggressive_02", new() { {Breakout2.ParamKey.AbsorptionThreshold,8.8},{Breakout2.ParamKey.MinSignalStrength,1.3},{Breakout2.ParamKey.VelocityToDepthRatio,0.15},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,10.0},{Breakout2.ParamKey.MinRatioDifference,0.030},{Breakout2.ParamKey.ReversalExtraStrength,0.9},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.30},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,7.5} }),
            ("B2_Aggressive_03", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.2},{Breakout2.ParamKey.MinSignalStrength,1.5},{Breakout2.ParamKey.VelocityToDepthRatio,0.135},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,8.5},{Breakout2.ParamKey.MinRatioDifference,0.035},{Breakout2.ParamKey.ReversalExtraStrength,1.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.75},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.27},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8} }),
            ("B2_Aggressive_04", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.5},{Breakout2.ParamKey.MinSignalStrength,1.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.13},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,12.0},{Breakout2.ParamKey.MinRatioDifference,0.030},{Breakout2.ParamKey.ReversalExtraStrength,0.9},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.65},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.28},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5} }),
            ("B2_Aggressive_05", new() { {Breakout2.ParamKey.AbsorptionThreshold,8.6},{Breakout2.ParamKey.MinSignalStrength,1.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.16},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,3},{Breakout2.ParamKey.MaxVolumeRatio,12.5},{Breakout2.ParamKey.MinRatioDifference,0.030},{Breakout2.ParamKey.ReversalExtraStrength,0.85},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.6},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.32},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,7.8} }),
            ("B2_Aggressive_06", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.3},{Breakout2.ParamKey.MinSignalStrength,1.4},{Breakout2.ParamKey.VelocityToDepthRatio,0.145},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,3},{Breakout2.ParamKey.MaxVolumeRatio,11.0},{Breakout2.ParamKey.MinRatioDifference,0.033},{Breakout2.ParamKey.ReversalExtraStrength,0.95},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.7},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.29},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,7.2} }),
            ("B2_Aggressive_07", new() { {Breakout2.ParamKey.AbsorptionThreshold,8.9},{Breakout2.ParamKey.MinSignalStrength,1.3},{Breakout2.ParamKey.VelocityToDepthRatio,0.152},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,3},{Breakout2.ParamKey.MaxVolumeRatio,13.0},{Breakout2.ParamKey.MinRatioDifference,0.028},{Breakout2.ParamKey.ReversalExtraStrength,0.9},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.55},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.31},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,7.0} }),
            ("B2_Aggressive_08", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.1},{Breakout2.ParamKey.MinSignalStrength,1.5},{Breakout2.ParamKey.VelocityToDepthRatio,0.138},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,9.5},{Breakout2.ParamKey.MinRatioDifference,0.034},{Breakout2.ParamKey.ReversalExtraStrength,0.98},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.27},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2} }),
            ("B2_Aggressive_09", new() { {Breakout2.ParamKey.AbsorptionThreshold,8.7},{Breakout2.ParamKey.MinSignalStrength,1.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.158},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,3},{Breakout2.ParamKey.MaxVolumeRatio,13.5},{Breakout2.ParamKey.MinRatioDifference,0.028},{Breakout2.ParamKey.ReversalExtraStrength,0.85},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.55},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.33},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,8.0} }),
            ("B2_Aggressive_10", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.4},{Breakout2.ParamKey.MinSignalStrength,1.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.142},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,10.5},{Breakout2.ParamKey.MinRatioDifference,0.032},{Breakout2.ParamKey.ReversalExtraStrength,0.95},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.7},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.29},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),

            // --- Anti-Spike cluster (10) ---
            ("B2_AntiSpike_A", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,999.0},{Breakout2.ParamKey.SpikeWeightScale,0.0},{Breakout2.ParamKey.SpikeWeightCap,0.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.1},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_AntiSpike_B", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,500.0},{Breakout2.ParamKey.SpikeWeightScale,0.1},{Breakout2.ParamKey.SpikeWeightCap,0.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,4.8} }),
            ("B2_AntiSpike_C", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,999.0},{Breakout2.ParamKey.SpikeWeightScale,0.0},{Breakout2.ParamKey.SpikeWeightCap,0.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,2.8},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.5} }),
            ("B2_AntiSpike_D", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,400.0},{Breakout2.ParamKey.SpikeWeightScale,0.2},{Breakout2.ParamKey.SpikeWeightCap,1.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.1},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8} }),
            ("B2_AntiSpike_E", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,999.0},{Breakout2.ParamKey.SpikeWeightScale,0.0},{Breakout2.ParamKey.SpikeWeightCap,0.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,2.9},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,4.5} }),
            ("B2_AntiSpike_F", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,300.0},{Breakout2.ParamKey.SpikeWeightScale,0.3},{Breakout2.ParamKey.SpikeWeightCap,1.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),
            ("B2_AntiSpike_G", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,999.0},{Breakout2.ParamKey.SpikeWeightScale,0.0},{Breakout2.ParamKey.SpikeWeightCap,0.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,3.0},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.7},{Breakout2.ParamKey.ExitRsiDevThreshold,4.2} }),
            ("B2_AntiSpike_H", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,600.0},{Breakout2.ParamKey.SpikeWeightScale,0.15},{Breakout2.ParamKey.SpikeWeightCap,0.8},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.05},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2} }),
            ("B2_AntiSpike_I", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,999.0},{Breakout2.ParamKey.SpikeWeightScale,0.0},{Breakout2.ParamKey.SpikeWeightCap,0.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,3.1},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.9},{Breakout2.ParamKey.ExitRsiDevThreshold,4.0} }),
            ("B2_AntiSpike_J", new() { {Breakout2.ParamKey.SpikeMinRelativeIncrease,700.0},{Breakout2.ParamKey.SpikeWeightScale,0.12},{Breakout2.ParamKey.SpikeWeightCap,0.7},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.0},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.6} }),

            // --- Spike-only cluster (10) ---
            ("B2_SpikeOnly_01", new() { {Breakout2.ParamKey.VelocityToDepthRatio,10.0},{Breakout2.ParamKey.MinSignalStrength,1.2},{Breakout2.ParamKey.MaxVolumeRatio,100.0},{Breakout2.ParamKey.MinRatioDifference,10.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.5},{Breakout2.ParamKey.SpikeWeightScale,1.2},{Breakout2.ParamKey.SpikeWeightCap,9.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,1.0},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,7.0} }),
            ("B2_SpikeOnly_02", new() { {Breakout2.ParamKey.VelocityToDepthRatio,8.0},{Breakout2.ParamKey.MinSignalStrength,1.3},{Breakout2.ParamKey.MaxVolumeRatio,80.0},{Breakout2.ParamKey.MinRatioDifference,8.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.6},{Breakout2.ParamKey.SpikeWeightScale,1.4},{Breakout2.ParamKey.SpikeWeightCap,8.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.8},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5} }),
            ("B2_SpikeOnly_03", new() { {Breakout2.ParamKey.VelocityToDepthRatio,6.0},{Breakout2.ParamKey.MinSignalStrength,1.4},{Breakout2.ParamKey.MaxVolumeRatio,60.0},{Breakout2.ParamKey.MinRatioDifference,6.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.7},{Breakout2.ParamKey.SpikeWeightScale,1.6},{Breakout2.ParamKey.SpikeWeightCap,7.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.7},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),
            ("B2_SpikeOnly_04", new() { {Breakout2.ParamKey.VelocityToDepthRatio,12.0},{Breakout2.ParamKey.MinSignalStrength,1.1},{Breakout2.ParamKey.MaxVolumeRatio,120.0},{Breakout2.ParamKey.MinRatioDifference,12.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.4},{Breakout2.ParamKey.SpikeWeightScale,1.8},{Breakout2.ParamKey.SpikeWeightCap,10.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,1.2},{Breakout2.ParamKey.ExitOppositeSignalStrength,1.8},{Breakout2.ParamKey.ExitRsiDevThreshold,7.5} }),
            ("B2_SpikeOnly_05", new() { {Breakout2.ParamKey.VelocityToDepthRatio,9.0},{Breakout2.ParamKey.MinSignalStrength,1.5},{Breakout2.ParamKey.MaxVolumeRatio,90.0},{Breakout2.ParamKey.MinRatioDifference,9.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.6},{Breakout2.ParamKey.SpikeWeightScale,1.5},{Breakout2.ParamKey.SpikeWeightCap,8.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.9},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8} }),
            ("B2_SpikeOnly_06", new() { {Breakout2.ParamKey.VelocityToDepthRatio,5.0},{Breakout2.ParamKey.MinSignalStrength,1.6},{Breakout2.ParamKey.MaxVolumeRatio,50.0},{Breakout2.ParamKey.MinRatioDifference,5.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.SpikeWeightScale,1.7},{Breakout2.ParamKey.SpikeWeightCap,6.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.6},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),
            ("B2_SpikeOnly_07", new() { {Breakout2.ParamKey.VelocityToDepthRatio,7.0},{Breakout2.ParamKey.MinSignalStrength,1.2},{Breakout2.ParamKey.MaxVolumeRatio,70.0},{Breakout2.ParamKey.MinRatioDifference,7.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.5},{Breakout2.ParamKey.SpikeWeightScale,1.3},{Breakout2.ParamKey.SpikeWeightCap,7.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.8},{Breakout2.ParamKey.ExitOppositeSignalStrength,1.9},{Breakout2.ParamKey.ExitRsiDevThreshold,7.2} }),
            ("B2_SpikeOnly_08", new() { {Breakout2.ParamKey.VelocityToDepthRatio,11.0},{Breakout2.ParamKey.MinSignalStrength,1.3},{Breakout2.ParamKey.MaxVolumeRatio,110.0},{Breakout2.ParamKey.MinRatioDifference,11.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.4},{Breakout2.ParamKey.SpikeWeightScale,1.9},{Breakout2.ParamKey.SpikeWeightCap,9.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,1.1},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,7.8} }),
            ("B2_SpikeOnly_09", new() { {Breakout2.ParamKey.VelocityToDepthRatio,4.0},{Breakout2.ParamKey.MinSignalStrength,1.7},{Breakout2.ParamKey.MaxVolumeRatio,40.0},{Breakout2.ParamKey.MinRatioDifference,4.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.SpikeWeightScale,1.8},{Breakout2.ParamKey.SpikeWeightCap,6.0},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.5},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8} }),
            ("B2_SpikeOnly_10", new() { {Breakout2.ParamKey.VelocityToDepthRatio,3.0},{Breakout2.ParamKey.MinSignalStrength,1.8},{Breakout2.ParamKey.MaxVolumeRatio,30.0},{Breakout2.ParamKey.MinRatioDifference,3.0},{Breakout2.ParamKey.ReversalExtraStrength,0.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.SpikeWeightScale,2.0},{Breakout2.ParamKey.SpikeWeightCap,5.5},{Breakout2.ParamKey.SpikeVolumeWeightScale,0.4},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2} }),

            // --- High Liquidity cluster (10) ---
            ("B2_HighLiq_01", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.0},{Breakout2.ParamKey.MinSignalStrength,1.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.12},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,9.0},{Breakout2.ParamKey.MinRatioDifference,0.045},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.27},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_HighLiq_02", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.2},{Breakout2.ParamKey.MinSignalStrength,2.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.115},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,9.5},{Breakout2.ParamKey.MinRatioDifference,0.042},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.28},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.3} }),
            ("B2_HighLiq_03", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.9},{Breakout2.ParamKey.MinSignalStrength,1.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.125},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,10.0},{Breakout2.ParamKey.MinRatioDifference,0.040},{Breakout2.ParamKey.ReversalExtraStrength,1.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.30},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),
            ("B2_HighLiq_04", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.4},{Breakout2.ParamKey.MinSignalStrength,2.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,8.5},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.ReversalExtraStrength,1.2},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.2},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.26},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,4.8} }),
            ("B2_HighLiq_05", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.7},{Breakout2.ParamKey.MinSignalStrength,1.9},{Breakout2.ParamKey.VelocityToDepthRatio,0.122},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,9.2},{Breakout2.ParamKey.MinRatioDifference,0.044},{Breakout2.ParamKey.ReversalExtraStrength,1.05},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.0},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.29},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.6} }),
            ("B2_HighLiq_06", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.1},{Breakout2.ParamKey.MinSignalStrength,2.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.118},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,5},{Breakout2.ParamKey.MaxVolumeRatio,9.7},{Breakout2.ParamKey.MinRatioDifference,0.043},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.1},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.28},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.1} }),
            ("B2_HighLiq_07", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.8},{Breakout2.ParamKey.MinSignalStrength,1.8},{Breakout2.ParamKey.VelocityToDepthRatio,0.124},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,10.5},{Breakout2.ParamKey.MinRatioDifference,0.039},{Breakout2.ParamKey.ReversalExtraStrength,1.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.9},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.30},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.3} }),
            ("B2_HighLiq_08", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.3},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.112},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,8.8},{Breakout2.ParamKey.MinRatioDifference,0.046},{Breakout2.ParamKey.ReversalExtraStrength,1.2},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.27},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.9},{Breakout2.ParamKey.ExitRsiDevThreshold,4.6} }),
            ("B2_HighLiq_09", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.6},{Breakout2.ParamKey.MinSignalStrength,1.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.126},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,4},{Breakout2.ParamKey.MaxVolumeRatio,10.8},{Breakout2.ParamKey.MinRatioDifference,0.038},{Breakout2.ParamKey.ReversalExtraStrength,1.0},{Breakout2.ParamKey.SpikeMinRelativeIncrease,1.8},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.31},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.1},{Breakout2.ParamKey.ExitRsiDevThreshold,6.6} }),
            ("B2_HighLiq_10", new() { {Breakout2.ParamKey.AbsorptionThreshold,11.5},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.VelocityToDepthRatio,0.110},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,8.3},{Breakout2.ParamKey.MinRatioDifference,0.048},{Breakout2.ParamKey.ReversalExtraStrength,1.25},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.26},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,4.4} }),

            // --- Low Liquidity cluster (10) ---
            ("B2_LowLiq_01", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.0},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.VelocityToDepthRatio,0.09},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,3.0},{Breakout2.ParamKey.MinRatioDifference,0.060},{Breakout2.ParamKey.ReversalExtraStrength,1.25},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.6},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.20},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.5},{Breakout2.ParamKey.ExitRsiDevThreshold,4.0} }),
            ("B2_LowLiq_02", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.5},{Breakout2.ParamKey.MinSignalStrength,2.4},{Breakout2.ParamKey.VelocityToDepthRatio,0.088},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,2.8},{Breakout2.ParamKey.MinRatioDifference,0.062},{Breakout2.ParamKey.ReversalExtraStrength,1.3},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.7},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.20},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,3.8} }),
            ("B2_LowLiq_03", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.8},{Breakout2.ParamKey.MinSignalStrength,2.2},{Breakout2.ParamKey.VelocityToDepthRatio,0.095},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,3.2},{Breakout2.ParamKey.MinRatioDifference,0.058},{Breakout2.ParamKey.ReversalExtraStrength,1.2},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.21},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,4.6} }),
            ("B2_LowLiq_04", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.2},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.VelocityToDepthRatio,0.09},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,2.6},{Breakout2.ParamKey.MinRatioDifference,0.064},{Breakout2.ParamKey.ReversalExtraStrength,1.35},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.8},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.19},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.8},{Breakout2.ParamKey.ExitRsiDevThreshold,3.6} }),
            ("B2_LowLiq_05", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.3},{Breakout2.ParamKey.MinSignalStrength,2.6},{Breakout2.ParamKey.VelocityToDepthRatio,0.087},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,2.4},{Breakout2.ParamKey.MinRatioDifference,0.066},{Breakout2.ParamKey.ReversalExtraStrength,1.4},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.9},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.19},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.0},{Breakout2.ParamKey.ExitRsiDevThreshold,3.4} }),
            ("B2_LowLiq_06", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.9},{Breakout2.ParamKey.MinSignalStrength,2.1},{Breakout2.ParamKey.VelocityToDepthRatio,0.096},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,3.4},{Breakout2.ParamKey.MinRatioDifference,0.057},{Breakout2.ParamKey.ReversalExtraStrength,1.15},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.4},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.21},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,4.8} }),
            ("B2_LowLiq_07", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.1},{Breakout2.ParamKey.MinSignalStrength,2.3},{Breakout2.ParamKey.VelocityToDepthRatio,0.092},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,3.1},{Breakout2.ParamKey.MinRatioDifference,0.061},{Breakout2.ParamKey.ReversalExtraStrength,1.25},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.5},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.20},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,4.2} }),
            ("B2_LowLiq_08", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.4},{Breakout2.ParamKey.MinSignalStrength,2.5},{Breakout2.ParamKey.VelocityToDepthRatio,0.089},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,2.7},{Breakout2.ParamKey.MinRatioDifference,0.063},{Breakout2.ParamKey.ReversalExtraStrength,1.3},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.7},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.20},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.7},{Breakout2.ParamKey.ExitRsiDevThreshold,3.7} }),
            ("B2_LowLiq_09", new() { {Breakout2.ParamKey.AbsorptionThreshold,9.7},{Breakout2.ParamKey.MinSignalStrength,2.0},{Breakout2.ParamKey.VelocityToDepthRatio,0.098},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,6},{Breakout2.ParamKey.MaxVolumeRatio,3.6},{Breakout2.ParamKey.MinRatioDifference,0.056},{Breakout2.ParamKey.ReversalExtraStrength,1.1},{Breakout2.ParamKey.SpikeMinRelativeIncrease,2.3},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.22},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_LowLiq_10", new() { {Breakout2.ParamKey.AbsorptionThreshold,10.6},{Breakout2.ParamKey.MinSignalStrength,2.7},{Breakout2.ParamKey.VelocityToDepthRatio,0.086},{Breakout2.ParamKey.MinNumberOfPointsFromResolved,7},{Breakout2.ParamKey.MaxVolumeRatio,2.3},{Breakout2.ParamKey.MinRatioDifference,0.067},{Breakout2.ParamKey.ReversalExtraStrength,1.45},{Breakout2.ParamKey.SpikeMinRelativeIncrease,3.0},{Breakout2.ParamKey.MaxVelocityThresholdRatio,0.19},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.2},{Breakout2.ParamKey.ExitRsiDevThreshold,3.2} }),

            // --- Volume-Amplified cluster (10) ---
            ("B2_VolumeAmp_01", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,0.7},{Breakout2.ParamKey.SpikeWeightScale,1.1},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_VolumeAmp_02", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,0.8},{Breakout2.ParamKey.SpikeWeightScale,1.2},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.5},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2} }),
            ("B2_VolumeAmp_03", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,0.9},{Breakout2.ParamKey.SpikeWeightScale,1.3},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.5} }),
            ("B2_VolumeAmp_04", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,1.0},{Breakout2.ParamKey.SpikeWeightScale,1.4},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8} }),
            ("B2_VolumeAmp_05", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,1.1},{Breakout2.ParamKey.SpikeWeightScale,1.5},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},{Breakout2.ParamKey.ExitRsiDevThreshold,6.0} }),
            ("B2_VolumeAmp_06", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,1.2},{Breakout2.ParamKey.SpikeWeightScale,1.6},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,6.2} }),
            ("B2_VolumeAmp_07", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,0.6},{Breakout2.ParamKey.SpikeWeightScale,1.0},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.3},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_VolumeAmp_08", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,1.3},{Breakout2.ParamKey.SpikeWeightScale,1.7},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,6.5} }),
            ("B2_VolumeAmp_09", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,1.4},{Breakout2.ParamKey.SpikeWeightScale,1.8},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,6.8} }),
            ("B2_VolumeAmp_10", new() { {Breakout2.ParamKey.SpikeVolumeWeightScale,1.5},{Breakout2.ParamKey.SpikeWeightScale,2.0},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,7.0} }),

            // --- Confirmations sensitivity (10) ---
            ("B2_Confirm_01", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.50},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.30},{Breakout2.ParamKey.TradeRateShareMin_No,0.50},{Breakout2.ParamKey.TradeEventShareMin_No,0.30},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.6},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_Confirm_02", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.52},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.32},{Breakout2.ParamKey.TradeRateShareMin_No,0.52},{Breakout2.ParamKey.TradeEventShareMin_No,0.32},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.7},{Breakout2.ParamKey.ExitRsiDevThreshold,5.0} }),
            ("B2_Confirm_03", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.54},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.34},{Breakout2.ParamKey.TradeRateShareMin_No,0.54},{Breakout2.ParamKey.TradeEventShareMin_No,0.34},{Breakout2.ParamKey.ExitOppositeSignalStrength,2.8},{Breakout2.ParamKey.ExitRsiDevThreshold,5.2} }),
            ("B2_Confirm_04", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.56},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.36},{Breakout2.ParamKey.TradeRateShareMin_No,0.56},{Breakout2.ParamKey.TradeEventShareMin_No,0.36},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.0},{Breakout2.ParamKey.ExitRsiDevThreshold,5.4} }),
            ("B2_Confirm_05", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.58},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.38},{Breakout2.ParamKey.TradeRateShareMin_No,0.58},{Breakout2.ParamKey.TradeEventShareMin_No,0.38},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.2},{Breakout2.ParamKey.ExitRsiDevThreshold,5.6} }),
            ("B2_Confirm_06", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.60},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.40},{Breakout2.ParamKey.TradeRateShareMin_No,0.60},{Breakout2.ParamKey.TradeEventShareMin_No,0.40},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.4},{Breakout2.ParamKey.ExitRsiDevThreshold,5.8} }),
            ("B2_Confirm_07", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.62},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.42},{Breakout2.ParamKey.TradeRateShareMin_No,0.62},{Breakout2.ParamKey.TradeEventShareMin_No,0.42},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.6},{Breakout2.ParamKey.ExitRsiDevThreshold,4.8} }),
            ("B2_Confirm_08", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.64},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.44},{Breakout2.ParamKey.TradeRateShareMin_No,0.64},{Breakout2.ParamKey.TradeEventShareMin_No,0.44},{Breakout2.ParamKey.ExitOppositeSignalStrength,3.8},{Breakout2.ParamKey.ExitRsiDevThreshold,4.6} }),
            ("B2_Confirm_09", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.66},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.46},{Breakout2.ParamKey.TradeRateShareMin_No,0.66},{Breakout2.ParamKey.TradeEventShareMin_No,0.46},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.0},{Breakout2.ParamKey.ExitRsiDevThreshold,4.4} }),
            ("B2_Confirm_10", new() { {Breakout2.ParamKey.TradeRateShareMin_Yes,0.68},{Breakout2.ParamKey.TradeEventShareMin_Yes,0.48},{Breakout2.ParamKey.TradeRateShareMin_No,0.68},{Breakout2.ParamKey.TradeEventShareMin_No,0.48},{Breakout2.ParamKey.ExitOppositeSignalStrength,4.2},{Breakout2.ParamKey.ExitRsiDevThreshold,4.2} }),
        // =========================
        // Massive Reversal Bias (around B2_Extreme_Wild_05_MassiveReversalBias)
        // High absorption threshold + high reversal strength
        // =========================
        ("B2_MRB_Focus_01", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 14.5 },
            { Breakout2.ParamKey.ReversalExtraStrength, 2.2 },
            { Breakout2.ParamKey.MinSignalStrength, 2.4 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.4 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.0 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.6 }
        }),
        ("B2_MRB_Focus_02", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 15.0 },
            { Breakout2.ParamKey.ReversalExtraStrength, 2.6 },
            { Breakout2.ParamKey.MinSignalStrength, 2.6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.2 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 6.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.55 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.55 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.35 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.35 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.5 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.7 }
        }),
        ("B2_MRB_Focus_03", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 16.0 },
            { Breakout2.ParamKey.ReversalExtraStrength, 2.8 },
            { Breakout2.ParamKey.MinSignalStrength, 2.8 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.0 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 6.2 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.58 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.58 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.38 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.38 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.4 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.5 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.5 }
        }),
        ("B2_MRB_Focus_04", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 13.8 },
            { Breakout2.ParamKey.ReversalExtraStrength, 2.0 },
            { Breakout2.ParamKey.MinSignalStrength, 2.2 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.8 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.54 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.54 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.34 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.34 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.7 },
            { Breakout2.ParamKey.SpikeWeightCap, 7.0 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.8 }
        }),
        ("B2_MRB_Focus_05", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 17.2 },
            { Breakout2.ParamKey.ReversalExtraStrength, 3.0 },
            { Breakout2.ParamKey.MinSignalStrength, 2.9 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.1 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 6.4 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.60 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.60 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.40 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.40 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.3 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.0 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.5 }
        }),
        ("B2_MRB_Focus_06", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 14.0 },
            { Breakout2.ParamKey.ReversalExtraStrength, 2.4 },
            { Breakout2.ParamKey.MinSignalStrength, 2.5 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.5 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.2 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.57 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.57 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.37 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.37 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.0 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.6 }
        }),
        ("B2_MRB_Focus_07", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 15.8 },
            { Breakout2.ParamKey.ReversalExtraStrength, 2.9 },
            { Breakout2.ParamKey.MinSignalStrength, 2.7 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.3 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 6.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.59 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.59 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.39 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.39 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.4 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.8 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.7 }
        }),
        ("B2_MRB_Focus_08", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 13.5 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.9 },
            { Breakout2.ParamKey.MinSignalStrength, 2.3 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.9 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.55 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.55 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.35 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.35 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.8 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.8 }
        }),
        ("B2_MRB_Focus_09", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 18.0 },
            { Breakout2.ParamKey.ReversalExtraStrength, 3.0 },
            { Breakout2.ParamKey.MinSignalStrength, 3.0 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.0 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 6.6 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.61 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.61 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.41 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.41 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.3 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.0 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.4 }
        }),
        ("B2_MRB_Focus_10", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 14.8 },
            { Breakout2.ParamKey.ReversalExtraStrength, 2.5 },
            { Breakout2.ParamKey.MinSignalStrength, 2.6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.6 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.6 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.2 },
            { Breakout2.ParamKey.SpikeVolumeWeightScale, 0.6 }
        }),

        // =========================
        // Velocity/Depth Minimum (around B2_Extreme_Edge_03_VelDepth_Min)
        // Lower velocity-to-depth thresholds + tighter cap
        // =========================
        ("B2_VDMin_Focus_01", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.09 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.22 },
            { Breakout2.ParamKey.MinSignalStrength, 2.2 },
            { Breakout2.ParamKey.AbsorptionThreshold, 11.5 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.8 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.4 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.55 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.55 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.35 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.35 }
        }),
        ("B2_VDMin_Focus_02", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.085 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.21 },
            { Breakout2.ParamKey.MinSignalStrength, 2.1 },
            { Breakout2.ParamKey.AbsorptionThreshold, 11.8 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.0 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.2 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),
        ("B2_VDMin_Focus_03", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.08 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.MinSignalStrength, 2.0 },
            { Breakout2.ParamKey.AbsorptionThreshold, 12.0 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.1 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.0 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.5 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.57 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.57 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.37 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.37 }
        }),
        ("B2_VDMin_Focus_04", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.075 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.19 },
            { Breakout2.ParamKey.MinSignalStrength, 2.3 },
            { Breakout2.ParamKey.AbsorptionThreshold, 11.3 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.9 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.6 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.2 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.55 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.55 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.35 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.35 }
        }),
        ("B2_VDMin_Focus_05", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.07 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { Breakout2.ParamKey.MinSignalStrength, 2.4 },
            { Breakout2.ParamKey.AbsorptionThreshold, 11.0 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.0 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.8 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.5 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),
        ("B2_VDMin_Focus_06", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.09 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.MinSignalStrength, 2.1 },
            { Breakout2.ParamKey.AbsorptionThreshold, 12.2 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.7 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.3 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.7 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.54 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.54 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.34 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.34 }
        }),
        ("B2_VDMin_Focus_07", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.085 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.19 },
            { Breakout2.ParamKey.MinSignalStrength, 2.2 },
            { Breakout2.ParamKey.AbsorptionThreshold, 11.7 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.8 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.1 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.7 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.55 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.55 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.35 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.35 }
        }),
        ("B2_VDMin_Focus_08", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.08 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { Breakout2.ParamKey.MinSignalStrength, 2.3 },
            { Breakout2.ParamKey.AbsorptionThreshold, 11.2 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.2 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.4 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),
        ("B2_VDMin_Focus_09", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.075 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.MinSignalStrength, 2.2 },
            { Breakout2.ParamKey.AbsorptionThreshold, 12.0 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 6 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.9 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.6 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.7 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.5 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.55 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.55 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.35 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.35 }
        }),
        ("B2_VDMin_Focus_10", new() {
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.07 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.19 },
            { Breakout2.ParamKey.MinSignalStrength, 2.4 },
            { Breakout2.ParamKey.AbsorptionThreshold, 11.0 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.1 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.0 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.2 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),

        // =========================
        // Max Velocity Threshold MIN (around B2_Extreme_Edge_05_MaxVelThresh_Min)
        // Strong clamp on max threshold
        // =========================
        ("B2_MaxVThrMin_Focus_01", new() {
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.112 },
            { Breakout2.ParamKey.MinSignalStrength, 2.0 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.7 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.2 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.6 },
            { Breakout2.ParamKey.SpikeWeightCap, 6.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),
        ("B2_MaxVThrMin_Focus_02", new() {
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.19 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.115 },
            { Breakout2.ParamKey.MinSignalStrength, 2.1 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 2.9 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.0 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.55 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.55 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.35 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.35 }
        }),
        ("B2_MaxVThrMin_Focus_03", new() {
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.18 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.118 },
            { Breakout2.ParamKey.MinSignalStrength, 2.2 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.1 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.8 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.5 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.5 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),
        ("B2_MaxVThrMin_Focus_04", new() {
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.17 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.120 },
            { Breakout2.ParamKey.MinSignalStrength, 2.3 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.2 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.4 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.4 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.57 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.57 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.37 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.37 }
        }),
        ("B2_MaxVThrMin_Focus_05", new() {
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.16 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.122 },
            { Breakout2.ParamKey.MinSignalStrength, 2.4 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.4 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.0 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.4 },
            { Breakout2.ParamKey.SpikeWeightCap, 5.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.58 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.58 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.38 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.38 }
        }),

        // =========================
        // Min Ratio Difference HUGE (around B2_Extreme_Edge_02_MinRatDiffHuge)
        // Require large flow advantage + stricter confirmations
        // =========================
        ("B2_MinRatDiffHuge_Focus_01", new() {
            { Breakout2.ParamKey.MinRatioDifference, 0.08 },
            { Breakout2.ParamKey.MinSignalStrength, 2.6 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.60 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.60 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.40 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.40 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.4 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.8 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.3 },
            { Breakout2.ParamKey.SpikeWeightCap, 4.8 }
        }),
        ("B2_MinRatDiffHuge_Focus_02", new() {
            { Breakout2.ParamKey.MinRatioDifference, 0.09 },
            { Breakout2.ParamKey.MinSignalStrength, 2.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.62 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.62 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.42 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.42 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.6 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.6 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.25 },
            { Breakout2.ParamKey.SpikeWeightCap, 4.5 }
        }),
        ("B2_MinRatDiffHuge_Focus_03", new() {
            { Breakout2.ParamKey.MinRatioDifference, 0.10 },
            { Breakout2.ParamKey.MinSignalStrength, 3.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.64 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.64 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.44 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.44 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.8 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.5 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.2 },
            { Breakout2.ParamKey.SpikeWeightCap, 4.2 }
        }),
        ("B2_MinRatDiffHuge_Focus_04", new() {
            { Breakout2.ParamKey.MinRatioDifference, 0.12 },
            { Breakout2.ParamKey.MinSignalStrength, 3.1 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.66 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.66 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.46 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.46 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 4.0 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.3 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.15 },
            { Breakout2.ParamKey.SpikeWeightCap, 4.0 }
        }),
        ("B2_MinRatDiffHuge_Focus_05", new() {
            { Breakout2.ParamKey.MinRatioDifference, 0.14 },
            { Breakout2.ParamKey.MinSignalStrength, 3.2 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.68 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.68 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.48 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.48 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 4.2 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.0 },
            { Breakout2.ParamKey.SpikeWeightScale, 0.10 },
            { Breakout2.ParamKey.SpikeWeightCap, 3.8 }
        }),

        // =========================
        // Conservative 04 cluster (around B2_Conservative_04D/E and _02)
        // Tight skew, higher confirmation, modest spikes, position-aware exits
        // =========================
        ("B2_Conservative04_Focus_01", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.0 },
            { Breakout2.ParamKey.MinSignalStrength, 2.7 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.094 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 4.2 },
            { Breakout2.ParamKey.MinRatioDifference, 0.066 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.3 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.0 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.21 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.0 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),
        ("B2_Conservative04_Focus_02", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.4 },
            { Breakout2.ParamKey.MinSignalStrength, 2.8 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.093 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 4.1 },
            { Breakout2.ParamKey.MinRatioDifference, 0.067 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.35 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.1 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.21 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.2 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.4 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.57 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.57 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.37 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.37 }
        }),
        ("B2_Conservative04_Focus_03", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.6 },
            { Breakout2.ParamKey.MinSignalStrength, 2.9 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.092 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 4.0 },
            { Breakout2.ParamKey.MinRatioDifference, 0.068 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.4 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.2 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.3 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.58 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.58 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.38 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.38 }
        }),
        ("B2_Conservative04_Focus_04", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.8 },
            { Breakout2.ParamKey.MinSignalStrength, 3.0 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.091 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 4.0 },
            { Breakout2.ParamKey.MinRatioDifference, 0.070 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.45 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.1 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.5 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.59 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.59 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.39 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.39 }
        }),
        ("B2_Conservative04_Focus_05", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 13.0 },
            { Breakout2.ParamKey.MinSignalStrength, 3.1 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.090 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 3.9 },
            { Breakout2.ParamKey.MinRatioDifference, 0.072 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.5 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.2 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.6 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.6 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.60 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.60 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.40 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.40 }
        }),
        ("B2_Conservative04_Focus_06", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.2 },
            { Breakout2.ParamKey.MinSignalStrength, 2.6 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.095 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 4.3 },
            { Breakout2.ParamKey.MinRatioDifference, 0.066 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.3 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 2.9 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.21 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.0 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 6.0 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.56 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.56 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.36 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.36 }
        }),
        ("B2_Conservative04_Focus_07", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.5 },
            { Breakout2.ParamKey.MinSignalStrength, 2.8 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.094 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 4.1 },
            { Breakout2.ParamKey.MinRatioDifference, 0.067 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.35 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 2.8 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.21 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.2 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.6 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.57 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.57 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.37 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.37 }
        }),
        ("B2_Conservative04_Focus_08", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.7 },
            { Breakout2.ParamKey.MinSignalStrength, 2.9 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.093 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 4.0 },
            { Breakout2.ParamKey.MinRatioDifference, 0.069 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.4 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.0 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.4 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 5.2 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.58 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.58 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.38 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.38 }
        }),
        ("B2_Conservative04_Focus_09", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 12.9 },
            { Breakout2.ParamKey.MinSignalStrength, 3.0 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.092 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 3.9 },
            { Breakout2.ParamKey.MinRatioDifference, 0.070 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.45 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.2 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.5 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.8 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.59 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.59 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.39 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.39 }
        }),
        ("B2_Conservative04_Focus_10", new() {
            { Breakout2.ParamKey.AbsorptionThreshold, 13.2 },
            { Breakout2.ParamKey.MinSignalStrength, 3.1 },
            { Breakout2.ParamKey.VelocityToDepthRatio, 0.091 },
            { Breakout2.ParamKey.MinNumberOfPointsFromResolved, 7 },
            { Breakout2.ParamKey.MaxVolumeRatio, 3.8 },
            { Breakout2.ParamKey.MinRatioDifference, 0.072 },
            { Breakout2.ParamKey.ReversalExtraStrength, 1.5 },
            { Breakout2.ParamKey.SpikeMinRelativeIncrease, 3.1 },
            { Breakout2.ParamKey.MaxVelocityThresholdRatio, 0.20 },
            { Breakout2.ParamKey.ExitOppositeSignalStrength, 3.6 },
            { Breakout2.ParamKey.ExitRsiDevThreshold, 4.6 },
            { Breakout2.ParamKey.TradeRateShareMin_Yes, 0.60 },
            { Breakout2.ParamKey.TradeRateShareMin_No, 0.60 },
            { Breakout2.ParamKey.TradeEventShareMin_Yes, 0.40 },
            { Breakout2.ParamKey.TradeEventShareMin_No, 0.40 }
        })
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