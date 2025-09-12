using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between SnapshotGroup model and SnapshotGroupDTO,
    /// supporting grouped snapshot data transfer for market analysis over time periods.
    /// </summary>
    public static class SnapshotGroupExtensions
    {
        /// <summary>
        /// Converts a SnapshotGroup model to its DTO representation,
        /// mapping all snapshot group properties including time ranges and price data.
        /// </summary>
        /// <param name="snapshotGroup">The SnapshotGroup model to convert.</param>
        /// <returns>A new SnapshotGroupDTO with all snapshot group properties mapped.</returns>
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

        /// <summary>
        /// Converts a SnapshotGroupDTO to its model representation,
        /// creating a new SnapshotGroup with all properties mapped from the DTO.
        /// </summary>
        /// <param name="snapshotGroupDTO">The SnapshotGroupDTO to convert.</param>
        /// <returns>A new SnapshotGroup model with all properties mapped from the DTO.</returns>
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

        /// <summary>
        /// Updates an existing SnapshotGroup model with data from a SnapshotGroupDTO,
        /// validating market ticker and start time match before applying changes.
        /// </summary>
        /// <param name="snapshotGroup">The SnapshotGroup model to update.</param>
        /// <param name="snapshotGroupDTO">The SnapshotGroupDTO containing updated data.</param>
        /// <returns>The updated SnapshotGroup model.</returns>
        /// <exception cref="Exception">Thrown when market tickers or start times do not match.</exception>
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
