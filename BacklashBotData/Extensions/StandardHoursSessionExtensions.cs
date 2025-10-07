using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between StandardHoursSession model and StandardHoursSessionDTO,
    /// supporting individual session data transfer within exchange standard hours.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class StandardHoursSessionExtensions
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
        /// Converts a StandardHoursSession model to its DTO representation,
        /// mapping all session properties for data transfer.
        /// </summary>
        /// <param name="standardHoursSessionModel">The StandardHoursSession model to convert.</param>
        /// <returns>A new StandardHoursSessionDTO with all session properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursSessionModel is null.</exception>
        public static StandardHoursSessionDTO ToStandardHoursSessionDTO(this StandardHoursSession standardHoursSessionModel)
        {
            if (standardHoursSessionModel == null)
                throw new ArgumentNullException(nameof(standardHoursSessionModel));

            var stopwatch = Stopwatch.StartNew();

            var result = new StandardHoursSessionDTO
            {
                SessionID = standardHoursSessionModel.SessionID,
                StandardHoursID = standardHoursSessionModel.StandardHoursID,
                DayOfWeek = standardHoursSessionModel.DayOfWeek,
                StartTime = standardHoursSessionModel.StartTime,
                EndTime = standardHoursSessionModel.EndTime,
                CreatedDate = standardHoursSessionModel.CreatedDate,
                LastModifiedDate = standardHoursSessionModel.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHoursSessionDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a StandardHoursSessionDTO to its model representation,
        /// creating a new StandardHoursSession with all properties mapped from the DTO.
        /// </summary>
        /// <param name="standardHoursSessionDTO">The StandardHoursSessionDTO to convert.</param>
        /// <returns>A new StandardHoursSession model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursSessionDTO is null.</exception>
        public static StandardHoursSession ToStandardHoursSession(this StandardHoursSessionDTO standardHoursSessionDTO)
        {
            if (standardHoursSessionDTO == null)
                throw new ArgumentNullException(nameof(standardHoursSessionDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new StandardHoursSession
            {
                SessionID = standardHoursSessionDTO.SessionID,
                StandardHoursID = standardHoursSessionDTO.StandardHoursID,
                DayOfWeek = standardHoursSessionDTO.DayOfWeek,
                StartTime = standardHoursSessionDTO.StartTime,
                EndTime = standardHoursSessionDTO.EndTime,
                CreatedDate = standardHoursSessionDTO.CreatedDate,
                LastModifiedDate = standardHoursSessionDTO.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHoursSession", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing StandardHoursSession model with data from a StandardHoursSessionDTO,
        /// validating session ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="standardHoursSessionModel">The StandardHoursSession model to update.</param>
        /// <param name="standardHoursSessionDTO">The StandardHoursSessionDTO containing updated data.</param>
        /// <returns>The updated StandardHoursSession model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursSessionModel or standardHoursSessionDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when session IDs do not match.</exception>
        public static StandardHoursSession UpdateStandardHoursSession(this StandardHoursSession standardHoursSessionModel, StandardHoursSessionDTO standardHoursSessionDTO)
        {
            if (standardHoursSessionModel == null)
                throw new ArgumentNullException(nameof(standardHoursSessionModel));
            if (standardHoursSessionDTO == null)
                throw new ArgumentNullException(nameof(standardHoursSessionDTO));

            if (standardHoursSessionModel.SessionID != standardHoursSessionDTO.SessionID)
            {
                throw new ArgumentException("SessionIDs don't match for Update StandardHoursSession", nameof(standardHoursSessionDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            standardHoursSessionModel.StandardHoursID = standardHoursSessionDTO.StandardHoursID;
            standardHoursSessionModel.DayOfWeek = standardHoursSessionDTO.DayOfWeek;
            standardHoursSessionModel.StartTime = standardHoursSessionDTO.StartTime;
            standardHoursSessionModel.EndTime = standardHoursSessionDTO.EndTime;
            standardHoursSessionModel.CreatedDate = standardHoursSessionDTO.CreatedDate;
            standardHoursSessionModel.LastModifiedDate = DateTime.UtcNow;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateStandardHoursSession", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return standardHoursSessionModel;
        }

        /// <summary>
        /// Converts a collection of StandardHoursSession models to their corresponding DTO representations.
        /// </summary>
        /// <param name="standardHoursSessions">The collection of StandardHoursSession models to convert.</param>
        /// <returns>A list of StandardHoursSessionDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursSessions is null.</exception>
        public static List<StandardHoursSessionDTO> ToStandardHoursSessionDTOs(this IEnumerable<StandardHoursSession> standardHoursSessions)
        {
            if (standardHoursSessions == null)
                throw new ArgumentNullException(nameof(standardHoursSessions));

            var stopwatch = Stopwatch.StartNew();

            var result = standardHoursSessions.Select(shs => shs.ToStandardHoursSessionDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHoursSessionDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of StandardHoursSessionDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="standardHoursSessionDTOs">The collection of StandardHoursSessionDTOs to convert.</param>
        /// <returns>A list of StandardHoursSession models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursSessionDTOs is null.</exception>
        public static List<StandardHoursSession> ToStandardHoursSessions(this IEnumerable<StandardHoursSessionDTO> standardHoursSessionDTOs)
        {
            if (standardHoursSessionDTOs == null)
                throw new ArgumentNullException(nameof(standardHoursSessionDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = standardHoursSessionDTOs.Select(dto => dto.ToStandardHoursSession()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHoursSessions", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a StandardHoursSession model to prevent unintended mutations.
        /// </summary>
        /// <param name="standardHoursSession">The StandardHoursSession model to clone.</param>
        /// <returns>A new StandardHoursSession instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursSession is null.</exception>
        public static StandardHoursSession DeepClone(this StandardHoursSession standardHoursSession)
        {
            if (standardHoursSession == null)
                throw new ArgumentNullException(nameof(standardHoursSession));

            return new StandardHoursSession
            {
                SessionID = standardHoursSession.SessionID,
                StandardHoursID = standardHoursSession.StandardHoursID,
                DayOfWeek = standardHoursSession.DayOfWeek,
                StartTime = standardHoursSession.StartTime,
                EndTime = standardHoursSession.EndTime,
                CreatedDate = standardHoursSession.CreatedDate,
                LastModifiedDate = standardHoursSession.LastModifiedDate
            };
        }

        /// <summary>
        /// Creates a deep clone of a StandardHoursSessionDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="standardHoursSessionDTO">The StandardHoursSessionDTO to clone.</param>
        /// <returns>A new StandardHoursSessionDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursSessionDTO is null.</exception>
        public static StandardHoursSessionDTO DeepClone(this StandardHoursSessionDTO standardHoursSessionDTO)
        {
            if (standardHoursSessionDTO == null)
                throw new ArgumentNullException(nameof(standardHoursSessionDTO));

            return new StandardHoursSessionDTO
            {
                SessionID = standardHoursSessionDTO.SessionID,
                StandardHoursID = standardHoursSessionDTO.StandardHoursID,
                DayOfWeek = standardHoursSessionDTO.DayOfWeek,
                StartTime = standardHoursSessionDTO.StartTime,
                EndTime = standardHoursSessionDTO.EndTime,
                CreatedDate = standardHoursSessionDTO.CreatedDate,
                LastModifiedDate = standardHoursSessionDTO.LastModifiedDate
            };
        }
    }
}
