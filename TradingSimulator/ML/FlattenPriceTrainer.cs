using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;

namespace TradingSimulator.ML
{
    /// <summary>
    /// Configuration class for the FlattenPriceTrainer, containing parameters for training, feature selection, and model hyperparameters.
    /// This class defines the settings used to control the machine learning pipeline for predicting price flattening in trading markets.
    /// </summary>
    public sealed class FlattenPriceConfig
    {
        /// <summary>
        /// The number of minutes to look ahead from the current snapshot to find the peak price for labeling.
        /// This defines the prediction horizon for the regression model.
        /// </summary>
        public int HorizonMinutes { get; set; } = 15;

        /// <summary>
        /// The minimum slope required for the Yes bid to be considered climbing. Used in conjunction with MinTopVelYes.
        /// </summary>
        public double MinSlopeYes { get; set; } = 0.0;

        /// <summary>
        /// The minimum top velocity required for the Yes bid to be considered climbing. Used in conjunction with MinSlopeYes.
        /// </summary>
        public double MinTopVelYes { get; set; } = 0.0;

        /// <summary>
        /// Whether to use only Yes-side features or include No-side features as well.
        /// When true, No-side features are zeroed out in the training examples.
        /// </summary>
        public bool UseYesSideOnly { get; set; } = true;

        /// <summary>
        /// Whether the label should be the delta (future_peak - current) or the absolute peak price.
        /// When true, the model learns to predict the price change; when false, it predicts the absolute peak.
        /// </summary>
        public bool LabelIsDelta { get; set; } = true;

        /// <summary>
        /// The minimum number of training examples required to proceed with training or evaluation.
        /// </summary>
        public int MinRowsToTrain { get; set; } = 200;

        /// <summary>
        /// The random seed used for ML.NET context to ensure reproducible results.
        /// </summary>
        public int Seed { get; set; } = 1337;

        // LightGBM hyperparameters (good defaults)

        /// <summary>
        /// The maximum number of leaves in each tree for the LightGBM model.
        /// </summary>
        public int NumberOfLeaves { get; set; } = 64;

        /// <summary>
        /// The minimum number of examples required in a leaf node.
        /// </summary>
        public int MinExamplesPerLeaf { get; set; } = 10;

        /// <summary>
        /// The number of boosting iterations (trees) to perform.
        /// </summary>
        public int NumIterations { get; set; } = 300;

        /// <summary>
        /// The learning rate (shrinkage) for each boosting step.
        /// </summary>
        public double LearningRate { get; set; } = 0.05;

        /// <summary>
        /// The fraction of features to consider for each tree split.
        /// </summary>
        public double FeatureFraction { get; set; } = 0.9;

        /// <summary>
        /// The fraction of data to use for bagging (subsampling).
        /// </summary>
        public double BaggingFraction { get; set; } = 0.8;

        /// <summary>
        /// The frequency of bagging (how often to perform bagging).
        /// </summary>
        public int BaggingFrequency { get; set; } = 1;
    }

    /// <summary>
    /// Summary of regression model performance metrics after training or evaluation.
    /// </summary>
    public sealed class RegressionSummary
    {
        /// <summary>
        /// The number of training examples used in the model.
        /// </summary>
        public int TrainRows { get; init; }

        /// <summary>
        /// Mean Absolute Error of the model's predictions.
        /// </summary>
        public double MAE { get; init; }

        /// <summary>
        /// Root Mean Squared Error of the model's predictions.
        /// </summary>
        public double RMSE { get; init; }

        /// <summary>
        /// R-squared coefficient of determination, indicating the proportion of variance explained by the model.
        /// </summary>
        public double R2 { get; init; }
    }

    /// <summary>
    /// Prediction result containing the predicted price flattening information.
    /// </summary>
    public sealed class FlattenPrediction
    {
        /// <summary>
        /// The timestamp of the snapshot used for prediction.
        /// </summary>
        public DateTime SnapshotTime { get; init; }

        /// <summary>
        /// The current Yes bid price at the time of the snapshot.
        /// </summary>
        public float CurrentYes { get; init; }

        /// <summary>
        /// The predicted delta (change) in price, or the predicted absolute peak if LabelIsDelta is false.
        /// </summary>
        public float PredictedDelta { get; init; }

        /// <summary>
        /// The predicted flattened price, calculated as current + delta and clamped to [0,100].
        /// </summary>
        public float PredictedFlatten { get; init; }
    }

    // ===== PUBLIC ENTRYPOINTS (call these from button handlers) =====
    /// <summary>
    /// Main entry point class for training and using the price flattening prediction model.
    /// Provides static methods for training from CSV data, evaluating models, and making predictions.
    /// </summary>
    public static class FlattenPriceQuickstart
    {
        /// <summary>
        /// Trains a new LightGBM regression model from CSV data and saves it to the specified path.
        /// </summary>
        /// <param name="csvPath">Path to the CSV file containing historical market data.</param>
        /// <param name="modelPath">Path where the trained model should be saved.</param>
        /// <param name="cfg">Optional configuration parameters. If null, default configuration is used.</param>
        /// <returns>A summary of the training performance metrics.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there are insufficient training examples.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the CSV file cannot be found.</exception>
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

