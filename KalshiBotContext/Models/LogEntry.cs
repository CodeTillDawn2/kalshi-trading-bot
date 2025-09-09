namespace KalshiBotData.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Level { get; set; }
        public required string Message { get; set; }
        public required string Exception { get; set; }
        public required string Environment { get; set; }
        public required string BrainInstance { get; set; }
        public required string SessionIdentifier { get; set; }
        public required string Source { get; set; }
    }
}
