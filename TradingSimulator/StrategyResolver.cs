using TradingStrategies.Strategies.Strats;
using TradingStrategies.Strategies;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingSimulator.TradingSimulatorService;
using TradingStrategies.Trading.Helpers;

namespace TradingSimulator
{
    public class StrategyResolver
    {
        private readonly StrategySelectionHelper _helper;

        public StrategyResolver()
        {
            _helper = new StrategySelectionHelper();
        }

        public (List<Dictionary<MarketType, List<Strategy>>> Strategies,
                List<(string Name, object Parameters)> ParamSets,
                string Label) ResolveFamily(StrategyFamily family)
        {
            switch (family)
            {
                case StrategyFamily.Bollinger:
                    return (
                        _helper.GetTrainingMappings("Bollinger"),
                        ConvertBollingerParameterSets(),
                        "Bollinger"
                    );
                case StrategyFamily.FlowMo:
                    return (
                        _helper.GetTrainingMappings("FlowMo"),
                        ConvertFlowMomentumParameterSets(),
                        "FlowMo"
                    );
                case StrategyFamily.TryAgain:
                    return (
                        _helper.GetTrainingMappings("TryAgain"),
                        ConvertTryAgainParameterSets(),
                        "TryAgain"
                    );
                case StrategyFamily.SloMo:
                    return (
                        _helper.GetTrainingMappings("SloMo"),
                        ConvertSlopeMomentumParameterSets(),
                        "SloMo"
                    );
                case StrategyFamily.Breakout:
                    return (
                        _helper.GetTrainingMappings("Breakout2"),
                        ConvertBreakoutParameterSets(),
                        "Breakout"
                    );
                case StrategyFamily.NothingHappens:
                    return (
                        _helper.GetTrainingMappings("Nothing"),
                        ConvertNothingEverHappensParameterSets(),
                        "NothingHappens"
                    );
                case StrategyFamily.Momentum:
                    return (
                        _helper.GetTrainingMappings("Momentum"),
                        ConvertMomentumTradingParameterSets(),
                        "Momentum"
                    );
                case StrategyFamily.MLShared:
                    return (
                        _helper.GetTrainingMappings("MLShared"),
                        ConvertMLSharedParameterSets(),
                        "MLShared"
                    );
                default:
                    throw new ArgumentOutOfRangeException(nameof(family));
            }
        }

        private List<(string Name, object Parameters)> ConvertBollingerParameterSets()
        {
            return StrategySelectionHelper.BollingerParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        private List<(string Name, object Parameters)> ConvertFlowMomentumParameterSets()
        {
            return StrategySelectionHelper.FlowMomentumParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        private List<(string Name, object Parameters)> ConvertTryAgainParameterSets()
        {
            return TryAgainStrat.TryAgainStratParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        private List<(string Name, object Parameters)> ConvertSlopeMomentumParameterSets()
        {
            return SlopeMomentumStrat.SlopeMomentumParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        private List<(string Name, object Parameters)> ConvertBreakoutParameterSets()
        {
            return StrategySelectionHelper.BreakoutParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        private List<(string Name, object Parameters)> ConvertNothingEverHappensParameterSets()
        {
            return StrategySelectionHelper.NothingEverHappensParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        private List<(string Name, object Parameters)> ConvertMomentumTradingParameterSets()
        {
            return StrategySelectionHelper.MomentumTradingParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        private List<(string Name, object Parameters)> ConvertMLSharedParameterSets()
        {
            return MLEntrySeekerShared.MLSharedParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        public StrategyFamily MapFamilyFromSetKey(string setKey)
        {
            if (string.IsNullOrWhiteSpace(setKey)) throw new ArgumentException("setKey is required.", nameof(setKey));
            var k = setKey.Trim().ToLowerInvariant();
            if (k.Contains("breakout")) return StrategyFamily.Breakout;
            if (k.Contains("bollinger")) return StrategyFamily.Bollinger;
            if (k.Contains("flowmo")) return StrategyFamily.FlowMo;
            if (k.Contains("tryagain")) return StrategyFamily.TryAgain;
            if (k.Contains("slomo")) return StrategyFamily.SloMo;
            if (k.Contains("nothing")) return StrategyFamily.NothingHappens;
            if (k.Contains("momentum")) return StrategyFamily.Momentum;
            if (k.Contains("ml") || k == "mlshared") return StrategyFamily.MLShared;
            // specific short names
            if (k is "b2" or "breakout2") return StrategyFamily.Breakout;
            if (k is "bb" or "bollingerbreakout") return StrategyFamily.Bollinger;
            throw new ArgumentOutOfRangeException(nameof(setKey), $"Unrecognized setKey: {setKey}");
        }
    }

}