        /// <summary>
        /// Evaluates an existing model against CSV data and returns performance metrics.
        /// </summary>
        /// <param name="csvPath">Path to the CSV file containing historical market data.</param>
        /// <param name="modelPath">Path to the trained model file.</param>
        /// <param name="cfg">Optional configuration parameters. If null, default configuration is used.</param>
        /// <returns>A summary of the evaluation performance metrics.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there are insufficient evaluation examples.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the CSV or model file cannot be found.</exception>
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

        /// <summary>
        /// Makes a prediction for the latest data point in the CSV file using a trained model.
        /// </summary>
        /// <param name="csvPath">Path to the CSV file containing historical market data.</param>
        /// <param name="modelPath">Path to the trained model file.</param>
        /// <param name="cfg">Optional configuration parameters. If null, default configuration is used.</param>
        /// <returns>A prediction result containing the predicted price flattening information.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the CSV contains no rows.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the CSV or model file cannot be found.</exception>
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

        /// <summary>
        /// Internal class for holding prediction scores from the ML model.
        /// </summary>
        private sealed class ScoreOut { public float Score { get; set; } }

        /// <summary>
        /// Determines the feature columns to include in the model based on configuration.
        /// </summary>
        /// <param name="cfg">The configuration specifying which features to include.</param>
        /// <returns>An array of feature column names.</returns>
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

        /// <summary>
        /// Builds training examples from CSV rows by identifying climbing periods and calculating labels.
        /// </summary>
        /// <param name="rows">The list of CSV rows containing market data.</param>
        /// <param name="cfg">The configuration parameters for example generation.</param>
        /// <returns>A list of training examples ready for ML training.</returns>
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

        /// <summary>
        /// Converts a CSV row to a training example, applying configuration-based feature filtering.
        /// </summary>
        /// <param name="r">The CSV row to convert.</param>
        /// <param name="cfg">The configuration parameters.</param>
        /// <param name="label">The target label for this example.</param>
        /// <returns>A training example ready for ML processing.</returns>
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

    /// <summary>
    /// Data structure representing a single training example for the ML model.
    /// Contains market features and the target label for supervised learning.
    /// </summary>
    internal sealed class Example
    {
        /// <summary>
        /// The target label (price delta or absolute peak) for this training example.
        /// </summary>
        [LoadColumn(0)] public float Label { get; set; }

        /// <summary>
        /// The current Yes bid price.
        /// </summary>
        [LoadColumn(1)] public float CurYesBid { get; set; }

        /// <summary>
        /// Velocity at the top of the Yes bid order book.
        /// </summary>
        [LoadColumn(2)] public float VelTopYes { get; set; }

        /// <summary>
        /// Number of levels at the top of the Yes bid order book.
        /// </summary>
        [LoadColumn(3)] public float LevelsTopYes { get; set; }

        /// <summary>
        /// Velocity at the bottom of the Yes bid order book.
        /// </summary>
        [LoadColumn(4)] public float VelBottomYes { get; set; }

        /// <summary>
        /// Number of levels at the bottom of the Yes bid order book.
        /// </summary>
        [LoadColumn(5)] public float LevelsBottomYes { get; set; }

        /// <summary>
        /// The spread between best Yes bid and ask.
        /// </summary>
        [LoadColumn(6)] public float YesSpread { get; set; }

        /// <summary>
        /// Depth at the best Yes bid price.
        /// </summary>
        [LoadColumn(7)] public float DepthAtBestYesBid { get; set; }

        /// <summary>
        /// Slope of the Yes bid price per minute.
        /// </summary>
        [LoadColumn(8)] public float YesBidSlopePerMinute { get; set; }

        /// <summary>
        /// Velocity at the top of the No bid order book.
        /// </summary>
        [LoadColumn(9)] public float VelTopNo { get; set; }

        /// <summary>
        /// Number of levels at the top of the No bid order book.
        /// </summary>
        [LoadColumn(10)] public float LevelsTopNo { get; set; }

        /// <summary>
        /// Velocity at the bottom of the No bid order book.
        /// </summary>
        [LoadColumn(11)] public float VelBottomNo { get; set; }

        /// <summary>
        /// Number of levels at the bottom of the No bid order book.
        /// </summary>
        [LoadColumn(12)] public float LevelsBottomNo { get; set; }

        /// <summary>
        /// Slope of the No bid price per minute.
        /// </summary>
        [LoadColumn(13)] public float NoBidSlopePerMinute { get; set; }
    }

    /// <summary>
    /// Data structure representing a single row from the CSV input file.
    /// Contains raw market data parsed from the CSV format.
    /// </summary>
    internal sealed class CsvRow
    {
        /// <summary>
        /// The timestamp of this market snapshot.
        /// </summary>
        public DateTime Timestamp;

        /// <summary>
        /// The best Yes bid price.
        /// </summary>
        public float BestYesBid;

        /// <summary>
        /// The best No bid price.
        /// </summary>
        public float BestNoBid;

