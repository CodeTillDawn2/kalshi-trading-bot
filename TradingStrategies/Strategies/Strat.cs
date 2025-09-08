using SmokehouseDTOs;

namespace TradingStrategies.Strategies
{
    public abstract class Strat
    {
        public abstract double Weight { get; }

        public abstract ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0);

        public abstract string ToJson();


    }
}