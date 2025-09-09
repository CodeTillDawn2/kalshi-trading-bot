using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class LogEntryExtensions
    {
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
    }
}
