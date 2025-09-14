using KalshiBotData.Models;
using BacklashDTOs.Data;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between StandardHours model and StandardHoursDTO,
    /// including nested sessions collection for comprehensive exchange hours data transfer.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class StandardHoursExtensions
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
        /// Converts a StandardHours model to its DTO representation,
        /// including all nested session data for complete hours information.
        /// </summary>
        /// <param name="standardHoursModel">The StandardHours model to convert.</param>
        /// <returns>A new StandardHoursDTO with all hours properties and nested sessions mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursModel is null.</exception>
        public static StandardHoursDTO ToStandardHoursDTO(this StandardHours standardHoursModel)
        {
            if (standardHoursModel == null)
                throw new ArgumentNullException(nameof(standardHoursModel));

            var stopwatch = Stopwatch.StartNew();

            var result = new StandardHoursDTO
            {
                StandardHoursID = standardHoursModel.StandardHoursID,
                ExchangeScheduleID = standardHoursModel.ExchangeScheduleID,
                StartTime = standardHoursModel.StartTime,
                EndTime = standardHoursModel.EndTime,
                CreatedDate = standardHoursModel.CreatedDate,
                LastModifiedDate = standardHoursModel.LastModifiedDate,
                Sessions = standardHoursModel.Sessions?.Select(s => s.ToStandardHoursSessionDTO()).ToList() ?? new List<StandardHoursSessionDTO>()
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHoursDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a StandardHoursDTO to its model representation,
        /// creating a new StandardHours with all properties and nested sessions mapped from the DTO.
        /// </summary>
        /// <param name="standardHoursDTO">The StandardHoursDTO to convert.</param>
        /// <returns>A new StandardHours model with all properties and nested sessions mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursDTO is null.</exception>
        public static StandardHours ToStandardHours(this StandardHoursDTO standardHoursDTO)
        {
            if (standardHoursDTO == null)
                throw new ArgumentNullException(nameof(standardHoursDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new StandardHours
            {
                StandardHoursID = standardHoursDTO.StandardHoursID,
                ExchangeScheduleID = standardHoursDTO.ExchangeScheduleID,
                StartTime = standardHoursDTO.StartTime,
                EndTime = standardHoursDTO.EndTime,
                CreatedDate = standardHoursDTO.CreatedDate,
                LastModifiedDate = standardHoursDTO.LastModifiedDate,
                Sessions = standardHoursDTO.Sessions?.Select(s => s.ToStandardHoursSession()).ToList() ?? new List<StandardHoursSession>()
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHours", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing StandardHours model with data from a StandardHoursDTO,
        /// validating standard hours ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="standardHoursModel">The StandardHours model to update.</param>
        /// <param name="standardHoursDTO">The StandardHoursDTO containing updated data.</param>
        /// <returns>The updated StandardHours model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursModel or standardHoursDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when standard hours IDs do not match.</exception>
        public static StandardHours UpdateStandardHours(this StandardHours standardHoursModel, StandardHoursDTO standardHoursDTO)
        {
            if (standardHoursModel == null)
                throw new ArgumentNullException(nameof(standardHoursModel));
            if (standardHoursDTO == null)
                throw new ArgumentNullException(nameof(standardHoursDTO));

            if (standardHoursModel.StandardHoursID != standardHoursDTO.StandardHoursID)
            {
                throw new ArgumentException("StandardHoursIDs don't match for Update StandardHours", nameof(standardHoursDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            standardHoursModel.ExchangeScheduleID = standardHoursDTO.ExchangeScheduleID;
            standardHoursModel.StartTime = standardHoursDTO.StartTime;
            standardHoursModel.EndTime = standardHoursDTO.EndTime;
            standardHoursModel.CreatedDate = standardHoursDTO.CreatedDate;
            standardHoursModel.LastModifiedDate = DateTime.UtcNow;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateStandardHours", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return standardHoursModel;
        }

        /// <summary>
        /// Converts a collection of StandardHours models to their corresponding DTO representations.
        /// </summary>
        /// <param name="standardHours">The collection of StandardHours models to convert.</param>
        /// <returns>A list of StandardHoursDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHours is null.</exception>
        public static List<StandardHoursDTO> ToStandardHoursDTOs(this IEnumerable<StandardHours> standardHours)
        {
            if (standardHours == null)
                throw new ArgumentNullException(nameof(standardHours));

            var stopwatch = Stopwatch.StartNew();

            var result = standardHours.Select(sh => sh.ToStandardHoursDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHoursDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of StandardHoursDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="standardHoursDTOs">The collection of StandardHoursDTOs to convert.</param>
        /// <returns>A list of StandardHours models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursDTOs is null.</exception>
        public static List<StandardHours> ToStandardHoursList(this IEnumerable<StandardHoursDTO> standardHoursDTOs)
        {
            if (standardHoursDTOs == null)
                throw new ArgumentNullException(nameof(standardHoursDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = standardHoursDTOs.Select(dto => dto.ToStandardHours()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToStandardHoursList", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a StandardHours model to prevent unintended mutations.
        /// </summary>
        /// <param name="standardHours">The StandardHours model to clone.</param>
        /// <returns>A new StandardHours instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHours is null.</exception>
        public static StandardHours DeepClone(this StandardHours standardHours)
        {
            if (standardHours == null)
                throw new ArgumentNullException(nameof(standardHours));

            return new StandardHours
            {
                StandardHoursID = standardHours.StandardHoursID,
                ExchangeScheduleID = standardHours.ExchangeScheduleID,
                StartTime = standardHours.StartTime,
                EndTime = standardHours.EndTime,
                CreatedDate = standardHours.CreatedDate,
                LastModifiedDate = standardHours.LastModifiedDate,
                Sessions = standardHours.Sessions?.Select(s => new StandardHoursSession
                {
                    SessionID = s.SessionID,
                    StandardHoursID = s.StandardHoursID,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    CreatedDate = s.CreatedDate,
                    LastModifiedDate = s.LastModifiedDate
                }).ToList() ?? new List<StandardHoursSession>()
            };
        }

        /// <summary>
        /// Creates a deep clone of a StandardHoursDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="standardHoursDTO">The StandardHoursDTO to clone.</param>
        /// <returns>A new StandardHoursDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when standardHoursDTO is null.</exception>
        public static StandardHoursDTO DeepClone(this StandardHoursDTO standardHoursDTO)
        {
            if (standardHoursDTO == null)
                throw new ArgumentNullException(nameof(standardHoursDTO));

            return new StandardHoursDTO
            {
                StandardHoursID = standardHoursDTO.StandardHoursID,
                ExchangeScheduleID = standardHoursDTO.ExchangeScheduleID,
                StartTime = standardHoursDTO.StartTime,
                EndTime = standardHoursDTO.EndTime,
                CreatedDate = standardHoursDTO.CreatedDate,
                LastModifiedDate = standardHoursDTO.LastModifiedDate,
                Sessions = standardHoursDTO.Sessions?.Select(s => new StandardHoursSessionDTO
                {
                    SessionID = s.SessionID,
                    StandardHoursID = s.StandardHoursID,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    CreatedDate = s.CreatedDate,
                    LastModifiedDate = s.LastModifiedDate
                }).ToList() ?? new List<StandardHoursSessionDTO>()
            };
        }
    }
}
