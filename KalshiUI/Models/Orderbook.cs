namespace KalshiUI.Models
{
    public class Orderbook
    {
        public string market_ticker { get; set; }
        public int price { get; set; }
        public string side { get; set; }
        public int resting_contracts { get; set; }
        public DateTime? LastModifiedDate { get; set; }

        public Market Market { get; set; }
    }
}
