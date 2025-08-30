// MarketDashboardControl2.cs
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

        public SnapshotViewer()
        {
            InitializeComponent();
            timeframeCombo.SelectedIndexChanged += TimeframeCombo_SelectedIndexChanged;
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

        private void TimeframeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void UpdateChart()
        {
            priceChart.Plot.Clear();
            if (historySnapshots == null || historySnapshots.Count == 0) return;
            DateTime maxTime = currentSnapshot.Timestamp;
            TimeSpan span;
            switch (timeframeCombo.SelectedItem?.ToString())
            {
                case "15 Minutes": span = TimeSpan.FromMinutes(15); break;
                case "1 Hour": span = TimeSpan.FromHours(1); break;
                case "1 Day": span = TimeSpan.FromDays(1); break;
                case "3 Days": span = TimeSpan.FromDays(3); break;
                case "1 Week": span = TimeSpan.FromDays(7); break;
                case "1 Month": span = TimeSpan.FromDays(30); break;
                default: span = maxTime - historySnapshots.FirstOrDefault()?.Timestamp ?? TimeSpan.Zero; break;
            }
            DateTime minTime = maxTime - span;
            priceChart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            priceChart.Plot.AxisAuto();
            priceChart.Refresh();
        }
    }
}