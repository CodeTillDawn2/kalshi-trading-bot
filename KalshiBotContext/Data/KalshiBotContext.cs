using KalshiBotData.Data.Interfaces;
using KalshiBotData.Extensions;
using KalshiBotData.Models;
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

namespace KalshiBotData.Data
{
    public class KalshiBotContext : DbContext, IKalshiBotContext
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
        private readonly IConfiguration _config;
        private readonly ILogger<KalshiBotContext>? _logger;

        public KalshiBotContext(IConfiguration config, ILogger<KalshiBotContext>? logger = null)
        {
            _config = config;
            _logger = logger;
            _connectionString = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection connection string is not configured.");
        }

        #region Series
        public async Task<SeriesDTO?> GetSeriesByTicker(string seriesTicker)
        {
            SeriesDTO? seriesDTO = null;
            Series? series = await Series.AsNoTracking().FirstOrDefaultAsync(x => x.series_ticker == seriesTicker);
            if (series != null)
            {
                seriesDTO = series.ToSeriesDTO();
            }
            return seriesDTO;
        }

        public async Task<SeriesDTO?> GetSeriesByTicker_cached(string seriesTicker)
        {
            return await GetSeriesByTicker(seriesTicker);
        }

        public async Task AddOrUpdateSeries(SeriesDTO dto)
        {
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
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception($"Failed to add or update series for ticker {dto.series_ticker}: {ex.Message}", ex);
            }
        }

        #endregion

        #region Events
        public async Task<EventDTO?> GetEventByTicker(string eventTicker)
        {
            var rawEvent = await Events.AsNoTracking().FirstOrDefaultAsync(x => x.event_ticker == eventTicker);
            if (rawEvent == null) return null;
            var rawSeries = await Series.AsNoTracking().FirstOrDefaultAsync(x => x.series_ticker == rawEvent.series_ticker);
            if (rawSeries != null)
            {
                rawEvent.Series = rawSeries;
            }
            return rawEvent.ToEventDTO();
        }

        public async Task<EventDTO?> GetEventByTicker_cached(string eventTicker)
        {
            return await GetEventByTicker(eventTicker);
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
        public async Task<MarketDTO?> GetMarketByTicker(string marketTicker)
        {
            Market? market = await Markets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.market_ticker == marketTicker);

            if (market == null) return null;

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
            return market.ToMarketDTO();
        }

        public async Task<MarketDTO?> GetMarketByTicker_cached(string marketTicker)
        {
            return await GetMarketByTicker(marketTicker);
        }

        public async Task<HashSet<string>> GetMarketsWithSnapshots()
        {
            return await Snapshots.AsNoTracking().Select(x => x.MarketTicker).Distinct().ToHashSetAsync();
        }

        public async Task<HashSet<string>> GetInactiveMarketsWithNoSnapshots()
        {
            return await Markets.AsNoTracking()
                .GroupJoin(Snapshots,
                    m => m.market_ticker,
                    s => s.MarketTicker,
                    (m, s) => new { Market = m, Snapshots = s })
                .Where(ms => ms.Market.status != KalshiConstants.Status_Active && !ms.Snapshots.Any())
                .Select(ms => ms.Market.market_ticker)
                .ToHashSetAsync();
        }

        public async Task<HashSet<string>> GetProcessedMarkets()
        {
            return await Markets.AsNoTracking()
                .GroupJoin(SnapshotGroups,
                    m => m.market_ticker,
                    s => s.MarketTicker,
                    (m, s) => new { Market = m, SnapshotGroups = s })
                .Where(ms => ms.Market.status != KalshiConstants.Status_Active && ms.SnapshotGroups.Any())
                .Select(ms => ms.Market.market_ticker)
                .ToHashSetAsync();
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
            foreach (MarketDTO dto in dtos)
            {
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
                    Thread.Sleep(50);
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
                catch (Exception ex)
                {
                    // For other exceptions, retry once
                    try
                    {
                        await SaveChangesAsync();
                        Thread.Sleep(50);
                    }
                    catch (Exception retryEx)
                    {
                        _logger?.LogError(retryEx, "Failed to save market {MarketTicker} after retry", dto.market_ticker);
                    }
                }
            }
        }

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

        public async Task<List<MarketDTO>> GetMarkets_cached(
            HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null,
            bool? hasMarketWatch = null, double? minimumInterestScore = null,
            DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null)
        {
            return await GetMarkets(includedStatuses, excludedStatuses, includedMarkets, excludedMarkets, hasMarketWatch, minimumInterestScore, maxInterestScoreDate, maxAPILastFetchTime);
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

        public async Task<(bool MarketFound, bool EventFound, bool SeriesFound)> GetMarketStatus_cached(string marketTicker)
        {
            return await GetMarketStatus(marketTicker);
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

        public async Task<List<TickerDTO>> GetTickers_cached(string? marketTicker = null, DateTime? loggedDate = null)
        {
            return await GetTickers(marketTicker, loggedDate);
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

        public async Task<List<CandlestickDTO>> GetCandlesticks_cached(string marketTicker, int? intervalType = null)
        {
            return await GetCandlesticks(marketTicker, intervalType);
        }

        public async Task<CandlestickDTO?> GetLastCandlestick(string marketTicker, int? intervalType)
        {
            IQueryable<Candlestick> query = Candlesticks.AsNoTracking().Where(x => x.market_ticker == marketTicker);
            if (intervalType != null)
                query = query.Where(x => x.interval_type == intervalType);
            Candlestick? candlestick = await query.OrderByDescending(x => x.end_period_datetime_utc).FirstOrDefaultAsync();
            return candlestick?.ToCandlestickDTO();
        }

        public async Task<CandlestickDTO?> GetLastCandlestick_cached(string marketTicker, int? intervalType)
        {
            return await GetLastCandlestick(marketTicker, intervalType);
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

        public async Task<List<CandlestickData>> RetrieveCandlesticksAsync_cached(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime)
        {
            return await RetrieveCandlesticksAsync(token, intervalType, marketTicker, sqlStartTime);
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
            return await query.Select(x => x.ToMarketWatchDTO()).ToHashSetAsync();
        }

        public async Task RemoveClosedWatches()
        {
            var marketWatches = await MarketWatches.Include(x => x.Market)
                .Where(x => x.Market != null && x.Market.status != KalshiConstants.Status_Active)
                .ToListAsync();

            for (int i = 0; i < marketWatches.Count; i += 1)
            {
                var batch = marketWatches.Skip(i).Take(1).ToList();
                MarketWatches.RemoveRange(batch);
                await SaveChangesAsync();
            }
        }

        public async Task<HashSet<MarketWatchDTO>> GetMarketWatches_cached(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null)
        {
            return await GetMarketWatches(marketTickers, brainLocksIncluded, brainLocksExcluded,
                brainLockIsNull, minInterestScore, maxInterestScore, maxInterestScoreDate);
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
            for (int i = 0; i < dtoRange.Count; i += 1)
            {
                var batch = dtoRange.Skip(i).Take(1).ToList();
                var marketWatch = await MarketWatches
                    .Where(x => batch.Select(y => y.market_ticker).Contains(x.market_ticker))
                    .ToListAsync();
                MarketWatches.RemoveRange(marketWatch);
                await SaveChangesAsync();
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

        public async Task<List<MarketPositionDTO>> GetMarketPositions_cached(HashSet<string>? marketTickers = null,
            bool? hasPosition = null, bool? hasRestingOrder = null)
        {
            return await GetMarketPositions(marketTickers, hasPosition, hasRestingOrder);
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

        public async Task<List<OrderDTO>> GetOrders_cached(string? marketTicker = null, string? status = null)
        {
            return await GetOrders(marketTicker, status);
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

        public async Task<List<SnapshotDTO>> GetSnapshots_cached(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null)
        {
            return await GetSnapshots(marketTicker, isValidated, startDate, endDate, MaxRecords, MaxSnapshotVersion);
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

        public async Task<SnapshotSchemaDTO?> GetSnapshotSchema_cached(int version)
        {
            return await GetSnapshotSchema(version);
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
        public async Task<BrainInstanceDTO?> GetBrainInstance(string? instanceName)
        {
            BrainInstance? instance = await BrainInstances.AsNoTracking().FirstOrDefaultAsync(x => x.BrainInstanceName == instanceName);
            return instance?.ToBrainInstanceDTO();
        }

        public async Task<List<BrainInstanceDTO>> GetBrainInstances(string? instanceName = null, bool? hasBrainLock = null)
        {
            IQueryable<BrainInstance> query = BrainInstances.AsNoTracking();
            if (instanceName != null)
                query = query.Where(x => x.BrainInstanceName == instanceName);
            if (hasBrainLock != null)
                query = query.Where(x => (x.BrainLock != null) == hasBrainLock);
            return await query.Select(x => x.ToBrainInstanceDTO()).ToListAsync();
        }

        public async Task<List<BrainInstanceDTO>> GetBrainInstances_cached(string? instanceName = null, bool? hasBrainLock = null)
        {
            return await GetBrainInstances(instanceName, hasBrainLock);
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

        public async Task<List<SnapshotGroupDTO>> GetSnapshotGroups_cached(List<string>? marketTickersToInclude = null, int? maxGroups = null)
        {
            return await GetSnapshotGroups(marketTickersToInclude, maxGroups);
        }

        public async Task<HashSet<string>> GetSnapshotGroupNames()
        {
            return await SnapshotGroups.AsNoTracking().Select(x => x.MarketTicker).Distinct().ToHashSetAsync();
        }

        public async Task<HashSet<string>> GetSnapshotGroupNames_cached()
        {
            return await GetSnapshotGroupNames();
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

        public async Task<WeightSetDTO?> GetWeightSetByStrategyName_cached(string strategyName)
        {
            return await GetWeightSetByStrategyName(strategyName);
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

        public async Task<List<MarketLiquidityStatsDTO>> GetMarketLiquidityStates_cached()
        {
            return await GetMarketLiquidityStates();
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

        public async Task<BacklashDTOs.SignalRClient?> GetSignalRClient(string clientId)
        {
            return await SignalRClients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == clientId);
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

        public async Task<List<BacklashDTOs.Data.OverseerInfo>> GetActiveOverseers()
        {
            return await OverseerInfos
                .Where(oi => oi.IsActive)
                .OrderByDescending(oi => oi.LastHeartbeat ?? oi.StartTime)
                .ToListAsync();
        }

        public async Task<BacklashDTOs.Data.OverseerInfo?> GetOverseerByHostName(string hostName)
        {
            return await OverseerInfos.FirstOrDefaultAsync(oi => oi.HostName == hostName && oi.IsActive);
        }
        private async Task SaveChangesWithRetryAsync()
        {
            var retryPolicy = Policy.Handle<SqlException>(ex => IsTransient(ex)).WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));
            await retryPolicy.ExecuteAsync(async () => await SaveChangesAsync());
        }
        private static bool IsTransient(SqlException ex)
        {
            var transientErrors = new HashSet<int> { 1205, 1222, 49918, 49919, 49920, 4060, 40197, 40501, 40613, 40143, 233, 64 };
            return transientErrors.Contains(ex.Number);
        }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

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
