namespace BacklashDTOs.Data
{
    public class LogEntryDTO
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public string Environment { get; set; }
        public string BrainInstance { get; set; }
        public string SessionIdentifier { get; set; }
        public string Source { get; set; }
    }
}
