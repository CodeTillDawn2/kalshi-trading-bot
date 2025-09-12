using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between LogEntry models and DTOs,
    /// supporting both standard log entries and overseer-specific log entry variants for comprehensive logging data transfer.
    /// </summary>
    public static class LogEntryExtensions
    {
        /// <summary>
        /// Converts a LogEntry model to its DTO representation,
        /// mapping all log entry properties for data transfer operations.
        /// </summary>
        /// <param name="logEntry">The LogEntry model to convert.</param>
        /// <returns>A new LogEntryDTO with all log entry properties mapped.</returns>
        public static LogEntryDTO ToLogEntryDTO(this LogEntry logEntry)
        {
            return new LogEntryDTO
            {
                Id = logEntry.Id,
                Timestamp = logEntry.Timestamp,
                Level = logEntry.Level,
                Message = logEntry.Message,
                Exception = logEntry.Exception,
                Environment = logEntry.Environment,
                BrainInstance = logEntry.BrainInstance,
                SessionIdentifier = logEntry.SessionIdentifier,
                Source = logEntry.Source
            };
        }

        /// <summary>
        /// Converts a LogEntryDTO to its model representation,
        /// creating a new LogEntry with all properties mapped from the DTO.
        /// </summary>
        /// <param name="logEntryDTO">The LogEntryDTO to convert.</param>
        /// <returns>A new LogEntry model with all properties mapped from the DTO.</returns>
        public static LogEntry ToLogEntry(this LogEntryDTO logEntryDTO)
        {
            return new LogEntry
            {
                Id = logEntryDTO.Id,
                Timestamp = logEntryDTO.Timestamp,
                Level = logEntryDTO.Level,
                Message = logEntryDTO.Message,
                Exception = logEntryDTO.Exception,
                Environment = logEntryDTO.Environment,
                BrainInstance = logEntryDTO.BrainInstance,
                SessionIdentifier = logEntryDTO.SessionIdentifier,
                Source = logEntryDTO.Source
            };
        }

        /// <summary>
        /// Updates an existing LogEntry model with data from a LogEntryDTO,
        /// validating log entry ID match before applying all property changes.
        /// </summary>
        /// <param name="logEntry">The LogEntry model to update.</param>
        /// <param name="logEntryDTO">The LogEntryDTO containing updated data.</param>
        /// <returns>The updated LogEntry model.</returns>
        /// <exception cref="Exception">Thrown when log entry IDs do not match.</exception>
        public static LogEntry UpdateLogEntry(this LogEntry logEntry, LogEntryDTO logEntryDTO)
        {
            if (logEntry.Id != logEntryDTO.Id)
            {
                throw new Exception("Log entry ID doesn't match for Update LogEntry");
            }
            logEntry.Timestamp = logEntryDTO.Timestamp;
            logEntry.Level = logEntryDTO.Level;
            logEntry.Message = logEntryDTO.Message;
            logEntry.Exception = logEntryDTO.Exception;
            logEntry.Environment = logEntryDTO.Environment;
            logEntry.BrainInstance = logEntryDTO.BrainInstance;
            logEntry.SessionIdentifier = logEntryDTO.SessionIdentifier;
            logEntry.Source = logEntryDTO.Source;
            return logEntry;
        }

        /// <summary>
        /// Converts a LogEntryDTO to an OverseerLogEntry model,
        /// creating a specialized log entry variant for overseer system operations.
        /// </summary>
        /// <param name="logEntryDTO">The LogEntryDTO to convert.</param>
        /// <returns>A new OverseerLogEntry with all properties mapped from the DTO.</returns>
        public static OverseerLogEntry ToOverseerLogEntry(this LogEntryDTO logEntryDTO)
        {
            return new OverseerLogEntry
            {
                Id = logEntryDTO.Id,
                Timestamp = logEntryDTO.Timestamp,
                Level = logEntryDTO.Level,
                Message = logEntryDTO.Message,
                Exception = logEntryDTO.Exception,
                Environment = logEntryDTO.Environment,
                BrainInstance = logEntryDTO.BrainInstance,
                SessionIdentifier = logEntryDTO.SessionIdentifier,
                Source = logEntryDTO.Source
            };
        }

        /// <summary>
        /// Converts an OverseerLogEntry model to its DTO representation,
        /// mapping all overseer log entry properties for data transfer.
        /// </summary>
        /// <param name="overseerLogEntry">The OverseerLogEntry model to convert.</param>
        /// <returns>A new LogEntryDTO with all overseer log entry properties mapped.</returns>
        public static LogEntryDTO ToOverseerLogEntryDTO(this OverseerLogEntry overseerLogEntry)
        {
            return new LogEntryDTO
            {
                Id = overseerLogEntry.Id,
                Timestamp = overseerLogEntry.Timestamp,
                Level = overseerLogEntry.Level,
                Message = overseerLogEntry.Message,
                Exception = overseerLogEntry.Exception,
                Environment = overseerLogEntry.Environment,
                BrainInstance = overseerLogEntry.BrainInstance,
                SessionIdentifier = overseerLogEntry.SessionIdentifier,
                Source = overseerLogEntry.Source
            };
        }
    }
}
