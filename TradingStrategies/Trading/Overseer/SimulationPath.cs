using TradingStrategies.Strategies;
using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies.Trading.Overseer
{
    public class SimulationPath
    {
        public Dictionary<MarketType, HashSet<Strategy>> StrategiesByMarketConditions { get; }
        public int Position { get; set; }
        public double Cash { get; set; }
        public double CurrentRisk { get; set; } = 0.0;
        public List<ReportGenerator.EventLog> Events { get; set; }
        public SimulatedOrderbook? SimulatedBook { get; set; }
        public List<(string action, string side, string type, int count, int price, DateTime? expiration)> SimulatedRestingOrders { get; set; } = new List<(string, string, string, int, int, DateTime?)>();

        public SimulationPath(Dictionary<MarketType, HashSet<Strategy>> strategiesByMarketConditions, int position, double cash)
        {
            StrategiesByMarketConditions = strategiesByMarketConditions;
            Position = position;
            Cash = cash;
        }
    }
}