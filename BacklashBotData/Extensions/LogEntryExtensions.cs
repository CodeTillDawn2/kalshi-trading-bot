using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between LogEntry models and DTOs,
    /// supporting both standard log entries and overseer-specific log entry variants for comprehensive logging data transfer.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class LogEntryExtensions
    {
        private static readonly ConcurrentDictionary<string, List<TimeSpan>> _performanceMetrics = new();

        /// <summary>
        /// Gets the performance metrics for transformation operations
        /// </summary>
        public static IReadOnlyDictionary<string, List<TimeSpan>> GetPerformanceMetrics()
        {
            return _performanceMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        /// <summary>
        /// Converts a LogEntry model to its DTO representation,
        /// mapping all log entry properties for data transfer operations.
        /// </summary>
        /// <param name="logEntry">The LogEntry model to convert.</param>
        /// <returns>A new LogEntryDTO with all log entry properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntry is null.</exception>
        public static LogEntryDTO ToLogEntryDTO(this LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            var stopwatch = Stopwatch.StartNew();

            var result = new LogEntryDTO
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToLogEntryDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a LogEntryDTO to its model representation,
        /// creating a new LogEntry with all properties mapped from the DTO.
        /// </summary>
        /// <param name="logEntryDTO">The LogEntryDTO to convert.</param>
        /// <returns>A new LogEntry model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntryDTO is null.</exception>
        public static LogEntry ToLogEntry(this LogEntryDTO logEntryDTO)
        {
            if (logEntryDTO == null)
                throw new ArgumentNullException(nameof(logEntryDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new LogEntry
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToLogEntry", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing LogEntry model with data from a LogEntryDTO,
        /// validating log entry ID match before applying all property changes.
        /// </summary>
        /// <param name="logEntry">The LogEntry model to update.</param>
        /// <param name="logEntryDTO">The LogEntryDTO containing updated data.</param>
        /// <returns>The updated LogEntry model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntry or logEntryDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when log entry IDs do not match.</exception>
        public static LogEntry UpdateLogEntry(this LogEntry logEntry, LogEntryDTO logEntryDTO)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));
            if (logEntryDTO == null)
                throw new ArgumentNullException(nameof(logEntryDTO));

            if (logEntry.Id != logEntryDTO.Id)
            {
                throw new ArgumentException("Log entry ID doesn't match for Update LogEntry", nameof(logEntryDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            logEntry.Timestamp = logEntryDTO.Timestamp;
            logEntry.Level = logEntryDTO.Level;
            logEntry.Message = logEntryDTO.Message;
            logEntry.Exception = logEntryDTO.Exception;
            logEntry.Environment = logEntryDTO.Environment;
            logEntry.BrainInstance = logEntryDTO.BrainInstance;
            logEntry.SessionIdentifier = logEntryDTO.SessionIdentifier;
            logEntry.Source = logEntryDTO.Source;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateLogEntry", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return logEntry;
        }

        /// <summary>
        /// Converts a LogEntryDTO to an OverseerLogEntry model,
        /// creating a specialized log entry variant for overseer system operations.
        /// </summary>
        /// <param name="logEntryDTO">The LogEntryDTO to convert.</param>
        /// <returns>A new OverseerLogEntry with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntryDTO is null.</exception>
        public static OverseerLogEntry ToOverseerLogEntry(this LogEntryDTO logEntryDTO)
        {
            if (logEntryDTO == null)
                throw new ArgumentNullException(nameof(logEntryDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new OverseerLogEntry
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToOverseerLogEntry", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts an OverseerLogEntry model to its DTO representation,
        /// mapping all overseer log entry properties for data transfer.
        /// </summary>
        /// <param name="overseerLogEntry">The OverseerLogEntry model to convert.</param>
        /// <returns>A new LogEntryDTO with all overseer log entry properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when overseerLogEntry is null.</exception>
        public static LogEntryDTO ToOverseerLogEntryDTO(this OverseerLogEntry overseerLogEntry)
        {
            if (overseerLogEntry == null)
                throw new ArgumentNullException(nameof(overseerLogEntry));

            var stopwatch = Stopwatch.StartNew();

            var result = new LogEntryDTO
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToOverseerLogEntryDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a LogEntryDTO to a BacktestingLogEntry model,
        /// creating a specialized log entry variant for backtesting/GUI system operations.
        /// </summary>
        /// <param name="logEntryDTO">The LogEntryDTO to convert.</param>
        /// <returns>A new BacktestingLogEntry with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntryDTO is null.</exception>
        public static BacktestingLogEntry ToBacktestingLogEntry(this LogEntryDTO logEntryDTO)
        {
            if (logEntryDTO == null)
                throw new ArgumentNullException(nameof(logEntryDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new BacktestingLogEntry
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToBacktestingLogEntry", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a BacktestingLogEntry model to its DTO representation,
        /// mapping all backtesting log entry properties for data transfer.
        /// </summary>
        /// <param name="backtestingLogEntry">The BacktestingLogEntry model to convert.</param>
        /// <returns>A new LogEntryDTO with all backtesting log entry properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when backtestingLogEntry is null.</exception>
        public static LogEntryDTO ToBacktestingLogEntryDTO(this BacktestingLogEntry backtestingLogEntry)
        {
            if (backtestingLogEntry == null)
                throw new ArgumentNullException(nameof(backtestingLogEntry));

            var stopwatch = Stopwatch.StartNew();

            var result = new LogEntryDTO
            {
                Id = backtestingLogEntry.Id,
                Timestamp = backtestingLogEntry.Timestamp,
                Level = backtestingLogEntry.Level,
                Message = backtestingLogEntry.Message,
                Exception = backtestingLogEntry.Exception,
                Environment = backtestingLogEntry.Environment,
                BrainInstance = backtestingLogEntry.BrainInstance,
                SessionIdentifier = backtestingLogEntry.SessionIdentifier,
                Source = backtestingLogEntry.Source
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToBacktestingLogEntryDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of LogEntry models to their corresponding DTO representations.
        /// </summary>
        /// <param name="logEntries">The collection of LogEntry models to convert.</param>
        /// <returns>A list of LogEntryDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntries is null.</exception>
        public static List<LogEntryDTO> ToLogEntryDTOs(this IEnumerable<LogEntry> logEntries)
        {
            if (logEntries == null)
                throw new ArgumentNullException(nameof(logEntries));

            var stopwatch = Stopwatch.StartNew();

            var result = logEntries.Select(le => le.ToLogEntryDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToLogEntryDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of LogEntryDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="logEntryDTOs">The collection of LogEntryDTOs to convert.</param>
        /// <returns>A list of LogEntry models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntryDTOs is null.</exception>
        public static List<LogEntry> ToLogEntries(this IEnumerable<LogEntryDTO> logEntryDTOs)
        {
            if (logEntryDTOs == null)
                throw new ArgumentNullException(nameof(logEntryDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = logEntryDTOs.Select(dto => dto.ToLogEntry()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToLogEntries", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a LogEntry model to prevent unintended mutations.
        /// </summary>
        /// <param name="logEntry">The LogEntry model to clone.</param>
        /// <returns>A new LogEntry instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntry is null.</exception>
        public static LogEntry DeepClone(this LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException(nameof(logEntry));

            return new LogEntry
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
        /// Creates a deep clone of a LogEntryDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="logEntryDTO">The LogEntryDTO to clone.</param>
        /// <returns>A new LogEntryDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when logEntryDTO is null.</exception>
        public static LogEntryDTO DeepClone(this LogEntryDTO logEntryDTO)
        {
            if (logEntryDTO == null)
                throw new ArgumentNullException(nameof(logEntryDTO));

            return new LogEntryDTO
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
        /// Creates a deep clone of an OverseerLogEntry model to prevent unintended mutations.
        /// </summary>
        /// <param name="overseerLogEntry">The OverseerLogEntry model to clone.</param>
        /// <returns>A new OverseerLogEntry instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when overseerLogEntry is null.</exception>
        public static OverseerLogEntry DeepClone(this OverseerLogEntry overseerLogEntry)
        {
            if (overseerLogEntry == null)
                throw new ArgumentNullException(nameof(overseerLogEntry));

            return new OverseerLogEntry
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
