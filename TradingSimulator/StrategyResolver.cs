using TradingStrategies.Strategies;
using TradingStrategies.Strategies.Strategies.Strats;
using TradingStrategies.Strategies.Strats;
using TradingStrategies.Trading.Helpers;
using static BacklashInterfaces.Enums.StrategyEnums;
using static TradingSimulator.TradingSimulatorService;

namespace TradingSimulator
{
    /// <summary>
    /// Central resolver for trading strategy families in the trading simulator.
    /// This class acts as a bridge between strategy family enums and their concrete implementations,
    /// providing a unified interface for accessing training mappings and parameter sets for different
    /// trading strategies. It encapsulates the logic for resolving strategy families to their
    /// corresponding configurations and mappings.
    /// </summary>
    /// <remarks>
    /// The StrategyResolver works closely with the StrategySelectionHelper to provide:
    /// - Training mappings for different market types
    /// - Parameter sets for strategy optimization
    /// - Family-to-set-key mapping for user input
    ///
    /// Supported strategy families include:
    /// - Bollinger: Bollinger Band breakout strategies
    /// - FlowMo: Flow momentum-based strategies
    /// - TryAgain: Retry-based strategies
    /// - SloMo: Slope momentum strategies
    /// - Breakout: Breakout detection strategies
    /// - NothingHappens: Baseline strategies for comparison
    /// - Momentum: Momentum trading strategies
    /// - MLShared: Machine learning-based strategies
    /// </remarks>
    public class StrategyResolver
    {
        /// <summary>
        /// Helper class that provides access to strategy configurations and mappings.
        /// </summary>
        private readonly StrategySelectionHelper _helper;

        /// <summary>
        /// Initializes a new instance of the StrategyResolver class.
        /// </summary>
        /// <param name="helper">The strategy selection helper for accessing strategy configurations.</param>
        public StrategyResolver(StrategySelectionHelper helper)
        {
            _helper = helper;
        }

        /// <summary>
        /// Resolves a strategy family to its training mappings, parameter sets, and display label.
        /// This method acts as the main entry point for accessing strategy configurations for a given family.
        /// </summary>
        /// <param name="family">The strategy family to resolve (e.g., Bollinger, FlowMo, etc.)</param>
        /// <returns>
        /// A tuple containing:
        /// - Strategies: List of market-to-strategy mappings for training
        /// - ParamSets: List of parameter set names and their configurations
        /// - Label: Human-readable label for the strategy family
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an unsupported strategy family is provided</exception>
        /// <remarks>
        /// This method uses a switch statement to handle different strategy families, each mapping to:
        /// - Training mappings from StrategySelectionHelper
        /// - Parameter sets specific to each strategy type
        /// - A consistent label for UI/display purposes
        /// </remarks>
        public (List<Dictionary<MarketType, List<Strategy>>> Strategies,
                List<(string Name, object Parameters)> ParamSets,
                string Label) ResolveFamily(StrategyFamily family)
        {
            switch (family)
            {
                case StrategyFamily.Bollinger:
                    return (
                        _helper.CreateTrainingStrategyInstances("Bollinger"),
                        GetBollingerParameterSets(),
                        "Bollinger"
                    );
                case StrategyFamily.FlowMo:
                    return (
                        _helper.CreateTrainingStrategyInstances("FlowMo"),
                        GetFlowMomentumParameterSets(),
                        "FlowMo"
                    );
                case StrategyFamily.TryAgain:
                    return (
                        _helper.CreateTrainingStrategyInstances("TryAgain"),
                        GetTryAgainParameterSets(),
                        "TryAgain"
                    );
                case StrategyFamily.SloMo:
                    return (
                        _helper.CreateTrainingStrategyInstances("SloMo"),
                        GetSlopeMomentumParameterSets(),
                        "SloMo"
                    );
                case StrategyFamily.Breakout:
                    return (
                        _helper.CreateTrainingStrategyInstances("Breakout2"),
                        GetBreakoutParameterSets(),
                        "Breakout"
                    );
                case StrategyFamily.NothingHappens:
                    return (
                        _helper.CreateTrainingStrategyInstances("Nothing"),
                        GetNothingEverHappensParameterSets(),
                        "NothingHappens"
                    );
                case StrategyFamily.Momentum:
                    return (
                        _helper.CreateTrainingStrategyInstances("Momentum"),
                        GetMomentumTradingParameterSets(),
                        "Momentum"
                    );
                case StrategyFamily.MLShared:
                    return (
                        _helper.CreateTrainingStrategyInstances("MLShared"),
                        GetMLSharedParameterSets(),
                        "MLShared"
                    );
                default:
                    throw new ArgumentOutOfRangeException(nameof(family));
            }
        }

