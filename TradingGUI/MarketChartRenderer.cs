/// <summary>
/// Provides static methods for rendering market data charts using ScottPlot.
/// This class is responsible for loading cached market data from JSON files,
/// merging multiple data sources if necessary, and plotting various market series
/// (bids, asks, trades, events) onto a ScottPlot chart control. It handles
/// data aggregation, sorting, and visualization to support trading analysis
/// and simulation in the GUI application.
/// </summary>
using ScottPlot;
using ScottPlot.WinForms;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TradingSimulator;


namespace TradingGUI.Charting
{
    internal static class MarketChartRenderer
    {
        private static class ChartConfig
        {
            public static readonly Color AskColor = Color.OrangeRed;
            public static readonly Color BidColor = Color.DodgerBlue;
            public static readonly Color BuyColor = Color.Green;
            public static readonly Color SellColor = Color.Red;
            public static readonly Color ExitColor = Color.Black;
            public static readonly Color EventColor = Color.Purple;
            public static readonly Color DiscrepancyColor = Color.Magenta;
            public static readonly int AskSize = 4;
            public static readonly int BidSize = 4;
            public static readonly int BuySize = 12;
            public static readonly int SellSize = 12;
            public static readonly int ExitSize = 12;
            public static readonly int EventSize = 0;
            public static readonly int DiscrepancySize = 8;
        }
        /// <summary>
        /// Renders a complete market chart by loading cached data and plotting all series.
        /// This method handles data loading from either a single canonical JSON file or
        /// multiple partitioned files, merges and sorts the data chronologically,
        /// and adds each series to the plot with appropriate styling and tooltips.
        /// The chart is automatically scaled and rendered for display.
        /// </summary>
        /// <param name="plot">The ScottPlot FormsPlot control to render the chart onto.</param>
        /// <param name="cacheDir">Directory path containing the cached market data JSON files.</param>
        /// <param name="market">Market identifier, potentially with a suffix that gets stripped for file matching.</param>
        /// <param name="log">Optional logging action for operational messages and errors.</param>
        /// <param name="collectTooltips">Whether to collect tooltip data for interactive chart points.</param>
        /// <returns>Task containing list of tooltip points with coordinates and labels, or empty list if tooltips disabled.</returns>
        public static async Task<List<(double x, double y, string memo)>> Render(
            FormsPlot plot,
            string cacheDir,
            string market,
            Action<string>? log,
            bool collectTooltips = true)  // Optional, but we'll override per series to match old
        {
            if (string.IsNullOrEmpty(cacheDir))
            {
                log?.Invoke("Cache directory cannot be null or empty");
                return new List<(double x, double y, string memo)>();
            }
            if (string.IsNullOrEmpty(market))
            {
                log?.Invoke("Market cannot be null or empty");
                return new List<(double x, double y, string memo)>();
            }

            plot.Plot.Clear();
            var tooltipPoints = collectTooltips ? new List<(double x, double y, string memo)>() : null;

            var baseMarket = Regex.Replace(market ?? "", @"_(\d+)$", "");
            var canonical = Path.Combine(cacheDir, $"{baseMarket}.json");

            CachedMarketData? merged = null;

            if (File.Exists(canonical))
            {
                merged = JsonSerializer.Deserialize<CachedMarketData>(await File.ReadAllTextAsync(canonical));
            }
            else
            {
                var parts = Directory.GetFiles(cacheDir, $"{baseMarket}_*.json");
                if (parts == null || parts.Length == 0)
                {
                    log?.Invoke($"Missing files for {baseMarket}");
                    return tooltipPoints ?? new List<(double x, double y, string memo)>();
                }

                merged = new CachedMarketData
                {
                    Market = baseMarket,
                    PnL = 0,
                    BidPoints = new(),
                    AskPoints = new(),
                    BuyPoints = new(),
                    SellPoints = new(),
                    ExitPoints = new(),
                    EventPoints = new(),
                    IntendedLongPoints = new(),
                    IntendedShortPoints = new(),
                    DiscrepancyPoints = new()
                };

                foreach (var fp in parts)
                {
                    var d = JsonSerializer.Deserialize<CachedMarketData>(await File.ReadAllTextAsync(fp));
                    if (d == null) continue;
                    merged.PnL += d.PnL;
                    if (d.BidPoints != null) merged.BidPoints.AddRange(d.BidPoints);
                    if (d.AskPoints != null) merged.AskPoints.AddRange(d.AskPoints);
                    if (d.BuyPoints != null) merged.BuyPoints.AddRange(d.BuyPoints);
                    if (d.SellPoints != null) merged.SellPoints.AddRange(d.SellPoints);
                    if (d.ExitPoints != null) merged.ExitPoints.AddRange(d.ExitPoints);
                    if (d.EventPoints != null) merged.EventPoints.AddRange(d.EventPoints);
                    if (d.IntendedLongPoints != null) merged.IntendedLongPoints.AddRange(d.IntendedLongPoints);
                    if (d.IntendedShortPoints != null) merged.IntendedShortPoints.AddRange(d.IntendedShortPoints);
                    if (d.DiscrepancyPoints != null) merged.DiscrepancyPoints.AddRange(d.DiscrepancyPoints);
                }

                merged.BidPoints = merged.BidPoints.OrderBy(p => p.Date).ToList();
                merged.AskPoints = merged.AskPoints.OrderBy(p => p.Date).ToList();
                merged.BuyPoints = merged.BuyPoints.OrderBy(p => p.Date).ToList();
                merged.SellPoints = merged.SellPoints.OrderBy(p => p.Date).ToList();
                merged.ExitPoints = merged.ExitPoints.OrderBy(p => p.Date).ToList();
                merged.EventPoints = merged.EventPoints.OrderBy(p => p.Date).ToList();
                merged.IntendedLongPoints = merged.IntendedLongPoints.OrderBy(p => p.Date).ToList();
                merged.IntendedShortPoints = merged.IntendedShortPoints.OrderBy(p => p.Date).ToList();
                merged.DiscrepancyPoints = merged.DiscrepancyPoints.OrderBy(p => p.Date).ToList();
            }

            if (merged == null)
            {
                log?.Invoke($"No data for {baseMarket}");
                return tooltipPoints ?? new List<(double x, double y, string memo)>();
            }

            // Match old behavior: no tooltips for lines (Ask, Bid), yes for points
            Add(plot, merged.AskPoints, "Ask", ChartConfig.AskColor, ChartConfig.AskSize, false, true, tooltipPoints);
            Add(plot, merged.BidPoints, "Bid", ChartConfig.BidColor, ChartConfig.BidSize, false, true, tooltipPoints);
            Add(plot, merged.BuyPoints, "Buy", ChartConfig.BuyColor, ChartConfig.BuySize, true, false, tooltipPoints);
            Add(plot, merged.SellPoints, "Sell", ChartConfig.SellColor, ChartConfig.SellSize, true, false, tooltipPoints);
            Add(plot, merged.ExitPoints, "Exit", ChartConfig.ExitColor, ChartConfig.ExitSize, true, false, tooltipPoints);
            Add(plot, merged.EventPoints, "Event", ChartConfig.EventColor, ChartConfig.EventSize, true, false, tooltipPoints);
            Add(plot, merged.DiscrepancyPoints, "Discrepancy", ChartConfig.DiscrepancyColor, ChartConfig.DiscrepancySize, true, false, tooltipPoints);

            plot.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            plot.Plot.AxisAuto();
            var lim = plot.Plot.GetAxisLimits();
            double xPad = (lim.XMax - lim.XMin) * 0.05;
            double yPad = (lim.YMax - lim.YMin) * 0.10;
            plot.Plot.SetAxisLimits(lim.XMin - xPad, lim.XMax + xPad, lim.YMin - yPad, lim.YMax + yPad);

            plot.Render();
            log?.Invoke($"Loaded Chart for {market}");

            return tooltipPoints ?? new List<(double x, double y, string memo)>();
        }

