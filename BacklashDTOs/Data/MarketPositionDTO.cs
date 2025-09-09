namespace BacklashDTOs.Data
{
    public class MarketPositionDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public long TotalTraded { get; set; }
        public int Position { get; set; }
        public long MarketExposure { get; set; }
        public long RealizedPnl { get; set; }
        public int RestingOrdersCount { get; set; }
        public long FeesPaid { get; set; }
        public DateTime LastUpdatedUTC { get; set; }
        public DateTime? LastModified { get; set; }
    }


}
