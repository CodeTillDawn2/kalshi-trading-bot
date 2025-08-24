using KalshiBotData.Models;
using SmokehouseDTOs.Data;

namespace KalshiBotData.Extensions
{
    public static class MarketPositionExtensions
    {
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
            marketPosition.LastModified = DateTime.Now;
            return marketPosition;
        }
    }
}