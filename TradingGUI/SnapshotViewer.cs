// Updated SnapshotViewer.cs with intelligent isotropic font scaling

// SnapshotViewer.cs (replace the entire class with this updated version)
using SmokehouseDTOs;

namespace SimulatorWinForms
{
    public partial class SnapshotViewer : UserControl
    {
        private MarketSnapshot currentSnapshot;
        private List<MarketSnapshot> historySnapshots;
        private int currentIndex;

        public Action BackAction { get; set; }

        public string CacheDir { get; set; }  // New property to receive cache dir from MainForm

        // Original dimensions for scaling reference (based on designer default size)
        private const int OriginalWidth = 933;
        private const int OriginalHeight = 692;
        private const float MinScale = 0.8f;  // Minimum font scale (80% of original)
        private const float MaxScale = 2.0f;  // Maximum font scale (200% of original)

        public SnapshotViewer()
        {
            InitializeComponent();
            backButton.Click += (s, e) => BackAction?.Invoke();

            // Add AutoScroll to containers to handle overflow
            marketInfoContainer.AutoScroll = true;
            positionsContainer.AutoScroll = true;
            orderbookContainer.AutoScroll = true;
            chartContainer.AutoScroll = true;

            AddMouseDownHandlers(this);

            // Add resize handler for dynamic scaling
            this.Resize += SnapshotViewer_ResizeEnd;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Left)
            {
                Navigate(-1);
                return true;  // Mark as handled to prevent further propagation
            }
            else if (keyData == Keys.Right)
            {
                Navigate(1);
                return true;  // Mark as handled
            }
            return base.ProcessCmdKey(ref msg, keyData);  // Allow other keys to pass through
        }

