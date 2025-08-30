// MarketDashboardControl2.cs
using ScottPlot;
using SmokehouseDTOs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
// Assuming SmokehouseDTOs for MarketSnapshot; adjust namespace as needed
// Replace 'MarketSnapshot' and 'OrderBook' with actual types and properties as per your project

namespace SimulatorWinForms
{
    public partial class MarketDashboardControl2 : UserControl
    {
        // Colors from CSS
        private Color backgroundDefault = Color.FromArgb(30, 30, 30); // #1e1e1e
        private Color textColor = Color.FromArgb(224, 224, 224); // #e0e0e0
        private Color panelBg = Color.FromArgb(37, 37, 37); // #252525
        private Color borderColor = Color.FromArgb(68, 68, 68); // #444
        private Color askColor = Color.FromArgb(0, 206, 209); // #00CED1 teal
        private Color bidColor = Color.FromArgb(255, 171, 64); // #FFAB40 orange
        private Color profitColor = Color.FromArgb(85, 255, 85); // #55ff55
        private Color lossColor = Color.FromArgb(255, 85, 85); // #ff5555

        // Data
        private MarketSnapshot currentSnapshot;
        private List<MarketSnapshot> historySnapshots;

        // Action for back button
        public Action BackAction { get; set; }

        public MarketDashboardControl2()
        {
            InitializeComponent();
            BackColor = backgroundDefault;
            ForeColor = textColor;

            // Event handlers
            timeframeCombo.SelectedIndexChanged += TimeframeCombo_SelectedIndexChanged;
            backButton.Click += (s, e) => BackAction?.Invoke();
        }

        public void Populate(MarketSnapshot snapshot, List<MarketSnapshot> history)
        {
            currentSnapshot = snapshot;
            historySnapshots = history;

         

            // Update chart
            UpdateChart();
        }

        private void TimeframeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateChart();
        }

        private void UpdateChart()
        {
            priceChart.Plot.Clear();
            // Extract points from historySnapshots
            // Example: 
            // var bidPointsX = historySnapshots.Select(s => s.Timestamp.ToOADate()).ToArray();
            // var bidPointsY = historySnapshots.Select(s => s.CurrentBid).ToArray();
            // priceChart.Plot.AddScatter(bidPointsX, bidPointsY, color: bidColor);
            // Similarly for asks, etc.

            // Filter by timeframe
            if (historySnapshots == null || historySnapshots.Count == 0) return;

            DateTime maxTime = currentSnapshot.Timestamp; // Assuming Timestamp property
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

            // Filter historySnapshots.Where(s => s.Timestamp >= minTime)
            // Add filtered plots accordingly

            priceChart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            priceChart.Plot.AxisAuto();
            priceChart.Refresh();
        }
    }
}