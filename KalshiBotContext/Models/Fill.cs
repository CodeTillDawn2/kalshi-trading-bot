namespace KalshiBotData.Models
{
    public class Fill
    {
        public Guid trade_id { get; set; }
        public Guid order_id { get; set; }
        public string market_ticker { get; set; }
        public bool is_taker { get; set; }
        public string side { get; set; }
        public int yes_price { get; set; }
        public int no_price { get; set; }
        public int count { get; set; }
        public string action { get; set; }
        public long ts { get; set; }
        public DateTime LoggedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public Market Market { get; set; }
    }


}
