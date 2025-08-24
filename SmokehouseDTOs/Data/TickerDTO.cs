namespace SmokehouseDTOs.Data
{
    public class TickerDTO
    {
        public Guid market_id { get; set; }
        public string market_ticker { get; set; }
        public int price { get; set; }
        public int yes_bid { get; set; }
        public int yes_ask { get; set; }
        public int volume { get; set; }
        public int open_interest { get; set; }
        public int dollar_volume { get; set; }
        public int dollar_open_interest { get; set; }
        public long ts { get; set; }
        public DateTime LoggedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
    }
}