using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SmokehouseDTOs;
using SmokehouseDTOs.Data;
using SmokehousePatterns.Data.Interfaces;
using System.Data;

namespace SmokehousePatterns.Data
{
    public class KalshiBotContext : DbContext, IKalshiBotContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<SeriesTag> SeriesTags { get; set; }
        public DbSet<SeriesSettlementSource> SeriesSettlementSources { get; set; }
        public DbSet<Market> Markets { get; set; }
        public DbSet<Fill> Fills { get; set; }
        public DbSet<EventLifecycle> EventLifecycles { get; set; }
        public DbSet<Ticker> Tickers { get; set; }
        public DbSet<Trade> Trades { get; set; }
        public DbSet<Candlestick> Candlesticks { get; set; }
        public DbSet<MarketWatch> MarketWatches { get; set; }
        public DbSet<MarketPosition> MarketPositions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<Snapshot> Snapshots { get; set; }
        public DbSet<SnapshotSchema> SnapshotSchemas { get; set; }
        public DbSet<BrainInstance> BrainInstances { get; set; }
        public DbSet<StratData> StratData { get; set; }
        public DbSet<MarketType> MarketTypes { get; set; }

        public DbSet<SnapshotGroup> SnapshotGroups { get; set; }

        private readonly string _connectionString;

        private string _environment;

        private readonly IConfiguration _config;

        public IConfiguration Config => _config;

        public KalshiBotContext(IConfiguration config)
        {
            // Load configuration from appsettings.json and appsettings.local.json (optional)
            _config = config;

            // Retrieve the correct connection string based on the environment
            _connectionString = _config.GetConnectionString("DefaultConnection");
        }
        public async Task<List<CandlestickData>> RetrieveCandlesticksAsync(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime)
        {
            if (token.IsCancellationRequested)
                return new List<CandlestickData>();

            using var context = new KalshiBotContext(_config);
            var rawCandlesticks = await context.Candlesticks
                .Where(x => x.interval_type == intervalType && x.market_ticker == marketTicker &&
                            x.end_period_datetime >= sqlStartTime && x.end_period_datetime <= DateTime.UtcNow)
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

        public async Task<MarketDTO?> GetMarketByTicker(string marketTicker)
        {
            var rawMarket = await Markets.Where(x => x.market_ticker == marketTicker).FirstOrDefaultAsync();
            return rawMarket.ToMarketDTO();
        }

        public async Task<EventDTO?> GetEventByTicker(string eventTicker)
        {
            var rawEvent = await Events.Where(x => x.event_ticker == eventTicker).FirstOrDefaultAsync();
            return rawEvent.ToEventDTO();
        }
        public async Task AddOrUpdateEvent(EventDTO dto)
        {
            var existingEvent = await Events.Where(x => x.event_ticker == dto.event_ticker).FirstOrDefaultAsync();
            if (existingEvent == null)
            {
                var newEvent = new Event
                {
                    event_ticker = dto.event_ticker,
                    series_ticker = dto.series_ticker,
                    title = dto.title,
                    sub_title = dto.sub_title,
                    collateral_return_type = dto.collateral_return_type,
                    mutually_exclusive = dto.mutually_exclusive,
                    category = dto.category,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };
                Events.Add(newEvent);
            }
            else
            {
                Events.Update(existingEvent);
            }
            await SaveChangesAsync();
        }

        public async Task UpdateMarketLastCandlestick(string marketTicker)
        {

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(
                "UPDATE [dbo].[t_Markets] WITH (ROWLOCK) SET LastCandlestick = ("
                + " SELECT MAX(end_period_datetime)"
                + " FROM t_Candlesticks c"
                + " WHERE c.market_ticker = m.market_ticker AND c.interval_type = 1"
                + ") FROM t_Markets m "
                + " INNER JOIN t_Candlesticks c ON m.market_ticker = c.market_ticker "
                + " WHERE m.market_ticker = @marketTicker",
                conn);
            cmd.Parameters.AddWithValue("@marketTicker", marketTicker);
            await cmd.ExecuteNonQueryAsync();



        }

        public async Task<List<MarketLiquidityStatsDTO>> GetMarketLiquidityStates()
        {
            var states = await Markets
                        .Select(m => new { m.volume_24h, m.liquidity, m.open_interest, m.yes_bid, m.no_bid}).ToListAsync();

            return states.Select(m => new MarketLiquidityStatsDTO() 
            { 
                liquidity = m.liquidity, 
                open_interest = m.open_interest, 
                volume_24h = m.volume_24h, 
                yes_bid = m.yes_bid, 
                no_bid = m.no_bid }
            ).ToList();   
        }

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
                .Property(x => x.end_period_datetime)
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken(false);

            modelBuilder.Entity<SeriesTag>()
                .ToTable("t_Series_Tags", x => x.HasTrigger("trg_t_Series_Tags_InsertLastModifiedDate"))
                .HasKey(st => new { st.series_ticker, st.tag });

            modelBuilder.Entity<SeriesTag>()
                .HasOne(st => st.Series)
                .WithMany(s => s.Tags)
                .HasForeignKey(st => st.series_ticker)
                .OnDelete(DeleteBehavior.Cascade);  // Cascade on delete

            modelBuilder.Entity<SeriesSettlementSource>()
                .ToTable("t_Series_SettlementSources", x => x.HasTrigger("trg_t_Series_SettlementSources_InsertLastModifiedDate"))
                .HasKey(st => new { st.series_ticker, st.name });

            modelBuilder.Entity<SeriesSettlementSource>()
                .HasOne(ss => ss.Series)
                .WithMany(s => s.SettlementSources)
                .HasForeignKey(ss => ss.series_ticker)
                .OnDelete(DeleteBehavior.Cascade);  // Cascade on delete

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
                .HasIndex(m => m.LastCandlestick).HasDatabaseName("IX_t_Markets_LastCandlestick").IncludeProperties(m => new { m.market_ticker, m.status });

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
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false); // Allow nulls in market_ticker

