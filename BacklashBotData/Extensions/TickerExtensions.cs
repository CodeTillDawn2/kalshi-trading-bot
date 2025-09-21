using BacklashDTOs.Data;
using KalshiBotData.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace KalshiBotData.Extensions
{
    /// <summary>
    /// Provides extension methods for converting between Ticker model and TickerDTO,
    /// supporting real-time market ticker data transfer and price information updates.
    /// Includes performance metrics collection and batch transformation capabilities.
    /// </summary>
    public static class TickerExtensions
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
        /// Converts a Ticker model to its DTO representation,
        /// mapping all ticker data including market information, prices, and volume metrics.
        /// </summary>
        /// <param name="ticker">The Ticker model to convert.</param>
        /// <returns>A new TickerDTO with all ticker properties mapped.</returns>
        /// <exception cref="ArgumentNullException">Thrown when ticker is null.</exception>
        public static TickerDTO ToTickerDTO(this Ticker ticker)
        {
            if (ticker == null)
                throw new ArgumentNullException(nameof(ticker));

            var stopwatch = Stopwatch.StartNew();

            var result = new TickerDTO
            {
                market_id = ticker.market_id,
                market_ticker = ticker.market_ticker,
                price = ticker.price,
                yes_bid = ticker.yes_bid,
                yes_ask = ticker.yes_ask,
                volume = ticker.volume,
                open_interest = ticker.open_interest,
                dollar_volume = ticker.dollar_volume,
                dollar_open_interest = ticker.dollar_open_interest,
                ts = ticker.ts,
                LoggedDate = ticker.LoggedDate,
                ProcessedDate = ticker.ProcessedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToTickerDTO", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a TickerDTO to its model representation,
        /// creating a new Ticker with all properties mapped from the DTO.
        /// </summary>
        /// <param name="tickerDTO">The TickerDTO to convert.</param>
        /// <returns>A new Ticker model with all properties mapped from the DTO.</returns>
        /// <exception cref="ArgumentNullException">Thrown when tickerDTO is null.</exception>
        public static Ticker ToTicker(this TickerDTO tickerDTO)
        {
            if (tickerDTO == null)
                throw new ArgumentNullException(nameof(tickerDTO));

            var stopwatch = Stopwatch.StartNew();

            var result = new Ticker
            {
                market_id = tickerDTO.market_id,
                market_ticker = tickerDTO.market_ticker,
                price = tickerDTO.price,
                yes_bid = tickerDTO.yes_bid,
                yes_ask = tickerDTO.yes_ask,
                volume = tickerDTO.volume,
                open_interest = tickerDTO.open_interest,
                dollar_volume = tickerDTO.dollar_volume,
                dollar_open_interest = tickerDTO.dollar_open_interest,
                ts = tickerDTO.ts,
                LoggedDate = tickerDTO.LoggedDate,
                ProcessedDate = tickerDTO.ProcessedDate
            };

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToTicker", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Updates an existing Ticker model with data from a TickerDTO,
        /// validating market ticker and logged date match before updating price and volume data.
        /// </summary>
        /// <param name="ticker">The Ticker model to update.</param>
        /// <param name="tickerDTO">The TickerDTO containing updated data.</param>
        /// <returns>The updated Ticker model.</returns>
        /// <exception cref="ArgumentNullException">Thrown when ticker or tickerDTO is null.</exception>
        /// <exception cref="ArgumentException">Thrown when market tickers or logged dates do not match.</exception>
        public static Ticker UpdateTicker(this Ticker ticker, TickerDTO tickerDTO)
        {
            if (ticker == null)
                throw new ArgumentNullException(nameof(ticker));
            if (tickerDTO == null)
                throw new ArgumentNullException(nameof(tickerDTO));

            if (ticker.market_ticker != tickerDTO.market_ticker || ticker.LoggedDate != tickerDTO.LoggedDate)
            {
                throw new ArgumentException("Market ticker or logged dates don't match for Update Ticker", nameof(tickerDTO));
            }

            var stopwatch = Stopwatch.StartNew();

            ticker.price = tickerDTO.price;
            ticker.yes_bid = tickerDTO.yes_bid;
            ticker.yes_ask = tickerDTO.yes_ask;
            ticker.volume = tickerDTO.volume;
            ticker.open_interest = tickerDTO.open_interest;
            ticker.dollar_volume = tickerDTO.dollar_volume;
            ticker.dollar_open_interest = tickerDTO.dollar_open_interest;
            ticker.ts = tickerDTO.ts;
            ticker.ProcessedDate = tickerDTO.ProcessedDate;

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("UpdateTicker", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return ticker;
        }

        /// <summary>
        /// Converts a collection of Ticker models to their corresponding DTO representations.
        /// </summary>
        /// <param name="tickers">The collection of Ticker models to convert.</param>
        /// <returns>A list of TickerDTOs with all properties mapped from the models.</returns>
        /// <exception cref="ArgumentNullException">Thrown when tickers is null.</exception>
        public static List<TickerDTO> ToTickerDTOs(this IEnumerable<Ticker> tickers)
        {
            if (tickers == null)
                throw new ArgumentNullException(nameof(tickers));

            var stopwatch = Stopwatch.StartNew();

            var result = tickers.Select(t => t.ToTickerDTO()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToTickerDTOs", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Converts a collection of TickerDTOs to their corresponding model representations.
        /// </summary>
        /// <param name="tickerDTOs">The collection of TickerDTOs to convert.</param>
        /// <returns>A list of Ticker models with all properties mapped from the DTOs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when tickerDTOs is null.</exception>
        public static List<Ticker> ToTickers(this IEnumerable<TickerDTO> tickerDTOs)
        {
            if (tickerDTOs == null)
                throw new ArgumentNullException(nameof(tickerDTOs));

            var stopwatch = Stopwatch.StartNew();

            var result = tickerDTOs.Select(dto => dto.ToTicker()).ToList();

            stopwatch.Stop();
            _performanceMetrics.GetOrAdd("ToTickers", _ => new List<TimeSpan>()).Add(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a Ticker model to prevent unintended mutations.
        /// </summary>
        /// <param name="ticker">The Ticker model to clone.</param>
        /// <returns>A new Ticker instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when ticker is null.</exception>
        public static Ticker DeepClone(this Ticker ticker)
        {
            if (ticker == null)
                throw new ArgumentNullException(nameof(ticker));

            return new Ticker
            {
                market_id = ticker.market_id,
                market_ticker = ticker.market_ticker,
                price = ticker.price,
                yes_bid = ticker.yes_bid,
                yes_ask = ticker.yes_ask,
                volume = ticker.volume,
                open_interest = ticker.open_interest,
                dollar_volume = ticker.dollar_volume,
                dollar_open_interest = ticker.dollar_open_interest,
                ts = ticker.ts,
                LoggedDate = ticker.LoggedDate,
                ProcessedDate = ticker.ProcessedDate
            };
        }

        /// <summary>
        /// Creates a deep clone of a TickerDTO to prevent unintended mutations.
        /// </summary>
        /// <param name="tickerDTO">The TickerDTO to clone.</param>
        /// <returns>A new TickerDTO instance with all properties copied from the original.</returns>
        /// <exception cref="ArgumentNullException">Thrown when tickerDTO is null.</exception>
        public static TickerDTO DeepClone(this TickerDTO tickerDTO)
        {
            if (tickerDTO == null)
                throw new ArgumentNullException(nameof(tickerDTO));

            return new TickerDTO
            {
                market_id = tickerDTO.market_id,
                market_ticker = tickerDTO.market_ticker,
                price = tickerDTO.price,
                yes_bid = tickerDTO.yes_bid,
                yes_ask = tickerDTO.yes_ask,
                volume = tickerDTO.volume,
                open_interest = tickerDTO.open_interest,
                dollar_volume = tickerDTO.dollar_volume,
                dollar_open_interest = tickerDTO.dollar_open_interest,
                ts = tickerDTO.ts,
                LoggedDate = tickerDTO.LoggedDate,
                ProcessedDate = tickerDTO.ProcessedDate
            };
        }
    }
}
