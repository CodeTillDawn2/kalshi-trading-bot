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
            priceChart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            priceChart.Plot.AxisAuto();
            priceChart.Refresh();
        }
    }
}