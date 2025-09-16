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
    /// Provides extension methods for converting between BrainInstance model and BrainInstanceDTO,
    /// facilitating data transfer for brain instance management and monitoring operations.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class BrainInstanceExtensions
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
        /// Converts a BrainInstance model to its DTO representation,
        /// mapping all brain instance properties for external data transfer.
        /// </summary>
        /// <param name="brainInstance">The BrainInstance model to convert.</param>
        /// <returns>A new BrainInstanceDTO with all properties mapped from the model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when brainInstance is null.</exception>
        public static BrainInstanceDTO ToBrainInstanceDTO(this BrainInstance brainInstance)
        {
            if (brainInstance == null)
                throw new ArgumentNullException(nameof(brainInstance));

            var stopwatch = Stopwatch.StartNew();

            var result = new BrainInstanceDTO
            {
                BrainInstanceName = brainInstance.BrainInstanceName,
                WatchPositions = brainInstance.WatchPositions,
                WatchOrders = brainInstance.WatchOrders,
                ManagedWatchList = brainInstance.ManagedWatchList,
                TargetWatches = brainInstance.TargetWatches,
                BrainLock = brainInstance.BrainLock,
                UsageMin = brainInstance.UsageMin,
                UsageMax = brainInstance.UsageMax,
                CaptureSnapshots = brainInstance.CaptureSnapshots,
                MinimumInterest = brainInstance.MinimumInterest,
                LastSeen = brainInstance.LastSeen
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToBrainInstanceDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a BrainInstanceDTO to its model representation,
        /// creating a new BrainInstance with all properties mapped from the DTO.
        /// </summary>
        /// <param name="brainInstanceDTO">The BrainInstanceDTO to convert.</param>
        /// <returns>A new BrainInstance model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when brainInstanceDTO is null.</exception>
        public static BrainInstance ToBrainInstance(this BrainInstanceDTO brainInstanceDTO)
        {
            if (brainInstanceDTO == null)
                throw new ArgumentNullException(nameof(brainInstanceDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new BrainInstance
            {
                BrainInstanceName = brainInstanceDTO.BrainInstanceName,
                WatchPositions = brainInstanceDTO.WatchPositions,
                WatchOrders = brainInstanceDTO.WatchOrders,
                ManagedWatchList = brainInstanceDTO.ManagedWatchList,
                TargetWatches = brainInstanceDTO.TargetWatches,
                BrainLock = brainInstanceDTO.BrainLock,
                CaptureSnapshots = brainInstanceDTO.CaptureSnapshots,
                MinimumInterest = brainInstanceDTO.MinimumInterest,
                UsageMin = brainInstanceDTO.UsageMin,
                UsageMax = brainInstanceDTO.UsageMax,
                LastSeen = brainInstanceDTO.LastSeen
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToBrainInstance", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing BrainInstance model with data from a BrainInstanceDTO,
        /// validating the brain instance name match before applying selective property updates.
        /// </summary>
        /// <param name="brainInstance">The BrainInstance model to update.</param>
        /// <param name="brainInstanceDTO">The BrainInstanceDTO containing updated data.</param>
        /// <returns>The updated BrainInstance model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when brainInstance or brainInstanceDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when brain instance names do not match.</exception>
        public static BrainInstance UpdateBrainInstance(this BrainInstance brainInstance, BrainInstanceDTO brainInstanceDTO)
        {
            if (brainInstance == null)
                throw new ArgumentNullException(nameof(brainInstance));
            if (brainInstanceDTO == null)
                throw new ArgumentNullException(nameof(brainInstanceDTO));

            if (brainInstance.BrainInstanceName != brainInstanceDTO.BrainInstanceName)
            {
                throw new ArgumentException("Brain instance name doesn't match for Update BrainInstance", nameof(brainInstanceDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            brainInstance.BrainLock = brainInstanceDTO.BrainLock;
            brainInstance.LastSeen = brainInstanceDTO.LastSeen;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateBrainInstance", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return brainInstance;
        }

        /// <summary>
        /// Converts a collection of BrainInstance models to their corresponding DTO representations.
        /// </summary>
        /// <param name="brainInstances">The collection of BrainInstance models to convert.</param>
        /// <returns>A list of BrainInstanceDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when brainInstances is null.</exception>
        public static List<BrainInstanceDTO> ToBrainInstanceDTOs(this IEnumerable<BrainInstance> brainInstances)
        {
            if (brainInstances == null)
                throw new ArgumentNullException(nameof(brainInstances));

            var stopwatch = Stopwatch.StartNew();

            var result = brainInstances.Select(b => b.ToBrainInstanceDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToBrainInstanceDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of BrainInstanceDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="brainInstanceDTOs">The collection of BrainInstanceDTOs to convert.</param>
        /// <returns>A list of BrainInstance models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when brainInstanceDTOs is null.</exception>
        public static List<BrainInstance> ToBrainInstances(this IEnumerable<BrainInstanceDTO> brainInstanceDTOs)
        {
            if (brainInstanceDTOs == null)
                throw new ArgumentNullException(nameof(brainInstanceDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = brainInstanceDTOs.Select(dto => dto.ToBrainInstance()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToBrainInstances", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a BrainInstance model to prevent unintended mutations.
        /// </summary>
        /// <param name="brainInstance">The BrainInstance model to clone.</param>
        /// <returns>A new BrainInstance instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when brainInstance is null.</exception>
        public static BrainInstance DeepClone(this BrainInstance brainInstance)
        {
            if (brainInstance == null)
                throw new ArgumentNullException(nameof(brainInstance));

            return new BrainInstance
            {
                BrainInstanceName = brainInstance.BrainInstanceName,
                WatchPositions = brainInstance.WatchPositions,
                WatchOrders = brainInstance.WatchOrders,
                ManagedWatchList = brainInstance.ManagedWatchList,
                TargetWatches = brainInstance.TargetWatches,
                BrainLock = brainInstance.BrainLock,
                UsageMin = brainInstance.UsageMin,
                UsageMax = brainInstance.UsageMax,
                CaptureSnapshots = brainInstance.CaptureSnapshots,
                MinimumInterest = brainInstance.MinimumInterest,
                LastSeen = brainInstance.LastSeen
            };
        }

        /// <summary>
        /// Creates a deep clone of a BrainInstanceDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="brainInstanceDTO">The BrainInstanceDTO to clone.</param>
        /// <returns>A new BrainInstanceDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when brainInstanceDTO is null.</exception>
        public static BrainInstanceDTO DeepClone(this BrainInstanceDTO brainInstanceDTO)
        {
            if (brainInstanceDTO == null)
                throw new ArgumentNullException(nameof(brainInstanceDTO));

            return new BrainInstanceDTO
            {
                BrainInstanceName = brainInstanceDTO.BrainInstanceName,
                WatchPositions = brainInstanceDTO.WatchPositions,
                WatchOrders = brainInstanceDTO.WatchOrders,
                ManagedWatchList = brainInstanceDTO.ManagedWatchList,
                TargetWatches = brainInstanceDTO.TargetWatches,
                BrainLock = brainInstanceDTO.BrainLock,
                UsageMin = brainInstanceDTO.UsageMin,
                UsageMax = brainInstanceDTO.UsageMax,
                CaptureSnapshots = brainInstanceDTO.CaptureSnapshots,
                MinimumInterest = brainInstanceDTO.MinimumInterest,
                LastSeen = brainInstanceDTO.LastSeen
            };
        }
    }
}
