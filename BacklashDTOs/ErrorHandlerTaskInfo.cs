using Microsoft.Extensions.Logging;

namespace BacklashDTOs
{
    /// <summary>
    /// Represents information about an error handler task.
    /// </summary>
    public class ErrorHandlerTaskInfo
    {
        /// <summary>
        /// Gets or sets the formatted message of the error.
        /// </summary>
        public string? FormattedMessage { get; set; }
        /// <summary>
        /// Gets or sets the log source category.
        /// </summary>
        public string? LogSourceCategory { get; set; }
        /// <summary>
        /// Gets or sets the severity level of the log.
        /// </summary>
        public LogLevel Severity { get; set; }
        /// <summary>
        /// Gets or sets the original exception that caused the error.
        /// </summary>
        public Exception? OriginalException { get; set; }
        /// <summary>
        /// Gets or sets the timestamp when the error occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
