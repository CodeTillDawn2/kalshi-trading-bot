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
        private List<string> memos;
        private string memoText = "";

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
            memoText = memos[newIndex];
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

        public void Populate(MarketSnapshot snapshot, List<MarketSnapshot> history, List<string> memosList)
        {
            currentSnapshot = snapshot;
            historySnapshots = history.OrderBy(s => s.Timestamp).ToList();
            currentIndex = historySnapshots.FindIndex(s => s.Timestamp == snapshot.Timestamp);
            if (currentIndex < 0) currentIndex = 0; // Fallback to first if not found
            memos = memosList;
            memoText = memosList[currentIndex];
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

            strategyOutputTextbox.Text = memoText;

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
            supportValue.Text = currentSnapshot.AllSupportResistanceLevels.Count != 1 ? currentSnapshot.AllSupportResistanceLevels.Count.ToString()
                : $"{currentSnapshot.AllSupportResistanceLevels.First().Price}";

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
            slopeYesValue.Text = currentSnapshot.YesBidSlopePerMinute_Short.ToString("F2") ?? "--";
            slopeNoValue.Text = currentSnapshot.NoBidSlopePerMinute_Short.ToString("F2") ?? "--";

            // Context (displaying Yes and No separately where applicable to match index.html)
            spreadValue.Text = currentSnapshot.YesSpread.ToString();

            double imbalance = 1;
            if (currentSnapshot.TotalOrderbookDepth_Yes == 0 || currentSnapshot.TotalOrderbookDepth_No == 0)
            {
                imbalance = 0;
            }
            else
            {
                imbalance = Math.Round((double)currentSnapshot.TotalOrderbookDepth_Yes / currentSnapshot.TotalOrderbookDepth_No, 2);
            }
            imbalValue.Text = imbalance.ToString();
            depthTop4YesValue.Text = currentSnapshot.DepthAtTop4YesBids.ToString();
            depthTop4NoValue.Text = currentSnapshot.DepthAtTop4NoBids.ToString();
            totalDepthYesValue.Text = currentSnapshot.TotalOrderbookDepth_Yes.ToString();
            totalDepthNoValue.Text = currentSnapshot.TotalOrderbookDepth_No.ToString();
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

            // Now add indicators based on checkbox states
            var legendItems = new List<string>();
            PlotTradingMetricsIndicatorsOverTime(legendItems);
            PlotFlowMomentumIndicatorsOverTime(legendItems);
            PlotContextIndicatorsOverTime(legendItems);
            PlotPositionIndicatorsOverTime(legendItems);
            PlotSupportResistance(legendItems);

            // Add legend if there are items
            if (legendItems.Any())
            {
                priceChart.Plot.Legend(location: ScottPlot.Alignment.UpperRight);
            }

            priceChart.Refresh();  // Ensure the plot is refreshed after adding the line
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

        private void EvaluateChartFilters()
        {
            if (currentSnapshot == null) return;

            // Save current axis limits to preserve zoom/pan
            var currentLimits = priceChart.Plot.GetAxisLimits();
            bool hasExistingLimits = currentLimits.XMax > currentLimits.XMin;

            // Re-render the entire chart with indicators
            UpdateChart();

            // Only restore axis limits if we had valid ones (preserve user's zoom/pan)
            if (hasExistingLimits)
            {
                priceChart.Plot.SetAxisLimits(currentLimits.XMin, currentLimits.XMax, currentLimits.YMin, currentLimits.YMax);
                priceChart.Refresh();
            }
        }

        private void PlotTradingMetricsIndicatorsOverTime(List<string> legendItems)
        {
            // RSI - Plot over time
            if (rsiLabel.Checked)
            {
                var rsiPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.RSI_Medium.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        rsiPoints.Add(snapshot.RSI_Medium.Value);
                    }
                }

                if (rsiPoints.Count > 1)
                {
                    var scatter = priceChart.Plot.AddScatter(timePoints.ToArray(), rsiPoints.ToArray(), Color.Blue, 2);
                    scatter.Label = "RSI";
                    legendItems.Add("RSI");
                }
            }

            // MACD - Plot over time
            if (macdLabel.Checked)
            {
                var macdPoints = new List<double>();
                var signalPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.MACD_Medium.MACD.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        macdPoints.Add(snapshot.MACD_Medium.MACD.Value);
                        signalPoints.Add(snapshot.MACD_Medium.Signal.GetValueOrDefault());
                    }
                }

                if (macdPoints.Count > 1)
                {
                    var macdScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), macdPoints.ToArray(), Color.Red, 2);
                    macdScatter.Label = "MACD";
                    var signalScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), signalPoints.ToArray(), Color.Blue, 2);
                    signalScatter.Label = "MACD Signal";
                    legendItems.Add("MACD");
                }
            }

            // EMA - Plot over time
            if (emaLabel.Checked)
            {
                var emaPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.EMA_Medium.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        emaPoints.Add(snapshot.EMA_Medium.Value); // Already in dollars
                    }
                }

                if (emaPoints.Count > 1)
                {
                    var emaScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), emaPoints.ToArray(), Color.Purple, 2);
                    emaScatter.Label = "EMA";
                    legendItems.Add("EMA");
                }
            }

            // Bollinger Bands - Plot over time
            if (bollingerLabel.Checked)
            {
                var upperPoints = new List<double>();
                var middlePoints = new List<double>();
                var lowerPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.BollingerBands_Medium.Upper.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        upperPoints.Add(snapshot.BollingerBands_Medium.Upper.Value); // Already in dollars
                        middlePoints.Add(snapshot.BollingerBands_Medium.Middle.Value); // Already in dollars
                        lowerPoints.Add(snapshot.BollingerBands_Medium.Lower.Value); // Already in dollars
                    }
                }

                if (upperPoints.Count > 1)
                {
                    var upperScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), upperPoints.ToArray(), Color.Red, 1);
                    upperScatter.Label = "BB Upper";
                    var middleScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), middlePoints.ToArray(), Color.Yellow, 1);
                    middleScatter.Label = "BB Middle";
                    var lowerScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), lowerPoints.ToArray(), Color.Green, 1);
                    lowerScatter.Label = "BB Lower";
                    legendItems.Add("Bollinger Bands");
                }
            }

            // ATR - Plot over time
            if (atrLabel.Checked)
            {
                var atrPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.ATR_Medium.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        atrPoints.Add(snapshot.ATR_Medium.Value); // Already in dollars
                    }
                }

                if (atrPoints.Count > 1)
                {
                    var atrScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), atrPoints.ToArray(), Color.Orange, 2);
                    atrScatter.Label = "ATR";
                    legendItems.Add("ATR");
                }
            }

            // VWAP - Plot over time
            if (vwapLabel.Checked)
            {
                var vwapPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.VWAP_Medium.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        vwapPoints.Add(snapshot.VWAP_Medium.Value); // Already in dollars
                    }
                }

                if (vwapPoints.Count > 1)
                {
                    var vwapScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), vwapPoints.ToArray(), Color.Cyan, 2);
                    vwapScatter.Label = "VWAP";
                    legendItems.Add("VWAP");
                }
            }

            // Stochastic - Plot over time
            if (stochasticLabel.Checked)
            {
                var kPoints = new List<double>();
                var dPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.StochasticOscillator_Medium.K.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        kPoints.Add(snapshot.StochasticOscillator_Medium.K.Value);
                        dPoints.Add(snapshot.StochasticOscillator_Medium.D.Value);
                    }
                }

                if (kPoints.Count > 1)
                {
                    var kScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), kPoints.ToArray(), Color.Black, 2);
                    kScatter.Label = "Stoch K";
                    var dScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), dPoints.ToArray(), Color.Gray, 2);
                    dScatter.Label = "Stoch D";
                    legendItems.Add("Stochastic");
                }
            }

            // OBV - Plot over time
            if (obvLabel.Checked)
            {
                var obvPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    obvPoints.Add(snapshot.OBV_Medium / 1000000.0); // Scale for visibility
                }

                if (obvPoints.Count > 1)
                {
                    var obvScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), obvPoints.ToArray(), Color.Magenta, 2);
                    obvScatter.Label = "OBV";
                    legendItems.Add("OBV");
                }
            }

            // PSAR - Plot over time
            if (psarLabel.Checked)
            {
                var psarPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.PSAR.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        psarPoints.Add(snapshot.PSAR.Value); // Already in dollars
                    }
                }

                if (psarPoints.Count > 1)
                {
                    var psarScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), psarPoints.ToArray(), Color.Red, 3);
                    psarScatter.Label = "PSAR";
                    legendItems.Add("PSAR");
                }
            }

            // ADX - Plot over time
            if (adxLabel.Checked)
            {
                var adxPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.ADX.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        adxPoints.Add(snapshot.ADX.Value);
                    }
                }

                if (adxPoints.Count > 1)
                {
                    var adxScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), adxPoints.ToArray(), Color.Brown, 2);
                    adxScatter.Label = "ADX";
                    legendItems.Add("ADX");
                }
            }
        }

        private void PlotFlowMomentumIndicatorsOverTime(List<string> legendItems)
        {
            // Top Velocity
            if (topVelocityCB.Checked)
            {
                var topVelPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    topVelPoints.Add(snapshot.VelocityPerMinute_Top_Yes_Bid);
                }

                if (topVelPoints.Count > 1)
                {
                    var topVelScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), topVelPoints.ToArray(), Color.DarkGreen, 2);
                    topVelScatter.Label = "Top Velocity";
                    legendItems.Add("Top Velocity");
                }
            }

            // Bottom Velocity
            if (bottomVelocityCB.Checked)
            {
                var bottomVelPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    bottomVelPoints.Add(snapshot.VelocityPerMinute_Bottom_Yes_Bid);
                }

                if (bottomVelPoints.Count > 1)
                {
                    var bottomVelScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), bottomVelPoints.ToArray(), Color.DarkRed, 2);
                    bottomVelScatter.Label = "Bottom Velocity";
                    legendItems.Add("Bottom Velocity");
                }
            }

            // Net Order Rate
            if (netOrderRateCB.Checked)
            {
                var netOrderPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    netOrderPoints.Add(snapshot.TradeRatePerMinute_Yes);
                }

                if (netOrderPoints.Count > 1)
                {
                    var netOrderScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), netOrderPoints.ToArray(), Color.Purple, 2);
                    netOrderScatter.Label = "Net Order Rate";
                    legendItems.Add("Net Order Rate");
                }
            }

            // Trade Volume
            if (tradeVolumeCB.Checked)
            {
                var tradeVolPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    tradeVolPoints.Add(snapshot.TradeVolumePerMinute_Yes);
                }

                if (tradeVolPoints.Count > 1)
                {
                    var tradeVolScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), tradeVolPoints.ToArray(), Color.Orange, 2);
                    tradeVolScatter.Label = "Trade Volume";
                    legendItems.Add("Trade Volume");
                }
            }

            // Average Trade Size
            if (avgTradeSizeCB.Checked)
            {
                var avgSizePoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    avgSizePoints.Add(snapshot.AverageTradeSize_Yes);
                }

                if (avgSizePoints.Count > 1)
                {
                    var avgSizeScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), avgSizePoints.ToArray(), Color.Pink, 2);
                    avgSizeScatter.Label = "Avg Trade Size";
                    legendItems.Add("Avg Trade Size");
                }
            }

            // Slope
            if (slopeCB.Checked)
            {
                var slopePoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    slopePoints.Add(snapshot.YesBidSlopePerMinute_Short);
                }

                if (slopePoints.Count > 1)
                {
                    var slopeScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), slopePoints.ToArray(), Color.Teal, 2);
                    slopeScatter.Label = "Slope";
                    legendItems.Add("Slope");
                }
            }
        }

        private void PlotContextIndicatorsOverTime(List<string> legendItems)
        {
            // Imbalance
            if (imbalCB.Checked)
            {
                var imbalPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    double imbalance = 1;
                    if (snapshot.TotalOrderbookDepth_Yes == 0 || snapshot.TotalOrderbookDepth_No == 0)
                    {
                        imbalance = 0;
                    }
                    else
                    {
                        imbalance = Math.Round((double)snapshot.TotalOrderbookDepth_Yes / snapshot.TotalOrderbookDepth_No, 2);
                    }
                    imbalPoints.Add(imbalance);
                }

                if (imbalPoints.Count > 1)
                {
                    var imbalScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), imbalPoints.ToArray(), Color.Navy, 2);
                    imbalScatter.Label = "Imbalance";
                    legendItems.Add("Imbalance");
                }
            }

            // Depth Top 4
            if (depthTop4CB.Checked)
            {
                var depthPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    depthPoints.Add(snapshot.DepthAtTop4YesBids);
                }

                if (depthPoints.Count > 1)
                {
                    var depthScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), depthPoints.ToArray(), Color.Maroon, 2);
                    depthScatter.Label = "Depth Top 4";
                    legendItems.Add("Depth Top 4");
                }
            }

            // Center Mass
            if (centerMassCB.Checked)
            {
                var centerMassPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    centerMassPoints.Add(snapshot.YesBidCenterOfMass);
                }

                if (centerMassPoints.Count > 1)
                {
                    var centerMassScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), centerMassPoints.ToArray(), Color.Olive, 2);
                    centerMassScatter.Label = "Center Mass";
                    legendItems.Add("Center Mass");
                }
            }

            // Total Contracts
            if (totalContractsCB.Checked)
            {
                var contractsPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    contractsPoints.Add(snapshot.TotalBidContracts_Yes);
                }

                if (contractsPoints.Count > 1)
                {
                    var contractsScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), contractsPoints.ToArray(), Color.Silver, 2);
                    contractsScatter.Label = "Total Contracts";
                    legendItems.Add("Total Contracts");
                }
            }

            // Total Depth
            if (totalDepthCB.Checked)
            {
                var depthPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    depthPoints.Add(snapshot.TotalOrderbookDepth_Yes);
                }

                if (depthPoints.Count > 1)
                {
                    var depthScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), depthPoints.ToArray(), Color.Gold, 2);
                    depthScatter.Label = "Total Depth";
                    legendItems.Add("Total Depth");
                }
            }
        }

        private void PlotPositionIndicatorsOverTime(List<string> legendItems)
        {
            // Position Size
            if (positionSizeLabel.Checked)
            {
                var positionSizePoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    positionSizePoints.Add(snapshot.PositionSize);
                }

                if (positionSizePoints.Count > 1)
                {
                    var positionSizeScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), positionSizePoints.ToArray(), Color.Violet, 2);
                    positionSizeScatter.Label = "Position Size";
                    legendItems.Add("Position Size");
                }
            }

            // Simulated Position
            if (simulatedPositionLabel.Checked)
            {
                var simulatedPosPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    simulatedPosPoints.Add(snapshot.PositionSize); // Using position size as proxy
                }

                if (simulatedPosPoints.Count > 1)
                {
                    var simulatedPosScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), simulatedPosPoints.ToArray(), Color.Indigo, 2);
                    simulatedPosScatter.Label = "Simulated Position";
                    legendItems.Add("Simulated Position");
                }
            }

            // Position ROI
            if (positionRoiLabel.Checked)
            {
                var positionRoiPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    positionRoiPoints.Add(snapshot.PositionROI);
                }

                if (positionRoiPoints.Count > 1)
                {
                    var positionRoiScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), positionRoiPoints.ToArray(), Color.Crimson, 2);
                    positionRoiScatter.Label = "Position ROI";
                    legendItems.Add("Position ROI");
                }
            }

            // Resting Orders
            if (restingOrdersLabel.Checked)
            {
                var restingOrdersPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    restingOrdersPoints.Add(snapshot.RestingOrders?.Count ?? 0);
                }

                if (restingOrdersPoints.Count > 1)
                {
                    var restingOrdersScatter = priceChart.Plot.AddScatter(timePoints.ToArray(), restingOrdersPoints.ToArray(), Color.Sienna, 2);
                    restingOrdersScatter.Label = "Resting Orders";
                    legendItems.Add("Resting Orders");
                }
            }
        }

        private void PlotSupportResistance(List<string> legendItems)
        {
            double centerX = currentSnapshot.Timestamp.ToOADate();

            // Support/Resistance Levels
            if (supportLabel.Checked && currentSnapshot.AllSupportResistanceLevels != null)
            {
                int count = 0;
                foreach (var level in currentSnapshot.AllSupportResistanceLevels)
                {
                    double levelPrice = level.Price; // Keep in pennies as-is
                    var supportLine = priceChart.Plot.AddHorizontalLine(levelPrice, Color.DarkBlue, 1, ScottPlot.LineStyle.Dot);
                    // Add text label for the first few levels to avoid clutter
                    if (count < 3)
                    {
                        priceChart.Plot.AddText($"S/R: {levelPrice:F2}", centerX, levelPrice, 10, Color.DarkBlue);
                    }
                    count++;
                }
                legendItems.Add("Support/Resistance");
            }
        }

        private void leftColumn_Paint(object sender, PaintEventArgs e)
        {

        }

        private void positionsLayout_Paint(object sender, PaintEventArgs e)
        {

        }

        private void rsiLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void macdLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void emaLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void bollingerLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void atrLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void vwapLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void stochasticLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void obvLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void psarLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void adxLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void supportLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void simulatedPositionLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void positionRoiLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void positionSizeLabel_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void topVelocityCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void bottomVelocityCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void netOrderRateCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void tradeVolumeCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void avgTradeSizeCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void slopeCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void imbalCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void depthTop4CB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void totalDepthCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void centerMassCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }

        private void totalContractsCB_CheckedChanged(object sender, EventArgs e)
        {
            EvaluateChartFilters();
        }
    }
}