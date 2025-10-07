using BacklashDTOs.Data;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between ExchangeSchedule model and ExchangeScheduleDTO,
    /// including nested maintenance windows and standard hours collections for comprehensive schedule data transfer.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class ExchangeScheduleExtensions
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
        /// Converts an ExchangeSchedule model to its DTO representation,
        /// including all nested maintenance windows and standard hours data.
        /// </summary>
        /// <param name="exchangeScheduleModel">The ExchangeSchedule model to convert.</param>
        /// <returns>A new ExchangeScheduleDTO with all schedule properties and nested collections mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchangeScheduleModel is null.</exception>
        public static ExchangeScheduleDTO ToExchangeScheduleDTO(this ExchangeSchedule exchangeScheduleModel)
        {
            if (exchangeScheduleModel == null)
                throw new ArgumentNullException(nameof(exchangeScheduleModel));

            var stopwatch = Stopwatch.StartNew();

            var result = new ExchangeScheduleDTO
            {
                ExchangeScheduleID = exchangeScheduleModel.ExchangeScheduleID,
                LastUpdated = exchangeScheduleModel.LastUpdated,
                CreatedDate = exchangeScheduleModel.CreatedDate,
                LastModifiedDate = exchangeScheduleModel.LastModifiedDate,
                MaintenanceWindows = exchangeScheduleModel.MaintenanceWindows?.Select(mw => mw.ToMaintenanceWindowDTO()).ToList() ?? new List<MaintenanceWindowDTO>(),
                StandardHours = exchangeScheduleModel.StandardHours?.Select(sh => sh.ToStandardHoursDTO()).ToList() ?? new List<StandardHoursDTO>()
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToExchangeScheduleDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts an ExchangeScheduleDTO to its model representation,
        /// creating a new ExchangeSchedule with all properties and nested collections mapped from the DTO.
        /// </summary>
        /// <param name="exchangeScheduleDTO">The ExchangeScheduleDTO to convert.</param>
        /// <returns>A new ExchangeSchedule model with all properties and nested collections mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchangeScheduleDTO is null.</exception>
        public static ExchangeSchedule ToExchangeSchedule(this ExchangeScheduleDTO exchangeScheduleDTO)
        {
            if (exchangeScheduleDTO == null)
                throw new ArgumentNullException(nameof(exchangeScheduleDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new ExchangeSchedule
            {
                ExchangeScheduleID = exchangeScheduleDTO.ExchangeScheduleID,
                LastUpdated = exchangeScheduleDTO.LastUpdated,
                CreatedDate = exchangeScheduleDTO.CreatedDate,
                LastModifiedDate = exchangeScheduleDTO.LastModifiedDate,
                MaintenanceWindows = exchangeScheduleDTO.MaintenanceWindows?.Select(mw => mw.ToMaintenanceWindow()).ToList() ?? new List<MaintenanceWindow>(),
                StandardHours = exchangeScheduleDTO.StandardHours?.Select(sh => sh.ToStandardHours()).ToList() ?? new List<StandardHours>()
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToExchangeSchedule", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing ExchangeSchedule model with data from an ExchangeScheduleDTO,
        /// validating schedule ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="exchangeScheduleModel">The ExchangeSchedule model to update.</param>
        /// <param name="exchangeScheduleDTO">The ExchangeScheduleDTO containing updated data.</param>
        /// <returns>The updated ExchangeSchedule model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchangeScheduleModel or exchangeScheduleDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when exchange schedule IDs do not match.</exception>
        public static ExchangeSchedule UpdateExchangeSchedule(this ExchangeSchedule exchangeScheduleModel, ExchangeScheduleDTO exchangeScheduleDTO)
        {
            if (exchangeScheduleModel == null)
                throw new ArgumentNullException(nameof(exchangeScheduleModel));
            if (exchangeScheduleDTO == null)
                throw new ArgumentNullException(nameof(exchangeScheduleDTO));

            if (exchangeScheduleModel.ExchangeScheduleID != exchangeScheduleDTO.ExchangeScheduleID)
            {
                throw new ArgumentException("ExchangeScheduleIDs don't match for Update ExchangeSchedule", nameof(exchangeScheduleDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            exchangeScheduleModel.LastUpdated = exchangeScheduleDTO.LastUpdated;
            exchangeScheduleModel.CreatedDate = exchangeScheduleDTO.CreatedDate;
            exchangeScheduleModel.LastModifiedDate = DateTime.UtcNow;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateExchangeSchedule", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return exchangeScheduleModel;
        }

        /// <summary>
        /// Converts a collection of ExchangeSchedule models to their corresponding DTO representations.
        /// </summary>
        /// <param name="exchangeSchedules">The collection of ExchangeSchedule models to convert.</param>
        /// <returns>A list of ExchangeScheduleDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchangeSchedules is null.</exception>
        public static List<ExchangeScheduleDTO> ToExchangeScheduleDTOs(this IEnumerable<ExchangeSchedule> exchangeSchedules)
        {
            if (exchangeSchedules == null)
                throw new ArgumentNullException(nameof(exchangeSchedules));

            var stopwatch = Stopwatch.StartNew();

            var result = exchangeSchedules.Select(es => es.ToExchangeScheduleDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToExchangeScheduleDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of ExchangeScheduleDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="exchangeScheduleDTOs">The collection of ExchangeScheduleDTOs to convert.</param>
        /// <returns>A list of ExchangeSchedule models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchangeScheduleDTOs is null.</exception>
        public static List<ExchangeSchedule> ToExchangeSchedules(this IEnumerable<ExchangeScheduleDTO> exchangeScheduleDTOs)
        {
            if (exchangeScheduleDTOs == null)
                throw new ArgumentNullException(nameof(exchangeScheduleDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = exchangeScheduleDTOs.Select(dto => dto.ToExchangeSchedule()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToExchangeSchedules", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of an ExchangeSchedule model to prevent unintended mutations.
        /// </summary>
        /// <param name="exchangeSchedule">The ExchangeSchedule model to clone.</param>
        /// <returns>A new ExchangeSchedule instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchangeSchedule is null.</exception>
        public static ExchangeSchedule DeepClone(this ExchangeSchedule exchangeSchedule)
        {
            if (exchangeSchedule == null)
                throw new ArgumentNullException(nameof(exchangeSchedule));

            return new ExchangeSchedule
            {
                ExchangeScheduleID = exchangeSchedule.ExchangeScheduleID,
                LastUpdated = exchangeSchedule.LastUpdated,
                CreatedDate = exchangeSchedule.CreatedDate,
                LastModifiedDate = exchangeSchedule.LastModifiedDate,
                MaintenanceWindows = exchangeSchedule.MaintenanceWindows?.Select(mw => new MaintenanceWindow
                {
                    MaintenanceWindowID = mw.MaintenanceWindowID,
                    ExchangeScheduleID = mw.ExchangeScheduleID,
                    StartDateTime = mw.StartDateTime,
                    EndDateTime = mw.EndDateTime,
                    CreatedDate = mw.CreatedDate,
                    LastModifiedDate = mw.LastModifiedDate
                }).ToList() ?? new List<MaintenanceWindow>(),
                StandardHours = exchangeSchedule.StandardHours?.Select(sh => new StandardHours
                {
                    StandardHoursID = sh.StandardHoursID,
                    ExchangeScheduleID = sh.ExchangeScheduleID,
                    StartTime = sh.StartTime,
                    EndTime = sh.EndTime,
                    CreatedDate = sh.CreatedDate,
                    LastModifiedDate = sh.LastModifiedDate,
                    Sessions = sh.Sessions?.Select(s => new StandardHoursSession
                    {
                        SessionID = s.SessionID,
                        StandardHoursID = s.StandardHoursID,
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        CreatedDate = s.CreatedDate,
                        LastModifiedDate = s.LastModifiedDate
                    }).ToList() ?? new List<StandardHoursSession>()
                }).ToList() ?? new List<StandardHours>()
            };
        }

        /// <summary>
        /// Creates a deep clone of an ExchangeScheduleDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="exchangeScheduleDTO">The ExchangeScheduleDTO to clone.</param>
        /// <returns>A new ExchangeScheduleDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchangeScheduleDTO is null.</exception>
        public static ExchangeScheduleDTO DeepClone(this ExchangeScheduleDTO exchangeScheduleDTO)
        {
            if (exchangeScheduleDTO == null)
                throw new ArgumentNullException(nameof(exchangeScheduleDTO));

            return new ExchangeScheduleDTO
            {
                ExchangeScheduleID = exchangeScheduleDTO.ExchangeScheduleID,
                LastUpdated = exchangeScheduleDTO.LastUpdated,
                CreatedDate = exchangeScheduleDTO.CreatedDate,
                LastModifiedDate = exchangeScheduleDTO.LastModifiedDate,
                MaintenanceWindows = exchangeScheduleDTO.MaintenanceWindows?.Select(mw => new MaintenanceWindowDTO
                {
                    MaintenanceWindowID = mw.MaintenanceWindowID,
                    ExchangeScheduleID = mw.ExchangeScheduleID,
                    StartDateTime = mw.StartDateTime,
                    EndDateTime = mw.EndDateTime,
                    CreatedDate = mw.CreatedDate,
                    LastModifiedDate = mw.LastModifiedDate
                }).ToList() ?? new List<MaintenanceWindowDTO>(),
                StandardHours = exchangeScheduleDTO.StandardHours?.Select(sh => new StandardHoursDTO
                {
                    StandardHoursID = sh.StandardHoursID,
                    ExchangeScheduleID = sh.ExchangeScheduleID,
                    StartTime = sh.StartTime,
                    EndTime = sh.EndTime,
                    CreatedDate = sh.CreatedDate,
                    LastModifiedDate = sh.LastModifiedDate,
                    Sessions = sh.Sessions?.Select(s => new StandardHoursSessionDTO
                    {
                        SessionID = s.SessionID,
                        StandardHoursID = s.StandardHoursID,
                        DayOfWeek = s.DayOfWeek,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        CreatedDate = s.CreatedDate,
                        LastModifiedDate = s.LastModifiedDate
                    }).ToList() ?? new List<StandardHoursSessionDTO>()
                }).ToList() ?? new List<StandardHoursDTO>()
            };
        }
    }
}