            modelBuilder.Entity<EventLifecycle>()
                .ToTable("t_feed_lifecycle")
                .HasKey(ff => new { ff.market_ticker, ff.LoggedDate });

            modelBuilder.Entity<EventLifecycle>()
                .HasIndex(ff => ff.is_deactivated);

            modelBuilder.Entity<EventLifecycle>()
                .HasIndex(ff => ff.result);

            modelBuilder.Entity<EventLifecycle>()
                .HasOne(o => o.Market)
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false); // Allow nulls in market_ticker

            modelBuilder.Entity<Ticker>()
                .ToTable("t_feed_ticker")
                .HasKey(ff => new { ff.market_ticker, ff.LoggedDate });

            modelBuilder.Entity<Ticker>()
                .HasIndex(ff => ff.market_ticker);

            modelBuilder.Entity<Ticker>()
                .HasOne(o => o.Market)
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false); // Allow nulls in market_ticker

            modelBuilder.Entity<Trade>()
                .ToTable("t_feed_trade")
                .HasKey(ff => new { ff.market_id, ff.yes_price, ff.no_price, ff.LoggedDate });

            modelBuilder.Entity<Trade>()
                .HasIndex(ff => ff.market_ticker);

            modelBuilder.Entity<Trade>()
                .HasOne(o => o.Market)
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false); // Allow nulls in market_ticker

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
                .HasIndex(o => o.CreatedTime);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Market)
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.Ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity
                .ToTable("t_LogEntries")
                .HasKey(e => e.Id); // Primary key

                entity.Property(e => e.Timestamp)
                      .IsRequired()
                      .HasColumnType("datetime");

                entity.Property(e => e.Level)
                      .IsRequired()
                      .HasMaxLength(50); // Adjust length as needed

                entity.Property(e => e.SessionIdentifier)
                      .IsRequired()
                      .HasMaxLength(5);
                entity.Property(e => e.Message)
                      .IsRequired()
                      .HasMaxLength(4000); // SQL Server VARCHAR(MAX) or adjust as needed

                entity.Property(e => e.Exception)
                      .HasMaxLength(4000); // Optional, can be null

                entity.Property(e => e.Source)
                      .IsRequired()
                      .HasMaxLength(255); // Adjust length based on your category names
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
                .Property(sg => sg.MarketTicker)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SnapshotGroup>()
                .HasOne(sg => sg.Market)
                .WithMany()
                .HasForeignKey(sg => sg.MarketTicker)
                .OnDelete(DeleteBehavior.NoAction);

        }


        Task<SeriesDTO?> IKalshiBotContext.GetSeriesByTicker(string seriesTicker)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateSeries(SeriesDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<EventDTO?> IKalshiBotContext.GetEventByTicker(string eventTicker)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateEvent(EventDTO dto)
        {

            Event? existingEvent = Events.FirstOrDefault(x => x.event_ticker == dto.event_ticker);
            if (existingEvent == null)
            {

            }



        }

        Task<MarketDTO?> IKalshiBotContext.GetMarketByTicker(string marketName)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateMarket(EventDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<List<MarketDTO>> IKalshiBotContext.GetMarkets(HashSet<string>? includedStatuses, HashSet<string>? excludedStatuses, HashSet<string>? includedMarkets, HashSet<string>? excludedMarkets, bool? hasMarketWatch, double? minimumInterestScore, DateTime? maxInterestScoreDate, DateTime? maxAPILastFetchTime)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.UpdateMarketLastCandlestick(string marketTicker)
        {
            throw new NotImplementedException();
        }

        Task<(bool MarketFound, bool EventFound, bool SeriesFound)> IKalshiBotContext.GetMarketStatus(string marketTicker)
        {
            throw new NotImplementedException();
        }

        Task<TickerDTO?> IKalshiBotContext.GetTicker(string marketTicker, DateTime? loggedDate)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateTicker(TickerDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<List<CandlestickDTO?>> IKalshiBotContext.GetCandlesticks(string marketTicker, int? intervalType)
        {
            throw new NotImplementedException();
        }

        Task<CandlestickDTO?> IKalshiBotContext.GetLastCandlestick(string marketTicker, int? intervalType)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateCandlestick(CandlestickDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<List<CandlestickData>> IKalshiBotContext.RetrieveCandlesticksAsync(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.ImportJsonCandlesticks()
        {
            throw new NotImplementedException();
        }

        Task<MarketWatchDTO> IKalshiBotContext.GetMarketWatch(List<string>? marketTickers, string? brainLock)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateMarketWatch(MarketWatchDTO dto)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.RemoveMarketWatches(HashSet<MarketWatchDTO> dto)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.RemoveOrphanedBrainLocks()
        {
            throw new NotImplementedException();
        }

        Task<MarketPositionDTO?> IKalshiBotContext.GetMarketPosition(string marketTicker)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateMarketPosition(MarketPositionDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<OrderDTO?> IKalshiBotContext.GetOrders(string? marketTicker, string? status)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateOrder(OrderDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<SnapshotDTO?> IKalshiBotContext.GetSnapshots(string? marketTicker, bool? isValidated, DateTime? startDate, DateTime? endDate)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateSnapshot(SnapshotDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<SnapshotSchemaDTO?> IKalshiBotContext.GetSnapshotSchemas(string? version)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddSnapshotSchema(SnapshotSchemaDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<BrainInstanceDTO?> IKalshiBotContext.GetBrainInstance(string? instanceName, bool? hasBrainLock)
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddOrUpdateBrainInstance(BrainInstanceDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<SnapshotGroupDTO?> IKalshiBotContext.GetSnapshotGroup(string? marketTicker)
        {
            throw new NotImplementedException();
        }

        Task<HashSet<string>> IKalshiBotContext.GetSnapshotGroupNames()
        {
            throw new NotImplementedException();
        }

        Task IKalshiBotContext.AddLogEntry(LogEntryDTO dto)
        {
            throw new NotImplementedException();
        }

        Task<List<MarketLiquidityStatsDTO>> IKalshiBotContext.GetMarketLiquidityStates()
        {
            throw new NotImplementedException();
        }
    }
}
