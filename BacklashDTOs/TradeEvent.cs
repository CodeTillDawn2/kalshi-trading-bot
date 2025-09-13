namespace BacklashDTOs
{
    public class TradeEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? TakerSide { get; set; }
        public int YesPrice { get; set; }
        public int NoPrice { get; set; }
        public int Count { get; set; }
        public DateTime Timestamp { get; set; }
        public bool HasMatchingOrderbookChange { get; set; } = false;
    }

}

