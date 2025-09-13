namespace BacklashDTOs.Data
{
    public class OrderDTO
    {
        public string? OrderId { get; set; }
        public string? Ticker { get; set; }
        public Guid UserId { get; set; }
        public string? Action { get; set; }
        public string? Side { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
        public int YesPrice { get; set; }
        public int NoPrice { get; set; }
        public DateTime CreatedTimeUTC { get; set; }
        public DateTime? LastUpdateTimeUTC { get; set; }
        public DateTime? ExpirationTimeUTC { get; set; }
        public string? ClientOrderId { get; set; }
        public int PlaceCount { get; set; }
        public int DecreaseCount { get; set; }
        public int AmendCount { get; set; }
        public int AmendTakerFillCount { get; set; }
        public int MakerFillCount { get; set; }
        public int TakerFillCount { get; set; }
        public int RemainingCount { get; set; }
        public int QueuePosition { get; set; }
        public long MakerFillCost { get; set; }
        public long TakerFillCost { get; set; }
        public long MakerFees { get; set; }
        public long TakerFees { get; set; }
        public int FccCancelCount { get; set; }
        public int CloseCancelCount { get; set; }
        public int TakerSelfTradeCancelCount { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
