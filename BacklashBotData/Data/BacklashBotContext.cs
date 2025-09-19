using BacklashBotData.Data.Interfaces;
using KalshiBotData.Extensions;
using KalshiBotData.Models;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using System.Data;
using BacklashBotData.Configuration;
using BacklashDTOs.Configuration;

namespace BacklashBotData.Data
{
    /// <summary>
    /// Entity Framework DbContext implementation for the Kalshi trading bot system.
    /// Provides comprehensive data access layer for managing trading entities including markets,
    /// events, series, snapshots, brain instances, and various trading-related data.
    /// Implements IKalshiBotContext interface for dependency injection and testing.
    /// </summary>
    /// <remarks>
    /// This context manages the complete lifecycle of trading data, including:
    /// - Market and event data synchronization
    /// - Trading snapshots and historical data
    /// - Brain instance management and coordination
    /// - Order and position tracking
    /// - SignalR client management
    /// - Logging and audit trails
    /// - Weight set and strategy management
    /// </remarks>
    public class BacklashBotContext : DbContext, IBacklashBotContext, IKalshiBotContextPerformanceMetrics
    {
        private DbSet<Event> Events { get; set; }
        private DbSet<Series> Series { get; set; }
        private DbSet<SeriesTag> SeriesTags { get; set; }
        private DbSet<SeriesSettlementSource> SeriesSettlementSources { get; set; }
        private DbSet<Market> Markets { get; set; }
        private DbSet<Fill> Fills { get; set; }
        private DbSet<EventLifecycle> EventLifecycles { get; set; }
        private DbSet<Ticker> Tickers { get; set; }
        private DbSet<Trade> Trades { get; set; }
        private DbSet<Candlestick> Candlesticks { get; set; }
        private DbSet<MarketWatch> MarketWatches { get; set; }
        private DbSet<MarketPosition> MarketPositions { get; set; }
        private DbSet<Order> Orders { get; set; }
        private DbSet<LogEntry> LogEntries { get; set; }
        private DbSet<OverseerLogEntry> OverseerLogEntries { get; set; }
        private DbSet<Snapshot> Snapshots { get; set; }
        private DbSet<SnapshotSchema> SnapshotSchemas { get; set; }
        private DbSet<BrainInstance> BrainInstances { get; set; }
        private DbSet<BrainPersistenceEntity> BrainPersistenceEntities { get; set; }
        private DbSet<StratData> StratData { get; set; }
        private DbSet<MarketType> MarketTypes { get; set; }
        private DbSet<SnapshotGroup> SnapshotGroups { get; set; }
        private DbSet<WeightSet> WeightSets { get; set; }
        private DbSet<WeightSetMarket> WeightSetMarkets { get; set; }
        private DbSet<Announcement> Announcements { get; set; }
        private DbSet<ExchangeSchedule> ExchangeSchedules { get; set; }
        private DbSet<MaintenanceWindow> MaintenanceWindows { get; set; }
        private DbSet<StandardHours> StandardHours { get; set; }
        private DbSet<StandardHoursSession> StandardHoursSessions { get; set; }
        private DbSet<BacklashDTOs.SignalRClient> SignalRClients { get; set; }
        private DbSet<BacklashDTOs.Data.OverseerInfo> OverseerInfos { get; set; }

        private readonly string _connectionString;
        private readonly ILogger<BacklashBotContext>? _logger;

        // Configuration options
        private readonly int _maxRetryCount;
        private readonly TimeSpan _retryDelay;
        private readonly int _batchSize;
        private readonly Dictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime)> _performanceMetrics;