        /// <summary>
        /// Retrieves the parameter sets for Bollinger Band breakout strategies.
        /// These parameter sets define various configurations for Bollinger Band-based trading strategies,
        /// including squeeze thresholds, absorption thresholds, signal strengths, and velocity parameters.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetBollingerParameterSets()
        {
            return StrategySelectionHelper.BollingerParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Retrieves the parameter sets for Flow Momentum strategies.
        /// These parameter sets define configurations for momentum-based trading strategies
        /// that analyze market flow and momentum patterns.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetFlowMomentumParameterSets()
        {
            return StrategySelectionHelper.FlowMomentumParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Retrieves the parameter sets for Try Again strategies.
        /// These parameter sets define configurations for retry-based trading strategies
        /// that implement logic for re-entering positions after initial attempts.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetTryAgainParameterSets()
        {
            return TryAgainStrat.TryAgainStratParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Retrieves the parameter sets for Slope Momentum strategies.
        /// These parameter sets define configurations for momentum strategies that analyze
        /// price slope and momentum characteristics in market data.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetSlopeMomentumParameterSets()
        {
            return SlopeMomentumStrat.SlopeMomentumParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Retrieves the parameter sets for Breakout strategies.
        /// These parameter sets define configurations for breakout detection strategies
        /// that identify and capitalize on significant price movements and breakouts.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetBreakoutParameterSets()
        {
            return StrategySelectionHelper.BreakoutParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Retrieves the parameter sets for Nothing Ever Happens strategies.
        /// These parameter sets define baseline configurations for comparison strategies
        /// that serve as a control group for evaluating other strategy performance.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetNothingEverHappensParameterSets()
        {
            return StrategySelectionHelper.NothingEverHappensParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Retrieves the parameter sets for Momentum Trading strategies.
        /// These parameter sets define configurations for momentum-based trading strategies
        /// that follow and capitalize on market momentum trends.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetMomentumTradingParameterSets()
        {
            return StrategySelectionHelper.MomentumTradingParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Retrieves the parameter sets for ML Shared strategies.
        /// These parameter sets define configurations for machine learning-based trading strategies
        /// that use shared ML models and parameters for market prediction and trading decisions.
        /// </summary>
        /// <returns>A list of tuples containing parameter set names and their configurations</returns>
        private List<(string Name, object Parameters)> GetMLSharedParameterSets()
        {
            return MLEntrySeekerShared.MLSharedParameterSets
                .Select(ps => (ps.Name, (object)ps.Parameters))
                .ToList();
        }

        /// <summary>
        /// Maps a user-provided set key string to its corresponding StrategyFamily enum value.
        /// This method provides flexible string matching to allow users to specify strategy families
        /// using various common names, abbreviations, or partial matches.
        /// </summary>
        /// <param name="setKey">The string key representing a strategy family (case-insensitive)</param>
        /// <returns>The corresponding StrategyFamily enum value</returns>
        /// <exception cref="ArgumentException">Thrown when setKey is null, empty, or whitespace</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when setKey cannot be mapped to any known strategy family</exception>
        /// <remarks>
        /// This method supports multiple ways to specify each strategy family:
        /// - Bollinger: "bollinger", "bb", "bollingerbreakout"
        /// - Breakout: "breakout", "b2", "breakout2"
        /// - FlowMo: "flowmo"
        /// - TryAgain: "tryagain"
        /// - SloMo: "slomo"
        /// - NothingHappens: "nothing"
        /// - Momentum: "momentum"
        /// - MLShared: "ml", "mlshared"
        ///
        /// The method performs case-insensitive matching and supports partial string matches
        /// for better user experience.
        /// </remarks>
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
