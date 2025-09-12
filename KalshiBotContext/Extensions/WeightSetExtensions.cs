using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between WeightSet model and WeightSetDTO,
    /// supporting trading strategy weight configuration data transfer and management.
    /// </summary>
    public static class WeightSetExtensions
    {
        /// <summary>
        /// Converts a WeightSet model to its DTO representation,
        /// mapping all weight set properties for data transfer.
        /// </summary>
        /// <param name="weightSet">The WeightSet model to convert.</param>
        /// <returns>A new WeightSetDTO with all weight set properties mapped.</returns>
        public static WeightSetDTO ToWeightSetDTO(this WeightSet weightSet)
        {
            return new WeightSetDTO
            {
                WeightSetID = weightSet.WeightSetID,
                StrategyName = weightSet.StrategyName,
                Weights = weightSet.Weights,
                LastRun = weightSet.LastRun
            };
        }

        /// <summary>
        /// Converts a WeightSetDTO to its model representation,
        /// creating a new WeightSet with all properties mapped from the DTO.
        /// </summary>
        /// <param name="weightSetDTO">The WeightSetDTO to convert.</param>
        /// <returns>A new WeightSet model with all properties mapped from the DTO.</returns>
        public static WeightSet ToWeightSet(this WeightSetDTO weightSetDTO)
        {
            return new WeightSet
            {
                WeightSetID = weightSetDTO.WeightSetID,
                StrategyName = weightSetDTO.StrategyName,
                Weights = weightSetDTO.Weights,
                LastRun = weightSetDTO.LastRun
            };
        }

        /// <summary>
        /// Updates an existing WeightSet model with data from a WeightSetDTO,
        /// applying all property changes for strategy configuration updates.
        /// </summary>
        /// <param name="weightSet">The WeightSet model to update.</param>
        /// <param name="weightSetDTO">The WeightSetDTO containing updated data.</param>
        /// <returns>The updated WeightSet model.</returns>
        public static WeightSet UpdateWeightSet(this WeightSet weightSet, WeightSetDTO weightSetDTO)
        {
            weightSet.StrategyName = weightSetDTO.StrategyName;
            weightSet.Weights = weightSetDTO.Weights;
            weightSet.LastRun = weightSetDTO.LastRun;
            return weightSet;
        }
    }
}
