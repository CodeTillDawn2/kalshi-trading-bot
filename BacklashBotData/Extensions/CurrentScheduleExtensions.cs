using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between CurrentSchedule model and CurrentScheduleDTO,
    /// including performance metrics collection and transformation capabilities.
    /// </summary>
    public static class CurrentScheduleExtensions
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
        /// Converts a CurrentSchedule model to its DTO representation.
        /// </summary>
        /// <param name="currentScheduleModel">The CurrentSchedule model to convert.</param>
        /// <returns>A new CurrentScheduleDTO with all properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentScheduleModel is null.</exception>
        public static CurrentScheduleDTO ToCurrentScheduleDTO(this CurrentSchedule currentScheduleModel)
        {
            if (currentScheduleModel == null)
                throw new ArgumentNullException(nameof(currentScheduleModel));

            var stopwatch = Stopwatch.StartNew();

            var result = new CurrentScheduleDTO
            {
                DayOfWeek = currentScheduleModel.DayOfWeek,
                StartTime = currentScheduleModel.StartTime,
                EndTime = currentScheduleModel.EndTime,
                LastModifiedDate = currentScheduleModel.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCurrentScheduleDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a CurrentScheduleDTO to its model representation.
        /// </summary>
        /// <param name="currentScheduleDTO">The CurrentScheduleDTO to convert.</param>
        /// <returns>A new CurrentSchedule model with all properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentScheduleDTO is null.</exception>
        public static CurrentSchedule ToCurrentSchedule(this CurrentScheduleDTO currentScheduleDTO)
        {
            if (currentScheduleDTO == null)
                throw new ArgumentNullException(nameof(currentScheduleDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new CurrentSchedule
            {
                DayOfWeek = currentScheduleDTO.DayOfWeek,
                StartTime = currentScheduleDTO.StartTime,
                EndTime = currentScheduleDTO.EndTime,
                LastModifiedDate = currentScheduleDTO.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCurrentSchedule", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing CurrentSchedule model with data from a CurrentScheduleDTO,
        /// validating day of week match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="currentScheduleModel">The CurrentSchedule model to update.</param>
        /// <param name="currentScheduleDTO">The CurrentScheduleDTO containing updated data.</param>
        /// <returns>The updated CurrentSchedule model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentScheduleModel or currentScheduleDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when day of week values do not match.</exception>
        public static CurrentSchedule UpdateCurrentSchedule(this CurrentSchedule currentScheduleModel, CurrentScheduleDTO currentScheduleDTO)
        {
            if (currentScheduleModel == null)
                throw new ArgumentNullException(nameof(currentScheduleModel));
            if (currentScheduleDTO == null)
                throw new ArgumentNullException(nameof(currentScheduleDTO));

            if (currentScheduleModel.DayOfWeek != currentScheduleDTO.DayOfWeek)
            {
                throw new ArgumentException("DayOfWeek values don't match for Update CurrentSchedule", nameof(currentScheduleDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            currentScheduleModel.StartTime = currentScheduleDTO.StartTime;
            currentScheduleModel.EndTime = currentScheduleDTO.EndTime;
            currentScheduleModel.LastModifiedDate = DateTime.UtcNow;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateCurrentSchedule", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return currentScheduleModel;
        }

        /// <summary>
        /// Converts a collection of CurrentSchedule models to their corresponding DTO representations.
        /// </summary>
        /// <param name="currentSchedules">The collection of CurrentSchedule models to convert.</param>
        /// <returns>A list of CurrentScheduleDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentSchedules is null.</exception>
        public static List<CurrentScheduleDTO> ToCurrentScheduleDTOs(this IEnumerable<CurrentSchedule> currentSchedules)
        {
            if (currentSchedules == null)
                throw new ArgumentNullException(nameof(currentSchedules));

            var stopwatch = Stopwatch.StartNew();

            var result = currentSchedules.Select(cs => cs.ToCurrentScheduleDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCurrentScheduleDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of CurrentScheduleDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="currentScheduleDTOs">The collection of CurrentScheduleDTOs to convert.</param>
        /// <returns>A list of CurrentSchedule models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentScheduleDTOs is null.</exception>
        public static List<CurrentSchedule> ToCurrentScheduleList(this IEnumerable<CurrentScheduleDTO> currentScheduleDTOs)
        {
            if (currentScheduleDTOs == null)
                throw new ArgumentNullException(nameof(currentScheduleDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = currentScheduleDTOs.Select(dto => dto.ToCurrentSchedule()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToCurrentScheduleList", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a CurrentSchedule model to prevent unintended mutations.
        /// </summary>
        /// <param name="currentSchedule">The CurrentSchedule model to clone.</param>
        /// <returns>A new CurrentSchedule instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentSchedule is null.</exception>
        public static CurrentSchedule DeepClone(this CurrentSchedule currentSchedule)
        {
            if (currentSchedule == null)
                throw new ArgumentNullException(nameof(currentSchedule));

            return new CurrentSchedule
            {
                DayOfWeek = currentSchedule.DayOfWeek,
                StartTime = currentSchedule.StartTime,
                EndTime = currentSchedule.EndTime,
                LastModifiedDate = currentSchedule.LastModifiedDate
            };
        }

        /// <summary>
        /// Creates a deep clone of a CurrentScheduleDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="currentScheduleDTO">The CurrentScheduleDTO to clone.</param>
        /// <returns>A new CurrentScheduleDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when currentScheduleDTO is null.</exception>
        public static CurrentScheduleDTO DeepClone(this CurrentScheduleDTO currentScheduleDTO)
        {
            if (currentScheduleDTO == null)
                throw new ArgumentNullException(nameof(currentScheduleDTO));

            return new CurrentScheduleDTO
            {
                DayOfWeek = currentScheduleDTO.DayOfWeek,
                StartTime = currentScheduleDTO.StartTime,
                EndTime = currentScheduleDTO.EndTime,
                LastModifiedDate = currentScheduleDTO.LastModifiedDate
            };
        }
    }
}