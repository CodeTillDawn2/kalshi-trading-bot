namespace BacklashDTOs
{
    public class OrderbookChange
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public long Sequence { get; set; }
        public string? Side { get; set; }
        public int Price { get; set; }
        public int DeltaContracts { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsTradeRelated { get; set; }
        public bool IsCanceled { get; set; } = false;
        public string? MatchedTradeId { get; set; } // New property
        public List<(OrderbookChange ExistingChange, OrderbookChange NewChange)> CanceledPairs { get; set; } = new List<(OrderbookChange, OrderbookChange)>();
    }

}

