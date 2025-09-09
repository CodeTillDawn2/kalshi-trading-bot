namespace BacklashDTOs.Data
{

    public class WeightSetMarketDTO
    {
        public int WeightSetID { get; set; }
        public required string MarketTicker { get; set; }
        public decimal PnL { get; set; }
        public DateTime? LastRun { get; set; }
    }

}
