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

            // Create simple line plot for demonstration
            var dates = candles.Select(c => c.Timestamp.ToOADate()).ToArray();
            var closes = candles.Select(c => c.Close).ToArray();

            // Add line plot
            plot.Add.Scatter(dates, closes);

            // Highlight the pattern candles
            var patternIndices = pattern.Candles.Select(c => c - startIndex).Where(i => i >= 0 && i < candles.Count).ToArray();
            foreach (int idx in patternIndices)
            {
                // Add a marker or annotation for pattern candles
                plot.Add.Marker(candles[idx].Timestamp.ToOADate(), candles[idx].High + (candles[idx].High - candles[idx].Low) * 0.1,
                    MarkerShape.FilledCircle, 10, Colors.Red);
            }

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