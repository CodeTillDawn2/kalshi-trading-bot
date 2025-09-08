using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;

namespace TradingSimulator.ML
{
    public sealed class FlattenPriceConfig
    {
        public int HorizonMinutes { get; set; } = 15;     // look-ahead window to find the peak
        public double MinSlopeYes { get; set; } = 0.0;    // require rising slope OR...
        public double MinTopVelYes { get; set; } = 0.0;   // ...strong top velocity to mark a "climb" row
        public bool UseYesSideOnly { get; set; } = true;  // set false to include No-side features too
        public bool LabelIsDelta { get; set; } = true;    // learn delta = future_peak - current
        public int MinRowsToTrain { get; set; } = 200;
        public int Seed { get; set; } = 1337;

        // LightGBM knobs (good defaults)
        public int NumberOfLeaves { get; set; } = 64;
        public int MinExamplesPerLeaf { get; set; } = 10;
        public int NumIterations { get; set; } = 300;
        public double LearningRate { get; set; } = 0.05;
        public double FeatureFraction { get; set; } = 0.9;
        public double BaggingFraction { get; set; } = 0.8;
        public int BaggingFrequency { get; set; } = 1;
    }

    public sealed class RegressionSummary
    {
        public int TrainRows { get; init; }
        public double MAE { get; init; }
        public double RMSE { get; init; }
        public double R2 { get; init; }
    }

    public sealed class FlattenPrediction
    {
        public DateTime SnapshotTime { get; init; }
        public float CurrentYes { get; init; }
        public float PredictedDelta { get; init; }
        public float PredictedFlatten { get; init; } // current + delta, clamped [0,100]
    }

    // ===== PUBLIC ENTRYPOINTS (call these from button handlers) =====
    public static class FlattenPriceQuickstart
    {
        public static RegressionSummary TrainFromCsv(string csvPath, string modelPath, FlattenPriceConfig? cfg = null)
        {
            cfg ??= new FlattenPriceConfig();
            var ml = new MLContext(seed: cfg.Seed);

            var rows = CsvLoader.Load(csvPath);
            var examples = BuildExamples(rows, cfg);
            if (examples.Count < cfg.MinRowsToTrain)
                throw new InvalidOperationException($"Not enough climb rows to train: {examples.Count} < {cfg.MinRowsToTrain}");

            var shuffled = examples.OrderBy(_ => Guid.NewGuid()).ToList();
            int testN = Math.Max(1, (int)Math.Round(0.2 * shuffled.Count));
            var test = shuffled.Take(testN);
            var train = shuffled.Skip(testN);

            var trainDv = ml.Data.LoadFromEnumerable(train);
            var testDv = ml.Data.LoadFromEnumerable(test);

            var pipeline =
                ml.Transforms.Concatenate("Features", FeatureColumns(cfg))
                  .Append(ml.Transforms.NormalizeMinMax("Features"))
                  .Append(ml.Regression.Trainers.LightGbm(new LightGbmRegressionTrainer.Options
                  {
                      NumberOfLeaves = cfg.NumberOfLeaves,
                      MinimumExampleCountPerLeaf = cfg.MinExamplesPerLeaf,
                      NumberOfIterations = cfg.NumIterations,
                      LearningRate = cfg.LearningRate

                  }));

            var model = pipeline.Fit(trainDv);
            var scored = model.Transform(testDv);
            var m = ml.Regression.Evaluate(scored, labelColumnName: "Label", scoreColumnName: "Score");

            ml.Model.Save(model, trainDv.Schema, modelPath);

            return new RegressionSummary
            {
                TrainRows = examples.Count - testN,
                MAE = m.MeanAbsoluteError,
                RMSE = m.RootMeanSquaredError,
                R2 = m.RSquared
            };
        }

        public static RegressionSummary EvaluateFromCsv(string csvPath, string modelPath, FlattenPriceConfig? cfg = null)
        {
            cfg ??= new FlattenPriceConfig();
            var ml = new MLContext(seed: cfg.Seed);

            var rows = CsvLoader.Load(csvPath);
            var examples = BuildExamples(rows, cfg);
            if (examples.Count < cfg.MinRowsToTrain)
                throw new InvalidOperationException($"Not enough climb rows to evaluate: {examples.Count} < {cfg.MinRowsToTrain}");

            var dv = ml.Data.LoadFromEnumerable(examples);
            var model = ml.Model.Load(modelPath, out _);
            var scored = model.Transform(dv);
            var m = ml.Regression.Evaluate(scored, labelColumnName: nameof(Example.Label), scoreColumnName: "Score");

            return new RegressionSummary { TrainRows = examples.Count, MAE = m.MeanAbsoluteError, RMSE = m.RootMeanSquaredError, R2 = m.RSquared };
        }

