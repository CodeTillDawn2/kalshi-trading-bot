namespace KalshiBotData.Models
{

    public class WeightSet
    {
        public int WeightSetID { get; set; }
        public string StrategyName { get; set; }
        public string Weights { get; set; }

        public DateTime? LastRun { get; set; }

        public virtual ICollection<WeightSetMarket> WeightSetMarkets { get; set; } = new List<WeightSetMarket>();

    }

}