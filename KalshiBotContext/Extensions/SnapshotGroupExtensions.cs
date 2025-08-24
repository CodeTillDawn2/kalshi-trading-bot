using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class SnapshotGroupExtensions
    {
        public static SnapshotGroupDTO ToSnapshotGroupDTO(this SnapshotGroup snapshotGroup)
        {
            return new SnapshotGroupDTO
            {
                MarketTicker = snapshotGroup.MarketTicker,
                StartTime = snapshotGroup.StartTime,
                EndTime = snapshotGroup.EndTime,
                YesStart = snapshotGroup.YesStart,
                NoStart = snapshotGroup.NoStart,
                YesEnd = snapshotGroup.YesEnd,
                NoEnd = snapshotGroup.NoEnd,
                AverageLiquidity = snapshotGroup.AverageLiquidity,
                SnapshotSchema = snapshotGroup.SnapshotSchema,
                ProcessedDttm = snapshotGroup.ProcessedDttm,
                JsonPath = snapshotGroup.JsonPath,
                SnapshotGroupID = snapshotGroup.SnapshotGroupID
            };
        }

        public static SnapshotGroup ToSnapshotGroup(this SnapshotGroupDTO snapshotGroupDTO)
        {
            return new SnapshotGroup
            {
                MarketTicker = snapshotGroupDTO.MarketTicker,
                StartTime = snapshotGroupDTO.StartTime,
                EndTime = snapshotGroupDTO.EndTime,
                YesStart = snapshotGroupDTO.YesStart,
                NoStart = snapshotGroupDTO.NoStart,
                YesEnd = snapshotGroupDTO.YesEnd,
                NoEnd = snapshotGroupDTO.NoEnd,
                AverageLiquidity = snapshotGroupDTO.AverageLiquidity,
                ProcessedDttm = snapshotGroupDTO.ProcessedDttm,
                JsonPath = snapshotGroupDTO.JsonPath,
                SnapshotSchema = snapshotGroupDTO.SnapshotSchema,
                SnapshotGroupID = snapshotGroupDTO.SnapshotGroupID
            };
        }

        public static SnapshotGroup UpdateSnapshotGroup(this SnapshotGroup snapshotGroup, SnapshotGroupDTO snapshotGroupDTO)
        {
            if (snapshotGroup.MarketTicker != snapshotGroupDTO.MarketTicker || snapshotGroup.StartTime != snapshotGroupDTO.StartTime)
            {
                throw new Exception("Market ticker or start time don't match for Update SnapshotGroup");
            }
            snapshotGroup.EndTime = snapshotGroupDTO.EndTime;
            snapshotGroup.YesStart = snapshotGroupDTO.YesStart;
            snapshotGroup.NoStart = snapshotGroupDTO.NoStart;
            snapshotGroup.YesEnd = snapshotGroupDTO.YesEnd;
            snapshotGroup.NoEnd = snapshotGroupDTO.NoEnd;
            snapshotGroup.AverageLiquidity = snapshotGroupDTO.AverageLiquidity;
            snapshotGroup.ProcessedDttm = snapshotGroupDTO.ProcessedDttm;
            snapshotGroup.JsonPath = snapshotGroupDTO.JsonPath;
            snapshotGroup.SnapshotSchema = snapshotGroupDTO.SnapshotSchema;
            return snapshotGroup;
        }
    }
}