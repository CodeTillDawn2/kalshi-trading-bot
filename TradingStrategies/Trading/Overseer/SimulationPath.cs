using SmokehouseDTOs;
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
        public double TotalPaid { get; set; } = 0.0; // Cumulative amount paid for long positions
        public double TotalReceived { get; set; } = 0.0; // Cumulative amount received for short positions
        public List<ReportGenerator.EventLog> Events { get; set; }
        public SimulatedOrderbook? SimulatedBook { get; set; }
        public List<(string action, string side, string type, int count, int price, DateTime? expiration)> SimulatedRestingOrders { get; set; } = new List<(string, string, string, int, int, DateTime?)>();

        public SimulationPath(Dictionary<MarketType, HashSet<Strategy>> strategiesByMarketConditions, int position, double cash)
        {
            StrategiesByMarketConditions = strategiesByMarketConditions;
            Position = position;
            Cash = cash;
        }

        // For buyin price, show the average entry price regardless of position direction
        public double AverageCost
        {
            get
            {
                if (Position > 0)
                {
                    // Long position: average price paid to buy
                    return TotalPaid / Position;
                }
                else if (Position < 0)
                {
                    // Short position: average price received from selling (entry price for short)
                    double avgEntryPrice = TotalReceived / Math.Abs(Position);
                    return avgEntryPrice > 0 ? avgEntryPrice : 0.0;
                }
                return 0.0;
            }
        }
    }
}