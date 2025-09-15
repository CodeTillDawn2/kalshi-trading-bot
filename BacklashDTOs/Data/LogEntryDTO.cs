namespace BacklashDTOs.Data
{
    /// <summary>
    /// Data transfer object for log entry data.
    /// </summary>
    public class LogEntryDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier for the log entry.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the log entry.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public string? Level { get; set; }

        /// <summary>
        /// Gets or sets the log message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the exception details.
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// Gets or sets the environment where the log was generated.
        /// </summary>
        public string? Environment { get; set; }

        /// <summary>
        /// Gets or sets the brain instance identifier.
        /// </summary>
        public string? BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public string? SessionIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the source of the log entry.
        /// </summary>
        public string? Source { get; set; }
    }
}
