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
    /// Provides extension methods for converting between Announcement model and AnnouncementDTO,
    /// enabling seamless data transfer between the database layer and external interfaces.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class AnnouncementExtensions
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
        /// Converts an Announcement model instance to its corresponding DTO representation,
        /// mapping all properties for data transfer operations.
        /// </summary>
        /// <param name="announcementModel">The Announcement model to convert.</param>
        /// <returns>A new AnnouncementDTO with all properties mapped from the model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when announcementModel is null.</exception>
        public static AnnouncementDTO ToAnnouncementDTO(this Announcement announcementModel)
        {
            if (announcementModel == null)
                throw new ArgumentNullException(nameof(announcementModel));

            var stopwatch = Stopwatch.StartNew();

            var result = new AnnouncementDTO
            {
                AnnouncementID = announcementModel.AnnouncementID,
                DeliveryTime = announcementModel.DeliveryTime,
                Message = announcementModel.Message,
                Status = announcementModel.Status,
                Type = announcementModel.Type,
                CreatedDate = announcementModel.CreatedDate,
                LastModifiedDate = announcementModel.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToAnnouncementDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts an AnnouncementDTO to its corresponding model representation,
        /// creating a new Announcement instance with all properties mapped from the DTO.
        /// </summary>
        /// <param name="announcementDTO">The AnnouncementDTO to convert.</param>
        /// <returns>A new Announcement model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when announcementDTO is null.</exception>
        public static Announcement ToAnnouncement(this AnnouncementDTO announcementDTO)
        {
            if (announcementDTO == null)
                throw new ArgumentNullException(nameof(announcementDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new Announcement
            {
                AnnouncementID = announcementDTO.AnnouncementID,
                DeliveryTime = announcementDTO.DeliveryTime,
                Message = announcementDTO.Message,
                Status = announcementDTO.Status,
                Type = announcementDTO.Type,
                CreatedDate = announcementDTO.CreatedDate,
                LastModifiedDate = announcementDTO.LastModifiedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToAnnouncement", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Announcement model with data from an AnnouncementDTO,
        /// ensuring the announcement IDs match before applying changes and updating the modification timestamp.
        /// Only updates mutable fields; CreatedDate is preserved from the original model.
        /// </summary>
        /// <param name="announcementModel">The Announcement model to update.</param>
        /// <param name="announcementDTO">The AnnouncementDTO containing the updated data.</param>
        /// <returns>The updated Announcement model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when announcementModel or announcementDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the announcement IDs do not match.</exception>
        public static Announcement UpdateAnnouncement(this Announcement announcementModel, AnnouncementDTO announcementDTO)
        {
            if (announcementModel == null)
                throw new ArgumentNullException(nameof(announcementModel));
            if (announcementDTO == null)
                throw new ArgumentNullException(nameof(announcementDTO));

            if (announcementModel.AnnouncementID != announcementDTO.AnnouncementID)
            {
                throw new ArgumentException("AnnouncementIDs don't match for Update Announcement", nameof(announcementDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            announcementModel.DeliveryTime = announcementDTO.DeliveryTime;
            announcementModel.Message = announcementDTO.Message;
            announcementModel.Status = announcementDTO.Status;
            announcementModel.Type = announcementDTO.Type;
            // CreatedDate is not updated to preserve original creation time
            announcementModel.LastModifiedDate = ExtensionConfiguration.TimestampProvider();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateAnnouncement", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return announcementModel;
        }

        /// <summary>
        /// Converts a collection of Announcement models to their corresponding DTO representations.
        /// </summary>
        /// <param name="announcements">The collection of Announcement models to convert.</param>
        /// <returns>A list of AnnouncementDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when announcements is null.</exception>
        public static List<AnnouncementDTO> ToAnnouncementDTOs(this IEnumerable<Announcement> announcements)
        {
            if (announcements == null)
                throw new ArgumentNullException(nameof(announcements));

            var stopwatch = Stopwatch.StartNew();

            var result = announcements.Select(a => a.ToAnnouncementDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToAnnouncementDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of AnnouncementDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="announcementDTOs">The collection of AnnouncementDTOs to convert.</param>
        /// <returns>A list of Announcement models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when announcementDTOs is null.</exception>
        public static List<Announcement> ToAnnouncements(this IEnumerable<AnnouncementDTO> announcementDTOs)
        {
            if (announcementDTOs == null)
                throw new ArgumentNullException(nameof(announcementDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = announcementDTOs.Select(dto => dto.ToAnnouncement()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToAnnouncements", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of an Announcement model to prevent unintended mutations.
        /// </summary>
        /// <param name="announcement">The Announcement model to clone.</param>
        /// <returns>A new Announcement instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when announcement is null.</exception>
        public static Announcement DeepClone(this Announcement announcement)
        {
            if (announcement == null)
                throw new ArgumentNullException(nameof(announcement));

            return new Announcement
            {
                AnnouncementID = announcement.AnnouncementID,
                DeliveryTime = announcement.DeliveryTime,
                Message = announcement.Message,
                Status = announcement.Status,
                Type = announcement.Type,
                CreatedDate = announcement.CreatedDate,
                LastModifiedDate = announcement.LastModifiedDate
            };
        }

        /// <summary>
        /// Creates a deep clone of an AnnouncementDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="announcementDTO">The AnnouncementDTO to clone.</param>
        /// <returns>A new AnnouncementDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when announcementDTO is null.</exception>
        public static AnnouncementDTO DeepClone(this AnnouncementDTO announcementDTO)
        {
            if (announcementDTO == null)
                throw new ArgumentNullException(nameof(announcementDTO));

            return new AnnouncementDTO
            {
                AnnouncementID = announcementDTO.AnnouncementID,
                DeliveryTime = announcementDTO.DeliveryTime,
                Message = announcementDTO.Message,
                Status = announcementDTO.Status,
                Type = announcementDTO.Type,
                CreatedDate = announcementDTO.CreatedDate,
                LastModifiedDate = announcementDTO.LastModifiedDate
            };
        }
    }
}
