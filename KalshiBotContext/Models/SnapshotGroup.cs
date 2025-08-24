namespace KalshiBotData.Models
{
    public class SnapshotGroup
    {
        public int SnapshotGroupID { get; set; }
        public string MarketTicker { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int YesStart { get; set; }
        public int NoStart { get; set; }
        public int YesEnd { get; set; }
        public int NoEnd { get; set; }
        public double AverageLiquidity { get; set; }
        public int SnapshotSchema {get; set; }
        public string JsonPath { get; set; }
        public DateTime ProcessedDttm { get; set; }

        // Navigation property to Market
        public Market Market { get; set; }
    }


}
