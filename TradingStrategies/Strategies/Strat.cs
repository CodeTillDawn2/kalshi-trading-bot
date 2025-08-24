using System.Text.Json;
using static SmokehouseInterfaces.Enums.StrategyEnums;
using SmokehouseDTOs;
using TradingStrategies.Strategies.Strats;

namespace TradingStrategies.Strategies
{
    public abstract class Strat
    {
        public abstract double Weight { get; }

        public abstract ActionDecision GetAction(MarketSnapshot snapshot, MarketSnapshot? previousSnapshot, int simulationPosition = 0);

        public abstract string ToJson();

        public static Strat FromJson(string json)
        {
            var jsonDoc = JsonDocument.Parse(json);
            string type = jsonDoc.RootElement.GetProperty("type").GetString();
            return type switch
            {
                "BollingerBreakout" => BollingerBreakout.FromJson(json),
                _ => throw new ArgumentException("Unknown strategy type")
            };
        }
    }
}