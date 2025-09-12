using KalshiBotData.Models;
using BacklashDTOs.Data;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Market model and MarketDTO,
    /// supporting comprehensive market data transfer including trading information and associated events.
    /// </summary>
    public static class MarketExtensions
    {
        /// <summary>
        /// Converts a Market model to its DTO representation,
        /// including optional associated event data if the market has a linked event.
        /// </summary>
        /// <param name="market">The Market model to convert.</param>
        /// <returns>A new MarketDTO with all market properties and associated event data mapped.</returns>
        public static MarketDTO ToMarketDTO(this Market market)
        {
            EventDTO? associatedEvent = null;
            if (market.Event != null)
                associatedEvent = market.Event.ToEventDTO();

            return new MarketDTO
            {
                market_ticker = market.market_ticker,
                event_ticker = market.event_ticker,
                market_type = market.market_type,
                title = market.title,
                subtitle = market.subtitle,
                yes_sub_title = market.yes_sub_title,
                no_sub_title = market.no_sub_title,
                open_time = market.open_time,
                close_time = market.close_time,
                expected_expiration_time = market.expected_expiration_time,
                expiration_time = market.expiration_time,
                latest_expiration_time = market.latest_expiration_time,
                settlement_timer_seconds = market.settlement_timer_seconds,
                status = market.status,
                response_price_units = market.response_price_units,
                notional_value = market.notional_value,
                tick_size = market.tick_size,
                yes_bid = market.yes_bid,
                yes_ask = market.yes_ask,
                no_bid = market.no_bid,
                no_ask = market.no_ask,
                last_price = market.last_price,
                previous_yes_bid = market.previous_yes_bid,
                previous_yes_ask = market.previous_yes_ask,
                previous_price = market.previous_price,
                volume = market.volume,
                volume_24h = market.volume_24h,
                liquidity = market.liquidity,
                open_interest = market.open_interest,
                result = market.result,
                can_close_early = market.can_close_early,
                expiration_value = market.expiration_value,
                category = market.category,
                risk_limit_cents = market.risk_limit_cents,
                strike_type = market.strike_type,
                floor_strike = market.floor_strike,
                rules_primary = market.rules_primary,
                rules_secondary = market.rules_secondary,
                CreatedDate = market.CreatedDate,
                LastModifiedDate = market.LastModifiedDate,
                LastCandlestickUTC = market.LastCandlestickUTC,
                APILastFetchedDate = market.APILastFetchedDate,
                Event = associatedEvent
            };
        }

        /// <summary>
        /// Converts a MarketDTO to its model representation,
        /// creating a new Market with all properties mapped from the DTO.
        /// </summary>
        /// <param name="marketDTO">The MarketDTO to convert.</param>
        /// <returns>A new Market model with all properties mapped from the DTO.</returns>
        public static Market ToMarket(this MarketDTO marketDTO)
        {
            return new Market
            {
                market_ticker = marketDTO.market_ticker,
                event_ticker = marketDTO.event_ticker,
                market_type = marketDTO.market_type,
                title = marketDTO.title,
                subtitle = marketDTO.subtitle,
                yes_sub_title = marketDTO.yes_sub_title,
                no_sub_title = marketDTO.no_sub_title,
                open_time = marketDTO.open_time,
                close_time = marketDTO.close_time,
                expected_expiration_time = marketDTO.expected_expiration_time,
                expiration_time = marketDTO.expiration_time,
                latest_expiration_time = marketDTO.latest_expiration_time,
                settlement_timer_seconds = marketDTO.settlement_timer_seconds,
                status = marketDTO.status,
                response_price_units = marketDTO.response_price_units,
                notional_value = marketDTO.notional_value,
                tick_size = marketDTO.tick_size,
                yes_bid = marketDTO.yes_bid,
                yes_ask = marketDTO.yes_ask,
                no_bid = marketDTO.no_bid,
                no_ask = marketDTO.no_ask,
                last_price = marketDTO.last_price,
                previous_yes_bid = marketDTO.previous_yes_bid,
                previous_yes_ask = marketDTO.previous_yes_ask,
                previous_price = marketDTO.previous_price,
                volume = marketDTO.volume,
                volume_24h = marketDTO.volume_24h,
                liquidity = marketDTO.liquidity,
                open_interest = marketDTO.open_interest,
                result = marketDTO.result,
                can_close_early = marketDTO.can_close_early,
                expiration_value = marketDTO.expiration_value,
                category = marketDTO.category,
                risk_limit_cents = marketDTO.risk_limit_cents,
                strike_type = marketDTO.strike_type,
                floor_strike = marketDTO.floor_strike,
                rules_primary = marketDTO.rules_primary,
                rules_secondary = marketDTO.rules_secondary,
                CreatedDate = marketDTO.CreatedDate,
                LastModifiedDate = marketDTO.LastModifiedDate,
                LastCandlestickUTC = marketDTO.LastCandlestickUTC,
                APILastFetchedDate = marketDTO.APILastFetchedDate
            };
        }

        /// <summary>
        /// Updates an existing Market model with data from a MarketDTO,
        /// validating market ticker match before applying changes and updating the modification timestamp.
        /// </summary>
        /// <param name="market">The Market model to update.</param>
        /// <param name="marketDTO">The MarketDTO containing updated data.</param>
        /// <returns>The updated Market model.</returns>
        /// <exception cref="Exception">Thrown when market tickers do not match.</exception>
        public static Market UpdateMarket(this Market market, MarketDTO marketDTO)
        {
            if (market.market_ticker != marketDTO.market_ticker)
            {
                throw new Exception("Market tickers don't match for Update Market");
            }
            market.event_ticker = marketDTO.event_ticker;
            market.market_type = marketDTO.market_type;
            market.title = marketDTO.title;
            market.subtitle = marketDTO.subtitle;
            market.yes_sub_title = marketDTO.yes_sub_title;
            market.no_sub_title = marketDTO.no_sub_title;
            market.open_time = marketDTO.open_time;
            market.close_time = marketDTO.close_time;
            market.expected_expiration_time = marketDTO.expected_expiration_time;
            market.expiration_time = marketDTO.expiration_time;
            market.latest_expiration_time = marketDTO.latest_expiration_time;
            market.settlement_timer_seconds = marketDTO.settlement_timer_seconds;
            market.status = marketDTO.status;
            market.response_price_units = marketDTO.response_price_units;
            market.notional_value = marketDTO.notional_value;
            market.tick_size = marketDTO.tick_size;
            market.yes_bid = marketDTO.yes_bid;
            market.yes_ask = marketDTO.yes_ask;
            market.no_bid = marketDTO.no_bid;
            market.no_ask = marketDTO.no_ask;
            market.last_price = marketDTO.last_price;
            market.previous_yes_bid = marketDTO.previous_yes_bid;
            market.previous_yes_ask = marketDTO.previous_yes_ask;
            market.previous_price = marketDTO.previous_price;
            market.volume = marketDTO.volume;
            market.volume_24h = marketDTO.volume_24h;
            market.liquidity = marketDTO.liquidity;
            market.open_interest = marketDTO.open_interest;
            market.result = marketDTO.result;
            market.can_close_early = marketDTO.can_close_early;
            market.expiration_value = marketDTO.expiration_value;
            if (!string.IsNullOrEmpty(marketDTO.category)) market.category = marketDTO.category;
            market.risk_limit_cents = marketDTO.risk_limit_cents;
            market.strike_type = marketDTO.strike_type;
            market.floor_strike = marketDTO.floor_strike;
            market.rules_primary = marketDTO.rules_primary;
            market.rules_secondary = marketDTO.rules_secondary;
            market.LastModifiedDate = DateTime.UtcNow;
            market.LastCandlestickUTC = marketDTO.LastCandlestickUTC;
            market.APILastFetchedDate = marketDTO.APILastFetchedDate;
            return market;
        }
    }
}
