// WeightSetExtensions.cs
using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class WeightSetExtensions
    {
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

        public static WeightSet ToWeightSet(this WeightSetDTO dto)
        {
            return new WeightSet
            {
                WeightSetID = dto.WeightSetID,
                StrategyName = dto.StrategyName,
                Weights = dto.Weights,
                LastRun = dto.LastRun
            };
        }

        public static WeightSet UpdateWeightSet(this WeightSet weightSet, WeightSetDTO dto)
        {
            weightSet.StrategyName = dto.StrategyName;
            weightSet.Weights = dto.Weights;
            weightSet.LastRun = dto.LastRun;
            return weightSet;
        }
    }
}