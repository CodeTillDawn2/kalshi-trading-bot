namespace KalshiUI.Models
{
    public class Lifecycle
    {
        public string market_ticker { get; set; }
        public long open_ts { get; set; }
        public long close_ts { get; set; }
        public long? determination_ts { get; set; }
        public long? settled_ts { get; set; }
        public string result { get; set; }
        public bool is_deactivated { get; set; }
        public DateTime LoggedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public Market Market { get; set; }
    }


}
