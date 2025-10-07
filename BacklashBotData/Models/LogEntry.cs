namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a structured log entry in the Kalshi trading bot system.
    /// This entity captures comprehensive logging information including severity levels,
    /// contextual data, and system state for debugging and monitoring purposes.
    /// Log entries are used throughout the system for troubleshooting, performance monitoring,
    /// and maintaining an audit trail of system activities.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for this log entry.
        /// This serves as the primary key in the database.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this log entry was created.
        /// This provides precise timing information for log analysis and correlation.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the severity level of this log entry.
        /// Common levels include Debug, Information, Warning, Error, and Critical.
        /// </summary>
        public required string Level { get; set; }

        /// <summary>
        /// Gets or sets the main log message describing the event or state being logged.
        /// This contains the primary information about what occurred.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets any exception details associated with this log entry.
        /// This is populated when logging errors or exceptions for debugging purposes.
        /// </summary>
        public required string Exception { get; set; }

        /// <summary>
        /// Gets or sets the environment context where this log entry was generated.
        /// This helps distinguish between development, staging, and production environments.
        /// </summary>
        public required string Environment { get; set; }

        /// <summary>
        /// Gets or sets the brain instance identifier that generated this log entry.
        /// This allows filtering logs by specific brain instances for targeted analysis.
        /// </summary>
        public required string BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets the session identifier for grouping related log entries.
        /// This helps track log entries that belong to the same execution session.
        /// </summary>
        public required string SessionIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the source component or module that generated this log entry.
        /// This provides context about which part of the system produced the log.
        /// </summary>
        public required string Source { get; set; }
    }
}
