// SnapshotViewer.cs (full non-designer code-behind: change line to black, remove TimeframeCombo_SelectedIndexChanged if not used)
using ScottPlot;
using SmokehouseDTOs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimulatorWinForms
{
    public partial class SnapshotViewer : UserControl
    {
        private MarketSnapshot currentSnapshot;
        private List<MarketSnapshot> historySnapshots;

        public Action BackAction { get; set; }

        public string CacheDir { get; set; }  // New property to receive cache dir from MainForm

        public SnapshotViewer()
        {
            InitializeComponent();
            backButton.Click += (s, e) => BackAction?.Invoke();
        }

        public void Populate(MarketSnapshot snapshot, List<MarketSnapshot> history)
        {
            currentSnapshot = snapshot;
            historySnapshots = history;
            orderbookGrid.Rows.Clear();
            //if (currentSnapshot.OrderbookData != null)
            //{
            //    foreach (var ask in currentSnapshot.OrderbookData[].OrderByDescending(a => a.Price))
            //    {
            //        int rowIdx = orderbookGrid.Rows.Add(ask.Price, ask.Size, ask.Value);
            //        orderbookGrid.Rows[rowIdx].Cells[0].Style.ForeColor = Color.FromArgb(255, 171, 64);
            //    }
            //    foreach (var bid in currentSnapshot.OrderbookData.Bids.OrderByDescending(b => b.Price))
            //    {
            //        int rowIdx = orderbookGrid.Rows.Add(bid.Price, bid.Size, bid.Value);
            //        orderbookGrid.Rows[rowIdx].Cells[0].Style.ForeColor = Color.FromArgb(0, 206, 209);
            //    }
            //}
            UpdateChart();
        }

        private void UpdateChart()
        {
            priceChart.Plot.Clear();

            if (historySnapshots == null || historySnapshots.Count == 0 || currentSnapshot == null) return;

            if (string.IsNullOrWhiteSpace(CacheDir))
            {
                // Fallback or error handling if cache dir not set
                return;
            }

            string market = currentSnapshot.MarketTicker;  // Use ticker from snapshot
            DateTime snapshotTime = currentSnapshot.Timestamp;

            // Render the same chart as MainForm, but without collecting tooltips
            Charting.MarketChartRenderer.Render(priceChart, CacheDir, market, null, collectTooltips: false);

            // Add stationary vertical line at current snapshot timestamp (black)
            double centerX = snapshotTime.ToOADate();
            priceChart.Plot.AddVerticalLine(centerX, Color.Black, 2);

            // Initially zoom in on the snapshot (e.g., +/- 30 minutes, total 1 hour span)
            double spanDays = 1.0 / 24;  // 1 hour
            priceChart.Plot.SetAxisLimitsX(centerX - spanDays / 2, centerX + spanDays / 2);

            priceChart.Plot.AxisAutoY();  // Auto-scale Y-axis
            priceChart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            priceChart.Refresh();
        }
    }
}