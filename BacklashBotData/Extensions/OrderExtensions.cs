using KalshiBotData.Models;
using BacklashDTOs.Data;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Order model and OrderDTO,
    /// supporting comprehensive order management and trading operation data transfer.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class OrderExtensions
    {
        private static readonly ConcurrentDictionary<string, List<TimeSpan>> _performanceMetrics = new();

        /// <summary>
        /// Gets the performance metrics for transformation operations
        /// </summary>
        public static IReadOnlyDictionary<string, List<TimeSpan>> GetPerformanceMetrics()
        {
            return _performanceMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        /// <summary>
        /// Converts an Order model to its DTO representation,
        /// mapping all order properties including trading details and execution counts.
        /// </summary>
        /// <param name="order">The Order model to convert.</param>
        /// <returns>A new OrderDTO with all order properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when order is null.</exception>
        public static OrderDTO ToOrderDTO(this Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var stopwatch = Stopwatch.StartNew();

            var result = new OrderDTO
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToOrderDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts an OrderDTO to its model representation,
        /// creating a new Order with all properties mapped from the DTO.
        /// </summary>
        /// <param name="orderDTO">The OrderDTO to convert.</param>
        /// <returns>A new Order model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderDTO is null.</exception>
        public static Order ToOrder(this OrderDTO orderDTO)
        {
            if (orderDTO == null)
                throw new ArgumentNullException(nameof(orderDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new Order
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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToOrder", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Order model with data from an OrderDTO,
        /// validating ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="order">The Order model to update.</param>
        /// <param name="orderDTO">The OrderDTO containing updated data.</param>
        /// <returns>The updated Order model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when order or orderDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when order tickers do not match.</exception>
        public static Order UpdateOrder(this Order order, OrderDTO orderDTO)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if (orderDTO == null)
                throw new ArgumentNullException(nameof(orderDTO));

            if (order.Ticker != orderDTO.Ticker)
            {
                throw new ArgumentException("Order Ticker doesn't match for Update Order", nameof(orderDTO));
            }

            var stopwatch = Stopwatch.StartNew();

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

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateOrder", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return order;
        }

        /// <summary>
        /// Converts a collection of Order models to their corresponding DTO representations.
        /// </summary>
        /// <param name="orders">The collection of Order models to convert.</param>
        /// <returns>A list of OrderDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orders is null.</exception>
        public static List<OrderDTO> ToOrderDTOs(this IEnumerable<Order> orders)
        {
            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            var stopwatch = Stopwatch.StartNew();

            var result = orders.Select(o => o.ToOrderDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToOrderDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of OrderDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="orderDTOs">The collection of OrderDTOs to convert.</param>
        /// <returns>A list of Order models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderDTOs is null.</exception>
        public static List<Order> ToOrders(this IEnumerable<OrderDTO> orderDTOs)
        {
            if (orderDTOs == null)
                throw new ArgumentNullException(nameof(orderDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = orderDTOs.Select(dto => dto.ToOrder()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToOrders", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of an Order model to prevent unintended mutations.
        /// </summary>
        /// <param name="order">The Order model to clone.</param>
        /// <returns>A new Order instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when order is null.</exception>
        public static Order DeepClone(this Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return new Order
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
        /// Creates a deep clone of an OrderDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="orderDTO">The OrderDTO to clone.</param>
        /// <returns>A new OrderDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderDTO is null.</exception>
        public static OrderDTO DeepClone(this OrderDTO orderDTO)
        {
            if (orderDTO == null)
                throw new ArgumentNullException(nameof(orderDTO));

            return new OrderDTO
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
    }
}
