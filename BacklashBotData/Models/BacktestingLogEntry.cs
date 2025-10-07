using System.ComponentModel.DataAnnotations.Schema;

namespace BacklashBotData.Models
{
    /// <summary>
    /// Represents a log entry from the backtesting/GUI system for monitoring and debugging.
    /// This entity stores detailed logging information from the backtesting component,
    /// including error details, environment context, and session tracking.
    /// Maps to the t_BacktestingLogs database table.
    /// </summary>
    [Table("t_BacktestingLogs")]
    public class BacktestingLogEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for this log entry.
        /// This serves as the primary key in the t_BacktestingLogs table.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this log entry was created.
        /// Used for chronological ordering and time-based filtering of logs.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the logging level (e.g., "Debug", "Information", "Warning", "Error").
        /// This categorizes the severity and importance of the log entry.
        /// </summary>
        public required string Level { get; set; }

        /// <summary>
        /// Gets or sets the actual log message content.
        /// This contains the descriptive text of what occurred or what was logged.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets exception details if this log entry is related to an error.
        /// Contains stack trace and exception information for debugging purposes.
        /// </summary>
        public required string Exception { get; set; }

        /// <summary>
        /// Gets or sets the environment context where this log entry was generated.
        /// Helps identify which deployment environment (dev, staging, prod) the log came from.
        /// </summary>
        public required string Environment { get; set; }

        /// <summary>
        /// Gets or sets the brain instance name that generated this log entry.
        /// Used to correlate logs with specific trading bot instances.
        /// </summary>
        public required string BrainInstance { get; set; }

        /// <summary>
        /// Gets or sets the session identifier for tracking related log entries.
        /// Groups logs from the same operational session for easier analysis.
        /// </summary>
        public required string SessionIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the source component or module that generated this log entry.
        /// Helps identify which part of the system produced the log message.
        /// </summary>
        public required string Source { get; set; }
    }
}