        public static FlattenPrediction PredictLatestFromCsv(string csvPath, string modelPath, FlattenPriceConfig? cfg = null)
        {
            cfg ??= new FlattenPriceConfig();
            var ml = new MLContext(seed: cfg.Seed);

            var rows = CsvLoader.Load(csvPath);
            if (rows.Count == 0) throw new InvalidOperationException("CSV contained no rows.");
            var last = rows[^1];

            var ex = ToExample(last, cfg, label: 0f);
            var model = ml.Model.Load(modelPath, out _);
            var engine = ml.Model.CreatePredictionEngine<Example, ScoreOut>(model);

            var score = engine.Predict(ex).Score; // predicted delta if LabelIsDelta=true
            var cur = last.BestYesBid;
            var delta = cfg.LabelIsDelta ? score : (score - cur);
            var flat = Math.Clamp(cur + delta, 0f, 100f);

            return new FlattenPrediction
            {
                SnapshotTime = last.Timestamp,
                CurrentYes = cur,
                PredictedDelta = delta,
                PredictedFlatten = flat
            };
        }

        // ===== INTERNALS =====

        private sealed class ScoreOut { public float Score { get; set; } }

        private static string[] FeatureColumns(FlattenPriceConfig cfg)
        {
            var cols = new List<string>
            {
                nameof(Example.CurYesBid),
                nameof(Example.VelTopYes),
                nameof(Example.LevelsTopYes),
                nameof(Example.VelBottomYes),
                nameof(Example.LevelsBottomYes),
                nameof(Example.YesSpread),
                nameof(Example.DepthAtBestYesBid),
                nameof(Example.YesBidSlopePerMinute)
            };
            if (!cfg.UseYesSideOnly)
            {
                cols.Add(nameof(Example.VelTopNo));
                cols.Add(nameof(Example.LevelsTopNo));
                cols.Add(nameof(Example.VelBottomNo));
                cols.Add(nameof(Example.LevelsBottomNo));
                cols.Add(nameof(Example.NoBidSlopePerMinute));
            }
            return cols.ToArray();
        }

        private static List<Example> BuildExamples(List<CsvRow> rows, FlattenPriceConfig cfg)
        {
            var ex = new List<Example>(rows.Count);
            if (rows.Count < 5) return ex;

            int H = Math.Max(1, cfg.HorizonMinutes);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                bool climbing = r.YesBidSlopePerMinute > (float)cfg.MinSlopeYes || r.VelTopYes > (float)cfg.MinTopVelYes;
                if (!climbing) continue;

                int end = Math.Min(rows.Count - 1, i + H);
                float peak = r.BestYesBid;
                for (int j = i; j <= end; j++)
                    if (rows[j].BestYesBid > peak) peak = rows[j].BestYesBid;

                float label = cfg.LabelIsDelta ? (peak - r.BestYesBid) : peak;
                if (cfg.LabelIsDelta && label < 0f) continue; // skip weird negatives
                ex.Add(ToExample(r, cfg, label));
            }
            return ex;
        }

