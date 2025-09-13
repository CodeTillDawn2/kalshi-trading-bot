namespace BacklashDTOs.Data
{

    public class WeightSetDTO
    {
        public int WeightSetID { get; set; }
        public string? StrategyName { get; set; }
        public string? Weights { get; set; }

        public DateTime? LastRun { get; set; }
        public List<WeightSetMarketDTO> WeightSetMarkets { get; set; } = new List<WeightSetMarketDTO>();
    }

}
