// MLTrainingTests.cs
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using BacklashBot.Services;
using BacklashBot.Services.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using System.Text;
using TradingStrategies.Configuration;

namespace TradingSimulator.ML
{
    [TestFixture]
    public sealed class MLTrainingTests
    {
        private IServiceProvider _sp;
        private IServiceScopeFactory _scopeFactory;
        private ITradingSnapshotService _snapshotService;
        private IOptions<SnapshotConfig> _snapOpts;
        private IOptions<TradingConfig> _tradeOpts;

        private string _outDir;

        [SetUp]
        public void Setup()
        {
            // Locate BacklashBot/appsettings.local.json exactly like your other tests
            var basePath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: false)
                .Build();

            // DI: EF context for real snapshot fetching
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddDbContext<KalshiBotContext>(options => options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
            services.AddScoped<IKalshiBotContext>(sp => sp.GetRequiredService<KalshiBotContext>());
            _sp = services.BuildServiceProvider();
            _scopeFactory = _sp.GetRequiredService<IServiceScopeFactory>();

            // Options from config
            _snapOpts  = Options.Create(config.GetSection("Snapshots").Get<SnapshotConfig>());
            _tradeOpts = Options.Create(config.GetSection("TradingConfig").Get<TradingConfig>());

            // Snapshot loader (same implementation you use elsewhere)
            _snapshotService = new TradingSnapshotService(
                NullLogger<ITradingSnapshotService>.Instance, _snapOpts, _tradeOpts, _scopeFactory);

            _outDir = Path.Combine("..", "..", "..", "..", "..", "TestingOutput");
            Directory.CreateDirectory(_outDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (_sp is IDisposable d) d.Dispose();
        }

        // --- Helper: pull several real markets and return consolidated MarketSnapshots per market ---
        private async Task<List<IReadOnlyList<MarketSnapshot>>> LoadRealMarketsAsync(
            int maxMarkets = 5, double minRecordedHours = 1.0)
        {
            var results = new List<IReadOnlyList<MarketSnapshot>>(maxMarkets);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            // Get all snapshot groups and filter to sufficiently long recordings
            var groups = await db.GetSnapshotGroups_cached().ConfigureAwait(false);
            var good = groups.Where(g => (g.EndTime - g.StartTime).TotalHours >= minRecordedHours).ToList();

            // Choose up to N markets with the most coverage
            var markets = good
                .GroupBy(g => g.MarketTicker)
                .OrderByDescending(grp => grp.Sum(x => (x.EndTime - x.StartTime).TotalMinutes))
                .Take(maxMarkets)
                .Select(grp => new { Ticker = grp.Key, Groups = grp.OrderBy(x => x.StartTime).ToList() })
                .ToList();

            foreach (var m in markets)
            {
                var allDto = new List<SnapshotDTO>();
                foreach (var g in m.Groups)
                {
                    var snaps = await db.GetSnapshots_cached(marketTicker: g.MarketTicker, startDate: g.StartTime, endDate: g.EndTime).ConfigureAwait(false);
                    allDto.AddRange(snaps);
                }

                allDto = allDto.OrderBy(x => x.SnapshotDate).ToList();

                // Convert SnapshotDTO -> MarketSnapshot using your snapshot loader
                var cache = await _snapshotService.LoadManySnapshots(allDto).ConfigureAwait(false);
                var ms = cache.SelectMany(kvp => kvp.Value)
                              .Where(s => s != null && s.Timestamp > DateTime.MinValue)
                              .OrderBy(s => s.Timestamp)
                              .ToList();

                if (ms.Count > 50) // avoid tiny/noisy series
                    results.Add(ms);
            }

            return results;
        }

        // --- Helper: write a CSV that FlattenPriceQuickstart understands ---
        private string WriteCsvForQuickstart(IEnumerable<IReadOnlyList<MarketSnapshot>> markets, string fileName)
        {
            var path = Path.Combine(_outDir, fileName);
            using var sw = new StreamWriter(path, false, new UTF8Encoding(false));

            // Header (matches CsvLoader mapping in FlattenPriceQuickstart.cs)
            sw.WriteLine(string.Join(",",
                "Timestamp",
                "BestYesBid", "BestNoBid",
                "VelocityPerMinute_Top_Yes_Bid", "LevelCount_Top_Yes_Bid",
                "VelocityPerMinute_Bottom_Yes_Bid", "LevelCount_Bottom_Yes_Bid",
                "VelocityPerMinute_Top_No_Bid", "LevelCount_Top_No_Bid",
                "VelocityPerMinute_Bottom_No_Bid", "LevelCount_Bottom_No_Bid",
                "YesSpread", "DepthAtBestYesBid", "YesBidSlopePerMinute_Short", "NoBidSlopePerMinute_Short"
                , "YesBidSlopePerMinute_Medium", "NoBidSlopePerMinute_Medium"));

            foreach (var series in markets)
            {
                foreach (var s in series)
                {
                    // Null-safe numeric access (assume zero if missing)
                    float F(double? v) => v.HasValue ? (float)v.Value : 0f;

                    sw.WriteLine(string.Join(",",
                        s.Timestamp.ToString("o"),
                        s.BestYesBid,
                        s.BestNoBid,
                        F(s.VelocityPerMinute_Top_Yes_Bid), s.LevelCount_Top_Yes_Bid,
                        F(s.VelocityPerMinute_Bottom_Yes_Bid), s.LevelCount_Bottom_Yes_Bid,
                        F(s.VelocityPerMinute_Top_No_Bid), s.LevelCount_Top_No_Bid,
                        F(s.VelocityPerMinute_Bottom_No_Bid), s.LevelCount_Bottom_No_Bid,
                        s.YesSpread,
                        s.DepthAtBestYesBid,
                        F(s.YesBidSlopePerMinute_Short),
                        F(s.NoBidSlopePerMinute_Short),
                        F(s.YesBidSlopePerMinute_Medium),
                        F(s.NoBidSlopePerMinute_Medium)
                        ));
                }
            }
            return path;
        }

        [Test]
        [Category("ML")]
        public async Task Train_Evaluate_Flatten_Model_On_Real_Data()
        {
            // 1) Load multiple markets from your real DB
            var markets = await LoadRealMarketsAsync(minRecordedHours: 1.0);

            // 2) Write a training CSV for the quickstart
            var csvPath = WriteCsvForQuickstart(markets, "ml_training_snapshots.csv");

            // 3) Train + Evaluate using the LightGBM quickstart
            var modelPath = Path.Combine(_outDir, "flatten_price_model.zip");
            var cfg = new FlattenPriceConfig
            {
                HorizonMinutes = 15,     // lookahead for peak
                MinSlopeYes = 0.0,       // include all rows (tune later)
                MinTopVelYes = 0.0,
                UseYesSideOnly = true,   // start simple
                LabelIsDelta = true,     // learn delta = peak - current
                MinRowsToTrain = 200,    // safety
                Seed = 1337,
                NumberOfLeaves = 64,
                MinExamplesPerLeaf = 10,
                NumIterations = 300,
                LearningRate = 0.05
            };

            var summary = FlattenPriceQuickstart.TrainFromCsv(csvPath, modelPath, cfg);
            TestContext.Out.WriteLine($"TrainRows={summary.TrainRows}, MAE={summary.MAE:F3}, RMSE={summary.RMSE:F3}, R2={summary.R2:F3}");

            // 4) Sanity: Evaluate on the same CSV (or point at another CSV if you prefer)
            var eval = FlattenPriceQuickstart.EvaluateFromCsv(csvPath, modelPath, cfg);
            TestContext.Out.WriteLine($"[Eval] MAE={eval.MAE:F3}, RMSE={eval.RMSE:F3}, R2={eval.R2:F3}");

            // 5) Optional: Predict for the latest row
            var latest = FlattenPriceQuickstart.PredictLatestFromCsv(csvPath, modelPath, cfg);
            TestContext.Out.WriteLine($"Latest: cur={latest.CurrentYes:F2}, ?={latest.PredictedDelta:F2}, flat={latest.PredictedFlatten:F2}");
        }
    }
}
