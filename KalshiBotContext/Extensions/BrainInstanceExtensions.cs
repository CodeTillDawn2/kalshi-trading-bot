using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between BrainInstance model and BrainInstanceDTO,
    /// facilitating data transfer for brain instance management and monitoring operations.
    /// </summary>
    public static class BrainInstanceExtensions
    {
        /// <summary>
        /// Converts a BrainInstance model to its DTO representation,
        /// mapping all brain instance properties for external data transfer.
        /// </summary>
        /// <param name="brainInstance">The BrainInstance model to convert.</param>
        /// <returns>A new BrainInstanceDTO with all properties mapped from the model.</returns>
        public static BrainInstanceDTO ToBrainInstanceDTO(this BrainInstance brainInstance)
        {
            return new BrainInstanceDTO
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
        /// Converts a BrainInstanceDTO to its model representation,
        /// creating a new BrainInstance with all properties mapped from the DTO.
        /// </summary>
        /// <param name="brainInstanceDTO">The BrainInstanceDTO to convert.</param>
        /// <returns>A new BrainInstance model with all properties mapped from the DTO.</returns>
        public static BrainInstance ToBrainInstance(this BrainInstanceDTO brainInstanceDTO)
        {
            return new BrainInstance
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
        }

        /// <summary>
        /// Updates an existing BrainInstance model with data from a BrainInstanceDTO,
        /// validating the brain instance name match before applying selective property updates.
        /// </summary>
        /// <param name="brainInstance">The BrainInstance model to update.</param>
        /// <param name="brainInstanceDTO">The BrainInstanceDTO containing updated data.</param>
        /// <returns>The updated BrainInstance model.</returns>
        /// <exception cref="Exception">Thrown when brain instance names do not match.</exception>
        public static BrainInstance UpdateBrainInstance(this BrainInstance brainInstance, BrainInstanceDTO brainInstanceDTO)
        {
            if (brainInstance.BrainInstanceName != brainInstanceDTO.BrainInstanceName)
            {
                throw new Exception("Brain instance name doesn't match for Update BrainInstance");
            }
            brainInstance.BrainLock = brainInstanceDTO.BrainLock;
            brainInstance.LastSeen = brainInstanceDTO.LastSeen;
            return brainInstance;
        }
    }
}
