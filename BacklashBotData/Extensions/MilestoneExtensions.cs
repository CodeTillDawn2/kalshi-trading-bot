using BacklashDTOs.Data;
using BacklashDTOs.KalshiAPI;
using BacklashBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace BacklashBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Milestone model and MilestoneDTO,
    /// enabling seamless data transfer between the database layer and external interfaces.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class MilestoneExtensions
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
        /// Converts a Milestone model instance to its corresponding DTO representation,
        /// mapping all properties for data transfer operations.
        /// </summary>
        /// <param name="milestoneModel">The Milestone model to convert.</param>
        /// <returns>A new MilestoneDTO with all properties mapped from the model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when milestoneModel is null.</exception>
        public static MilestoneDTO ToMilestoneDTO(this Milestone milestoneModel)
        {
            if (milestoneModel == null)
                throw new ArgumentNullException(nameof(milestoneModel));

            var stopwatch = Stopwatch.StartNew();

            var result = new MilestoneDTO
            {
                Id = milestoneModel.Id,
                Category = milestoneModel.Category,
                Details = milestoneModel.Details,
                EndDate = milestoneModel.EndDate,
                LastUpdatedTs = milestoneModel.LastUpdatedTs,
                NotificationMessage = milestoneModel.NotificationMessage,
                PrimaryEventTickers = milestoneModel.PrimaryEventTickers,
                RelatedEventTickers = milestoneModel.RelatedEventTickers,
                SourceId = milestoneModel.SourceId,
                StartDate = milestoneModel.StartDate,
                Title = milestoneModel.Title,
                Type = milestoneModel.Type,
                CreatedDate = milestoneModel.CreatedDate,
                LastModifiedDate = milestoneModel.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMilestoneDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a MilestoneDTO to its corresponding model representation,
        /// creating a new Milestone instance with all properties mapped from the DTO.
        /// </summary>
        /// <param name="milestoneDTO">The MilestoneDTO to convert.</param>
        /// <returns>A new Milestone model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when milestoneDTO is null.</exception>
        public static Milestone ToMilestone(this MilestoneDTO milestoneDTO)
        {
            if (milestoneDTO == null)
                throw new ArgumentNullException(nameof(milestoneDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new Milestone
            {
                Id = milestoneDTO.Id,
                Category = milestoneDTO.Category,
                Details = milestoneDTO.Details,
                EndDate = milestoneDTO.EndDate,
                LastUpdatedTs = milestoneDTO.LastUpdatedTs,
                NotificationMessage = milestoneDTO.NotificationMessage,
                PrimaryEventTickers = milestoneDTO.PrimaryEventTickers,
                RelatedEventTickers = milestoneDTO.RelatedEventTickers,
                SourceId = milestoneDTO.SourceId,
                StartDate = milestoneDTO.StartDate,
                Title = milestoneDTO.Title,
                Type = milestoneDTO.Type,
                CreatedDate = milestoneDTO.CreatedDate,
                LastModifiedDate = milestoneDTO.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMilestone", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Milestone model with data from a MilestoneDTO,
        /// ensuring the milestone IDs match before applying changes and updating the modification timestamp.
        /// Only updates mutable fields; CreatedDate is preserved from the original model.
        /// </summary>
        /// <param name="milestoneModel">The Milestone model to update.</param>
        /// <param name="milestoneDTO">The MilestoneDTO containing the updated data.</param>
        /// <returns>The updated Milestone model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when milestoneModel or milestoneDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the milestone IDs do not match.</exception>
        public static Milestone UpdateMilestone(this Milestone milestoneModel, MilestoneDTO milestoneDTO)
        {
            if (milestoneModel == null)
                throw new ArgumentNullException(nameof(milestoneModel));
            if (milestoneDTO == null)
                throw new ArgumentNullException(nameof(milestoneDTO));

            if (milestoneModel.Id != milestoneDTO.Id)
            {
                throw new ArgumentException("Milestone IDs don't match for Update Milestone", nameof(milestoneDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            milestoneModel.Category = milestoneDTO.Category;
            milestoneModel.Details = milestoneDTO.Details;
            milestoneModel.EndDate = milestoneDTO.EndDate;
            milestoneModel.LastUpdatedTs = milestoneDTO.LastUpdatedTs;
            milestoneModel.NotificationMessage = milestoneDTO.NotificationMessage;
            milestoneModel.PrimaryEventTickers = milestoneDTO.PrimaryEventTickers;
            milestoneModel.RelatedEventTickers = milestoneDTO.RelatedEventTickers;
            milestoneModel.SourceId = milestoneDTO.SourceId;
            milestoneModel.StartDate = milestoneDTO.StartDate;
            milestoneModel.Title = milestoneDTO.Title;
            milestoneModel.Type = milestoneDTO.Type;
            // CreatedDate is not updated to preserve original creation time
            milestoneModel.LastModifiedDate = ExtensionConfiguration.TimestampProvider();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateMilestone", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return milestoneModel;
        }

        /// <summary>
        /// Converts a collection of Milestone models to their corresponding DTO representations.
        /// </summary>
        /// <param name="milestones">The collection of Milestone models to convert.</param>
        /// <returns>A list of MilestoneDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when milestones is null.</exception>
        public static List<MilestoneDTO> ToMilestoneDTOs(this IEnumerable<Milestone> milestones)
        {
            if (milestones == null)
                throw new ArgumentNullException(nameof(milestones));

            var stopwatch = Stopwatch.StartNew();

            var result = milestones.Select(m => m.ToMilestoneDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMilestoneDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of MilestoneDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="milestoneDTOs">The collection of MilestoneDTOs to convert.</param>
        /// <returns>A list of Milestone models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when milestoneDTOs is null.</exception>
        public static List<Milestone> ToMilestones(this IEnumerable<MilestoneDTO> milestoneDTOs)
        {
            if (milestoneDTOs == null)
                throw new ArgumentNullException(nameof(milestoneDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = milestoneDTOs.Select(dto => dto.ToMilestone()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMilestones", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a Milestone model to prevent unintended mutations.
        /// </summary>
        /// <param name="milestone">The Milestone model to clone.</param>
        /// <returns>A new Milestone instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when milestone is null.</exception>
        public static Milestone DeepClone(this Milestone milestone)
        {
            if (milestone == null)
                throw new ArgumentNullException(nameof(milestone));

            return new Milestone
            {
                Id = milestone.Id,
                Category = milestone.Category,
                Details = milestone.Details,
                EndDate = milestone.EndDate,
                LastUpdatedTs = milestone.LastUpdatedTs,
                NotificationMessage = milestone.NotificationMessage,
                PrimaryEventTickers = new List<string>(milestone.PrimaryEventTickers),
                RelatedEventTickers = new List<string>(milestone.RelatedEventTickers),
                SourceId = milestone.SourceId,
                StartDate = milestone.StartDate,
                Title = milestone.Title,
                Type = milestone.Type,
                CreatedDate = milestone.CreatedDate,
                LastModifiedDate = milestone.LastModifiedDate
            };
        }

        /// <summary>
        /// Converts a KalshiMilestone from the API to its corresponding MilestoneDTO representation,
        /// mapping all properties for data transfer operations.
        /// </summary>
        /// <param name="kalshiMilestone">The KalshiMilestone from the API to convert.</param>
        /// <returns>A new MilestoneDTO with all properties mapped from the KalshiMilestone.</returns>
        /// <exception cref="ArgumentNullException">Thrown when kalshiMilestone is null.</exception>
        public static MilestoneDTO ToMilestoneDTO(this KalshiMilestone kalshiMilestone)
        {
            if (kalshiMilestone == null)
                throw new ArgumentNullException(nameof(kalshiMilestone));

            var stopwatch = Stopwatch.StartNew();

            var result = new MilestoneDTO
            {
                Id = kalshiMilestone.Id,
                Category = kalshiMilestone.Category,
                Details = kalshiMilestone.Details != null ? JsonSerializer.Serialize(kalshiMilestone.Details) : null,
                EndDate = kalshiMilestone.EndDate,
                LastUpdatedTs = kalshiMilestone.LastUpdatedTs,
                NotificationMessage = kalshiMilestone.NotificationMessage,
                PrimaryEventTickers = kalshiMilestone.PrimaryEventTickers ?? new List<string>(),
                RelatedEventTickers = kalshiMilestone.RelatedEventTickers ?? new List<string>(),
                SourceId = kalshiMilestone.SourceId,
                StartDate = kalshiMilestone.StartDate,
                Title = kalshiMilestone.Title,
                Type = kalshiMilestone.Type,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToMilestoneDTO_Kalshi", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a MilestoneDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="milestoneDTO">The MilestoneDTO to clone.</param>
        /// <returns>A new MilestoneDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when milestoneDTO is null.</exception>
        public static MilestoneDTO DeepClone(this MilestoneDTO milestoneDTO)
        {
            if (milestoneDTO == null)
                throw new ArgumentNullException(nameof(milestoneDTO));

            return new MilestoneDTO
            {
                Id = milestoneDTO.Id,
                Category = milestoneDTO.Category,
                Details = milestoneDTO.Details,
                EndDate = milestoneDTO.EndDate,
                LastUpdatedTs = milestoneDTO.LastUpdatedTs,
                NotificationMessage = milestoneDTO.NotificationMessage,
                PrimaryEventTickers = new List<string>(milestoneDTO.PrimaryEventTickers),
                RelatedEventTickers = new List<string>(milestoneDTO.RelatedEventTickers),
                SourceId = milestoneDTO.SourceId,
                StartDate = milestoneDTO.StartDate,
                Title = milestoneDTO.Title,
                Type = milestoneDTO.Type,
                CreatedDate = milestoneDTO.CreatedDate,
                LastModifiedDate = milestoneDTO.LastModifiedDate
            };
        }
    }
}