        /// <summary>
        /// Initializes a new instance of the KalshiBotContext with connection string and optional logging.
        /// </summary>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="logger">Optional logger for context operations. If null, logging is disabled.</param>
        /// <param name="dataConfig">Configuration options for database operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when connectionString or dataConfig is null.</exception>
        public BacklashBotContext(string connectionString, ILogger<BacklashBotContext>? logger, BacklashBotDataConfig dataConfig)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
            _maxRetryCount = dataConfig.MaxRetryCount;
            _retryDelay = TimeSpan.FromSeconds(dataConfig.RetryDelaySeconds);
            _batchSize = dataConfig.BatchSize;
            _performanceMetrics = new Dictionary<string, (int, int, TimeSpan)>();
        }

        #region Series
        /// <summary>
        /// Retrieves a series by its ticker symbol.
        /// </summary>
        /// <param name="seriesTicker">The unique ticker identifier for the series.</param>
        /// <returns>The series data transfer object, or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when seriesTicker is null or empty.</exception>
        public async Task<SeriesDTO?> GetSeriesByTicker(string seriesTicker)
        {
            if (string.IsNullOrWhiteSpace(seriesTicker))
                throw new ArgumentException("Series ticker cannot be null or empty.", nameof(seriesTicker));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                SeriesDTO? seriesDTO = null;
                Series? series = await Series.AsNoTracking().FirstOrDefaultAsync(x => x.series_ticker == seriesTicker);
                if (series != null)
                {
                    seriesDTO = series.ToSeriesDTO();
                }
                TrackPerformanceMetric("GetSeriesByTicker", true, stopwatch.Elapsed);
                return seriesDTO;
            }
            catch (Exception ex)
            {
                TrackPerformanceMetric("GetSeriesByTicker", false, stopwatch.Elapsed);
                _logger?.LogError(ex, "Error retrieving series by ticker {SeriesTicker}", seriesTicker);
                throw;
            }
        }


        /// <summary>
        /// Adds or updates a series with its associated tags and settlement sources.
        /// Performs idempotent operations with case-insensitive deduplication.
        /// </summary>
        /// <param name="dto">The series data transfer object containing series information and related data.</param>
        /// <exception cref="ArgumentNullException">Thrown when dto is null.</exception>
        /// <exception cref="ArgumentException">Thrown when dto.series_ticker is null or empty.</exception>
        /// <exception cref="Exception">Thrown when the operation fails, with details about the failure.</exception>
        /// <remarks>
        /// This method handles:
        /// - Creating new series or updating existing ones
        /// - Managing series tags with case-insensitive deduplication
        /// - Managing settlement sources with upsert operations
        /// - Transaction management with rollback on failure
        /// - Retry logic for transient database errors
        /// </remarks>
        public async Task AddOrUpdateSeries(SeriesDTO dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.series_ticker))
                throw new ArgumentException("Series ticker cannot be null or empty.", nameof(dto.series_ticker));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var tx = await this.Database.BeginTransactionAsync();
            try
            {
                // Load series WITHOUT tracking children to avoid duplicate-tracking issues
                var series = await this.Series.FirstOrDefaultAsync(x => x.series_ticker == dto.series_ticker);

                if (series == null)
                {
                    series = dto.ToSeries();
                    series.Tags = new List<SeriesTag>();
                    series.SettlementSources = new List<SeriesSettlementSource>();
                    await this.Series.AddAsync(series);
                }
                else
                {
                    series.UpdateSeries(dto);
                }

                // --- Tags (idempotent, case-insensitive) ---
                var incomingTags = (dto.Tags ?? new List<SeriesTagDTO>())
                    .GroupBy(t => t.tag, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                var existingTags = await this.SeriesTags
                    .Where(t => t.series_ticker == dto.series_ticker)
                    .ToListAsync();

                var tagsToRemove = existingTags
                    .Where(et => !incomingTags.Any(it => string.Equals(it.tag, et.tag, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (tagsToRemove.Count > 0) this.SeriesTags.RemoveRange(tagsToRemove);

                foreach (var tagDto in incomingTags)
                {
                    var match = existingTags.FirstOrDefault(et => string.Equals(et.tag, tagDto.tag, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        await this.SeriesTags.AddAsync(tagDto.ToSeriesTag());
                    }
                }

                // --- SettlementSources (dedupe + upsert, case-insensitive) ---
                var incomingSources = (dto.SettlementSources ?? new List<SeriesSettlementSourceDTO>())
                    .GroupBy(s => s.name, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                var existingSources = await this.SeriesSettlementSources
                    .Where(s => s.series_ticker == dto.series_ticker)
                    .ToListAsync();

                // Remove exact duplicate rows already in DB (keep one per name)
                var dupExisting = existingSources
                    .GroupBy(s => s.name, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(g => g.Skip(1))
                    .ToList();
                if (dupExisting.Count > 0) this.SeriesSettlementSources.RemoveRange(dupExisting);

                // Remove sources not present anymore
                var sourcesToRemove = existingSources
                    .Where(es => !incomingSources.Any(ins => string.Equals(ins.name, es.name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (sourcesToRemove.Count > 0) this.SeriesSettlementSources.RemoveRange(sourcesToRemove);

                // Add or update remaining
                foreach (var srcDto in incomingSources)
                {
                    var existing = existingSources.FirstOrDefault(es =>
                        string.Equals(es.name, srcDto.name, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        await this.SeriesSettlementSources.AddAsync(srcDto.ToSeriesSettlementSource());
                    }
                    else
                    {
                        existing.url = srcDto.url;
                        this.Entry(existing).State = EntityState.Modified;
                    }
                }

                await SaveChangesWithRetryAsync();
                await tx.CommitAsync();
                TrackPerformanceMetric("AddOrUpdateSeries", true, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TrackPerformanceMetric("AddOrUpdateSeries", false, stopwatch.Elapsed);
                _logger?.LogError(ex, "Error adding or updating series {SeriesTicker}", dto.series_ticker);
                throw new Exception($"Failed to add or update series for ticker {dto.series_ticker}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Events
        /// <summary>
        /// Retrieves an event by its ticker symbol, including associated series information.
        /// </summary>
        /// <param name="eventTicker">The unique ticker identifier for the event.</param>
        /// <returns>The event data transfer object with series information, or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when eventTicker is null or empty.</exception>
        public async Task<EventDTO?> GetEventByTicker(string eventTicker)
        {
            if (string.IsNullOrWhiteSpace(eventTicker))
                throw new ArgumentException("Event ticker cannot be null or empty.", nameof(eventTicker));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var rawEvent = await Events.AsNoTracking().FirstOrDefaultAsync(x => x.event_ticker == eventTicker);
                if (rawEvent == null)
                {
                    TrackPerformanceMetric("GetEventByTicker", true, stopwatch.Elapsed);
                    return null;
                }

                var rawSeries = await Series.AsNoTracking().FirstOrDefaultAsync(x => x.series_ticker == rawEvent.series_ticker);
                if (rawSeries != null)
                {
                    rawEvent.Series = rawSeries;
                }

                TrackPerformanceMetric("GetEventByTicker", true, stopwatch.Elapsed);
                return rawEvent.ToEventDTO();
            }
            catch (Exception ex)
            {
                TrackPerformanceMetric("GetEventByTicker", false, stopwatch.Elapsed);
                _logger?.LogError(ex, "Error retrieving event by ticker {EventTicker}", eventTicker);
                throw;
            }
        }


        public async Task AddOrUpdateEvent(EventDTO dto)
        {
            Event? existingEvent = await Events.FirstOrDefaultAsync(x => x.event_ticker == dto.event_ticker);
            if (existingEvent == null)
            {
                existingEvent = dto.ToEvent();
                Events.Add(existingEvent);
            }
            else
            {
                existingEvent.UpdateEvent(dto);
            }
            try
            {
                await SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    // Duplicate key violation - this is expected when multiple bots are running
                    _logger?.LogWarning(ex, "Duplicate event key {EventTicker} encountered during save, continuing", dto.event_ticker);
                }
                else
                {
                    // Re-throw for other DbUpdateExceptions
                    throw;
                }
            }
        }

        public async Task AddOrUpdateEvents(List<EventDTO> dtos)
        {
            foreach (EventDTO dto in dtos)
            {
                Event? existingEvent = await Events.FirstOrDefaultAsync(x => x.event_ticker == dto.event_ticker);
                if (existingEvent == null)
                {
                    existingEvent = dto.ToEvent();
                    Events.Add(existingEvent);
                }
                else
                {
                    existingEvent.UpdateEvent(dto);
                }
                try
                {
                    await SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                    {
                        // Duplicate key violation - this is expected when multiple bots are running
                        _logger?.LogWarning(ex, "Duplicate event key {EventTicker} encountered during save, continuing", dto.event_ticker);
                    }
                    else
                    {
                        // Re-throw for other DbUpdateExceptions
                        throw;
                    }
                }
            }
        }
        #endregion

        #region Markets
        /// <summary>
        /// Retrieves a market by its ticker symbol, including associated event and series information.
        /// </summary>
        /// <param name="marketTicker">The unique ticker identifier for the market.</param>
        /// <returns>The market data transfer object with event and series information, or null if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when marketTicker is null or empty.</exception>
        public async Task<MarketDTO?> GetMarketByTicker(string marketTicker)
        {
            if (string.IsNullOrWhiteSpace(marketTicker))
                throw new ArgumentException("Market ticker cannot be null or empty.", nameof(marketTicker));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Market? market = await Markets.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.market_ticker == marketTicker);

                if (market == null)
                {
                    TrackPerformanceMetric("GetMarketByTicker", true, stopwatch.Elapsed);
                    return null;
                }

                Event? thisEvent = await Events.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.event_ticker == market.event_ticker);

                if (thisEvent != null)
                {
                    market.Event = thisEvent;
                    Series? series = await Series.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.series_ticker == thisEvent.series_ticker);
                    if (series != null)
                    {
                        market.Event.Series = series;
                    }
                }

                TrackPerformanceMetric("GetMarketByTicker", true, stopwatch.Elapsed);
                return market.ToMarketDTO();
            }
            catch (Exception ex)
            {
                TrackPerformanceMetric("GetMarketByTicker", false, stopwatch.Elapsed);
                _logger?.LogError(ex, "Error retrieving market by ticker {MarketTicker}", marketTicker);
                throw;
            }
        }


        /// <summary>
        /// Retrieves a distinct set of market tickers that have associated snapshots.
        /// </summary>
        /// <returns>HashSet of market ticker strings that have at least one snapshot.</returns>
        /// <remarks>
        /// This method is useful for identifying markets that have historical trading data
        /// available for analysis or backtesting purposes.
        /// </remarks>
        public async Task<HashSet<string>> GetMarketsWithSnapshots()
        {
            var list = await Snapshots.AsNoTracking().Select(x => x.MarketTicker).Distinct().ToListAsync();
            return list.ToHashSet();
        }

        /// <summary>
        /// Retrieves inactive markets that don't have any snapshots.
        /// </summary>
        /// <returns>HashSet of market tickers for inactive markets without snapshot data.</returns>
        /// <remarks>
        /// This method identifies markets that are no longer active and have no historical
        /// snapshot data, which may be candidates for cleanup or special handling.
        /// </remarks>
        public async Task<HashSet<string>> GetInactiveMarketsWithNoSnapshots()
        {
            var list = await Markets.AsNoTracking()
                .GroupJoin(Snapshots,
                    m => m.market_ticker,
                    s => s.MarketTicker,
                    (m, s) => new { Market = m, Snapshots = s })
                .Where(ms => ms.Market.status != KalshiConstants.Status_Active && !ms.Snapshots.Any())
                .Select(ms => ms.Market.market_ticker)
                .ToListAsync();
            return list.ToHashSet();
        }

        public async Task<HashSet<string>> GetInactiveMarketTickersWithoutSnapshots()
        {
            return await GetInactiveMarketsWithNoSnapshots();
        }

        /// <summary>
        /// Retrieves inactive markets that have been processed into snapshot groups.
        /// </summary>
        /// <returns>HashSet of market tickers for inactive markets that have snapshot groups.</returns>
        /// <remarks>
        /// This method identifies markets that are no longer active but have been processed
        /// into snapshot groups for analysis, indicating they have completed their lifecycle.
        /// </remarks>
        public async Task<HashSet<string>> GetProcessedMarkets()
        {
            var list = await Markets.AsNoTracking()
                .GroupJoin(SnapshotGroups,
                    m => m.market_ticker,
                    s => s.MarketTicker,
                    (m, s) => new { Market = m, SnapshotGroups = s })
                .Where(ms => ms.Market.status != KalshiConstants.Status_Active && ms.SnapshotGroups.Any())
                .Select(ms => ms.Market.market_ticker)
                .ToListAsync();
            return list.ToHashSet();
        }

        public async Task AddOrUpdateMarket(MarketDTO dto)
        {
            Market? market = await Markets.FirstOrDefaultAsync(x => x.market_ticker == dto.market_ticker);
            if (market == null)
            {
                market = dto.ToMarket();
                market.CreatedDate = DateTime.Now;
                Markets.Add(market);
            }
            else
            {
                market.UpdateMarket(dto);
            }
            try
            {
                await SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    // Duplicate key violation - this is expected when multiple bots are running
                    _logger?.LogWarning(ex, "Duplicate market key {MarketTicker} encountered during save, continuing", dto.market_ticker);
                }
                else
                {
                    // Re-throw for other DbUpdateExceptions
                    throw;
                }
            }
        }
        public async Task DeleteMarket(string marketTicker)
        {
            Market? market = await Markets.FirstOrDefaultAsync(x => x.market_ticker == marketTicker);
            if (market != null)
                Markets.Remove(market);

            await SaveChangesAsync();
        }
        public async Task AddOrUpdateMarkets(List<MarketDTO> dtos)
        {
            if (dtos == null)
                throw new ArgumentNullException(nameof(dtos));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int successCount = 0;
            int failureCount = 0;

            foreach (MarketDTO dto in dtos)
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.market_ticker))
                {
                    _logger?.LogWarning("Invalid market DTO encountered, skipping");
                    failureCount++;
                    continue;
                }

                Market? market = await Markets.FirstOrDefaultAsync(x => x.market_ticker == dto.market_ticker);
                if (market == null)
                {
                    market = dto.ToMarket();
                    market.CreatedDate = DateTime.UtcNow;
                    Markets.Add(market);
                }
                else
                {
                    market.UpdateMarket(dto);
                }
                try
                {
                    await SaveChangesAsync();
                    successCount++;
                    await Task.Delay(_retryDelay); // Replace Thread.Sleep with Task.Delay
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                    {
                        // Duplicate key violation - this is expected when multiple bots are running
                        _logger?.LogWarning(ex, "Duplicate market key {MarketTicker} encountered during save, continuing", dto.market_ticker);
                        failureCount++;
                    }
                    else
                    {
                        // Re-throw for other DbUpdateExceptions
                        failureCount++;
                        throw;
                    }
                }
                catch (Exception)
                {
                    // For other exceptions, retry once
                    try
                    {
                        await SaveChangesAsync();
                        successCount++;
                        await Task.Delay(_retryDelay);
                    }
                    catch (Exception retryEx)
                    {
                        _logger?.LogError(retryEx, "Failed to save market {MarketTicker} after retry", dto.market_ticker);
                        failureCount++;
                    }
                }
            }

            TrackPerformanceMetric("AddOrUpdateMarkets", successCount > failureCount, stopwatch.Elapsed);
            _logger?.LogInformation("AddOrUpdateMarkets completed: {SuccessCount} successes, {FailureCount} failures",
                successCount, failureCount);
        }

        /// <summary>
        /// Retrieves a filtered list of markets based on various criteria.
        /// </summary>
        /// <param name="includedStatuses">Set of market statuses to include. If specified, only markets with these statuses are returned.</param>
        /// <param name="excludedStatuses">Set of market statuses to exclude. If specified, markets with these statuses are filtered out.</param>
        /// <param name="includedMarkets">Set of specific market tickers to include. If specified, only these markets are returned.</param>
        /// <param name="excludedMarkets">Set of specific market tickers to exclude. If specified, these markets are filtered out.</param>
        /// <param name="hasMarketWatch">Filter for markets that have (true) or don't have (false) market watch data.</param>
        /// <param name="minimumInterestScore">Minimum interest score threshold for markets with market watch data.</param>
        /// <param name="maxInterestScoreDate">Maximum interest score date for filtering market watch data.</param>
        /// <param name="maxAPILastFetchTime">Maximum API last fetch time for filtering recently updated markets.</param>
        /// <returns>List of market data transfer objects matching the specified criteria.</returns>
        /// <remarks>
        /// This method supports complex filtering combinations and is optimized for performance
        /// with efficient database queries that combine multiple filter conditions.
        /// </remarks>
        public async Task<List<MarketDTO>> GetMarkets(
            HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null,
            bool? hasMarketWatch = null, double? minimumInterestScore = null,
            DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null)
        {
            IQueryable<Market> query = Markets.AsNoTracking();
            if (includedMarkets != null && includedMarkets.Count > 0)
                query = query.Where(x => includedMarkets.Contains(x.market_ticker));
            if (excludedMarkets != null && excludedMarkets.Count > 0)
                query = query.Where(x => !excludedMarkets.Contains(x.market_ticker));
            if (includedStatuses != null && includedStatuses.Count > 0)
                query = query.Where(x => includedStatuses.Contains(x.status));
            if (excludedStatuses != null && excludedStatuses.Count > 0)
                query = query.Where(x => !excludedStatuses.Contains(x.status));
            if (maxAPILastFetchTime != null)
                query = query.Where(x => x.APILastFetchedDate <= maxAPILastFetchTime.Value);
            if (hasMarketWatch != null)
            {
                if (hasMarketWatch.Value && minimumInterestScore == null)
                    query = query.Where(x => x.MarketWatch != null);
                else if (!hasMarketWatch.Value)
                    query = query.Where(x => x.MarketWatch == null);
            }
            if (minimumInterestScore != null)
                query = query.Where(x => x.MarketWatch != null && x.MarketWatch.InterestScore >= minimumInterestScore.Value);
            if (maxInterestScoreDate != null)
                query = query.Where(x => x.MarketWatch == null || x.MarketWatch.InterestScoreDate <= maxInterestScoreDate.Value);
            return await query.Select(x => x.ToMarketDTO()).ToListAsync();
        }


        public async Task UpdateMarketLastCandlestick(string marketTicker)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(
                "UPDATE [dbo].[t_Markets] WITH (ROWLOCK) SET LastCandlestickUTC = ("
                + " SELECT MAX(end_period_datetime_utc)"
                + " FROM t_Candlesticks c"
                + " WHERE c.market_ticker = m.market_ticker AND c.interval_type = 1"
                + ") FROM t_Markets m "
                + " INNER JOIN t_Candlesticks c ON m.market_ticker = c.market_ticker "
                + " WHERE m.market_ticker = @marketTicker",
                conn);
            cmd.Parameters.AddWithValue("@marketTicker", marketTicker);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Checks the existence status of a market and its hierarchical relationships (event and series).
        /// </summary>
        /// <param name="marketTicker">The market ticker to check.</param>
        /// <returns>A tuple indicating whether the market, its event, and series were found in the database.</returns>
        /// <remarks>
        /// This method performs an efficient single query to check the complete hierarchy
        /// using RIGHT OUTER JOINs to ensure all relationships are verified in one database call.
        /// </remarks>
        public async Task<(bool MarketFound, bool EventFound, bool SeriesFound)> GetMarketStatus(string marketTicker)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(
                "SELECT " +
                "    CASE WHEN m.market_ticker IS NOT NULL THEN CAST(1 as bit) ELSE CAST(0 as bit) END AS MarketFound, " +
                "    CASE WHEN e.event_ticker IS NOT NULL THEN CAST(1 as bit) ELSE CAST(0 as bit) END AS EventFound, " +
                "    CASE WHEN s.series_ticker IS NOT NULL THEN CAST(1 as bit) ELSE CAST(0 as bit) END AS SeriesFound " +
                "FROM dbo.t_Series s " +
                "RIGHT OUTER JOIN dbo.t_Events e " +
                "    ON s.series_ticker = e.series_ticker " +
                "RIGHT OUTER JOIN dbo.t_Markets m " +
                "    ON e.event_ticker = m.event_ticker " +
                "WHERE m.market_ticker = @marketTicker",
                conn);
            cmd.Parameters.AddWithValue("@marketTicker", marketTicker);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (
                    reader.GetBoolean(reader.GetOrdinal("MarketFound")),
                    reader.GetBoolean(reader.GetOrdinal("EventFound")),
                    reader.GetBoolean(reader.GetOrdinal("SeriesFound"))
                );
            }
            return (false, false, false);
        }

        #endregion

        #region Tickers
        public async Task<List<TickerDTO>> GetTickers(string? marketTicker = null, DateTime? loggedDate = null)
        {
            IQueryable<Ticker> query = Tickers.AsNoTracking();
            if (marketTicker != null)
                query = query.Where(x => x.market_ticker == marketTicker);
            if (loggedDate != null)
                query = query.Where(x => x.LoggedDate == loggedDate);
            return await query.Select(x => x.ToTickerDTO()).ToListAsync();
        }


        public async Task AddOrUpdateTickers(List<TickerDTO> dtos)
        {
            var tickerKeys = dtos.Select(dto => new { dto.market_ticker, dto.LoggedDate }).ToList();
            var tickers = await Tickers
                .ToListAsync();
            var tickerDict = tickers
                .Where(t => tickerKeys.Any(k => k.market_ticker == t.market_ticker && k.LoggedDate == t.LoggedDate))
                .ToDictionary(t => (t.market_ticker, t.LoggedDate), t => t);

            foreach (var dto in dtos)
            {
                var key = (dto.market_ticker, dto.LoggedDate);
                if (!tickerDict.TryGetValue(key, out var ticker))
                {
                    Tickers.Add(dto.ToTicker());
                }
                else
                {
                    ticker.UpdateTicker(dto);
                }
            }
            await SaveChangesAsync();
        }

        public async Task AddOrUpdateTicker(TickerDTO dto)
        {
            Ticker? ticker = await Tickers.FirstOrDefaultAsync(x => x.market_ticker == dto.market_ticker && x.LoggedDate == dto.LoggedDate);
            if (ticker == null)
            {
                ticker = dto.ToTicker();
                Tickers.Add(ticker);
            }
            else
            {
                ticker.UpdateTicker(dto);
            }
            await SaveChangesAsync();
        }
        #endregion

        #region Candlesticks
        public async Task<List<CandlestickDTO>> GetCandlesticks(string marketTicker, int? intervalType = null)
        {
            IQueryable<Candlestick> query = Candlesticks.AsNoTracking().Where(x => x.market_ticker == marketTicker);
            if (intervalType != null)
                query = query.Where(x => x.interval_type == intervalType);
            return await query.Select(x => x.ToCandlestickDTO()).ToListAsync();
        }


        public async Task<CandlestickDTO?> GetLastCandlestick(string marketTicker, int? intervalType)
        {
            IQueryable<Candlestick> query = Candlesticks.AsNoTracking().Where(x => x.market_ticker == marketTicker);
            if (intervalType != null)
                query = query.Where(x => x.interval_type == intervalType);
            Candlestick? candlestick = await query.OrderByDescending(x => x.end_period_datetime_utc).FirstOrDefaultAsync();
            return candlestick?.ToCandlestickDTO();
        }

        public async Task<CandlestickDTO?> GetLatestCandlestick(string marketTicker, int? intervalType)
        {
            return await GetLastCandlestick(marketTicker, intervalType);
        }

        public async Task<MarketWatchDTO?> GetMarketWatchByTicker(string marketTicker)
        {
            HashSet<MarketWatchDTO> results = await GetMarketWatches(new HashSet<string> { marketTicker });
            if (results.Count > 1) throw new Exception($"Multiple market watches found for ticker {marketTicker}, in GetMarketWatchByTicker");
            return results.FirstOrDefault();
        }

        public async Task<List<MarketDTO>> GetMarketsFiltered(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null,
            bool? hasMarketWatch = null, double? minimumInterestScore = null,
            DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null)
        {
            return await GetMarkets(includedStatuses, excludedStatuses, includedMarkets, excludedMarkets, hasMarketWatch, minimumInterestScore, maxInterestScoreDate, maxAPILastFetchTime);
        }

        public async Task<HashSet<MarketWatchDTO>> GetMarketWatchesFiltered(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null)
        {
            return await GetMarketWatches(marketTickers, brainLocksIncluded, brainLocksExcluded, brainLockIsNull, minInterestScore, maxInterestScore, maxInterestScoreDate);
        }

        public async Task<List<SnapshotGroupDTO>> GetSnapshotGroupsFiltered(List<string>? marketTickersToInclude = null, int? maxGroups = null)
        {
            return await GetSnapshotGroups(marketTickersToInclude, maxGroups);
        }

        public async Task<List<SnapshotDTO>> GetSnapshotsFiltered(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null)
        {
            return await GetSnapshots(marketTicker, isValidated, startDate, endDate, MaxRecords, MaxSnapshotVersion);
        }

        public async Task<List<MarketWatchDTO>> GetFinalizedMarketWatchesByBrainLock(Guid brainLock)
        {
            return await GetFinalizedMarketWatches(brainLock);
        }

        public async Task<HashSet<string>> GetMarketTickersWithSnapshots()
        {
            return await GetMarketsWithSnapshots();
        }


        public async Task AddOrUpdateCandlestick(CandlestickDTO dto)
        {
            Candlestick? candlestick = await Candlesticks.FirstOrDefaultAsync(x => x.market_ticker == dto.market_ticker
                && x.interval_type == dto.interval_type && x.end_period_ts == dto.end_period_ts);
            if (candlestick == null)
            {
                candlestick = dto.ToCandlestick();
                Candlesticks.Add(candlestick);
            }
            else
            {
                candlestick.UpdateCandlestick(dto);
            }
            await SaveChangesAsync();
        }

        public async Task<List<CandlestickData>> RetrieveCandlesticksAsync(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime)
        {
            if (token.IsCancellationRequested)
                return new List<CandlestickData>();
            var rawCandlesticks = await Candlesticks.AsNoTracking()
                .Where(x => x.interval_type == intervalType && x.market_ticker == marketTicker &&
                            x.end_period_datetime_utc >= sqlStartTime && x.end_period_datetime_utc <= DateTime.UtcNow)
                .Select(c => new CandlestickData
                {
                    Date = new DateTime(c.year, c.month, c.day, c.hour, c.minute, 0, DateTimeKind.Utc),
                    MarketTicker = c.market_ticker,
                    IntervalType = c.interval_type,
                    OpenInterest = c.open_interest,
                    Volume = c.volume,
                    AskOpen = c.yes_ask_open,
                    AskHigh = c.yes_ask_high,
                    AskLow = c.yes_ask_low,
                    AskClose = c.yes_ask_close,
                    BidOpen = c.yes_bid_open,
                    BidHigh = c.yes_bid_high,
                    BidLow = c.yes_bid_low,
                    BidClose = c.yes_bid_close
                })
                .ToListAsync(token);
            return rawCandlesticks;
        }


        public async Task ImportJsonCandlesticks()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_ImportCandlesticksFromFile", conn) { CommandType = CommandType.StoredProcedure };
            await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        #region Market Watches
        public async Task<MarketWatchDTO?> GetMarketWatch(string marketTicker)
        {
            HashSet<MarketWatchDTO> results = await GetMarketWatches(new HashSet<string> { marketTicker });
            if (results.Count > 1) throw new Exception($"Multiple market watches found for ticker {marketTicker}, in GetMarketWatch");
            return results.FirstOrDefault();
        }

        public async Task<HashSet<MarketWatchDTO>> GetMarketWatches(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null)
        {
            IQueryable<MarketWatch> query = MarketWatches.AsNoTracking();
            if (marketTickers != null && marketTickers.Count > 0)
                query = query.Where(x => marketTickers.Contains(x.market_ticker));
            if (brainLocksIncluded != null)
                query = query.Where(x => x.BrainLock != null && brainLocksIncluded.Contains(x.BrainLock.Value));
            if (brainLockIsNull != null)
                query = query.Where(x => (x.BrainLock == null) == brainLockIsNull);
            if (brainLocksExcluded != null)
                query = query.Where(x => x.BrainLock == null || !brainLocksExcluded.Contains(x.BrainLock.Value));
            if (minInterestScore != null)
                query = query.Where(x => x.InterestScore != null && x.InterestScore >= minInterestScore);
            if (maxInterestScore != null)
                query = query.Where(x => x.InterestScore == null || x.InterestScore <= maxInterestScore);
            if (maxInterestScoreDate != null)
                query = query.Where(x => x.InterestScoreDate != null && x.InterestScoreDate <= maxInterestScoreDate);
            var list = await query.Select(x => x.ToMarketWatchDTO()).ToListAsync();
            return list.ToHashSet();
        }

        public async Task RemoveClosedWatches()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var marketWatches = await MarketWatches.Include(x => x.Market)
                    .Where(x => x.Market != null && x.Market.status != KalshiConstants.Status_Active)
                    .ToListAsync();

                int totalRemoved = 0;
                for (int i = 0; i < marketWatches.Count; i += _batchSize)
                {
                    var batch = marketWatches.Skip(i).Take(_batchSize).ToList();
                    MarketWatches.RemoveRange(batch);
                    await SaveChangesAsync();
                    totalRemoved += batch.Count;
                }

                TrackPerformanceMetric("RemoveClosedWatches", true, stopwatch.Elapsed);
                _logger?.LogInformation("Removed {Count} closed market watches", totalRemoved);
            }
            catch (Exception ex)
            {
                TrackPerformanceMetric("RemoveClosedWatches", false, stopwatch.Elapsed);
                _logger?.LogError(ex, "Error removing closed watches");
                throw;
            }
        }


        public async Task<List<MarketWatchDTO>> GetFinalizedMarketWatches(Guid brainLock)
        {
            List<MarketWatch> finalizedWatches = await MarketWatches.AsNoTracking()
                .Where(x => x.BrainLock == brainLock)
                .Include(x => x.Market)
                .ToListAsync();

            return finalizedWatches.Where(x => x.Market != null && KalshiConstants.IsMarketStatusEnded(x.Market.status))
                .Select(x => x.ToMarketWatchDTO())
                .ToList();
        }

        public async Task AddOrUpdateMarketWatches(List<MarketWatchDTO> watches)
        {
            foreach (MarketWatchDTO dto in watches)
            {
                MarketWatch? marketWatch = await MarketWatches.FirstOrDefaultAsync(x => x.market_ticker == dto.market_ticker);
                if (marketWatch == null)
                {
                    marketWatch = dto.ToMarketWatch();
                    MarketWatches.Add(marketWatch);
                }
                else
                {
                    marketWatch.UpdateMarketWatch(dto);
                }
            }
            await SaveChangesAsync();
        }

        public async Task AddOrUpdateMarketWatch(MarketWatchDTO dto)
        {
            MarketWatch? marketWatch = await MarketWatches.FirstOrDefaultAsync(x => x.market_ticker == dto.market_ticker);
            if (marketWatch == null)
            {
                marketWatch = dto.ToMarketWatch();
                MarketWatches.Add(marketWatch);
            }
            else
            {
                marketWatch.UpdateMarketWatch(dto);
            }
            try
            {
                await SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    // Duplicate key violation - this is expected when multiple bots are running
                    _logger?.LogWarning(ex, "Duplicate market watch key {MarketTicker} encountered during save, continuing", dto.market_ticker);
                }
                else
                {
                    // Re-throw for other DbUpdateExceptions
                    throw;
                }
            }
        }

        public async Task RemoveMarketWatches(List<MarketWatchDTO> dtoRange)
        {
            if (dtoRange == null)
                throw new ArgumentNullException(nameof(dtoRange));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                int totalRemoved = 0;
                for (int i = 0; i < dtoRange.Count; i += _batchSize)
                {
                    var batch = dtoRange.Skip(i).Take(_batchSize).ToList();
                    var marketWatch = await MarketWatches
                        .Where(x => batch.Select(y => y.market_ticker).Contains(x.market_ticker))
                        .ToListAsync();
                    MarketWatches.RemoveRange(marketWatch);
                    await SaveChangesAsync();
                    totalRemoved += marketWatch.Count;
                }

                TrackPerformanceMetric("RemoveMarketWatches", true, stopwatch.Elapsed);
                _logger?.LogInformation("Removed {Count} market watches", totalRemoved);
            }
            catch (Exception ex)
            {
                TrackPerformanceMetric("RemoveMarketWatches", false, stopwatch.Elapsed);
                _logger?.LogError(ex, "Error removing market watches");
                throw;
            }
        }

        public async Task RemoveOrphanedBrainLocks()
        {
            List<Guid> AllBrainLocks = await BrainInstances
                .Select(x => x.BrainLock)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToListAsync();
            List<MarketWatch> orphanedWatches = await MarketWatches.Where(x => x.BrainLock != null &&
                !AllBrainLocks.Contains(x.BrainLock.Value)).ToListAsync();
            foreach (MarketWatch orphanedWatch in orphanedWatches)
            {
                orphanedWatch.BrainLock = null;
            }
            await SaveChangesAsync();
        }
        #endregion

        #region Market Positions
        public async Task<MarketPositionDTO?> GetMarketPosition(string marketTicker)
        {
            MarketPosition? marketPosition = await MarketPositions.AsNoTracking().FirstOrDefaultAsync(x => x.Ticker == marketTicker);
            return marketPosition?.ToMarketPositionDTO();
        }

        public async Task<List<MarketPositionDTO>> GetMarketPositions(HashSet<string>? marketTickers = null,
            bool? hasPosition = null, bool? hasRestingOrder = null)
        {
            IQueryable<MarketPosition> query = MarketPositions.AsNoTracking();
            if (marketTickers != null && marketTickers.Count > 0)
                query = query.Where(x => marketTickers.Contains(x.Ticker));
            if (hasPosition != null)
                query = query.Where(x => (x.Position != 0) == hasPosition);
            if (hasRestingOrder != null)
                query = query.Where(x => (x.RestingOrdersCount != 0) == hasRestingOrder);
            return await query.Select(x => x.ToMarketPositionDTO()).ToListAsync();
        }


        public async Task AddOrUpdateMarketPosition(MarketPositionDTO dto)
        {
            MarketPosition? marketPosition = await MarketPositions.FirstOrDefaultAsync(x => x.Ticker == dto.Ticker);
            if (marketPosition == null)
            {
                marketPosition = dto.ToMarketPosition();
                MarketPositions.Add(marketPosition);
            }
            else
            {
                marketPosition.UpdateMarketPosition(dto);
            }
            await SaveChangesAsync();
        }

        public async Task RemoveMarketPosition(string marketTicker)
        {
            MarketPosition? marketPosition = await MarketPositions.FirstOrDefaultAsync(x => x.Ticker == marketTicker);
            if (marketPosition != null)
            {
                MarketPositions.Remove(marketPosition);
                await SaveChangesAsync();
            }
        }
        #endregion

        #region Orders
        public async Task<List<OrderDTO>> GetOrders(string? marketTicker = null, string? status = null)
        {
            IQueryable<Order> query = Orders.AsNoTracking();
            if (marketTicker != null)
                query = query.Where(x => x.Ticker == marketTicker);
            if (status != null)
                query = query.Where(x => x.Status == status);
            return await query.Select(x => x.ToOrderDTO()).ToListAsync();
        }


        public async Task AddOrUpdateOrder(OrderDTO dto)
        {
            Order? order = await Orders.FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);
            if (order == null)
            {
                order = dto.ToOrder();
                order.CreatedTimeUTC = DateTime.UtcNow;
                Orders.Add(order);
            }
            else
            {
                order.UpdateOrder(dto);
            }
            try
            {
                await SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    // Duplicate key violation - this is expected when multiple bots are running
                    _logger?.LogWarning(ex, "Duplicate order key {OrderId} encountered during save, continuing", dto.OrderId);
                }
                else
                {
                    // Re-throw for other DbUpdateExceptions
                    throw;
                }
            }
        }
        #endregion

        #region Snapshot
        public async Task<List<SnapshotDTO>> GetSnapshots(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null)
        {
            IQueryable<Snapshot> query = Snapshots.AsNoTracking();
            if (marketTicker != null)
                query = query.Where(x => x.MarketTicker == marketTicker);
            if (startDate != null)
                query = query.Where(x => x.SnapshotDate >= startDate);
            if (endDate != null)
                query = query.Where(x => x.SnapshotDate <= endDate);
            if (isValidated != null)
                query = query.Where(x => x.IsValidated == isValidated);
            if (MaxSnapshotVersion != null)
                query = query.Where(x => x.JSONSchemaVersion <= MaxSnapshotVersion);
            if (MaxRecords != null)
                query = query.Take(MaxRecords.Value);
            return await query.Select(x => x.ToSnapshotDTO()).ToListAsync();
        }


        public async Task AddOrUpdateSnapshot(SnapshotDTO dto)
        {
            Snapshot? snapshot = await Snapshots.FirstOrDefaultAsync(x => x.MarketTicker == dto.MarketTicker && x.SnapshotDate == dto.SnapshotDate);
            if (snapshot == null)
            {
                snapshot = dto.ToSnapshot();
                Snapshots.Add(snapshot);
            }
            else
            {
                snapshot.UpdateSnapshot(dto);
            }
            try
            {
                await SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
                {
                    // Duplicate key violation - this is expected when multiple bots are running
                    _logger?.LogWarning(ex, "Duplicate snapshot key {MarketTicker} {SnapshotDate} encountered during save, continuing", dto.MarketTicker, dto.SnapshotDate);
                }
                else
                {
                    // Re-throw for other DbUpdateExceptions
                    throw;
                }
            }
        }

        public async Task AddOrUpdateSnapshots(List<SnapshotDTO> dtos)
        {
            foreach (SnapshotDTO dto in dtos)
            {
                Snapshot? snapshot = await Snapshots.FirstOrDefaultAsync(x => x.MarketTicker == dto.MarketTicker && x.SnapshotDate == dto.SnapshotDate);
                if (snapshot == null)
                {
                    snapshot = dto.ToSnapshot();
                    Snapshots.Add(snapshot);
                }
                else
                {
                    snapshot.UpdateSnapshot(dto);
                }
            }
            await SaveChangesAsync();
        }


        public async Task AddSnapshots(List<SnapshotDTO> snapshots)
        {
            await Snapshots.AddRangeAsync(snapshots.Select(x => x.ToSnapshot()));
            await SaveChangesAsync();
        }

        public async Task<long> GetSnapshotCount(string marketTicker)
        {
            return await Snapshots.AsNoTracking().LongCountAsync(x => x.MarketTicker == marketTicker);
        }
        #endregion

        #region Snapshot Schemas
        public async Task<SnapshotSchemaDTO?> GetSnapshotSchema(int version)
        {
            SnapshotSchema? schema = await SnapshotSchemas.AsNoTracking().FirstOrDefaultAsync(x => x.SchemaVersion == version);
            return schema?.ToSnapshotSchemaDTO();
        }

        public async Task<List<SnapshotSchemaDTO>> GetSnapshotSchemas()
        {
            return await SnapshotSchemas.AsNoTracking()
                .Select(x => x.ToSnapshotSchemaDTO())
                .ToListAsync();
        }


        public async Task<SnapshotSchemaDTO> AddSnapshotSchema(SnapshotSchemaDTO dto)
        {
            SnapshotSchema? snapshotSchema = await SnapshotSchemas.FirstOrDefaultAsync(x => x.SchemaVersion == dto.SchemaVersion);
            if (snapshotSchema == null)
            {
                snapshotSchema = dto.ToSnapshotSchema();
                SnapshotSchemas.Add(snapshotSchema);
                await SaveChangesAsync();
                snapshotSchema.SchemaVersion = await SnapshotSchemas.Select(x => x.SchemaVersion).MaxAsync();
            }
            return snapshotSchema!.ToSnapshotSchemaDTO();
        }
        #endregion

        #region Brain Instances
        public async Task<BrainInstanceDTO?> GetBrainInstanceByName(string? instanceName)
        {
            BrainInstance? instance = await BrainInstances.AsNoTracking().FirstOrDefaultAsync(x => x.BrainInstanceName == instanceName);
            return instance?.ToBrainInstanceDTO();
        }

        public async Task<BrainInstanceDTO?> GetBrainInstance(string? instanceName)
        {
            return await GetBrainInstanceByName(instanceName);
        }

        public async Task<List<BrainInstanceDTO>> GetBrainInstancesFiltered(string? instanceName = null, bool? hasBrainLock = null)
        {
            IQueryable<BrainInstance> query = BrainInstances.AsNoTracking();
            if (instanceName != null)
                query = query.Where(x => x.BrainInstanceName == instanceName);
            if (hasBrainLock != null)
                query = query.Where(x => (x.BrainLock != null) == hasBrainLock);
            return await query.Select(x => x.ToBrainInstanceDTO()).ToListAsync();
        }


        public async Task<List<BrainInstanceDTO>> GetStaleBrains(Guid brainLock)
        {
            DateTime now = DateTime.Now;
            DateTime oneHourAgo = now.AddHours(-6);
            bool isMorningGracePeriod = now.Hour >= 3 && now.Hour < 8;
            DateTime specialCutoffTime = new DateTime(now.Year, now.Month, now.Day, 1, 30, 0);

            var staleBrains = await BrainInstances.AsNoTracking()
                .Where(x => x.BrainLock != brainLock && x.BrainLock != null &&
                            (x.LastSeen <= oneHourAgo && (!isMorningGracePeriod || x.LastSeen <= specialCutoffTime)))
                .ToListAsync();

            return staleBrains.ConvertAll(x => x.ToBrainInstanceDTO());
        }

        public async Task AddOrUpdateBrainInstance(BrainInstanceDTO dto)
        {
            BrainInstance? instance = await BrainInstances.FirstOrDefaultAsync(x => x.BrainInstanceName == dto.BrainInstanceName);
            if (instance == null)
            {
                instance = dto.ToBrainInstance();
                BrainInstances.Add(instance);
            }
            else
            {
                instance.UpdateBrainInstance(dto);
            }
            await SaveChangesAsync();
        }

        public async Task SaveBrainPersistence(string brainInstanceName, string persistenceData)
        {
            BrainPersistenceEntity? entity = await BrainPersistenceEntities
                .FirstOrDefaultAsync(x => x.BrainInstanceName == brainInstanceName);

            if (entity == null)
            {
                entity = new BrainPersistenceEntity
                {
                    BrainInstanceName = brainInstanceName,
                    PersistenceData = persistenceData,
                    LastUpdated = DateTime.UtcNow,
                    Version = 1
                };
                BrainPersistenceEntities.Add(entity);
            }
            else
            {
                entity.PersistenceData = persistenceData;
                entity.LastUpdated = DateTime.UtcNow;
            }

            await SaveChangesAsync();
        }

        public async Task<string?> LoadBrainPersistence(string brainInstanceName)
        {
            BrainPersistenceEntity? entity = await BrainPersistenceEntities
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BrainInstanceName == brainInstanceName);

            return entity?.PersistenceData;
        }

        public async Task DeleteBrainPersistence(string brainInstanceName)
        {
            BrainPersistenceEntity? entity = await BrainPersistenceEntities
                .FirstOrDefaultAsync(x => x.BrainInstanceName == brainInstanceName);

            if (entity != null)
            {
                BrainPersistenceEntities.Remove(entity);
                await SaveChangesAsync();
            }
        }

        public async Task<List<string>> GetAllBrainPersistenceNames()
        {
            return await BrainPersistenceEntities
                .AsNoTracking()
                .Select(x => x.BrainInstanceName)
                .ToListAsync();
        }
        #endregion

        #region Snapshot Groups
        public async Task<List<SnapshotGroupDTO>> GetSnapshotGroups(List<string>? marketTickersToInclude = null, int? maxGroups = null)
        {
            IQueryable<SnapshotGroup> query = SnapshotGroups.AsNoTracking();
            if (marketTickersToInclude != null)
                query = query.Where(x => marketTickersToInclude.Contains(x.MarketTicker));
            if (maxGroups != null)
                query = query.Take(maxGroups.Value);
            return await query.Select(x => x.ToSnapshotGroupDTO()).ToListAsync();
        }


        public async Task<HashSet<string>> GetSnapshotGroupNames()
        {
            var list = await SnapshotGroups.AsNoTracking().Select(x => x.MarketTicker).Distinct().ToListAsync();
            return list.ToHashSet();
        }


        public async Task<List<SnapshotDTO>> GetUngroupedSnapshots(int maxMarkets)
        {
            List<Market> markets = await Markets.AsNoTracking()
                .Where(x => x.status != KalshiConstants.Status_Active && !SnapshotGroups.Any(sg => sg.MarketTicker == x.market_ticker))
                .Take(maxMarkets)
                .ToListAsync();

            List<Snapshot> snapshots = await Snapshots.AsNoTracking()
                .Where(x => markets.Select(m => m.market_ticker).Contains(x.MarketTicker))
                .ToListAsync();

            return snapshots.ConvertAll(x => x.ToSnapshotDTO());


        }




        public async Task AddOrUpdateSnapshotGroup(SnapshotGroupDTO dto)
        {
            SnapshotGroup? existingSnapshot = await SnapshotGroups.FirstOrDefaultAsync(x => x.MarketTicker == dto.MarketTicker);
            if (existingSnapshot == null)
            {
                SnapshotGroups.Add(dto.ToSnapshotGroup());
            }
            else
            {
                existingSnapshot.UpdateSnapshotGroup(dto);
            }
            await SaveChangesAsync();
        }

        public async Task AddOrUpdateSnapshotGroups(List<SnapshotGroupDTO> dtoRange)
        {
            foreach (SnapshotGroupDTO dto in dtoRange)
            {
                SnapshotGroup? snapshotGroup = await SnapshotGroups.FirstOrDefaultAsync(x => x.MarketTicker == dto.MarketTicker);
                if (snapshotGroup == null)
                {
                    snapshotGroup = dto.ToSnapshotGroup();
                    SnapshotGroups.Add(snapshotGroup);
                }
                else
                {
                    snapshotGroup.UpdateSnapshotGroup(dto);
                }
            }
            await SaveChangesAsync();
        }
        #endregion

        #region WeightSets
        public async Task<WeightSetDTO?> GetWeightSetByStrategyName(string strategyName)
        {
            WeightSet? weightSet = await WeightSets.AsNoTracking()
                .Include(ws => ws.WeightSetMarkets)
                .FirstOrDefaultAsync(ws => ws.StrategyName == strategyName);

            if (weightSet == null) return null;

            WeightSetDTO dto = weightSet.ToWeightSetDTO();
            dto.WeightSetMarkets = weightSet.WeightSetMarkets.Select(m => m.ToWeightSetMarketDTO()).ToList();
            return dto;
        }


        public async Task<List<WeightSetDTO>> GetWeightSets(HashSet<string>? strategyNames = null)
        {
            IQueryable<WeightSet> query = WeightSets.AsNoTracking()
                .Include(ws => ws.WeightSetMarkets);

            if (strategyNames != null && strategyNames.Count > 0)
            {
                query = query.Where(ws => strategyNames.Contains(ws.StrategyName));
            }

            List<WeightSet> weightSets = await query.ToListAsync();

            return weightSets.Select(ws =>
            {
                WeightSetDTO dto = ws.ToWeightSetDTO();
                dto.WeightSetMarkets = ws.WeightSetMarkets.Select(m => m.ToWeightSetMarketDTO()).ToList();
                return dto;
            }).ToList();
        }

        public async Task<List<WeightSetDTO>> GetWeightSetsByMarketTicker(string marketTicker)
        {
            List<WeightSet> weightSets = await WeightSets.AsNoTracking()
                .Where(ws => ws.WeightSetMarkets.Any(wsm => wsm.MarketTicker == marketTicker))
                .Include(ws => ws.WeightSetMarkets.Where(wsm => wsm.MarketTicker == marketTicker))
                .ToListAsync();

            return weightSets.Select(ws =>
            {
                WeightSetDTO dto = ws.ToWeightSetDTO();
                dto.WeightSetMarkets = ws.WeightSetMarkets.Select(m => m.ToWeightSetMarketDTO()).ToList();
                return dto;
            }).ToList();
        }

        public async Task AddOrUpdateWeightSet(WeightSetDTO dto)
        {
            using var transaction = await Database.BeginTransactionAsync();
            try
            {
                WeightSet? weightSet = await WeightSets
                    .Include(ws => ws.WeightSetMarkets)
                    .FirstOrDefaultAsync(ws => ws.StrategyName == dto.StrategyName);

                if (weightSet == null)
                {
                    weightSet = dto.ToWeightSet();
                    weightSet.WeightSetMarkets.Clear();
                    await WeightSets.AddAsync(weightSet);
                }
                else
                {
                    weightSet.UpdateWeightSet(dto);

                    var marketsToRemove = weightSet.WeightSetMarkets
                        .Where(m => !dto.WeightSetMarkets.Any(dm => dm.WeightSetID == m.WeightSetID && dm.MarketTicker == m.MarketTicker))
                        .ToList();
                    foreach (var market in marketsToRemove)
                    {
                        weightSet.WeightSetMarkets.Remove(market);
                    }
                }

                foreach (var marketDTO in dto.WeightSetMarkets)
                {
                    var existingMarket = weightSet.WeightSetMarkets
                        .FirstOrDefault(m => m.MarketTicker == marketDTO.MarketTicker);

                    if (existingMarket == null)
                    {
                        marketDTO.WeightSetID = weightSet.WeightSetID;
                        weightSet.WeightSetMarkets.Add(marketDTO.ToWeightSetMarket());
                    }
                    else
                    {
                        existingMarket.UpdateWeightSetMarket(marketDTO);
                    }
                }

                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task AddOrUpdateWeightSets(List<WeightSetDTO> dtos)
        {
            foreach (WeightSetDTO dto in dtos)
            {
                await AddOrUpdateWeightSet(dto);
            }
        }

        public async Task DeleteWeightSet(string strategyName)
        {
            WeightSet? weightSet = await WeightSets
                .Include(ws => ws.WeightSetMarkets)
                .FirstOrDefaultAsync(ws => ws.StrategyName == strategyName);

            if (weightSet != null)
            {
                WeightSets.Remove(weightSet);
                await SaveChangesAsync();
            }
        }
        #endregion

        #region Log Entries
        public async Task AddLogEntry(LogEntryDTO dto)
        {
            LogEntries.Add(dto.ToLogEntry());
            await SaveChangesAsync();
        }

        public async Task AddOverseerLogEntry(LogEntryDTO dto)
        {
            OverseerLogEntries.Add(dto.ToOverseerLogEntry());
            await SaveChangesAsync();
        }

        public async Task<List<LogEntryDTO>> GetLogEntries(string? brainInstance = null, string? level = null,
            DateTime? startDate = null, DateTime? endDate = null, int? maxRecords = null)
        {
            IQueryable<LogEntry> query = LogEntries.AsNoTracking();

            if (!string.IsNullOrEmpty(brainInstance))
                query = query.Where(le => le.BrainInstance == brainInstance);

            if (!string.IsNullOrEmpty(level))
                query = query.Where(le => le.Level == level);

            if (startDate.HasValue)
                query = query.Where(le => le.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(le => le.Timestamp <= endDate.Value);

            if (maxRecords.HasValue)
                query = query.Take(maxRecords.Value);

            var logEntries = await query
                .OrderByDescending(le => le.Timestamp)
                .ToListAsync();

            return logEntries.Select(le => le.ToLogEntryDTO()).ToList();
        }

        public async Task<List<LogEntryDTO>> GetLogEntriesFiltered(string? brainInstance = null, string? level = null,
            DateTime? startDate = null, DateTime? endDate = null, int? maxRecords = null)
        {
            return await GetLogEntries(brainInstance, level, startDate, endDate, maxRecords);
        }

        #endregion

        #region Announcements
        public async Task AddAnnouncements(List<AnnouncementDTO> announcements)
        {
            var announcementModels = announcements.Select(a => a.ToAnnouncement()).ToList();
            await Announcements.AddRangeAsync(announcementModels);
            await SaveChangesAsync();
        }
        #endregion

        #region Exchange Schedule
        public async Task AddExchangeSchedule(ExchangeScheduleDTO exchangeSchedule)
        {
            var exchangeScheduleModel = exchangeSchedule.ToExchangeSchedule();
            await ExchangeSchedules.AddAsync(exchangeScheduleModel);
            await SaveChangesAsync();
        }
        #endregion

        #region Other
        public async Task<List<MarketLiquidityStatsDTO>> GetMarketLiquidityStates()
        {
            var states = await Markets.AsNoTracking()
                .Select(m => new { m.volume_24h, m.liquidity, m.open_interest, m.yes_bid, m.no_bid })
                .ToListAsync();
            return states.Select(m => new MarketLiquidityStatsDTO
            {
                liquidity = m.liquidity,
                open_interest = m.open_interest,
                volume_24h = m.volume_24h,
                yes_bid = m.yes_bid,
                no_bid = m.no_bid
            }).ToList();
        }

        #endregion

        #region SignalR Clients
        public async Task<List<BacklashDTOs.SignalRClient>> GetSignalRClients(string? clientId = null, string? ipAddress = null, bool? isActive = null)
        {
            IQueryable<BacklashDTOs.SignalRClient> query = SignalRClients.AsNoTracking();
            if (!string.IsNullOrEmpty(clientId))
                query = query.Where(c => c.ClientId == clientId);
            if (!string.IsNullOrEmpty(ipAddress))
                query = query.Where(c => c.IPAddress == ipAddress);
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);
            return await query.ToListAsync();
        }

        public async Task<BacklashDTOs.SignalRClient?> GetSignalRClientById(string clientId)
        {
            return await SignalRClients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == clientId);
        }

        public async Task<BacklashDTOs.SignalRClient?> GetSignalRClient(string clientId)
        {
            return await GetSignalRClientById(clientId);
        }

        public async Task AddOrUpdateSignalRClient(BacklashDTOs.SignalRClient client)
        {
            var existing = await SignalRClients.FirstOrDefaultAsync(c => c.ClientId == client.ClientId);
            if (existing == null)
            {
                SignalRClients.Add(client);
            }
            else
            {
                existing.ClientName = client.ClientName;
                existing.IPAddress = client.IPAddress;
                existing.AuthToken = client.AuthToken;
                existing.ClientType = client.ClientType;
                existing.IsActive = client.IsActive;
                existing.LastSeen = client.LastSeen;
                existing.ConnectionId = client.ConnectionId;
                this.Entry(existing).State = EntityState.Modified;
            }
            await SaveChangesAsync();
        }

        public async Task UpdateSignalRClientConnection(string clientId, string connectionId)
        {
            var client = await SignalRClients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            if (client != null)
            {
                client.ConnectionId = connectionId;
                client.LastSeen = DateTime.UtcNow;
                await SaveChangesAsync();
            }
        }

        public async Task UpdateSignalRClientLastSeen(string clientId)
        {
            var client = await SignalRClients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            if (client != null)
            {
                client.LastSeen = DateTime.UtcNow;
                await SaveChangesAsync();
            }
        }

        public async Task DeactivateSignalRClient(string clientId)
        {
            var client = await SignalRClients.FirstOrDefaultAsync(c => c.ClientId == clientId);
            if (client != null)
            {
                client.IsActive = false;
                client.LastSeen = DateTime.UtcNow;
                await SaveChangesAsync();
            }
        }
        #endregion

        #region Overseer Info
        public async Task AddOrUpdateOverseerInfo(BacklashDTOs.Data.OverseerInfo overseerInfo)
        {
            var existing = await OverseerInfos.FirstOrDefaultAsync(oi => oi.HostName == overseerInfo.HostName && oi.IPAddress == overseerInfo.IPAddress);
            if (existing == null)
            {
                OverseerInfos.Add(overseerInfo);
            }
            else
            {
                existing.LastHeartbeat = overseerInfo.LastHeartbeat ?? DateTime.UtcNow;
                existing.IsActive = overseerInfo.IsActive;
                existing.ServiceName = overseerInfo.ServiceName;
                existing.Version = overseerInfo.Version;
                this.Entry(existing).State = EntityState.Modified;
            }
            await SaveChangesAsync();
        }

        public async Task<List<BacklashDTOs.Data.OverseerInfo>> GetActiveOverseerInfos()
        {
            return await OverseerInfos
                .Where(oi => oi.IsActive)
                .OrderByDescending(oi => oi.LastHeartbeat ?? oi.StartTime)
                .ToListAsync();
        }

        public async Task<BacklashDTOs.Data.OverseerInfo?> GetOverseerInfoByHostName(string hostName)
        {
            return await OverseerInfos.FirstOrDefaultAsync(oi => oi.HostName == hostName && oi.IsActive);
        }
        /// <summary>
        /// Tracks performance metrics for database operations.
        /// </summary>
        /// <param name="operationName">Name of the operation being tracked.</param>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="duration">Time taken for the operation.</param>
        private void TrackPerformanceMetric(string operationName, bool success, TimeSpan duration)
        {
            lock (_performanceMetrics)
            {
                if (!_performanceMetrics.ContainsKey(operationName))
                {
                    _performanceMetrics[operationName] = (0, 0, TimeSpan.Zero);
                }

                var current = _performanceMetrics[operationName];
                if (success)
                {
                    _performanceMetrics[operationName] = (current.SuccessCount + 1, current.FailureCount, current.TotalTime + duration);
                }
                else
                {
                    _performanceMetrics[operationName] = (current.SuccessCount, current.FailureCount + 1, current.TotalTime + duration);
                }
            }

            _logger?.LogInformation("Operation {OperationName} completed in {Duration}ms, Success: {Success}",
                operationName, duration.TotalMilliseconds, success);
        }

        /// <summary>
        /// Saves changes to the database with retry logic for transient SQL errors.
        /// </summary>
        /// <remarks>
        /// Implements Polly retry policy to handle transient SQL Server errors such as:
        /// deadlocks (1205), lock timeouts (1222), connection issues, and throttling errors.
        /// Retries up to configured count with exponential backoff.
        /// </remarks>
        private async Task SaveChangesWithRetryAsync()
        {
            var retryPolicy = Policy.Handle<SqlException>(ex => IsTransient(ex))
                .WaitAndRetryAsync(_maxRetryCount, i => TimeSpan.FromSeconds(i) * _retryDelay.TotalSeconds);
            await retryPolicy.ExecuteAsync(async () => await SaveChangesAsync());
        }

        /// <summary>
        /// Determines if a SQL exception represents a transient error that should be retried.
        /// </summary>
        /// <param name="ex">The SQL exception to evaluate.</param>
        /// <returns>True if the exception is transient and should be retried.</returns>
        /// <remarks>
        /// Transient errors include deadlocks, timeouts, connection issues, and Azure SQL throttling.
        /// Non-transient errors (like constraint violations) are not retried.
        /// </remarks>
        private static bool IsTransient(SqlException ex)
        {
            var transientErrors = new HashSet<int> { 1205, 1222, 49918, 49919, 49920, 4060, 40197, 40501, 40613, 40143, 233, 64 };
            return transientErrors.Contains(ex.Number);
        }
        #endregion

        #region Database Health Check
        /// <summary>
        /// Tests basic database connectivity by executing a simple SELECT query.
        /// This method is used for health checks and doesn't depend on any existing data.
        /// </summary>
        public async Task TestDbAsync()
        {
            await Database.ExecuteSqlRawAsync("SELECT 1");
        }
        #endregion

        /// <summary>
        /// Retrieves current performance metrics for database operations.
        /// </summary>
        /// <returns>Dictionary containing operation names and their performance statistics.</returns>
        public IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> GetPerformanceMetrics()
        {
            lock (_performanceMetrics)
            {
                return _performanceMetrics.ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                    {
                        var totalOperations = kvp.Value.SuccessCount + kvp.Value.FailureCount;
                        var averageTime = totalOperations > 0
                            ? kvp.Value.TotalTime.TotalMilliseconds / totalOperations
                            : 0.0;
                        return (kvp.Value.SuccessCount, kvp.Value.FailureCount, kvp.Value.TotalTime, averageTime);
                    });
            }
        }

        /// <summary>
        /// Resets all performance metrics.
        /// </summary>
        public void ResetPerformanceMetrics()
        {
            lock (_performanceMetrics)
            {
                _performanceMetrics.Clear();
            }
        }

        /// <summary>
        /// Configures the database context to use SQL Server with the configured connection string.
        /// </summary>
        /// <param name="optionsBuilder">The options builder for configuring the context.</param>
        /// <remarks>
        /// This method is called by Entity Framework during context initialization.
        /// It sets up the SQL Server provider with the connection string from configuration.
        /// </remarks>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        /// <summary>
        /// Configures the Entity Framework model with table mappings, keys, relationships, and constraints.
        /// </summary>
        /// <param name="modelBuilder">The model builder for configuring entity mappings.</param>
        /// <remarks>
        /// This method defines:
        /// - Table names and schemas
        /// - Primary keys and composite keys
        /// - Foreign key relationships and cascade behaviors
        /// - Database indexes for performance optimization
        /// - Column constraints and default values
        /// - Concurrency tokens for optimistic locking
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Event>()
                .ToTable("t_Events", x => x.HasTrigger("trg_Events_LastModifiedDate"))
                .HasKey(e => e.event_ticker);

            modelBuilder.Entity<Event>()
                .Property(e => e.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Event>()
                .HasIndex(e => e.category);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Series)
                .WithMany(s => s.Events)
                .HasForeignKey(e => e.series_ticker)
                .HasPrincipalKey(s => s.series_ticker)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Series>()
                .ToTable("t_Series", x => x.HasTrigger("trg_Series_LastModifiedDate"))
                .HasKey(s => s.series_ticker);

            modelBuilder.Entity<Series>()
                .Property(s => s.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Series>()
                .HasIndex(s => s.category);

            modelBuilder.Entity<Series>()
                .HasIndex(s => s.title);

            modelBuilder.Entity<Candlestick>()
                .ToTable("t_Candlesticks")
                .HasKey(x => new { x.market_ticker, x.interval_type, x.end_period_ts });

            modelBuilder.Entity<Candlestick>()
                .HasIndex(x => new { x.year, x.month, x.day, x.hour, x.minute });

            modelBuilder.Entity<Candlestick>()
                .Property(x => x.open_interest).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.price_close).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.price_high).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.price_low).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.price_mean).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.price_open).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.price_previous).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.volume).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_ask_close).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_ask_high).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_ask_low).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_ask_open).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_bid_close).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_bid_high).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_bid_low).IsConcurrencyToken(false);
            modelBuilder.Entity<Candlestick>()
                .Property(x => x.yes_bid_open).IsConcurrencyToken(false);

            modelBuilder.Entity<Candlestick>()
                .Property(x => x.year)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken(false);

            modelBuilder.Entity<Candlestick>()
                .Property(x => x.month)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken(false);

            modelBuilder.Entity<Candlestick>()
                .Property(x => x.day)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken(false);

            modelBuilder.Entity<Candlestick>()
                .Property(x => x.hour)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken(false);

            modelBuilder.Entity<Candlestick>()
                .Property(x => x.minute)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken(false);

            modelBuilder.Entity<Candlestick>()
                .Property(x => x.end_period_datetime_utc)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken(false);

            modelBuilder.Entity<SeriesTag>()
                .ToTable("t_Series_Tags", x => x.HasTrigger("trg_t_Series_Tags_InsertLastModifiedDate"))
                .HasKey(st => new { st.series_ticker, st.tag });

            modelBuilder.Entity<SeriesTag>()
                .HasOne(st => st.Series)
                .WithMany(s => s.Tags)
                .HasForeignKey(st => st.series_ticker)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SeriesSettlementSource>()
                .ToTable("t_Series_SettlementSources", x => x.HasTrigger("trg_t_Series_SettlementSources_InsertLastModifiedDate"))
                .HasKey(st => new { st.series_ticker, st.name });

            modelBuilder.Entity<SeriesSettlementSource>()
                .HasOne(ss => ss.Series)
                .WithMany(s => s.SettlementSources)
                .HasForeignKey(ss => ss.series_ticker)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketWatch>()
                .ToTable("t_MarketWatches")
                .HasKey(m => m.market_ticker);

            modelBuilder.Entity<MarketWatch>()
                .HasIndex(m => new { m.InterestScore, m.InterestScoreDate })
                .HasDatabaseName("IX_t_MarketWatches_InterestScore_InterestScoreDate")
                .IncludeProperties(m => new { m.market_ticker, m.BrainLock });

            modelBuilder.Entity<MarketWatch>()
                .Property(m => m.market_ticker)
                .HasMaxLength(150)
                .IsRequired();

            modelBuilder.Entity<MarketWatch>()
                .Property(m => m.BrainLock)
                .HasColumnType("uniqueidentifier");

            modelBuilder.Entity<MarketWatch>()
                .Property(m => m.InterestScore)
                .HasColumnType("float");

            modelBuilder.Entity<MarketWatch>()
                .Property(m => m.InterestScoreDate)
                .HasColumnType("datetime");

            modelBuilder.Entity<MarketWatch>()
                .Property(m => m.LastWatched)
                .HasColumnType("datetime");

            modelBuilder.Entity<MarketWatch>()
                .HasIndex(m => m.InterestScore)
                .HasDatabaseName("IX_t_MarketWatches_InterestScore")
                .IncludeProperties(m => m.market_ticker);

            modelBuilder.Entity<MarketWatch>()
                .HasIndex(m => m.BrainLock)
                .HasDatabaseName("IX_t_MarketWatches_BrainLock_NonNull")
                .IncludeProperties(m => m.market_ticker);

            modelBuilder.Entity<MarketWatch>()
                .HasIndex(m => m.InterestScoreDate)
                .HasDatabaseName("IX_t_MarketWatches_InterestScoreDate");

            modelBuilder.Entity<MarketWatch>()
                .HasOne(m => m.Market)
                .WithOne(mw => mw.MarketWatch)
                .HasForeignKey<MarketWatch>(m => m.market_ticker)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BrainInstance>()
                .ToTable("t_BrainInstances")
                .HasKey(m => m.BrainInstanceName);

            modelBuilder.Entity<BrainPersistenceEntity>()
                .ToTable("t_BrainPersistence")
                .HasKey(m => m.BrainInstanceName);

            modelBuilder.Entity<BrainPersistenceEntity>()
                .Property(m => m.BrainInstanceName)
                .HasMaxLength(25)
                .IsRequired();

            modelBuilder.Entity<BrainPersistenceEntity>()
                .Property(m => m.PersistenceData)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            modelBuilder.Entity<BrainPersistenceEntity>()
                .Property(m => m.LastUpdated)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<BrainPersistenceEntity>()
                .Property(m => m.Version)
                .HasDefaultValue(1);

            modelBuilder.Entity<Market>()
                .ToTable("t_Markets")
                .HasKey(m => m.market_ticker);

            modelBuilder.Entity<Market>()
                .Property(m => m.market_ticker).HasMaxLength(150).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.event_ticker).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.market_type).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.title).HasMaxLength(1000).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.subtitle).HasMaxLength(255);
            modelBuilder.Entity<Market>()
                .Property(m => m.yes_sub_title).HasMaxLength(255).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.no_sub_title).HasMaxLength(255).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.open_time).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.close_time).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.expiration_time).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.latest_expiration_time).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.settlement_timer_seconds).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.status).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.response_price_units).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.result).HasMaxLength(20).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.expiration_value).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.category).HasMaxLength(50).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.strike_type).HasMaxLength(30).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.rules_primary).IsRequired();
            modelBuilder.Entity<Market>()
                .Property(m => m.CreatedDate).HasDefaultValueSql("GETDATE()").IsRequired();

            modelBuilder.Entity<Market>()
                .HasIndex(m => m.event_ticker).HasDatabaseName("IX_t_Markets_event_ticker").IncludeProperties(m => m.market_ticker);
            modelBuilder.Entity<Market>()
                .HasIndex(m => m.market_type).HasDatabaseName("IX_t_Markets_market_type");
            modelBuilder.Entity<Market>()
                .HasIndex(m => m.result).HasDatabaseName("IX_t_Markets_result");
            modelBuilder.Entity<Market>()
                .HasIndex(m => m.status).HasDatabaseName("IX_t_Markets_status");
            modelBuilder.Entity<Market>()
                .HasIndex(m => m.close_time).HasDatabaseName("IX_t_Markets_close_time").IncludeProperties(m => new { m.market_ticker, m.status });
            modelBuilder.Entity<Market>()
                .HasIndex(m => m.LastCandlestickUTC).HasDatabaseName("IX_t_Markets_LastCandlestick").IncludeProperties(m => new { m.market_ticker, m.status });

            modelBuilder.Entity<Market>()
                .HasOne(m => m.Event)
                .WithMany(e => e.Markets)
                .HasForeignKey(m => m.event_ticker)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Market>()
                .HasOne(m => m.MarketWatch)
                .WithOne(mw => mw.Market)
                .HasForeignKey<MarketWatch>(mw => mw.market_ticker)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Fill>()
                .ToTable("t_feed_fill")
                .HasKey(ff => new { ff.market_ticker, ff.ts });

            modelBuilder.Entity<Fill>()
                .HasIndex(ff => ff.action);

            modelBuilder.Entity<Fill>()
                .HasOne(o => o.Market)
                .WithMany()
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false);

            modelBuilder.Entity<EventLifecycle>()
                .ToTable("t_feed_lifecycle")
                .HasKey(ff => new { ff.market_ticker, ff.LoggedDate });

            modelBuilder.Entity<EventLifecycle>()
                .HasIndex(ff => ff.is_deactivated);

            modelBuilder.Entity<EventLifecycle>()
                .HasIndex(ff => ff.result);

            modelBuilder.Entity<EventLifecycle>()
                .HasOne(o => o.Market)
                .WithMany()
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false);

            modelBuilder.Entity<Ticker>()
                .ToTable("t_feed_ticker")
                .HasKey(ff => new { ff.market_ticker, ff.LoggedDate });

            modelBuilder.Entity<Ticker>()
                .HasIndex(ff => ff.market_ticker);

            modelBuilder.Entity<Ticker>()
                .HasOne(o => o.Market)
                .WithMany()
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false);

            modelBuilder.Entity<Trade>()
                .ToTable("t_feed_trade")
                .HasKey(ff => new { ff.market_id, ff.yes_price, ff.no_price, ff.LoggedDate });

            modelBuilder.Entity<Trade>()
                .HasIndex(ff => ff.market_ticker);

            modelBuilder.Entity<Trade>()
                .HasOne(o => o.Market)
                .WithMany()
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false);

            modelBuilder.Entity<MarketPosition>()
                .ToTable("t_MarketPositions")
                .HasKey(mp => mp.Ticker);

            modelBuilder.Entity<MarketPosition>()
                .Property(mp => mp.LastModified);

            modelBuilder.Entity<SnapshotSchema>()
                .ToTable("t_SnapshotSchemas")
                .HasKey(x => x.SchemaVersion);

            modelBuilder.Entity<MarketType>()
                .ToTable("t_MarketTypes")
                .HasKey(x => x.MarketTypeID);

            modelBuilder.Entity<Snapshot>()
                .ToTable("t_Snapshots")
                .HasKey(st => new { st.MarketTicker, st.SnapshotDate });

            modelBuilder.Entity<Snapshot>()
                .HasIndex(ff => ff.JSONSchemaVersion);

            modelBuilder.Entity<Snapshot>()
                .HasIndex(ff => ff.ChangeMetricsMature);

            modelBuilder.Entity<Snapshot>()
                .HasOne(o => o.Market)
                .WithMany()
                .HasForeignKey(o => o.MarketTicker)
                .HasPrincipalKey(m => m.market_ticker)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Snapshot>()
                .HasOne(o => o.MarketType)
                .WithMany()
                .HasForeignKey(o => o.MarketTypeID)
                .HasPrincipalKey(m => m.MarketTypeID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .ToTable("t_Orders", x => x.HasTrigger("trg_Orders_LastModifiedDate"))
                .HasKey(o => o.OrderId);

            modelBuilder.Entity<Order>()
                .Property(o => o.LastModified);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Ticker);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.CreatedTimeUTC);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Market)
                .WithMany()
                .HasForeignKey(o => o.Ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity
                    .ToTable("t_LogEntries")
                    .HasKey(e => e.Id);

                entity.Property(e => e.Timestamp)
                      .IsRequired()
                      .HasColumnType("datetime");

                entity.Property(e => e.Level)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.SessionIdentifier)
                      .IsRequired()
                      .HasMaxLength(5);

                entity.Property(e => e.Message)
                      .IsRequired()
                      .HasMaxLength(4000);

                entity.Property(e => e.Exception)
                      .HasMaxLength(4000);

                entity.Property(e => e.Source)
                      .IsRequired()
                      .HasMaxLength(255);
            });

            modelBuilder.Entity<OverseerLogEntry>(entity =>
            {
                entity
                    .HasKey(e => e.Id);

                entity.Property(e => e.Timestamp)
                      .IsRequired()
                      .HasColumnType("datetime");

                entity.Property(e => e.Level)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.SessionIdentifier)
                      .IsRequired()
                      .HasMaxLength(5);

                entity.Property(e => e.Message)
                      .IsRequired()
                      .HasMaxLength(4000);

                entity.Property(e => e.Exception)
                      .HasMaxLength(4000);

                entity.Property(e => e.Source)
                      .IsRequired()
                      .HasMaxLength(255);
            });

            modelBuilder.Entity<StratData>()
                .ToTable("t_StratData")
                .HasKey(s => s.StratID);

            modelBuilder.Entity<StratData>()
                .Property(s => s.StratName)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<StratData>()
                .Property(s => s.StratType)
                .IsRequired();

            modelBuilder.Entity<StratData>()
                .Property(s => s.RawJSON)
                .HasColumnType("NVARCHAR(MAX)")
                .IsRequired();

            modelBuilder.Entity<StratData>()
                .HasIndex(s => s.StratName)
                .IsUnique();

            modelBuilder.Entity<SnapshotGroup>()
                .ToTable("t_SnapshotGroups")
                .HasKey(sg => new { sg.MarketTicker, sg.StartTime, sg.EndTime });

            modelBuilder.Entity<SnapshotGroup>()
                .Property(sg => sg.SnapshotGroupID)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<SnapshotGroup>()
                .Property(sg => sg.MarketTicker)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SnapshotGroup>()
                .HasOne(sg => sg.Market)
                .WithMany()
                .HasForeignKey(sg => sg.MarketTicker)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WeightSet>()
                .ToTable("t_WeightSets")
                .HasKey(ws => ws.WeightSetID);

            modelBuilder.Entity<WeightSet>()
                .Property(ws => ws.WeightSetID)
                .ValueGeneratedOnAdd();  // IDENTITY(1,1)

            modelBuilder.Entity<WeightSet>()
                .Property(ws => ws.StrategyName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<WeightSet>()
                .Property(ws => ws.Weights)
                .HasColumnType("varchar(max)")
                .IsRequired();

            modelBuilder.Entity<WeightSet>()
                .Property(ws => ws.LastRun)
                .HasColumnType("datetime");

            modelBuilder.Entity<WeightSetMarket>()
                .ToTable("t_WeightSetMarkets")
                .HasKey(wsm => new { wsm.WeightSetID, wsm.MarketTicker });

            modelBuilder.Entity<WeightSetMarket>()
                .Property(wsm => wsm.MarketTicker)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<WeightSetMarket>()
                .Property(wsm => wsm.PnL)
                .HasColumnType("money")
                .IsRequired();

            modelBuilder.Entity<WeightSetMarket>()
                .Property(wsm => wsm.LastRun)
                .HasColumnType("datetime");

            modelBuilder.Entity<WeightSetMarket>()
                .HasOne(wsm => wsm.WeightSet)
                .WithMany(ws => ws.WeightSetMarkets)
                .HasForeignKey(wsm => wsm.WeightSetID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Announcement>()
                .ToTable("t_Announcements")
                .HasKey(a => a.AnnouncementID);

            modelBuilder.Entity<Announcement>()
                .Property(a => a.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Announcement>()
                .Property(a => a.LastModifiedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<ExchangeSchedule>()
                .ToTable("t_ExchangeSchedule")
                .HasKey(es => es.ExchangeScheduleID);

            modelBuilder.Entity<ExchangeSchedule>()
                .Property(es => es.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<ExchangeSchedule>()
                .Property(es => es.LastModifiedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<ExchangeSchedule>()
                .HasMany(es => es.MaintenanceWindows)
                .WithOne(mw => mw.ExchangeSchedule)
                .HasForeignKey(mw => mw.ExchangeScheduleID);

            modelBuilder.Entity<ExchangeSchedule>()
                .HasMany(es => es.StandardHours)
                .WithOne(sh => sh.ExchangeSchedule)
                .HasForeignKey(sh => sh.ExchangeScheduleID);

            modelBuilder.Entity<MaintenanceWindow>()
                .ToTable("t_MaintenanceWindows")
                .HasKey(mw => mw.MaintenanceWindowID);

            modelBuilder.Entity<MaintenanceWindow>()
                .Property(mw => mw.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<MaintenanceWindow>()
                .Property(mw => mw.LastModifiedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<StandardHours>()
                .ToTable("t_StandardHours")
                .HasKey(sh => sh.StandardHoursID);

            modelBuilder.Entity<StandardHours>()
                .Property(sh => sh.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<StandardHours>()
                .Property(sh => sh.LastModifiedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<StandardHours>()
                .HasMany(sh => sh.Sessions)
                .WithOne(s => s.StandardHours)
                .HasForeignKey(s => s.StandardHoursID);

            modelBuilder.Entity<StandardHoursSession>()
                .ToTable("t_StandardHoursSessions")
                .HasKey(s => s.SessionID);

            modelBuilder.Entity<StandardHoursSession>()
                .Property(s => s.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<StandardHoursSession>()
                .Property(s => s.LastModifiedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .ToTable("t_SignalRClients")
                .HasKey(c => c.ClientId);

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .Property(c => c.ClientId)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .Property(c => c.ClientName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .Property(c => c.IPAddress)
                .HasMaxLength(45)
                .IsRequired();

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .Property(c => c.AuthToken)
                .HasMaxLength(256)
                .IsRequired();

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .Property(c => c.ClientType)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .Property(c => c.RegisteredAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .HasIndex(c => c.IPAddress)
                .HasDatabaseName("IX_t_SignalRClients_IPAddress");

            modelBuilder.Entity<BacklashDTOs.SignalRClient>()
                .HasIndex(c => new { c.IsActive, c.LastSeen })
                .HasDatabaseName("IX_t_SignalRClients_IsActive_LastSeen");

            modelBuilder.Entity<OverseerInfo>()
                .ToTable("t_OverseerInfo")
                .HasKey(oi => oi.Id);

            modelBuilder.Entity<OverseerInfo>()
                .Property(oi => oi.HostName)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<OverseerInfo>()
                .Property(oi => oi.IPAddress)
                .HasMaxLength(45)
                .IsRequired();

            modelBuilder.Entity<OverseerInfo>()
                .Property(oi => oi.Port)
                .IsRequired();

            modelBuilder.Entity<OverseerInfo>()
                .Property(oi => oi.StartTime)
                .IsRequired();

            modelBuilder.Entity<OverseerInfo>()
                .Property(oi => oi.IsActive)
                .IsRequired();

            modelBuilder.Entity<OverseerInfo>()
                .Property(oi => oi.ServiceName)
                .HasMaxLength(100);

            modelBuilder.Entity<OverseerInfo>()
                .Property(oi => oi.Version)
                .HasMaxLength(50);

            modelBuilder.Entity<OverseerInfo>()
                .HasIndex(oi => oi.IsActive)
                .HasDatabaseName("IX_t_OverseerInfo_IsActive");

            modelBuilder.Entity<OverseerInfo>()
                .HasIndex(oi => oi.LastHeartbeat)
                .HasDatabaseName("IX_t_OverseerInfo_LastHeartbeat");
        }
    }
}
