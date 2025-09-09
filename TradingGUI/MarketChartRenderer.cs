// MarketChartRenderer.cs (full file with modifications: adjusted per-series collectTooltips to match old behavior)
using System.Text.Json;
using System.Text.RegularExpressions;
using TradingSimulator.TestObjects;

namespace SimulatorWinForms.Charting
{
    internal static class MarketChartRenderer
    {
        public static List<(double x, double y, string memo)> Render(
            ScottPlot.FormsPlot plot,
            string cacheDir,
            string market,
            Action<string>? log,
            bool collectTooltips = true)  // Optional, but we'll override per series to match old
        {
            plot.Plot.Clear();
            var tooltipPoints = collectTooltips ? new List<(double x, double y, string memo)>() : null;

            var baseMarket = Regex.Replace(market ?? "", @"_(\d+)$", "");
            var canonical = Path.Combine(cacheDir, $"{baseMarket}.json");

            CachedMarketData? merged = null;

            if (File.Exists(canonical))
            {
                merged = JsonSerializer.Deserialize<CachedMarketData>(File.ReadAllText(canonical));
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
                    var d = JsonSerializer.Deserialize<CachedMarketData>(File.ReadAllText(fp));
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
            Add(plot, merged.AskPoints, "Ask", Color.OrangeRed, 4, false, true, tooltipPoints);
            Add(plot, merged.BidPoints, "Bid", Color.DodgerBlue, 4, false, true, tooltipPoints);
            Add(plot, merged.BuyPoints, "Buy", Color.Green, 12, true, false, tooltipPoints);
            Add(plot, merged.SellPoints, "Sell", Color.Red, 12, true, false, tooltipPoints);
            Add(plot, merged.ExitPoints, "Exit", Color.Black, 12, true, false, tooltipPoints);
            Add(plot, merged.EventPoints, "Event", Color.Purple, 0, true, false, tooltipPoints);
            Add(plot, merged.DiscrepancyPoints, "Discrepancy", Color.Magenta, 8, true, false, tooltipPoints);

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

        private static void Add(
            ScottPlot.FormsPlot plot,
            List<PricePoint> pts,
            string fallbackLabel,
            Color color,
            int size,
            bool collectTooltips,
            bool connectLine,
            List<(double x, double y, string memo)> tooltipPoints)
        {
            if (pts == null || pts.Count == 0) return;

            double[] xs = pts.Select(p => p.Date.ToOADate()).ToArray();
            double[] ys = pts.Select(p => (double)p.Price).ToArray();

            plot.Plot.AddScatter(xs, ys, color, markerSize: size, lineWidth: connectLine ? 1 : 0);

            if (!collectTooltips || tooltipPoints == null) return;

            for (int i = 0; i < pts.Count; i++)
            {
                string memo = string.IsNullOrWhiteSpace(pts[i].Memo) ? fallbackLabel : pts[i].Memo.Trim();
                tooltipPoints.Add((xs[i], ys[i], memo));
            }
        }
    }
}
