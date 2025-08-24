namespace SmokehouseDTOs
{
    public class MarketLiquidityStatsDTO
    {
        public int volume_24h { get; set; }
        public long liquidity { get; set; }
        public int open_interest { get; set; }
        public int yes_bid { get; set; }
        public int no_bid { get; set; }


    }
}
