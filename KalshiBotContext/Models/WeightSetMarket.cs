
namespace KalshiBotData.Models
{

    public class WeightSetMarket
    {
        public int WeightSetID { get; set; }
        public required string MarketTicker { get; set; }
        public decimal PnL { get; set; }
        public DateTime? LastRun { get; set; }
        public virtual WeightSet? WeightSet { get; set; }
    }

}