        /// <summary>
        /// Velocity at the top of the Yes bid order book.
        /// </summary>
        public float VelTopYes;

        /// <summary>
        /// Velocity at the bottom of the Yes bid order book.
        /// </summary>
        public float VelBottomYes;

        /// <summary>
        /// Velocity at the top of the No bid order book.
        /// </summary>
        public float VelTopNo;

        /// <summary>
        /// Velocity at the bottom of the No bid order book.
        /// </summary>
        public float VelBottomNo;

        /// <summary>
        /// Number of levels at the top of the Yes bid order book.
        /// </summary>
        public int LevelsTopYes;

        /// <summary>
        /// Number of levels at the bottom of the Yes bid order book.
        /// </summary>
        public int LevelsBottomYes;

        /// <summary>
        /// Number of levels at the top of the No bid order book.
        /// </summary>
        public int LevelsTopNo;

        /// <summary>
        /// Number of levels at the bottom of the No bid order book.
        /// </summary>
        public int LevelsBottomNo;

        /// <summary>
        /// The spread for Yes orders.
        /// </summary>
        public float YesSpread;

        /// <summary>
        /// Depth at the best Yes bid.
        /// </summary>
        public float DepthAtBestYesBid;

        /// <summary>
        /// Slope of Yes bid price per minute.
        /// </summary>
        public float YesBidSlopePerMinute;

        /// <summary>
        /// Slope of No bid price per minute.
        /// </summary>
        public float NoBidSlopePerMinute;
    }

    /// <summary>
    /// Static utility class for loading and parsing CSV files containing market data.
    /// Handles flexible column naming and data type conversion.
    /// </summary>
    internal static class CsvLoader
    {
        /// <summary>
        /// Loads market data from a CSV file and returns a list of parsed CsvRow objects.
        /// </summary>
        /// <param name="path">The file path to the CSV file.</param>
        /// <returns>A list of CsvRow objects sorted by timestamp.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified CSV file does not exist.</exception>
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
                    BestYesBid = ParseFloat(Get(r, ix, "bestyesbid")),
                    BestNoBid  = ParseFloat(Get(r, ix, "bestnobid")),
                    VelTopYes = ParseFloat(Get(r, ix, "velocityperminute_top_yes_bid")),
                    LevelsTopYes = (int)ParseFloat(Get(r, ix, "levelcount_top_yes_bid")),
                    VelBottomYes = ParseFloat(Get(r, ix, "velocityperminute_bottom_yes_bid")),
                    LevelsBottomYes = (int)ParseFloat(Get(r, ix, "levelcount_bottom_yes_bid")),
                    VelTopNo = ParseFloat(Get(r, ix, "velocityperminute_top_no_bid")),
                    LevelsTopNo = (int)ParseFloat(Get(r, ix, "levelcount_top_no_bid")),
                    VelBottomNo = ParseFloat(Get(r, ix, "velocityperminute_bottom_no_bid")),
                    LevelsBottomNo = (int)ParseFloat(Get(r, ix, "levelcount_bottom_no_bid")),
                    YesSpread = ParseFloat(Get(r, ix, "yesspread")),
                    DepthAtBestYesBid = ParseFloat(Get(r, ix, "depthatbestyesbid")),
                    YesBidSlopePerMinute = ParseFloat(Get(r, ix, "yesbidslopeperminute")),
                    NoBidSlopePerMinute = ParseFloat(Get(r, ix, "nobidslopeperminute"))
                });
            }
            rows.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return rows;
        }

        /// <summary>
        /// Creates a mapping from column names to their indices in the CSV header.
        /// Supports multiple aliases for flexible column naming.
        /// </summary>
        /// <param name="h">The normalized header array.</param>
        /// <returns>A dictionary mapping column names to their indices.</returns>
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

        /// <summary>
        /// Normalizes header strings by converting to lowercase and removing non-alphanumeric characters except underscores.
        /// </summary>
        /// <param name="raw">The raw header array.</param>
        /// <returns>An array of normalized header strings.</returns>
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

        /// <summary>
        /// Retrieves a value from a CSV row array using the column index mapping.
        /// </summary>
        /// <param name="row">The row array.</param>
        /// <param name="ix">The column index mapping.</param>
        /// <param name="key">The column name to retrieve.</param>
        /// <returns>The value at the specified column, or empty string if not found.</returns>
        private static string Get(string[] row, Dictionary<string, int> ix, string key)
            => ix.TryGetValue(key, out var i) && i < row.Length ? row[i] : "";

        /// <summary>
        /// Parses a string to a float value, returning 0.0f if parsing fails.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed float value or 0.0f if parsing fails.</returns>
        private static float ParseFloat(string s)
            => float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0f;

        /// <summary>
        /// Parses a string to a DateTime, supporting standard formats and Unix timestamps.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The parsed DateTime, or DateTime.MinValue if parsing fails.</returns>
        private static DateTime ParseDate(string s)
        {
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                return dt;
            if (long.TryParse(s, out var unix)) return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            return DateTime.MinValue;
        }
    }
}
