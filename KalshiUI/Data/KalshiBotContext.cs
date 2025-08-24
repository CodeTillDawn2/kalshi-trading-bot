using KalshiUI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmokehousePatterns;
using System.Data;

namespace KalshiUI.Data
{
    public class KalshiBotContext : DbContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<Series> Series { get; set; }
        public DbSet<SeriesTag> SeriesTags { get; set; }
        public DbSet<SeriesSettlementSource> SeriesSettlementSources { get; set; }
        public DbSet<Market> Markets { get; set; }
        public DbSet<Fill> Fills { get; set; }
        public DbSet<Lifecycle> Lifecycles { get; set; }
        public DbSet<OrderbookSnapshot> OrderbookSnapshots { get; set; }
        public DbSet<Orderbook> Orderbooks { get; set; }
        public DbSet<Ticker> Tickers { get; set; }
        public DbSet<Trade> Trades { get; set; }
        public DbSet<Candlestick> Candlesticks { get; set; }
        public DbSet<MarketWatch> MarketWatches { get; set; }

        private readonly string _connectionString;

        private string _environment;

        private readonly IConfigurationRoot _config;

        public IConfiguration Config { get { return _config; } }

        public KalshiBotContext()
        {
            // Load configuration from appsettings.json and appsettings.local.json (optional)
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

            _config = builder.Build();

            // Retrieve the environment from appsettings.json
            _environment = ConfigService.GetEnvironment();

            // Retrieve the correct connection string based on the environment
            _connectionString = _config.GetConnectionString(_environment);  // Get the connection string from "dev" or "prd"
        }

        public async Task ImportJsonCandlesticks()
        {
            // Trigger the SQL import for this market's files immediately
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("dbo.sp_ImportCandlesticksFromFile", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public MarketProcessor ReturnMarketProcessor()
        {
            return new MarketProcessor(_connectionString);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Use the correct connection string
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

            modelBuilder.Entity<Orderbook>()
                .ToTable("t_Orderbook", x => x.HasTrigger("trg_Orderbook_LastModifiedDate"))
                .HasKey(e => new { e.market_ticker, e.price, e.side });

            modelBuilder.Entity<Orderbook>()
                .HasIndex(e => e.LastModifiedDate);

            modelBuilder.Entity<Orderbook>()
                .HasOne(e => e.Market)
                .WithMany(s => s.Orderbooks)
                .HasForeignKey(e => e.market_ticker)
                .HasPrincipalKey(s => s.market_ticker)
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
                .ToTable("t_Candlesticks", x => x.HasTrigger("trg_InsertCandlestickDetails"))
                .HasKey(x => new { x.market_ticker, x.interval_type, x.end_period_ts });

            modelBuilder.Entity<Candlestick>()
                .HasIndex(x => new { x.year, x.month, x.day, x.hour, x.minute });

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
                .HasKey(ss => ss.series_ticker);

            modelBuilder.Entity<SeriesSettlementSource>()
                .HasOne(ss => ss.Series)
                .WithMany(s => s.SettlementSources)
                .HasForeignKey(ss => ss.series_ticker)
                .OnDelete(DeleteBehavior.Cascade);  // Cascade on delete

            modelBuilder.Entity<MarketWatch>()
                .ToTable("t_MarketWatches")
                .HasKey(m => m.market_ticker);

            modelBuilder.Entity<Market>()
                .ToTable("t_Markets", x => x.HasTrigger("trg_Markets_LastModifiedDate"))
                .HasKey(m => m.market_ticker);

            modelBuilder.Entity<Market>()
                .HasIndex(m => m.event_ticker);

            modelBuilder.Entity<Market>()
                .HasIndex(m => m.market_type);

            modelBuilder.Entity<Market>()
                .HasIndex(m => m.result);

            modelBuilder.Entity<Market>()
                .HasIndex(m => m.status);

            modelBuilder.Entity<Market>()
                .Property(m => m.CreatedDate)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Market>()
                .HasOne(m => m.Event)
                .WithMany(e => e.Markets)
                .HasForeignKey(m => m.event_ticker)
                .HasPrincipalKey(e => e.event_ticker)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Fill>()
                .ToTable("t_feed_fill")
                .HasKey(ff => new { ff.market_id, ff.trade_id, ff.order_id });

            modelBuilder.Entity<Fill>()
                .HasIndex(ff => ff.action);

            modelBuilder.Entity<Fill>()
                .HasIndex(ff => ff.sid);

            modelBuilder.Entity<Fill>()
                .HasOne(o => o.Market)
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false); // Allow nulls in market_ticker

            modelBuilder.Entity<Lifecycle>()
                .ToTable("t_feed_lifecycle")
                .HasKey(ff => new { ff.market_ticker, ff.LoggedDate });

            modelBuilder.Entity<Lifecycle>()
                .HasIndex(ff => ff.is_deactivated);

            modelBuilder.Entity<Lifecycle>()
                .HasIndex(ff => ff.result);

            modelBuilder.Entity<Lifecycle>()
                .HasOne(o => o.Market)
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false); // Allow nulls in market_ticker

            modelBuilder.Entity<OrderbookSnapshot>()
                .ToTable("t_feed_orderbook")
                .HasKey(ff => new { ff.market_id, ff.kalshi_seq, ff.offer_type, ff.price });

            modelBuilder.Entity<OrderbookSnapshot>()
                .HasIndex(ff => ff.market_ticker);

            modelBuilder.Entity<OrderbookSnapshot>()
                .HasOne(o => o.Market)
                .WithMany() // No navigation property on Market
                .HasForeignKey(o => o.market_ticker)
                .HasPrincipalKey(m => m.market_ticker)
                .IsRequired(false); // Allow nulls in market_ticker

            modelBuilder.Entity<Ticker>()
                .ToTable("t_feed_ticker")
                .HasKey(ff => new { ff.market_id, ff.LoggedDate });

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

        }
    }
}
