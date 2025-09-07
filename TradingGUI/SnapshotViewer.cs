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
        private bool _isEvaluatingChartFilters = false; // Flag to prevent conflicts between UpdateChart and EvaluateChartFilters

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

            // DISABLE ScottPlot's built-in pan/zoom to prevent conflicts with custom panning
            priceChart.Configuration.Pan = false;
            priceChart.Configuration.Zoom = false;
            priceChart.Configuration.ScrollWheelZoom = false;

            secondaryChart.Configuration.Pan = false;
            secondaryChart.Configuration.Zoom = false;
            secondaryChart.Configuration.ScrollWheelZoom = false;

            // Add chart synchronization
            SetupChartSynchronization();

            // Add panning support for snapshot viewer charts
            SetupChartPanning();

            // Add detailed tooltips for trading metrics
            SetupTooltips();

            // Ensure secondary chart is properly initialized
            secondaryChart.Visible = false; // Start hidden, will be shown when metrics are checked
            secondaryChart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            secondaryChart.Plot.AxisAutoY();
        }

        private void SetupChartSynchronization()
        {
            // Use a timer to periodically check for axis changes and sync the secondary chart
            var syncTimer = new System.Windows.Forms.Timer();
            syncTimer.Interval = 100; // Check every 100ms
            syncTimer.Tick += (sender, args) =>
            {
                if (secondaryChart.Visible)
                {
                    CheckAndSyncSecondaryChart();
                }
            };
            syncTimer.Start();

            // Also sync when mouse is released on the main chart (after zoom/pan operations)
            priceChart.MouseUp += (sender, args) =>
            {
                if (secondaryChart.Visible)
                {
                    SyncSecondaryChartToMain();
                }
            };
        }

        private void SetupChartPanning()
        {
            // Add right-click panning support to both charts
            // Make sure to remove any existing handlers first to avoid duplicates
            priceChart.MouseDown -= PriceChart_MouseDown;
            priceChart.MouseMove -= PriceChart_MouseMove;
            priceChart.MouseUp -= PriceChart_MouseUp;

            secondaryChart.MouseDown -= SecondaryChart_MouseDown;
            secondaryChart.MouseMove -= SecondaryChart_MouseMove;
            secondaryChart.MouseUp -= SecondaryChart_MouseUp;

            // Add fresh handlers
            priceChart.MouseDown += PriceChart_MouseDown;
            priceChart.MouseMove += PriceChart_MouseMove;
            priceChart.MouseUp += PriceChart_MouseUp;

            secondaryChart.MouseDown += SecondaryChart_MouseDown;
            secondaryChart.MouseMove += SecondaryChart_MouseMove;
            secondaryChart.MouseUp += SecondaryChart_MouseUp;

            // Ensure charts can receive mouse events
            priceChart.Enabled = true;
            secondaryChart.Enabled = true;
        }

        private (double xMin, double xMax, double yMin, double yMax)? _lastMainLimits;

        private void CheckAndSyncSecondaryChart()
        {
            var currentLimits = priceChart.Plot.GetAxisLimits();

            // Check if the X-axis limits have changed significantly
            if (_lastMainLimits == null ||
                Math.Abs(currentLimits.XMin - _lastMainLimits.Value.xMin) > 0.0001 ||
                Math.Abs(currentLimits.XMax - _lastMainLimits.Value.xMax) > 0.0001)
            {
                _lastMainLimits = (currentLimits.XMin, currentLimits.XMax, currentLimits.YMin, currentLimits.YMax);
                SyncSecondaryChartToMain();
            }
        }

        private void SyncSecondaryChartToMain()
        {
            if (!secondaryChart.Visible) return;

            // Get the current axis limits from the main chart
            var mainLimits = priceChart.Plot.GetAxisLimits();

            // Apply the same X-axis limits to the secondary chart
            secondaryChart.Plot.SetAxisLimitsX(mainLimits.XMin, mainLimits.XMax);

            // Auto-scale the Y-axis for the secondary chart to fit visible data
            secondaryChart.Plot.AxisAutoY();

            // Refresh the secondary chart
            secondaryChart.Refresh();
        }

        // Panning support for snapshot viewer charts
        private bool _isPriceChartPanning = false;
        private bool _isSecondaryChartPanning = false;
        private Point _priceChartPanStartPx;
        private Point _secondaryChartPanStartPx;
        private (double xMin, double xMax, double yMin, double yMax) _priceChartPanStartLimits;
        private (double xMin, double xMax, double yMin, double yMax) _secondaryChartPanStartLimits;

        private void PriceChart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Start panning
                _isPriceChartPanning = true;
                _priceChartPanStartPx = e.Location;
                var lim = priceChart.Plot.GetAxisLimits();
                _priceChartPanStartLimits = (lim.XMin, lim.XMax, lim.YMin, lim.YMax);
                priceChart.Cursor = Cursors.SizeAll;

                // Note: Custom panning logic will override ScottPlot's default right-click behavior
            }
        }

        private void PriceChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPriceChartPanning && e.Button == MouseButtons.Right)
            {
                // Compute data-space deltas from pixel movement
                double xNow = priceChart.Plot.GetCoordinateX(e.X);
                double xStart = priceChart.Plot.GetCoordinateX(_priceChartPanStartPx.X);
                double yNow = priceChart.Plot.GetCoordinateY(e.Y);
                double yStart = priceChart.Plot.GetCoordinateY(_priceChartPanStartPx.Y);

                double dx = xNow - xStart;
                double dy = yNow - yStart;

                // Use larger movement threshold to prevent micro-movements from causing updates
                if (Math.Abs(dx) > 0.01 || Math.Abs(dy) > 0.01)
                {
                    // Temporarily disable secondary chart updates to prevent interference
                    bool wasSecondaryVisible = secondaryChart.Visible;
                    if (wasSecondaryVisible)
                    {
                        secondaryChart.Visible = false;
                    }

                    priceChart.Plot.SetAxisLimits(
                        _priceChartPanStartLimits.xMin - dx,
                        _priceChartPanStartLimits.xMax - dx,
                        _priceChartPanStartLimits.yMin - dy,
                        _priceChartPanStartLimits.yMax - dy);

                    // Single refresh to minimize flicker
                    priceChart.Refresh();

                    // Re-enable and sync secondary chart if it was visible
                    if (wasSecondaryVisible)
                    {
                        secondaryChart.Visible = true;
                        var mainLimits = priceChart.Plot.GetAxisLimits();
                        secondaryChart.Plot.SetAxisLimitsX(mainLimits.XMin, mainLimits.XMax);
                        secondaryChart.Refresh();
                    }
                }
            }
        }

        private void PriceChart_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isPriceChartPanning)
            {
                _isPriceChartPanning = false;
                priceChart.Cursor = Cursors.Default;
                _priceChartPanStartPx = Point.Empty;
            }
        }

        private void SecondaryChart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Start panning
                _isSecondaryChartPanning = true;
                _secondaryChartPanStartPx = e.Location;
                var lim = secondaryChart.Plot.GetAxisLimits();
                _secondaryChartPanStartLimits = (lim.XMin, lim.XMax, lim.YMin, lim.YMax);
                secondaryChart.Cursor = Cursors.SizeAll;
            }
        }

        private void SecondaryChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSecondaryChartPanning && e.Button == MouseButtons.Right)
            {
                // Compute data-space deltas from pixel movement
                double xNow = secondaryChart.Plot.GetCoordinateX(e.X);
                double xStart = secondaryChart.Plot.GetCoordinateX(_secondaryChartPanStartPx.X);
                double yNow = secondaryChart.Plot.GetCoordinateY(e.Y);
                double yStart = secondaryChart.Plot.GetCoordinateY(_secondaryChartPanStartPx.Y);

                double dx = xNow - xStart;
                double dy = yNow - yStart;

                // Use larger movement threshold to prevent micro-movements from causing updates
                if (Math.Abs(dx) > 0.01 || Math.Abs(dy) > 0.01)
                {
                    // Temporarily disable main chart updates to prevent interference
                    bool wasMainVisible = priceChart.Visible;
                    if (wasMainVisible)
                    {
                        priceChart.Visible = false;
                    }

                    secondaryChart.Plot.SetAxisLimits(
                        _secondaryChartPanStartLimits.xMin - dx,
                        _secondaryChartPanStartLimits.xMax - dx,
                        _secondaryChartPanStartLimits.yMin - dy,
                        _secondaryChartPanStartLimits.yMax - dy);

                    // Single refresh to minimize flicker
                    secondaryChart.Refresh();

                    // Re-enable and sync main chart if it was visible
                    if (wasMainVisible)
                    {
                        priceChart.Visible = true;
                        var secLimits = secondaryChart.Plot.GetAxisLimits();
                        priceChart.Plot.SetAxisLimitsX(secLimits.XMin, secLimits.XMax);
                        priceChart.Refresh();
                    }
                }
            }
        }

        private void SecondaryChart_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isSecondaryChartPanning)
            {
                _isSecondaryChartPanning = false;
                secondaryChart.Cursor = Cursors.Default;
                _secondaryChartPanStartPx = Point.Empty;
            }
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
            // Only handle right-click for back action if it's not on a chart control
            if (e.Button == MouseButtons.Right && !(sender is ScottPlot.FormsPlot || sender is System.Windows.Forms.PictureBox))
            {
                BackAction?.Invoke();
            }
        }

        private void SetupTooltips()
        {
            // Trading Metrics Tooltips
            toolTip1.SetToolTip(rsiLabel, "Relative Strength Index (RSI): Momentum oscillator measuring price changes to evaluate overbought/oversold conditions. Values above 70 indicate overbought (potential sell), below 30 indicate oversold (potential buy).");

            toolTip1.SetToolTip(macdLabel, "Moving Average Convergence Divergence (MACD): Trend-following momentum indicator showing relationship between two moving averages. Bullish when MACD crosses above signal line, bearish when below.");

            toolTip1.SetToolTip(emaLabel, "Exponential Moving Average (EMA): Weighted moving average giving more importance to recent prices. Used for trend identification and support/resistance levels.");

            toolTip1.SetToolTip(bollingerLabel, "Bollinger Bands: Volatility bands placed above and below a moving average. Price touching upper band suggests overbought, lower band suggests oversold. Band contraction indicates low volatility.");

            toolTip1.SetToolTip(atrLabel, "Average True Range (ATR): Volatility indicator measuring price movement range. Higher values indicate higher volatility, useful for stop-loss placement and position sizing.");

            toolTip1.SetToolTip(vwapLabel, "Volume Weighted Average Price (VWAP): Average price weighted by trading volume. Institutional benchmark - price above VWAP suggests bullish, below suggests bearish.");

            toolTip1.SetToolTip(stochasticLabel, "Stochastic Oscillator: Momentum indicator comparing closing price to price range over period. %K above 80 suggests overbought, below 20 suggests oversold.");

            toolTip1.SetToolTip(obvLabel, "On-Balance Volume (OBV): Cumulative volume indicator adding volume on up days, subtracting on down days. Rising OBV confirms uptrend, falling OBV confirms downtrend.");

            toolTip1.SetToolTip(psarLabel, "Parabolic SAR: Trend-following indicator plotting points below/above price during uptrends/downtrends. Used for trailing stop-losses and trend confirmation.");

            toolTip1.SetToolTip(adxLabel, "Average Directional Index (ADX): Strength of trend indicator. Values above 25 indicate strong trend, below 20 indicate weak/no trend. Used with +DI/-DI for trend direction.");

            toolTip1.SetToolTip(supportLabel, "Support/Resistance Levels: Historical price levels where buying/selling pressure caused reversals. Support levels catch falling prices, resistance levels cap rising prices.");

            // Flow/Momentum Tooltips
            toolTip1.SetToolTip(topVelocityCB, "Top Velocity: Rate of price change at the best bid/ask levels. High values indicate strong momentum in that direction.");

            toolTip1.SetToolTip(bottomVelocityCB, "Bottom Velocity: Rate of price change at lower levels in the order book. Indicates broader market participation.");

            toolTip1.SetToolTip(netOrderRateCB, "Net Order Rate: Difference between buy and sell order flow. Positive values indicate more buying pressure, negative indicates selling pressure.");

            toolTip1.SetToolTip(tradeVolumeCB, "Trade Volume: Total number of contracts traded in a given period. Higher volume often confirms trend strength.");

            toolTip1.SetToolTip(avgTradeSizeCB, "Average Trade Size: Mean size of individual trades. Large average sizes may indicate institutional activity.");

            toolTip1.SetToolTip(slopeCB, "5-Minute Slope: Rate of price change over 5-minute periods. Positive slope indicates upward momentum, negative indicates downward.");

            // Context Tooltips
            toolTip1.SetToolTip(imbalCB, "Order Book Imbalance: Ratio of buy orders to sell orders at current price levels. Values >1 indicate buying pressure, <1 indicate selling pressure.");

            toolTip1.SetToolTip(depthTop4CB, "Depth at Top 4 Levels: Total order size in the first 4 bid/ask levels. Indicates liquidity and potential price impact of large orders.");

            toolTip1.SetToolTip(centerMassCB, "Center of Mass: Weighted average price of all orders in the order book. Indicates where the majority of liquidity is concentrated.");

            toolTip1.SetToolTip(totalContractsCB, "Total Contracts: Sum of all contract sizes in the order book. Higher totals indicate deeper liquidity.");

            toolTip1.SetToolTip(totalDepthCB, "Total Depth: Combined value of all orders in the order book. Important for assessing market liquidity and slippage potential.");

            // Position Tooltips
            toolTip1.SetToolTip(positionSizeLabel, "Position Size: Current number of contracts held in the position. Larger positions have higher risk/reward ratios.");

            toolTip1.SetToolTip(simulatedPositionLabel, "Simulated Position: Hypothetical position size based on strategy parameters. Used for backtesting and risk assessment.");

            toolTip1.SetToolTip(positionRoiLabel, "Position ROI: Return on investment for the current position. Calculated as profit/loss divided by initial investment.");

            toolTip1.SetToolTip(restingOrdersLabel, "Resting Orders: Unfilled limit orders waiting in the order book. Indicates planned entry/exit points.");
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

            // Debug: Check timestamp values
            if (snapshot.Timestamp == DateTime.MinValue || snapshot.Timestamp.Year < 2000)
            {
                // If timestamp is invalid, try to use current time or first valid timestamp from history
                if (historySnapshots.Any(h => h.Timestamp != DateTime.MinValue && h.Timestamp.Year >= 2000))
                {
                    var validSnapshot = historySnapshots.First(h => h.Timestamp != DateTime.MinValue && h.Timestamp.Year >= 2000);
                    currentSnapshot = validSnapshot;
                    currentIndex = historySnapshots.IndexOf(validSnapshot);
                }
                else
                {
                    // Last resort: use current time
                    currentSnapshot = new MarketSnapshot { Timestamp = DateTime.Now };
                    currentIndex = 0;
                }
            }
            else
            {
                currentIndex = historySnapshots.FindIndex(s => s.Timestamp == snapshot.Timestamp);
                if (currentIndex < 0) currentIndex = 0; // Fallback to first if not found
            }

            memos = memosList;
            if (currentIndex < memosList.Count)
            {
                memoText = memosList[currentIndex];
            }
            else
            {
                memoText = memosList.FirstOrDefault() ?? "";
            }

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

            // Update chart (moves vertical line and zooms) - but only if not currently evaluating chart filters
            if (!_isEvaluatingChartFilters)
            {
                UpdateChart();
            }

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
            if (currentSnapshot == null) return;

            // Get current zoom/pan state before clearing
            var currentLimits = priceChart.Plot.GetAxisLimits();
            bool hasExistingLimits = currentLimits.XMax > currentLimits.XMin;

            // CRITICAL FIX: Only preserve zoom if it's NOT the default 2-hour span around current snapshot
            // This prevents restoring the year 1900 zoom that might have been saved previously
            bool isDefaultZoom = !hasExistingLimits ||
                Math.Abs((currentLimits.XMax - currentLimits.XMin) - (2.0 / 24.0)) < 0.001; // Default is 2 hour span (2/24 days)

            // Clear both charts
            priceChart.Plot.Clear();
            secondaryChart.Plot.Clear();

            // Always render base price data on main chart
            RenderBasePriceData(priceChart);

            // Render Y1 metrics (main chart) - RSI, EMA, Bollinger, VWAP, PSAR, Support
            RenderY1Metrics(priceChart);

            // Check if any secondary chart metrics are enabled
            bool hasSecondaryMetrics = HasSecondaryMetricsChecked();

            // Show/hide secondary chart based on whether any secondary metrics are checked
            secondaryChart.Visible = hasSecondaryMetrics;

            if (hasSecondaryMetrics)
            {
                // Render secondary chart metrics
                RenderSecondaryMetrics(secondaryChart);

                // Adjust main chart size to 3/4 when secondary chart is visible
                chartLayout.RowStyles[1].Height = 67.5f; // Main chart takes 3/4
                chartLayout.RowStyles[2].Height = 22.5f; // Secondary chart takes 1/4
            }
            else
            {
                // Secondary chart hidden, main chart takes full space
                chartLayout.RowStyles[1].Height = 90f; // Main chart takes full height
                chartLayout.RowStyles[2].Height = 0f; // Secondary chart hidden
            }

            // Update legends
            UpdateChartLegends();

            // CRITICAL FIX: Only restore zoom/pan state if it's NOT the default zoom
            // This prevents restoring corrupted zoom limits (like year 1900)
            if (hasExistingLimits && !isDefaultZoom)
            {
                // Additional validation: ensure the limits are reasonable (not year 1900)
                if (currentLimits.XMin > 40000 && currentLimits.XMax > 40000) // OA dates start from ~1900
                {
                    priceChart.Plot.SetAxisLimits(currentLimits.XMin, currentLimits.XMax, currentLimits.YMin, currentLimits.YMax);

                    // Sync secondary chart if visible
                    if (hasSecondaryMetrics)
                    {
                        SyncSecondaryChartToMain();
                    }
                }
            }

            // Refresh both charts
            priceChart.Refresh();
            if (hasSecondaryMetrics)
            {
                secondaryChart.Refresh();
            }

            // Force layout update
            chartLayout.PerformLayout();
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

            // Scale ScottPlot fonts (secondaryChart)
            var secondaryPlot = secondaryChart.Plot;
            secondaryPlot.XAxis.LabelStyle(fontSize: 12f * scaleFactor);
            secondaryPlot.YAxis.LabelStyle(fontSize: 12f * scaleFactor);
            secondaryPlot.XAxis.TickLabelStyle(fontSize: 10f * scaleFactor);
            secondaryPlot.YAxis.TickLabelStyle(fontSize: 10f * scaleFactor);

            // Refresh both charts to apply changes
            priceChart.Refresh();
            secondaryChart.Refresh();
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

            _isEvaluatingChartFilters = true; // Set flag to prevent UpdateChart conflicts

            try
            {
                // Get current zoom/pan state from main chart before clearing
                var mainLimits = priceChart.Plot.GetAxisLimits();
                bool hasExistingLimits = mainLimits.XMax > mainLimits.XMin;

                // Clear both charts
                priceChart.Plot.Clear();
                secondaryChart.Plot.Clear();

                // Always render base price data on main chart
                RenderBasePriceData(priceChart);

                // Render Y1 metrics (main chart) - RSI, EMA, Bollinger, VWAP, PSAR, Support
                RenderY1Metrics(priceChart);

                // Check if any secondary chart metrics are enabled
                bool hasSecondaryMetrics = HasSecondaryMetricsChecked();

                // Show/hide secondary chart based on whether any secondary metrics are checked
                secondaryChart.Visible = hasSecondaryMetrics;

                if (hasSecondaryMetrics)
                {
                    // Render secondary chart metrics with full context
                    RenderSecondaryMetrics(secondaryChart);

                    // Adjust main chart size to 3/4 when secondary chart is visible
                    chartLayout.RowStyles[1].Height = 67.5f; // Main chart takes 3/4
                    chartLayout.RowStyles[2].Height = 22.5f; // Secondary chart takes 1/4
                }
                else
                {
                    // Secondary chart hidden, main chart takes full space
                    chartLayout.RowStyles[1].Height = 90f; // Main chart takes full height
                    chartLayout.RowStyles[2].Height = 0f; // Secondary chart hidden
                }

                // Update legends
                UpdateChartLegends();

                // CRITICAL FIX: Only restore zoom/pan if user has actually zoomed/panned away from default
                // Don't restore if we're at the default zoom (which would show old timestamps)
                bool isDefaultZoom = !hasExistingLimits ||
                    Math.Abs((mainLimits.XMax - mainLimits.XMin) - (2.0 / 24.0)) < 0.001; // Default is 2 hour span (2/24 days)

                if (hasExistingLimits && !isDefaultZoom)
                {
                    // User has manually zoomed/panned - restore their view
                    priceChart.Plot.SetAxisLimits(mainLimits.XMin, mainLimits.XMax, mainLimits.YMin, mainLimits.YMax);

                    // Sync secondary chart if visible
                    if (hasSecondaryMetrics)
                    {
                        SyncSecondaryChartToMain();
                    }
                }
                else
                {
                    // User hasn't zoomed - set a reasonable default around current snapshot
                    DateTime snapshotTime = currentSnapshot.Timestamp;
                    if (snapshotTime == DateTime.MinValue || snapshotTime.Year < 2000)
                    {
                        snapshotTime = DateTime.Now;
                    }
                    double snapshotTimeOADate = snapshotTime.ToOADate();
                    double spanHours = 2.0; // 2 hour span for better visibility
                    double spanDays = spanHours / 24.0;
                    priceChart.Plot.SetAxisLimitsX(snapshotTimeOADate - spanDays / 2, snapshotTimeOADate + spanDays / 2);
                    priceChart.Plot.AxisAutoY();

                    // Sync secondary chart if visible - show full context, don't zoom
                    if (hasSecondaryMetrics)
                    {
                        secondaryChart.Plot.AxisAutoX(); // Show full context
                        secondaryChart.Plot.AxisAutoY();
                    }
                }

                // Force layout update - this is crucial for the chart to remain visible
                chartLayout.PerformLayout();

                // Ensure chart controls are properly sized after layout changes
                priceChart.Size = new Size(chartLayout.ClientSize.Width, (int)(chartLayout.ClientSize.Height * (hasSecondaryMetrics ? 0.675f : 0.9f)));
                if (hasSecondaryMetrics)
                {
                    secondaryChart.Size = new Size(chartLayout.ClientSize.Width, (int)(chartLayout.ClientSize.Height * 0.225f));
                }

                // Refresh both charts after layout is updated
                priceChart.Refresh();
                if (hasSecondaryMetrics)
                {
                    secondaryChart.Refresh();
                }

                // Additional refresh to ensure charts are properly displayed
                this.Refresh();

                // Force the chart container to refresh its layout
                chartContainer.Refresh();

                // Small delay to allow layout to settle
                System.Threading.Thread.Sleep(100);

                // Final refresh of the entire control
                this.Invalidate();
                this.Update();
            }
            finally
            {
                _isEvaluatingChartFilters = false; // Reset flag
            }
        }


        private void leftColumn_Paint(object sender, PaintEventArgs e)
        {

        }

        private void positionsLayout_Paint(object sender, PaintEventArgs e)
        {

        }

        private void RenderBasePriceData(ScottPlot.FormsPlot chart)
        {
            if (historySnapshots == null || historySnapshots.Count == 0 || currentSnapshot == null) return;

            string market = currentSnapshot.MarketTicker;  // Use ticker from snapshot
            DateTime snapshotTime = currentSnapshot.Timestamp;

            // Ensure we have a valid timestamp
            if (snapshotTime == DateTime.MinValue || snapshotTime.Year < 2000)
            {
                // Use current time as fallback
                snapshotTime = DateTime.Now;
            }

            // Try to render using cached data first
            if (!string.IsNullOrWhiteSpace(CacheDir) && Directory.Exists(CacheDir))
            {
                try
                {
                    // Render the same chart as MainForm, but without collecting tooltips
                    Charting.MarketChartRenderer.Render(chart, CacheDir, market, null, collectTooltips: false);
                }
                catch (Exception ex)
                {
                    // If cached rendering fails, fall back to snapshot data rendering
                    RenderFromSnapshotData(chart);
                }
            }
            else
            {
                // No cache directory, render from snapshot data directly
                RenderFromSnapshotData(chart);
            }

            // Add stationary vertical line at current snapshot timestamp (black)
            double centerX = snapshotTime.ToOADate();
            chart.Plot.AddVerticalLine(centerX, Color.Black, 2);

            // Initially zoom in on the snapshot (e.g., +/- 1 hour, total 2 hour span)
            double spanDays = 2.0 / 24;  // 2 hours
            chart.Plot.SetAxisLimitsX(centerX - spanDays / 2, centerX + spanDays / 2);
            chart.Plot.AxisAutoY();  // Auto-scale Y-axis
            chart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
        }

        private void RenderFromSnapshotData(ScottPlot.FormsPlot chart)
        {
            if (historySnapshots == null || historySnapshots.Count == 0) return;

            // Render basic price data from snapshots
            var askPrices = new List<double>();
            var bidPrices = new List<double>();
            var buyPrices = new List<double>();
            var sellPrices = new List<double>();
            var exitPrices = new List<double>();
            var timePoints = new List<double>();

            foreach (var snapshot in historySnapshots)
            {
                timePoints.Add(snapshot.Timestamp.ToOADate());

                // Use available price data from snapshots
                askPrices.Add(snapshot.BestNoBid);
                bidPrices.Add(snapshot.BestYesBid);

                // Add buy/sell/exit points if available (these might be zero if not set)
                buyPrices.Add(snapshot.BestYesBid); // Placeholder
                sellPrices.Add(snapshot.BestNoBid); // Placeholder
                exitPrices.Add((snapshot.BestYesBid + snapshot.BestNoBid) / 2); // Midpoint
            }

            if (timePoints.Count > 1)
            {
                // Plot ask and bid lines
                chart.Plot.AddScatter(timePoints.ToArray(), askPrices.ToArray(), Color.OrangeRed, 2, label: "Ask");
                chart.Plot.AddScatter(timePoints.ToArray(), bidPrices.ToArray(), Color.DodgerBlue, 2, label: "Bid");

                // Plot buy/sell/exit points
                chart.Plot.AddScatter(timePoints.ToArray(), buyPrices.ToArray(), Color.Green, 8, markerShape: ScottPlot.MarkerShape.filledCircle, label: "Buy");
                chart.Plot.AddScatter(timePoints.ToArray(), sellPrices.ToArray(), Color.Red, 8, markerShape: ScottPlot.MarkerShape.filledCircle, label: "Sell");
                chart.Plot.AddScatter(timePoints.ToArray(), exitPrices.ToArray(), Color.Black, 8, markerShape: ScottPlot.MarkerShape.filledCircle, label: "Exit");
            }
        }

        private void RenderY1Metrics(ScottPlot.FormsPlot chart)
        {
            var legendItems = new List<string>();

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
                    var scatter = chart.Plot.AddScatter(timePoints.ToArray(), rsiPoints.ToArray(), Color.Blue, 2);
                    scatter.Label = "RSI";
                    legendItems.Add("RSI");
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
                        emaPoints.Add(snapshot.EMA_Medium.Value);
                    }
                }

                if (emaPoints.Count > 1)
                {
                    var emaScatter = chart.Plot.AddScatter(timePoints.ToArray(), emaPoints.ToArray(), Color.Purple, 2);
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
                        upperPoints.Add(snapshot.BollingerBands_Medium.Upper.Value);
                        middlePoints.Add(snapshot.BollingerBands_Medium.Middle.Value);
                        lowerPoints.Add(snapshot.BollingerBands_Medium.Lower.Value);
                    }
                }

                if (upperPoints.Count > 1)
                {
                    var upperScatter = chart.Plot.AddScatter(timePoints.ToArray(), upperPoints.ToArray(), Color.Red, 1);
                    upperScatter.Label = "BB Upper";
                    var middleScatter = chart.Plot.AddScatter(timePoints.ToArray(), middlePoints.ToArray(), Color.Yellow, 1);
                    middleScatter.Label = "BB Middle";
                    var lowerScatter = chart.Plot.AddScatter(timePoints.ToArray(), lowerPoints.ToArray(), Color.Green, 1);
                    lowerScatter.Label = "BB Lower";
                    legendItems.Add("Bollinger Bands");
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
                        vwapPoints.Add(snapshot.VWAP_Medium.Value);
                    }
                }

                if (vwapPoints.Count > 1)
                {
                    var vwapScatter = chart.Plot.AddScatter(timePoints.ToArray(), vwapPoints.ToArray(), Color.Cyan, 2);
                    vwapScatter.Label = "VWAP";
                    legendItems.Add("VWAP");
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
                        psarPoints.Add(snapshot.PSAR.Value);
                    }
                }

                if (psarPoints.Count > 1)
                {
                    var psarScatter = chart.Plot.AddScatter(timePoints.ToArray(), psarPoints.ToArray(), Color.Red, 3);
                    psarScatter.Label = "PSAR";
                    legendItems.Add("PSAR");
                }
            }

            // Support/Resistance Levels
            if (supportLabel.Checked && currentSnapshot.AllSupportResistanceLevels != null)
            {
                double centerX = currentSnapshot.Timestamp.ToOADate();
                int count = 0;
                foreach (var level in currentSnapshot.AllSupportResistanceLevels)
                {
                    double levelPrice = level.Price;
                    var supportLine = chart.Plot.AddHorizontalLine(levelPrice, Color.DarkBlue, 1, ScottPlot.LineStyle.Dot);
                    if (count < 3)
                    {
                        chart.Plot.AddText($"S/R: {levelPrice:F2}", centerX, levelPrice, 10, Color.DarkBlue);
                    }
                    count++;
                }
                legendItems.Add("Support/Resistance");
            }

            // Add legend if there are items
            if (legendItems.Any())
            {
                chart.Plot.Legend(location: ScottPlot.Alignment.UpperRight);
            }
        }

        private void RenderSecondaryMetrics(ScottPlot.FormsPlot chart)
        {
            var legendItems = new List<string>();

            // Ensure we have a valid timestamp
            DateTime snapshotTime = currentSnapshot.Timestamp;
            if (snapshotTime == DateTime.MinValue || snapshotTime.Year < 2000)
            {
                snapshotTime = DateTime.Now;
            }

            // Add stationary vertical line at current snapshot timestamp (black) - same as main chart
            double centerX = snapshotTime.ToOADate();
            chart.Plot.AddVerticalLine(centerX, Color.Black, 2);

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
                    var macdScatter = chart.Plot.AddScatter(timePoints.ToArray(), macdPoints.ToArray(), Color.Red, 2);
                    macdScatter.Label = "MACD";
                    var signalScatter = chart.Plot.AddScatter(timePoints.ToArray(), signalPoints.ToArray(), Color.Blue, 2);
                    signalScatter.Label = "MACD Signal";
                    legendItems.Add("MACD");
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
                        atrPoints.Add(snapshot.ATR_Medium.Value);
                    }
                }

                if (atrPoints.Count > 1)
                {
                    var atrScatter = chart.Plot.AddScatter(timePoints.ToArray(), atrPoints.ToArray(), Color.Orange, 2);
                    atrScatter.Label = "ATR";
                    legendItems.Add("ATR");
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
                    var slopeScatter = chart.Plot.AddScatter(timePoints.ToArray(), slopePoints.ToArray(), Color.Teal, 2);
                    slopeScatter.Label = "Slope";
                    legendItems.Add("Slope");
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
                    var adxScatter = chart.Plot.AddScatter(timePoints.ToArray(), adxPoints.ToArray(), Color.Brown, 2);
                    adxScatter.Label = "ADX";
                    legendItems.Add("ADX");
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
                    var kScatter = chart.Plot.AddScatter(timePoints.ToArray(), kPoints.ToArray(), Color.Black, 2);
                    kScatter.Label = "Stoch K";
                    var dScatter = chart.Plot.AddScatter(timePoints.ToArray(), dPoints.ToArray(), Color.Gray, 2);
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
                    var obvScatter = chart.Plot.AddScatter(timePoints.ToArray(), obvPoints.ToArray(), Color.Magenta, 2);
                    obvScatter.Label = "OBV";
                    legendItems.Add("OBV");
                }
            }

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
                    var topVelScatter = chart.Plot.AddScatter(timePoints.ToArray(), topVelPoints.ToArray(), Color.DarkGreen, 2);
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
                    var bottomVelScatter = chart.Plot.AddScatter(timePoints.ToArray(), bottomVelPoints.ToArray(), Color.DarkRed, 2);
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
                    var netOrderScatter = chart.Plot.AddScatter(timePoints.ToArray(), netOrderPoints.ToArray(), Color.Purple, 2);
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
                    var tradeVolScatter = chart.Plot.AddScatter(timePoints.ToArray(), tradeVolPoints.ToArray(), Color.Orange, 2);
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
                    var avgSizeScatter = chart.Plot.AddScatter(timePoints.ToArray(), avgSizePoints.ToArray(), Color.Pink, 2);
                    avgSizeScatter.Label = "Avg Trade Size";
                    legendItems.Add("Avg Trade Size");
                }
            }

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
                    var imbalScatter = chart.Plot.AddScatter(timePoints.ToArray(), imbalPoints.ToArray(), Color.Navy, 2);
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
                    var depthScatter = chart.Plot.AddScatter(timePoints.ToArray(), depthPoints.ToArray(), Color.Maroon, 2);
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
                    var centerMassScatter = chart.Plot.AddScatter(timePoints.ToArray(), centerMassPoints.ToArray(), Color.Olive, 2);
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
                    var contractsScatter = chart.Plot.AddScatter(timePoints.ToArray(), contractsPoints.ToArray(), Color.Silver, 2);
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
                    var depthScatter = chart.Plot.AddScatter(timePoints.ToArray(), depthPoints.ToArray(), Color.Gold, 2);
                    depthScatter.Label = "Total Depth";
                    legendItems.Add("Total Depth");
                }
            }

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
                    var positionSizeScatter = chart.Plot.AddScatter(timePoints.ToArray(), positionSizePoints.ToArray(), Color.Violet, 2);
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
                    var simulatedPosScatter = chart.Plot.AddScatter(timePoints.ToArray(), simulatedPosPoints.ToArray(), Color.Indigo, 2);
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
                    var positionRoiScatter = chart.Plot.AddScatter(timePoints.ToArray(), positionRoiPoints.ToArray(), Color.Crimson, 2);
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
                    var restingOrdersScatter = chart.Plot.AddScatter(timePoints.ToArray(), restingOrdersPoints.ToArray(), Color.Sienna, 2);
                    restingOrdersScatter.Label = "Resting Orders";
                    legendItems.Add("Resting Orders");
                }
            }

            // Add legend if there are items
            if (legendItems.Any())
            {
                chart.Plot.Legend(location: ScottPlot.Alignment.UpperRight);
            }

            // Set up secondary chart axes - show full context, don't zoom to current snapshot
            chart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            chart.Plot.AxisAutoX(); // Auto-scale X-axis to show full context
            chart.Plot.AxisAutoY(); // Auto-scale Y-axis
        }

        private bool HasSecondaryMetricsChecked()
        {
            return macdLabel.Checked ||
                   atrLabel.Checked ||
                   slopeCB.Checked ||
                   adxLabel.Checked ||
                   stochasticLabel.Checked ||
                   obvLabel.Checked ||
                   topVelocityCB.Checked ||
                   bottomVelocityCB.Checked ||
                   netOrderRateCB.Checked ||
                   tradeVolumeCB.Checked ||
                   avgTradeSizeCB.Checked ||
                   imbalCB.Checked ||
                   depthTop4CB.Checked ||
                   totalDepthCB.Checked ||
                   centerMassCB.Checked ||
                   totalContractsCB.Checked ||
                   positionSizeLabel.Checked ||
                   simulatedPositionLabel.Checked ||
                   positionRoiLabel.Checked ||
                   restingOrdersLabel.Checked;
        }

        private void UpdateChartLegends()
        {
            // This method is now handled within RenderY1Metrics and RenderSecondaryMetrics
            // to avoid conflicts between the two charts
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