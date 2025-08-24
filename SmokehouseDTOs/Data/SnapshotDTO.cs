namespace SmokehouseDTOs.Data
{
    public class SnapshotDTO
    {
        public string MarketTicker { get; set; } = string.Empty;
        public DateTime SnapshotDate { get; set; }
        public int JSONSchemaVersion { get; set; }
        public bool ChangeMetricsMature { get; set; }
        public int PositionSize { get; set; }
        public double? VelocityPerMinute_Top_Yes_Bid { get; set; }
        public double? VelocityPerMinute_Top_No_Bid { get; set; }
        public double? VelocityPerMinute_Bottom_Yes_Bid { get; set; }
        public double? VelocityPerMinute_Bottom_No_Bid { get; set; }
        public double? OrderVolume_Yes_Bid { get; set; }
        public double? OrderVolume_No_Bid { get; set; }
        public double? TradeVolume_Yes { get; set; }
        public double? TradeVolume_No { get; set; }
        public double? AverageTradeSize_Yes { get; set; }
        public double? AverageTradeSize_No { get; set; }
        public int? MarketTypeID { get; set; }
        public bool? IsValidated { get; set; }
        public string RawJSON { get; set; } = string.Empty;
        public string? BrainInstance { get; set; }
    }
}