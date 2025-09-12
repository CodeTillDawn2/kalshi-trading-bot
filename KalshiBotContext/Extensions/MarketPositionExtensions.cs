using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between MarketPosition model and MarketPositionDTO,
    /// supporting trading position data transfer and portfolio management operations.
    /// </summary>
    public static class MarketPositionExtensions
    {
        /// <summary>
        /// Converts a MarketPosition model to its DTO representation,
        /// mapping all position-related properties for data transfer.
        /// </summary>
        /// <param name="marketPosition">The MarketPosition model to convert.</param>
        /// <returns>A new MarketPositionDTO with all position properties mapped.</returns>
        public static MarketPositionDTO ToMarketPositionDTO(this MarketPosition marketPosition)
        {
            return new MarketPositionDTO
            {
                Ticker = marketPosition.Ticker,
                TotalTraded = marketPosition.TotalTraded,
                Position = marketPosition.Position,
                MarketExposure = marketPosition.MarketExposure,
                RealizedPnl = marketPosition.RealizedPnl,
                RestingOrdersCount = marketPosition.RestingOrdersCount,
                FeesPaid = marketPosition.FeesPaid,
                LastUpdatedUTC = marketPosition.LastUpdatedUTC,
                LastModified = marketPosition.LastModified
            };
        }

        /// <summary>
        /// Converts a MarketPositionDTO to its model representation,
        /// creating a new MarketPosition with all properties mapped from the DTO.
        /// </summary>
        /// <param name="marketPositionDTO">The MarketPositionDTO to convert.</param>
        /// <returns>A new MarketPosition model with all properties mapped from the DTO.</returns>
        public static MarketPosition ToMarketPosition(this MarketPositionDTO marketPositionDTO)
        {
            return new MarketPosition
            {
                Ticker = marketPositionDTO.Ticker,
                TotalTraded = marketPositionDTO.TotalTraded,
                Position = marketPositionDTO.Position,
                MarketExposure = marketPositionDTO.MarketExposure,
                RealizedPnl = marketPositionDTO.RealizedPnl,
                RestingOrdersCount = marketPositionDTO.RestingOrdersCount,
                FeesPaid = marketPositionDTO.FeesPaid,
                LastUpdatedUTC = marketPositionDTO.LastUpdatedUTC,
                LastModified = marketPositionDTO.LastModified
            };
        }

        /// <summary>
        /// Updates an existing MarketPosition model with data from a MarketPositionDTO,
        /// validating ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="marketPosition">The MarketPosition model to update.</param>
        /// <param name="marketPositionDTO">The MarketPositionDTO containing updated data.</param>
        /// <returns>The updated MarketPosition model.</returns>
        /// <exception cref="Exception">Thrown when tickers do not match.</exception>
        public static MarketPosition UpdateMarketPosition(this MarketPosition marketPosition, MarketPositionDTO marketPositionDTO)
        {
            if (marketPosition.Ticker != marketPositionDTO.Ticker)
            {
                throw new Exception("Tickers don't match for Update MarketPosition");
            }
            marketPosition.TotalTraded = marketPositionDTO.TotalTraded;
            marketPosition.Position = marketPositionDTO.Position;
            marketPosition.MarketExposure = marketPositionDTO.MarketExposure;
            marketPosition.RealizedPnl = marketPositionDTO.RealizedPnl;
            marketPosition.RestingOrdersCount = marketPositionDTO.RestingOrdersCount;
            marketPosition.FeesPaid = marketPositionDTO.FeesPaid;
            marketPosition.LastUpdatedUTC = marketPositionDTO.LastUpdatedUTC;
            marketPosition.LastModified = DateTime.UtcNow;
            return marketPosition;
        }
    }
}