        private static Example ToExample(CsvRow r, FlattenPriceConfig cfg, float label)
        {
            var e = new Example
            {
                Label = label,
                CurYesBid = r.BestYesBid,
                VelTopYes = r.VelTopYes,
                LevelsTopYes = r.LevelsTopYes,
                VelBottomYes = r.VelBottomYes,
                LevelsBottomYes = r.LevelsBottomYes,
                YesSpread = r.YesSpread,
                DepthAtBestYesBid = r.DepthAtBestYesBid,
                YesBidSlopePerMinute = r.YesBidSlopePerMinute,
                VelTopNo = r.VelTopNo,
                LevelsTopNo = r.LevelsTopNo,
                VelBottomNo = r.VelBottomNo,
                LevelsBottomNo = r.LevelsBottomNo,
                NoBidSlopePerMinute = r.NoBidSlopePerMinute
            };
            if (cfg.UseYesSideOnly)
            {
                e.VelTopNo = 0f; e.LevelsTopNo = 0f; e.VelBottomNo = 0f; e.LevelsBottomNo = 0f; e.NoBidSlopePerMinute = 0f;
            }
            return e;
        }
    }

    internal sealed class Example
    {
        [LoadColumn(0)] public float Label { get; set; }
        [LoadColumn(1)] public float CurYesBid { get; set; }
        [LoadColumn(2)] public float VelTopYes { get; set; }
        [LoadColumn(3)] public float LevelsTopYes { get; set; }
        [LoadColumn(4)] public float VelBottomYes { get; set; }
        [LoadColumn(5)] public float LevelsBottomYes { get; set; }
        [LoadColumn(6)] public float YesSpread { get; set; }
        [LoadColumn(7)] public float DepthAtBestYesBid { get; set; }
        [LoadColumn(8)] public float YesBidSlopePerMinute { get; set; }
        [LoadColumn(9)] public float VelTopNo { get; set; }
        [LoadColumn(10)] public float LevelsTopNo { get; set; }
        [LoadColumn(11)] public float VelBottomNo { get; set; }
        [LoadColumn(12)] public float LevelsBottomNo { get; set; }
        [LoadColumn(13)] public float NoBidSlopePerMinute { get; set; }
    }

    internal sealed class CsvRow
    {
        public DateTime Timestamp;
        public float BestYesBid, BestNoBid;
        public float VelTopYes, VelBottomYes, VelTopNo, VelBottomNo;
        public int LevelsTopYes, LevelsBottomYes, LevelsTopNo, LevelsBottomNo;
        public float YesSpread, DepthAtBestYesBid, YesBidSlopePerMinute, NoBidSlopePerMinute;
    }

    internal static class CsvLoader
    {
        public static List<CsvRow> Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("CSV not found", path);
            using var p = new TextFieldParser(path)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            };
            p.SetDelimiters(",");
            if (p.EndOfData) return new List<CsvRow>();
            var header = Normalize(p.ReadFields() ?? Array.Empty<string>());
            var ix = Index(header);

            var rows = new List<CsvRow>(4096);
            while (!p.EndOfData)
            {
                var f = p.ReadFields();
                if (f == null || f.Length == 0) continue;
                var r = Normalize(f);

                rows.Add(new CsvRow
                {
                    Timestamp = ParseDate(Get(r, ix, "timestamp")),
                    BestYesBid = F(Get(r, ix, "bestyesbid")),
                    BestNoBid  = F(Get(r, ix, "bestnobid")),
                    VelTopYes = F(Get(r, ix, "velocityperminute_top_yes_bid")),
                    LevelsTopYes = (int)F(Get(r, ix, "levelcount_top_yes_bid")),
                    VelBottomYes = F(Get(r, ix, "velocityperminute_bottom_yes_bid")),
                    LevelsBottomYes = (int)F(Get(r, ix, "levelcount_bottom_yes_bid")),
                    VelTopNo = F(Get(r, ix, "velocityperminute_top_no_bid")),
                    LevelsTopNo = (int)F(Get(r, ix, "levelcount_top_no_bid")),
                    VelBottomNo = F(Get(r, ix, "velocityperminute_bottom_no_bid")),
                    LevelsBottomNo = (int)F(Get(r, ix, "levelcount_bottom_no_bid")),
                    YesSpread = F(Get(r, ix, "yesspread")),
                    DepthAtBestYesBid = F(Get(r, ix, "depthatbestyesbid")),
                    YesBidSlopePerMinute = F(Get(r, ix, "yesbidslopeperminute")),
                    NoBidSlopePerMinute = F(Get(r, ix, "nobidslopeperminute"))
                });
            }
            rows.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return rows;
        }

        private static Dictionary<string, int> Index(string[] h)
        {
            var m = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < h.Length; i++) if (!m.ContainsKey(h[i])) m[h[i]] = i;
            void alias(string key, params string[] alts)
            {
                foreach (var a in alts) if (!m.ContainsKey(key) && m.TryGetValue(a, out var j)) m[key] = j;
            }
            alias("timestamp", "time", "date", "datetime");
            alias("bestyesbid", "best_yes_bid", "yesbestbid", "yesbid", "bestyes");
            alias("bestnobid", "best_no_bid", "nobestbid", "nobid", "bestno");

            alias("velocityperminute_top_yes_bid", "vel_top_yes", "top_yes_velocity", "vpm_top_yes");
            alias("levelcount_top_yes_bid", "levels_top_yes", "top_yes_levels");
            alias("velocityperminute_bottom_yes_bid", "vel_bottom_yes", "bottom_yes_velocity", "vpm_bottom_yes");
            alias("levelcount_bottom_yes_bid", "levels_bottom_yes", "bottom_yes_levels");

            alias("velocityperminute_top_no_bid", "vel_top_no", "top_no_velocity", "vpm_top_no");
            alias("levelcount_top_no_bid", "levels_top_no", "top_no_levels");
            alias("velocityperminute_bottom_no_bid", "vel_bottom_no", "bottom_no_velocity", "vpm_bottom_no");
            alias("levelcount_bottom_no_bid", "levels_bottom_no", "bottom_no_levels");

            alias("yesspread", "yes_spread", "spread_yes");
            alias("depthatbestyesbid", "depth_yes_best", "best_yes_depth");
            alias("yesbidslopeperminute", "slope_yes", "yes_slope_per_min", "yes_slope");
            alias("nobidslopeperminute", "slope_no", "no_slope_per_min", "no_slope");
            return m;
        }

        private static string[] Normalize(string[] raw)
        {
            var n = new string[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                var s = (raw[i] ?? "").Trim().ToLowerInvariant();
                n[i] = new string(s.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());
            }
            return n;
        }

        private static string Get(string[] row, Dictionary<string, int> ix, string key)
            => ix.TryGetValue(key, out var i) && i < row.Length ? row[i] : "";

        private static float F(string s)
            => float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0f;

        private static DateTime ParseDate(string s)
        {
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                return dt;
            if (long.TryParse(s, out var unix)) return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            return DateTime.MinValue;
        }
    }
}
