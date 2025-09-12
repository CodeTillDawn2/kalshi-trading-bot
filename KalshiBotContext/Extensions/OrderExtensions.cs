using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Order model and OrderDTO,
    /// supporting comprehensive order management and trading operation data transfer.
    /// </summary>
    public static class OrderExtensions
    {
        /// <summary>
        /// Converts an Order model to its DTO representation,
        /// mapping all order properties including trading details and execution counts.
        /// </summary>
        /// <param name="order">The Order model to convert.</param>
        /// <returns>A new OrderDTO with all order properties mapped.</returns>
        public static OrderDTO ToOrderDTO(this Order order)
        {
            return new OrderDTO
            {
                OrderId = order.OrderId,
                Ticker = order.Ticker,
                UserId = order.UserId,
                Action = order.Action,
                Side = order.Side,
                Type = order.Type,
                Status = order.Status,
                YesPrice = order.YesPrice,
                NoPrice = order.NoPrice,
                CreatedTimeUTC = order.CreatedTimeUTC,
                LastUpdateTimeUTC = order.LastUpdateTimeUTC,
                ExpirationTimeUTC = order.ExpirationTimeUTC,
                ClientOrderId = order.ClientOrderId,
                PlaceCount = order.PlaceCount,
                DecreaseCount = order.DecreaseCount,
                AmendCount = order.AmendCount,
                AmendTakerFillCount = order.AmendTakerFillCount,
                MakerFillCount = order.MakerFillCount,
                TakerFillCount = order.TakerFillCount,
                RemainingCount = order.RemainingCount,
                QueuePosition = order.QueuePosition,
                MakerFillCost = order.MakerFillCost,
                TakerFillCost = order.TakerFillCost,
                MakerFees = order.MakerFees,
                TakerFees = order.TakerFees,
                FccCancelCount = order.FccCancelCount,
                CloseCancelCount = order.CloseCancelCount,
                TakerSelfTradeCancelCount = order.TakerSelfTradeCancelCount,
                LastModified = order.LastModified
            };
        }

        /// <summary>
        /// Converts an OrderDTO to its model representation,
        /// creating a new Order with all properties mapped from the DTO.
        /// </summary>
        /// <param name="orderDTO">The OrderDTO to convert.</param>
        /// <returns>A new Order model with all properties mapped from the DTO.</returns>
        public static Order ToOrder(this OrderDTO orderDTO)
        {
            return new Order
            {
                OrderId = orderDTO.OrderId,
                Ticker = orderDTO.Ticker,
                UserId = orderDTO.UserId,
                Action = orderDTO.Action,
                Side = orderDTO.Side,
                Type = orderDTO.Type,
                Status = orderDTO.Status,
                YesPrice = orderDTO.YesPrice,
                NoPrice = orderDTO.NoPrice,
                CreatedTimeUTC = orderDTO.CreatedTimeUTC,
                LastUpdateTimeUTC = orderDTO.LastUpdateTimeUTC,
                ExpirationTimeUTC = orderDTO.ExpirationTimeUTC,
                ClientOrderId = orderDTO.ClientOrderId,
                PlaceCount = orderDTO.PlaceCount,
                DecreaseCount = orderDTO.DecreaseCount,
                AmendCount = orderDTO.AmendCount,
                AmendTakerFillCount = orderDTO.AmendTakerFillCount,
                MakerFillCount = orderDTO.MakerFillCount,
                TakerFillCount = orderDTO.TakerFillCount,
                RemainingCount = orderDTO.RemainingCount,
                QueuePosition = orderDTO.QueuePosition,
                MakerFillCost = orderDTO.MakerFillCost,
                TakerFillCost = orderDTO.TakerFillCost,
                MakerFees = orderDTO.MakerFees,
                TakerFees = orderDTO.TakerFees,
                FccCancelCount = orderDTO.FccCancelCount,
                CloseCancelCount = orderDTO.CloseCancelCount,
                TakerSelfTradeCancelCount = orderDTO.TakerSelfTradeCancelCount,
                LastModified = orderDTO.LastModified
            };
        }

        /// <summary>
        /// Updates an existing Order model with data from an OrderDTO,
        /// validating ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="order">The Order model to update.</param>
        /// <param name="orderDTO">The OrderDTO containing updated data.</param>
        /// <returns>The updated Order model.</returns>
        /// <exception cref="Exception">Thrown when order tickers do not match.</exception>
        public static Order UpdateOrder(this Order order, OrderDTO orderDTO)
        {
            if (order.Ticker != orderDTO.Ticker)
            {
                throw new Exception("Order Ticker doesn't match for Update Order");
            }
            order.UserId = orderDTO.UserId;
            order.Action = orderDTO.Action;
            order.Side = orderDTO.Side;
            order.Type = orderDTO.Type;
            order.Status = orderDTO.Status;
            order.YesPrice = orderDTO.YesPrice;
            order.NoPrice = orderDTO.NoPrice;
            order.LastUpdateTimeUTC = orderDTO.LastUpdateTimeUTC;
            order.ExpirationTimeUTC = orderDTO.ExpirationTimeUTC;
            order.ClientOrderId = orderDTO.ClientOrderId;
            order.PlaceCount = orderDTO.PlaceCount;
            order.DecreaseCount = orderDTO.DecreaseCount;
            order.AmendCount = orderDTO.AmendCount;
            order.AmendTakerFillCount = orderDTO.AmendTakerFillCount;
            order.MakerFillCount = orderDTO.MakerFillCount;
            order.TakerFillCount = orderDTO.TakerFillCount;
            order.RemainingCount = orderDTO.RemainingCount;
            order.QueuePosition = orderDTO.QueuePosition;
            order.MakerFillCost = orderDTO.MakerFillCost;
            order.TakerFillCost = orderDTO.TakerFillCost;
            order.MakerFees = orderDTO.MakerFees;
            order.TakerFees = orderDTO.TakerFees;
            order.FccCancelCount = orderDTO.FccCancelCount;
            order.CloseCancelCount = orderDTO.CloseCancelCount;
            order.TakerSelfTradeCancelCount = orderDTO.TakerSelfTradeCancelCount;
            order.LastModified = DateTime.UtcNow;
            return order;
        }
    }
}
