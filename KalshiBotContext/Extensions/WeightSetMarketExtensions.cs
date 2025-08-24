// WeightSetMarketExtensions.cs
using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class WeightSetMarketExtensions
    {
        public static WeightSetMarketDTO ToWeightSetMarketDTO(this WeightSetMarket market)
        {
            return new WeightSetMarketDTO
            {
                WeightSetID = market.WeightSetID,
                MarketTicker = market.MarketTicker,
                PnL = market.PnL,
                LastRun = market.LastRun
            };
        }

        public static WeightSetMarket ToWeightSetMarket(this WeightSetMarketDTO dto)
        {
            return new WeightSetMarket
            {
                WeightSetID = dto.WeightSetID,
                MarketTicker = dto.MarketTicker,
                PnL = dto.PnL,
                LastRun = dto.LastRun
            };
        }

        public static WeightSetMarket UpdateWeightSetMarket(this WeightSetMarket market, WeightSetMarketDTO dto)
        {
            market.PnL = dto.PnL;
            market.LastRun = dto.LastRun;
            return market;
        }
    }
}