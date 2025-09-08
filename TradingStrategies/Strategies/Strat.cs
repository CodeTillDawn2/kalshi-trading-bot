using SmokehouseDTOs;
using System.Text.Json;
using TradingStrategies.Strategies.Strats;

namespace TradingStrategies.Strategies
{
    public abstract class Strat
    {
        public abstract double Weight { get; }

        public abstract ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0);

        public abstract string ToJson();


    }
}