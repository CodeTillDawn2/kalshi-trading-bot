// Updated Strategy.cs for merged Strat
using BacklashDTOs;
using static BacklashInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Strategies
{
    public class Strategy
    {
        private readonly List<Strat> _strats;

        public IReadOnlyList<Strat> Strats => _strats;

        public string Name { get; private set; }

        public Strategy(string name, List<Strat> strats)
        {
            Name = name;
            _strats = strats;
        }

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
