namespace KalshiUI.Models
{
    public class OrderbookSnapshot
    {
        public Guid market_id { get; set; }
        public int sid { get; set; }
        public int kalshi_seq { get; set; }
        public String? market_ticker { get; set; }
        public string offer_type { get; set; }
        public int price { get; set; }
        public Int32? delta { get; set; }
        public String? side { get; set; }
        public int resting_contracts { get; set; }
        public DateTime LoggedDate { get; set; }
        public int table_seq { get; set; }
        public DateTime? ProcessedDate { get; set; }

        public Market Market { get; set; }
    }


}
