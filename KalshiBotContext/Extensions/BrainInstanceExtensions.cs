using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class BrainInstanceExtensions
    {
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

        public static BrainInstance UpdateBrainInstance(this BrainInstance brainInstance, BrainInstanceDTO brainInstanceDTO)
        {
            if (brainInstance.BrainInstanceName != brainInstanceDTO.BrainInstanceName)
            {
                throw new Exception("Brain instance name doesn't match for Update BrainInstance");
            }
            brainInstance.BrainLock = brainInstanceDTO.BrainLock;
            brainInstance.WatchPositions = brainInstanceDTO.WatchPositions;
            brainInstance.WatchOrders = brainInstanceDTO.WatchOrders;
            brainInstance.ManagedWatchList = brainInstanceDTO.ManagedWatchList;
            brainInstance.TargetWatches = brainInstanceDTO.TargetWatches;
            brainInstance.LastSeen = brainInstanceDTO.LastSeen;
            brainInstance.CaptureSnapshots = brainInstanceDTO.CaptureSnapshots;
            brainInstance.MinimumInterest = brainInstanceDTO.MinimumInterest;
            brainInstance.UsageMin = brainInstanceDTO.UsageMin;
            brainInstance.UsageMax = brainInstanceDTO.UsageMax;
            return brainInstance;
        }
    }
}