        private void AddMouseDownHandlers(Control control)
        {
            control.MouseDown += Control_MouseDown;
            foreach (Control c in control.Controls)
            {
                AddMouseDownHandlers(c);
            }
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                BackAction?.Invoke();
            }
        }

        // Removed SnapshotViewer_KeyDown (handled by parent form)

        private void Navigate(int delta)
        {
            if (historySnapshots == null || historySnapshots.Count == 0) return;

            int newIndex = currentIndex + delta;
            if (newIndex < 0 || newIndex >= historySnapshots.Count) return;

            currentIndex = newIndex;
            currentSnapshot = historySnapshots[currentIndex];
            UpdateUIFromSnapshot();  // Full UI update
        }

        public void NavigateSnapshot(int delta)
        {
            // Updated to use currentIndex for consistency and reliability
            if (historySnapshots == null || historySnapshots.Count == 0) return;

            int newIndex = currentIndex + delta;
            if (newIndex < 0 || newIndex >= historySnapshots.Count) return;

            currentIndex = newIndex;
            currentSnapshot = historySnapshots[newIndex];
            UpdateUIFromSnapshot();  // Full UI update
        }

        public void Populate(MarketSnapshot snapshot, List<MarketSnapshot> history)
        {
            currentSnapshot = snapshot;
            historySnapshots = history.OrderBy(s => s.Timestamp).ToList();
            currentIndex = historySnapshots.FindIndex(s => s.Timestamp == snapshot.Timestamp);
            if (currentIndex < 0) currentIndex = 0; // Fallback to first if not found

            UpdateUIFromSnapshot();  // Full UI update
        }

        private string FormatTimeSpan(TimeSpan? ts)
        {
            return ts.HasValue ? $"{ts.Value.Days} days, {ts.Value.Hours} hours" : "--";
        }

        private void UpdateUIFromSnapshot()
        {
            // Clear and repopulate order book (assuming OrderbookData contains bid levels only, as per note)
            orderbookGrid.Rows.Clear();
            if (currentSnapshot.OrderbookData != null)
            {
                // Populate bids (highest price first); assuming each dict has "price", "size", "value" keys
                foreach (var level in currentSnapshot.OrderbookData.OrderByDescending(l => Convert.ToDouble(l["price"])))
                {
                    double price = Convert.ToDouble(level["price"]);
                    int size = Convert.ToInt32(level["resting_contracts"]);
                    double value = Convert.ToInt32(level["resting_contracts"]) * Convert.ToDouble(level["price"]);
                    int rowIdx = orderbookGrid.Rows.Add(price, size, value);
                    orderbookGrid.Rows[rowIdx].Cells[0].Style.ForeColor = Color.FromArgb(0, 206, 209);  // Teal for bids
                }
            }

            // Update chart (moves vertical line and zooms)
            UpdateChart();

            // Populate other UI elements based on actual MarketSnapshot properties
            // Prices section (mapping "Ask" to No_Bid properties, "Bid" to Yes_Bid properties)
            allTimeHighAskPrice.Text = currentSnapshot.AllTimeHighNo_Bid.Bid.ToString();
            allTimeHighAskTime.Text = currentSnapshot.AllTimeHighNo_Bid.When.ToString("yyyy-MM-dd HH:mm");
            allTimeHighBidPrice.Text = currentSnapshot.AllTimeHighYes_Bid.Bid.ToString();
            allTimeHighBidTime.Text = currentSnapshot.AllTimeHighYes_Bid.When.ToString("yyyy-MM-dd HH:mm");

            recentHighAskPrice.Text = currentSnapshot.RecentHighNo_Bid.Bid.ToString();
            recentHighAskTime.Text = currentSnapshot.RecentHighNo_Bid.When.ToString("yyyy-MM-dd HH:mm");
            recentHighBidPrice.Text = currentSnapshot.RecentHighYes_Bid.Bid.ToString();
            recentHighBidTime.Text = currentSnapshot.RecentHighYes_Bid.When.ToString("yyyy-MM-dd HH:mm");

            currentPriceAsk.Text = currentSnapshot.BestNoBid.ToString();
            currentPriceBid.Text = currentSnapshot.BestYesBid.ToString();

            recentLowAskPrice.Text = currentSnapshot.RecentLowNo_Bid.Bid.ToString();
            recentLowAskTime.Text = currentSnapshot.RecentLowNo_Bid.When.ToString("yyyy-MM-dd HH:mm");
            recentLowBidPrice.Text = currentSnapshot.RecentLowYes_Bid.Bid.ToString();
            recentLowBidTime.Text = currentSnapshot.RecentLowYes_Bid.When.ToString("yyyy-MM-dd HH:mm");

            allTimeLowAskPrice.Text = currentSnapshot.AllTimeLowNo_Bid.Bid.ToString();
            allTimeLowAskTime.Text = currentSnapshot.AllTimeLowNo_Bid.When.ToString("yyyy-MM-dd HH:mm");
            allTimeLowBidPrice.Text = currentSnapshot.AllTimeLowYes_Bid.Bid.ToString();
            allTimeLowBidTime.Text = currentSnapshot.AllTimeLowYes_Bid.When.ToString("yyyy-MM-dd HH:mm");

            // Trading metrics (using medium timeframe where applicable)
            rsiValue.Text = currentSnapshot.RSI_Medium?.ToString("F2") ?? "--";
            macdValue.Text = currentSnapshot.MACD_Medium.MACD.HasValue ? $"MACD: {currentSnapshot.MACD_Medium.MACD:F2}, Sig: {currentSnapshot.MACD_Medium.Signal:F2}, Hist: {currentSnapshot.MACD_Medium.Histogram:F2}" : "--";
            emaValue.Text = currentSnapshot.EMA_Medium?.ToString("F2") ?? "--";
            bollingerValue.Text = currentSnapshot.BollingerBands_Medium.Lower.HasValue ? $"L: {currentSnapshot.BollingerBands_Medium.Lower:F2}, M: {currentSnapshot.BollingerBands_Medium.Middle:F2}, U: {currentSnapshot.BollingerBands_Medium.Upper:F2}" : "--";
            atrValue.Text = currentSnapshot.ATR_Medium?.ToString("F2") ?? "--";
            vwapValue.Text = currentSnapshot.VWAP_Medium?.ToString("F2") ?? "--";
            stochasticValue.Text = currentSnapshot.StochasticOscillator_Medium.K.HasValue ? $"K: {currentSnapshot.StochasticOscillator_Medium.K:F2}, D: {currentSnapshot.StochasticOscillator_Medium.D:F2}" : "--";
            obvValue.Text = currentSnapshot.OBV_Medium.ToString();
            psarValue.Text = currentSnapshot.PSAR.ToString() ?? "--";
            adxValue.Text = currentSnapshot.ADX.ToString() ?? "--";

            // Other info
            chartHeader.Text = currentSnapshot.MarketTicker ?? "--";
            categoryValue.Text = currentSnapshot.MarketCategory ?? "--";
            timeLeftValue.Text = FormatTimeSpan(currentSnapshot.TimeLeft);
            marketAgeValue.Text = FormatTimeSpan(currentSnapshot.MarketAge);

            // Flow/Momentum (displaying Yes and No separately to match index.html)
            topVelocityYesValue.Text = currentSnapshot.VelocityPerMinute_Top_Yes_Bid.ToString("F2");
            topVelocityNoValue.Text = currentSnapshot.VelocityPerMinute_Top_No_Bid.ToString("F2");
            bottomVelocityYesValue.Text = currentSnapshot.VelocityPerMinute_Bottom_Yes_Bid.ToString("F2");
            bottomVelocityNoValue.Text = currentSnapshot.VelocityPerMinute_Bottom_No_Bid.ToString("F2");
            netOrderRateYesValue.Text = currentSnapshot.TradeRatePerMinute_Yes.ToString("F2");
            netOrderRateNoValue.Text = currentSnapshot.TradeRatePerMinute_No.ToString("F2");
            tradeVolumeYesValue.Text = currentSnapshot.TradeVolumePerMinute_Yes.ToString("F2");
            tradeVolumeNoValue.Text = currentSnapshot.TradeVolumePerMinute_No.ToString("F2");
            avgTradeSizeYesValue.Text = currentSnapshot.AverageTradeSize_Yes.ToString("F2");
            avgTradeSizeNoValue.Text = currentSnapshot.AverageTradeSize_No.ToString("F2");
            slopeYesValue.Text = currentSnapshot.YesBidSlopePerMinute.ToString("F2") ?? "--";
            slopeNoValue.Text = currentSnapshot.NoBidSlopePerMinute.ToString("F2") ?? "--";

            // Context (displaying Yes and No separately where applicable to match index.html)
            spreadValue.Text = currentSnapshot.YesSpread.ToString();
            imbalValue.Text = currentSnapshot.BidVolumeImbalance.ToString("F2");
            depthTop4YesValue.Text = currentSnapshot.DepthAtTop4YesBids.ToString();
            depthTop4NoValue.Text = currentSnapshot.DepthAtTop4NoBids.ToString();
            centerMassYesValue.Text = currentSnapshot.YesBidCenterOfMass.ToString("F2");
            centerMassNoValue.Text = currentSnapshot.NoBidCenterOfMass.ToString("F2");
            totalContractsYesValue.Text = currentSnapshot.TotalBidContracts_Yes.ToString();
            totalContractsNoValue.Text = currentSnapshot.TotalBidContracts_No.ToString();

            // Positions
            positionSizeValue.Text = currentSnapshot.PositionSize.ToString();
            lastTradeValue.Text = currentSnapshot.TotalTraded.ToString();  // Using TotalTraded as proxy for last trade value
            positionRoiValue.Text = currentSnapshot.PositionROI.ToString("F2");
            buyinPriceValue.Text = currentSnapshot.BuyinPrice.ToString("F2");
            positionUpsideValue.Text = currentSnapshot.PositionUpside.ToString("F2");
            positionDownsideValue.Text = currentSnapshot.PositionDownside.ToString("F2");
            restingOrdersValue.Text = currentSnapshot.RestingOrders?.Count.ToString() ?? "0";
            simulatedPositionValue.Text = "??";

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

        private void SnapshotViewer_ResizeEnd(object sender, EventArgs e)
        {
            // Calculate isotropic scale factor: min of width and height ratios (clamp between min and max)
            float widthScale = (float)this.Width / OriginalWidth;
            float heightScale = (float)this.Height / OriginalHeight;
            float scaleFactor = Math.Clamp(Math.Min(widthScale, heightScale), MinScale, MaxScale);

            // Scale fonts for labels, checkboxes, and values recursively
            ScaleFonts(this, scaleFactor);

            // Scale DataGridView (orderbookGrid)
            orderbookGrid.Font = new Font(orderbookGrid.Font.FontFamily, 8.25f * scaleFactor);
            orderbookGrid.ColumnHeadersDefaultCellStyle.Font = new Font(orderbookGrid.Font.FontFamily, 8.25f * scaleFactor, FontStyle.Bold);
            orderbookGrid.AutoResizeColumns();

            // Scale Button (backButton)
            backButton.Font = new Font(backButton.Font.FontFamily, 8.25f * scaleFactor);

            // Scale ScottPlot fonts (priceChart)
            var plot = priceChart.Plot;
            plot.XAxis.LabelStyle(fontSize: 12f * scaleFactor);
            plot.YAxis.LabelStyle(fontSize: 12f * scaleFactor);
            plot.XAxis.TickLabelStyle(fontSize: 10f * scaleFactor);
            plot.YAxis.TickLabelStyle(fontSize: 10f * scaleFactor);

            // Refresh the chart to apply changes
            priceChart.Refresh();
        }

        private void ScaleFonts(Control control, float scaleFactor)
        {
            if (control is Label label)
            {
                label.Font = new Font(label.Font.FontFamily, label.Font.Size * scaleFactor);
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.Font = new Font(checkBox.Font.FontFamily, checkBox.Font.Size * scaleFactor);
            }

            foreach (Control child in control.Controls)
            {
                ScaleFonts(child, scaleFactor);
            }
        }
    }
}