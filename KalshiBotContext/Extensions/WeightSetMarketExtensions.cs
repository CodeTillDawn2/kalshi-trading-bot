using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between WeightSetMarket model and WeightSetMarketDTO,
    /// supporting market-specific performance tracking within weight set configurations.
    /// </summary>
    public static class WeightSetMarketExtensions
    {
        /// <summary>
        /// Converts a WeightSetMarket model to its DTO representation,
        /// mapping all market-specific weight set properties for data transfer.
        /// </summary>
        /// <param name="market">The WeightSetMarket model to convert.</param>
        /// <returns>A new WeightSetMarketDTO with all market weight set properties mapped.</returns>
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

        /// <summary>
        /// Converts a WeightSetMarketDTO to its model representation,
        /// creating a new WeightSetMarket with all properties mapped from the DTO.
        /// </summary>
        /// <param name="weightSetMarketDTO">The WeightSetMarketDTO to convert.</param>
        /// <returns>A new WeightSetMarket model with all properties mapped from the DTO.</returns>
        public static WeightSetMarket ToWeightSetMarket(this WeightSetMarketDTO weightSetMarketDTO)
        {
            return new WeightSetMarket
            {
                WeightSetID = weightSetMarketDTO.WeightSetID,
                MarketTicker = weightSetMarketDTO.MarketTicker,
                PnL = weightSetMarketDTO.PnL,
                LastRun = weightSetMarketDTO.LastRun
            };
        }

        /// <summary>
        /// Updates an existing WeightSetMarket model with data from a WeightSetMarketDTO,
        /// applying performance and timing updates for market tracking.
        /// </summary>
        /// <param name="market">The WeightSetMarket model to update.</param>
        /// <param name="weightSetMarketDTO">The WeightSetMarketDTO containing updated data.</param>
        /// <returns>The updated WeightSetMarket model.</returns>
        public static WeightSetMarket UpdateWeightSetMarket(this WeightSetMarket market, WeightSetMarketDTO weightSetMarketDTO)
        {
            market.PnL = weightSetMarketDTO.PnL;
            market.LastRun = weightSetMarketDTO.LastRun;
            return market;
        }
    }
}
