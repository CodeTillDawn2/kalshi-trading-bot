using BacklashDTOs;
using BacklashPatterns.PatternDefinitions;
using ScottPlot;
using ScottPlot.Plottables;
using System.Drawing;

namespace BacklashPatterns
{
    /// <summary>
    /// Class responsible for generating candlestick chart images for patterns.
    /// </summary>
    public static class PatternVisualizer
    {
        private static readonly string ImagesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PatternImages");

        static PatternVisualizer()
        {
            // Ensure the images folder exists
            if (!Directory.Exists(ImagesFolder))
            {
                Directory.CreateDirectory(ImagesFolder);
            }
        }

        /// <summary>
        /// Generates a candlestick chart image for the given pattern and saves it to disk.
        /// </summary>
        /// <param name="pattern">The pattern to visualize.</param>
        /// <param name="prices">The array of candle data.</param>
        /// <param name="lookback">Number of candles to include before the pattern for context.</param>
        /// <returns>The path to the generated image file.</returns>
        public static string GeneratePatternImage(PatternDefinition pattern, CandleMids[] prices, int lookback = 10)
        {
            // Determine the range of candles to include
            int minIndex = pattern.Candles.Min();
            int maxIndex = pattern.Candles.Max();
            int startIndex = Math.Max(0, minIndex - lookback);
            int endIndex = Math.Min(prices.Length - 1, maxIndex + lookback);

            // Extract the relevant candles
            var candles = new List<CandleMids>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                candles.Add(prices[i]);
            }

            // Create the plot
            var plot = new Plot();

            // Convert to OHLC format for ScottPlot
            var ohlcs = candles.Select((c, idx) => new OHLC(
                open: c.Open,
                high: c.High,
                low: c.Low,
                close: c.Close
            )).ToArray();

            // Add candlestick plot
            var candlePlot = plot.Add.Candlestick(ohlcs.ToList());
            candlePlot.Sequential = true;

            // Highlight the pattern candles
            var patternIndices = pattern.Candles.Select(c => c - startIndex).Where(i => i >= 0 && i < candles.Count).ToArray();
            foreach (int idx in patternIndices)
            {
                // Add a marker or annotation for pattern candles
                plot.Add.Marker(candles[idx].Timestamp.ToOADate(), candles[idx].High + (candles[idx].High - candles[idx].Low) * 0.1,
                    MarkerShape.FilledCircle, 10, Colors.Red);
            }

            // Add pattern information labels
            AddPatternLabels(plot, pattern, candles);

            // Customize the plot
            plot.Title($"{pattern.Name} Pattern");
            plot.XLabel("Time");
            plot.YLabel("Price");
            plot.Axes.DateTimeTicksBottom();

            // Generate unique filename
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string filename = $"{pattern.Name}_{timestamp}.png";
            string filePath = Path.Combine(ImagesFolder, filename);

            // Save the plot as image
            plot.SavePng(filePath, 800, 600);

            return filePath;
        }

        /// <summary>
        /// Adds pattern information labels to the chart.
        /// </summary>
        /// <param name="plot">The ScottPlot chart to add labels to.</param>
        /// <param name="pattern">The pattern definition containing the information to display.</param>
        /// <param name="candles">The candle data for positioning the labels.</param>
        private static void AddPatternLabels(Plot plot, PatternDefinition pattern, List<CandleMids> candles)
        {
            if (candles.Count == 0) return;

            // Calculate position for labels (top-left corner of the chart)
            double minTime = candles.Min(c => c.Timestamp.ToOADate());
            double maxTime = candles.Max(c => c.Timestamp.ToOADate());
            double minPrice = candles.Min(c => c.Low);
            double maxPrice = candles.Max(c => c.High);

            // Position labels in the top-left area with some padding
            double labelX = minTime + (maxTime - minTime) * 0.02; // 2% from left
            double labelY = maxPrice - (maxPrice - minPrice) * 0.05; // 5% from top

            // Create label text with pattern information
            string labelText = $"Pattern: {pattern.Name}\n" +
                              $"Strength: {pattern.Strength:F3}\n" +
                              $"Certainty: {pattern.Certainty:F3}\n" +
                              $"Uncertainty: {pattern.Uncertainty:F3}";

            // Add the label as text on the plot
            var textLabel = plot.Add.Text(labelText, labelX, labelY);
            textLabel.LabelFontSize = 10;
            textLabel.LabelFontColor = Colors.Black;
            textLabel.LabelBold = true;
            textLabel.LabelFontName = "Arial";

            // Add a semi-transparent background rectangle for better readability
            double rectWidth = (maxTime - minTime) * 0.25; // 25% of chart width
            double rectHeight = (maxPrice - minPrice) * 0.15; // 15% of chart height

            var backgroundRect = plot.Add.Rectangle(
                labelX - (maxTime - minTime) * 0.01, // slight padding
                labelY + (maxPrice - minPrice) * 0.02,
                labelX + rectWidth,
                labelY - rectHeight
            );
            backgroundRect.FillColor = Colors.White.WithAlpha(0.8f);
            backgroundRect.LineColor = Colors.Black.WithAlpha(0.5f);
            backgroundRect.LineWidth = 1;
        }

        /// <summary>
        /// Generates images for a list of patterns.
        /// </summary>
        /// <param name="patterns">The list of patterns to visualize.</param>
        /// <param name="prices">The array of candle data.</param>
        /// <param name="lookback">Number of candles to include before the pattern for context.</param>
        /// <returns>A list of PatternVisualization objects with image paths.</returns>
        public static List<PatternVisualization> GeneratePatternImages(List<PatternDefinition> patterns, CandleMids[] prices, int lookback = 10)
        {
            var visualizations = new List<PatternVisualization>();

            foreach (var pattern in patterns)
            {
                try
                {
                    string imagePath = GeneratePatternImage(pattern, prices, lookback);
                    visualizations.Add(new PatternVisualization(pattern, imagePath));
                }
                catch (Exception ex)
                {
                    // Log error but continue with other patterns
                    Console.WriteLine($"Error generating image for pattern {pattern.Name}: {ex.Message}");
                    visualizations.Add(new PatternVisualization(pattern, null));
                }
            }

            return visualizations;
        }
    }
}