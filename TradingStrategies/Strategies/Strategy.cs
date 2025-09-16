using BacklashDTOs;
using TradingStrategies.Trading.Overseer;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies
{
    /// <summary>
    /// Represents a composite trading strategy that combines multiple individual strategies (Strats).
    /// This class aggregates the decisions from multiple strategies based on their weights,
    /// implementing a voting system where the strategy with the highest combined weight for a particular action wins.
    /// The composite approach allows for more robust trading decisions by considering multiple perspectives.
    /// </summary>
    public class Strategy
    {
        private readonly List<Strat> _strats;

        /// <summary>
        /// Gets the read-only list of individual strategies that make up this composite strategy.
        /// </summary>
        public IReadOnlyList<Strat> Strats => _strats;

        /// <summary>
        /// Gets the name of this strategy for identification and logging purposes.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Strategy class with a name and list of individual strategies.
        /// </summary>
        /// <param name="name">The name identifier for this strategy.</param>
        /// <param name="strats">The list of individual strategies to combine in this composite strategy.</param>
        public Strategy(string name, List<Strat> strats)
        {
            Name = name;
            _strats = strats;
        }

        /// <summary>
        /// Evaluates all individual strategies and returns the aggregated trading decision.
        /// Uses a weighted voting system where each strategy's weight contributes to its action type.
        /// The action type with the highest total weight is selected, and among strategies voting for that type,
        /// the one with the highest individual weight provides the final decision details.
        /// </summary>
        /// <param name="snapshot">The current market snapshot containing price, volume, and technical indicator data.</param>
        /// <param name="previousSnapshot">The previous market snapshot for comparison, or null if this is the first snapshot.</param>
        /// <param name="simulationPosition">The current position in the simulation (0 for no position, positive for long, negative for short).</param>
        /// <returns>An ActionDecision containing the recommended trading action and associated metadata from the highest-weighted strategy.</returns>
        public ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0)
        {
            var actionVotes = new Dictionary<ActionType, double>();
            var decisionsByType = new Dictionary<ActionType, List<(double weight, ActionDecision decision)>>();

            foreach (var strat in _strats)
            {
                var decision = strat.GetAction(snapshot, previousSnapshot, simulationPosition);
                var action = decision.Type;

                double weight = strat.Weight;
                if (actionVotes.ContainsKey(action))
                {
                    actionVotes[action] += weight;
                }
                else
                {
                    actionVotes[action] = weight;
                }
                if (!decisionsByType.ContainsKey(action))
                {
                    decisionsByType[action] = new List<(double, ActionDecision)>();
                }
                decisionsByType[action].Add((weight, decision));

            }

            if (actionVotes.Count == 0) return new ActionDecision { Type = ActionType.None, Memo = "No action votes" };
            var selectedType = actionVotes.OrderByDescending(kv => kv.Value).First().Key;

            // For selected type, take decision from highest-weight strat
            var selectedDecision = decisionsByType[selectedType].OrderByDescending(d => d.weight).First().decision;

            return selectedDecision;
        }
    }
}