        /// <summary>
        /// Adds a series of price points to the chart as a scatter plot.
        /// This helper method converts PricePoint data to coordinate arrays,
        /// adds the series to the plot with specified styling, and optionally
        /// collects tooltip information for interactive elements. It handles
        /// empty or null data gracefully by returning early.
        /// </summary>
        /// <param name="plot">The ScottPlot FormsPlot control to add the series to.</param>
        /// <param name="pts">List of PricePoint objects containing date, price, and memo data.</param>
        /// <param name="fallbackLabel">Default label to use for tooltips when memo is empty.</param>
        /// <param name="color">Color to use for the scatter plot markers and lines.</param>
        /// <param name="size">Size of the scatter plot markers.</param>
        /// <param name="collectTooltips">Whether to collect tooltip data for this series.</param>
        /// <param name="connectLine">Whether to connect points with a line (true) or show only markers (false).</param>
        /// <param name="tooltipPoints">List to append tooltip data to, if collecting tooltips. Can be null if tooltips are disabled.</param>
        private static void Add(
            FormsPlot plot,
            List<PricePoint> pts,
            string fallbackLabel,
            Color color,
            int size,
            bool collectTooltips,
            bool connectLine,
            List<(double x, double y, string memo)>? tooltipPoints)
        {
            if (pts == null || pts.Count == 0) return;

            double[] xs = pts.Select(p => p.Date.ToOADate()).ToArray();
            double[] ys = pts.Select(p => (double)p.Price).ToArray();

            plot.Plot.AddScatter(xs, ys, color, markerSize: size, lineWidth: connectLine ? 1 : 0);

            if (!collectTooltips || tooltipPoints == null) return;

            for (int i = 0; i < pts.Count; i++)
            {
                string memo = pts[i]?.Memo != null && !string.IsNullOrWhiteSpace(pts[i].Memo) ? pts[i].Memo.Trim() : fallbackLabel;
                tooltipPoints.Add((xs[i], ys[i], memo));
            }
        }
    }
}
