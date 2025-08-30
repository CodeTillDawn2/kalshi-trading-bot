using static SmokehouseInterfaces.Enums.StrategyEnums;

namespace TradingStrategies
{
    public class ActionDecision
    {
        public ActionType Type { get; set; }
        public int Price { get; set; } = 0; // For limit orders
        public int Qty { get; set; } = 1; // For limit orders
        public DateTime? Expiration { get; set; } = null; // Optional expiration for limits
        public string Memo { get; set; }
    }

}