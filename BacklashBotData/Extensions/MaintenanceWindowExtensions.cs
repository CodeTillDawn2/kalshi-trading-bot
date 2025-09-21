using BacklashDTOs.Data;
using KalshiBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between MaintenanceWindow model and MaintenanceWindowDTO,
    /// supporting exchange maintenance schedule data transfer operations.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class MaintenanceWindowExtensions
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
        /// Converts a MaintenanceWindow model to its DTO representation,
        /// mapping all maintenance window properties for data transfer.
        /// </summary>
        /// <param name="maintenanceWindowModel">The MaintenanceWindow model to convert.</param>
        /// <returns>A new MaintenanceWindowDTO with all maintenance window properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when maintenanceWindowModel is null.</exception>
        public static MaintenanceWindowDTO ToMaintenanceWindowDTO(this MaintenanceWindow maintenanceWindowModel)
        {
            if (maintenanceWindowModel == null)
                throw new ArgumentNullException(nameof(maintenanceWindowModel));

            var stopwatch = Stopwatch.StartNew();

            var result = new MaintenanceWindowDTO
            {
                MaintenanceWindowID = maintenanceWindowModel.MaintenanceWindowID,
                ExchangeScheduleID = maintenanceWindowModel.ExchangeScheduleID,
                StartDateTime = maintenanceWindowModel.StartDateTime,
                EndDateTime = maintenanceWindowModel.EndDateTime,
                CreatedDate = maintenanceWindowModel.CreatedDate,
                LastModifiedDate = maintenanceWindowModel.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMaintenanceWindowDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a MaintenanceWindowDTO to its model representation,
        /// creating a new MaintenanceWindow with all properties mapped from the DTO.
        /// </summary>
        /// <param name="maintenanceWindowDTO">The MaintenanceWindowDTO to convert.</param>
        /// <returns>A new MaintenanceWindow model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when maintenanceWindowDTO is null.</exception>
        public static MaintenanceWindow ToMaintenanceWindow(this MaintenanceWindowDTO maintenanceWindowDTO)
        {
            if (maintenanceWindowDTO == null)
                throw new ArgumentNullException(nameof(maintenanceWindowDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new MaintenanceWindow
            {
                MaintenanceWindowID = maintenanceWindowDTO.MaintenanceWindowID,
                ExchangeScheduleID = maintenanceWindowDTO.ExchangeScheduleID,
                StartDateTime = maintenanceWindowDTO.StartDateTime,
                EndDateTime = maintenanceWindowDTO.EndDateTime,
                CreatedDate = maintenanceWindowDTO.CreatedDate,
                LastModifiedDate = maintenanceWindowDTO.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMaintenanceWindow", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing MaintenanceWindow model with data from a MaintenanceWindowDTO,
        /// validating maintenance window ID match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="maintenanceWindowModel">The MaintenanceWindow model to update.</param>
        /// <param name="maintenanceWindowDTO">The MaintenanceWindowDTO containing updated data.</param>
        /// <returns>The updated MaintenanceWindow model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when maintenanceWindowModel or maintenanceWindowDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when maintenance window IDs do not match.</exception>
        public static MaintenanceWindow UpdateMaintenanceWindow(this MaintenanceWindow maintenanceWindowModel, MaintenanceWindowDTO maintenanceWindowDTO)
        {
            if (maintenanceWindowModel == null)
                throw new ArgumentNullException(nameof(maintenanceWindowModel));
            if (maintenanceWindowDTO == null)
                throw new ArgumentNullException(nameof(maintenanceWindowDTO));

            if (maintenanceWindowModel.MaintenanceWindowID != maintenanceWindowDTO.MaintenanceWindowID)
            {
                throw new ArgumentException("MaintenanceWindowIDs don't match for Update MaintenanceWindow", nameof(maintenanceWindowDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            maintenanceWindowModel.ExchangeScheduleID = maintenanceWindowDTO.ExchangeScheduleID;
            maintenanceWindowModel.StartDateTime = maintenanceWindowDTO.StartDateTime;
            maintenanceWindowModel.EndDateTime = maintenanceWindowDTO.EndDateTime;
            maintenanceWindowModel.CreatedDate = maintenanceWindowDTO.CreatedDate;
            maintenanceWindowModel.LastModifiedDate = DateTime.UtcNow;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateMaintenanceWindow", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return maintenanceWindowModel;
        }

        /// <summary>
        /// Converts a collection of MaintenanceWindow models to their corresponding DTO representations.
        /// </summary>
        /// <param name="maintenanceWindows">The collection of MaintenanceWindow models to convert.</param>
        /// <returns>A list of MaintenanceWindowDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when maintenanceWindows is null.</exception>
        public static List<MaintenanceWindowDTO> ToMaintenanceWindowDTOs(this IEnumerable<MaintenanceWindow> maintenanceWindows)
        {
            if (maintenanceWindows == null)
                throw new ArgumentNullException(nameof(maintenanceWindows));

            var stopwatch = Stopwatch.StartNew();

            var result = maintenanceWindows.Select(mw => mw.ToMaintenanceWindowDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMaintenanceWindowDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of MaintenanceWindowDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="maintenanceWindowDTOs">The collection of MaintenanceWindowDTOs to convert.</param>
        /// <returns>A list of MaintenanceWindow models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when maintenanceWindowDTOs is null.</exception>
        public static List<MaintenanceWindow> ToMaintenanceWindows(this IEnumerable<MaintenanceWindowDTO> maintenanceWindowDTOs)
        {
            if (maintenanceWindowDTOs == null)
                throw new ArgumentNullException(nameof(maintenanceWindowDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = maintenanceWindowDTOs.Select(dto => dto.ToMaintenanceWindow()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMaintenanceWindows", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a MaintenanceWindow model to prevent unintended mutations.
        /// </summary>
        /// <param name="maintenanceWindow">The MaintenanceWindow model to clone.</param>
        /// <returns>A new MaintenanceWindow instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when maintenanceWindow is null.</exception>
        public static MaintenanceWindow DeepClone(this MaintenanceWindow maintenanceWindow)
        {
            if (maintenanceWindow == null)
                throw new ArgumentNullException(nameof(maintenanceWindow));

            return new MaintenanceWindow
            {
                MaintenanceWindowID = maintenanceWindow.MaintenanceWindowID,
                ExchangeScheduleID = maintenanceWindow.ExchangeScheduleID,
                StartDateTime = maintenanceWindow.StartDateTime,
                EndDateTime = maintenanceWindow.EndDateTime,
                CreatedDate = maintenanceWindow.CreatedDate,
                LastModifiedDate = maintenanceWindow.LastModifiedDate
            };
        }

        /// <summary>
        /// Creates a deep clone of a MaintenanceWindowDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="maintenanceWindowDTO">The MaintenanceWindowDTO to clone.</param>
        /// <returns>A new MaintenanceWindowDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when maintenanceWindowDTO is null.</exception>
        public static MaintenanceWindowDTO DeepClone(this MaintenanceWindowDTO maintenanceWindowDTO)
        {
            if (maintenanceWindowDTO == null)
                throw new ArgumentNullException(nameof(maintenanceWindowDTO));

            return new MaintenanceWindowDTO
            {
                MaintenanceWindowID = maintenanceWindowDTO.MaintenanceWindowID,
                ExchangeScheduleID = maintenanceWindowDTO.ExchangeScheduleID,
                StartDateTime = maintenanceWindowDTO.StartDateTime,
                EndDateTime = maintenanceWindowDTO.EndDateTime,
                CreatedDate = maintenanceWindowDTO.CreatedDate,
                LastModifiedDate = maintenanceWindowDTO.LastModifiedDate
            };
        }
    }
}
