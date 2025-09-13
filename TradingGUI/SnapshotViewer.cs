// Updated SnapshotViewer.cs with intelligent isotropic font scaling

// SnapshotViewer.cs (replace the entire class with this updated version)
using KalshiBotData.Data;
using KalshiBotData.Models;
using Microsoft.Extensions.Configuration;
using BacklashDTOs;
using TradingSimulator;

namespace SimulatorWinForms
{
    /// <summary>
    /// A comprehensive Windows Forms user control for visualizing and analyzing Kalshi trading market snapshots.
    /// Provides interactive charts, order book display, technical indicators, and navigation through historical market data.
    /// Supports both real-time and historical snapshot analysis with advanced charting capabilities including panning, zooming, and technical overlays.
    /// </summary>
    /// <remarks>
    /// This control serves as the primary interface for backtesting and analyzing trading strategies against historical market data.
    /// It integrates with the KalshiBotContext for data access and provides rich visualization of market dynamics including:
    /// - Price charts with technical indicators (RSI, MACD, EMA, Bollinger Bands, etc.)
    /// - Order book visualization with bid/ask spreads
    /// - Market metrics and trading signals
    /// - Interactive navigation through time series data
    /// - Support for simulated position and strategy analysis
    /// </remarks>
    public partial class SnapshotViewer : UserControl
    {
        private MarketSnapshot currentSnapshot;
        private List<MarketSnapshot> historySnapshots;
        private int currentIndex;
        private List<string> memos;
        private string memoText = "";
        /// <summary>
        /// Flag to prevent recursive conflicts between chart update operations.
        /// Set to true during chart filter evaluation to avoid simultaneous updates.
        /// </summary>
        private bool _isEvaluatingChartFilters = false;
        private int _hypotheticalPositionSize = 0;
        private double _hypotheticalAverageCost = 0.0;
        private int _hypotheticalRestingOrdersCount = 0;
        private List<PricePoint> _historicalPositionDataPoints = new List<PricePoint>();
        private List<PricePoint> _historicalAverageCostDataPoints = new List<PricePoint>();
        private List<PricePoint> _historicalRestingOrdersDataPoints = new List<PricePoint>();
        private List<PricePoint> _historicalPatternDataPoints = new List<PricePoint>();
        private readonly KalshiBotContext _context;

        public Action BackAction { get; set; }

        public string CacheDir { get; set; }  // New property to receive cache dir from MainForm

        /// <summary>
        /// Original control width in pixels used as baseline for responsive scaling calculations.
        /// </summary>
        private const int OriginalWidth = 933;

        /// <summary>
        /// Original control height in pixels used as baseline for responsive scaling calculations.
        /// </summary>
        private const int OriginalHeight = 692;

        /// <summary>
        /// Minimum allowed font scaling factor (80% of original size) to maintain readability.
        /// </summary>
        private const float MinScale = 0.8f;

        /// <summary>
        /// Maximum allowed font scaling factor (200% of original size) to prevent UI overflow.
        /// </summary>
        private const float MaxScale = 2.0f;

        /// <summary>
        /// Initializes a new instance of the SnapshotViewer control.
        /// Sets up the user interface, configures chart controls, initializes navigation systems,
        /// and establishes event handlers for interactive functionality.
        /// </summary>
        /// <remarks>
        /// This constructor performs comprehensive initialization including:
        /// - Component initialization and layout setup
        /// - Database context configuration for market data access
        /// - Chart configuration (disabling built-in pan/zoom for custom controls)
        /// - Mouse and keyboard event handler registration
        /// - Navigation timer setup for deferred chart updates
        /// - Typography system initialization for responsive scaling
        /// - Tooltip configuration for user guidance
        /// </remarks>
        public SnapshotViewer()
        {
            InitializeComponent();
            backButton.Click += (s, e) => BackAction?.Invoke();

            // Initialize database context
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=KalshiBot;Trusted_Connection=True;"
                })
                .Build();
            _context = new KalshiBotContext(config);

            // Add AutoScroll to containers to handle overflow
            marketInfoContainer.AutoScroll = true;
            marketInfoContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            positionsContainer.AutoScroll = true;
            positionsContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            orderbookContainer.AutoScroll = true;
            orderbookContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            chartContainer.AutoScroll = true;
            chartContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            // Add borders around individual metric groups
            tradingMetricsGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            tradingMetricsGrid.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            flowMomentumGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            flowMomentumGrid.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            contextGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            contextGrid.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            positionsGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            positionsGrid.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            otherInfoGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            otherInfoGrid.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            pricesGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pricesGrid.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;

            // Add more opaque borders to internal grids
            orderbookGrid.GridColor = System.Drawing.Color.Black;
            orderbookGrid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            orderbookGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Single;
            orderbookGrid.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            orderbookGrid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;

            RegisterMouseDownHandlers(this);

            // Add resize handler for dynamic scaling
            this.Resize += SnapshotViewer_ResizeEnd;

            // Initialize typography system
            ApplyInitialTypography();

            // DISABLE ScottPlot's built-in pan/zoom to prevent conflicts with custom panning
            priceChart.Configuration.Pan = false;
            priceChart.Configuration.Zoom = false;
            priceChart.Configuration.ScrollWheelZoom = false;

            secondaryChart.Configuration.Pan = false;
            secondaryChart.Configuration.Zoom = false;
            secondaryChart.Configuration.ScrollWheelZoom = false;

            // Add mouse wheel zoom for both charts
            priceChart.MouseWheel += PriceChart_MouseWheel;
            secondaryChart.MouseWheel += SecondaryChart_MouseWheel;

            // Initialize navigation timer for deferred updates
            _chartUpdateTimer = new System.Windows.Forms.Timer();
            _chartUpdateTimer.Interval = 300; // 300ms delay after user stops pressing keys
            _chartUpdateTimer.Tick += NavigationTimer_Tick;

            // Add panning support for snapshot viewer charts
            InitializeChartPanning();

            // Add detailed tooltips for trading metrics
            ConfigureTooltips();

            // Ensure secondary chart is properly initialized
            secondaryChart.Visible = false; // Start hidden, will be shown when metrics are checked
            secondaryChart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            secondaryChart.Plot.AxisAutoY();
        }


        private void InitializeChartPanning()
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
            priceChart.MouseMove += PriceChart_MouseMove_Cursor;
            priceChart.MouseLeave += PriceChart_MouseLeave;
            priceChart.MouseDown += PriceChart_MouseDown_Cursor;

            secondaryChart.MouseDown += SecondaryChart_MouseDown;
            secondaryChart.MouseMove += SecondaryChart_MouseMove;
            secondaryChart.MouseUp += SecondaryChart_MouseUp;
            secondaryChart.MouseMove += SecondaryChart_MouseMove_Cursor;
            secondaryChart.MouseLeave += SecondaryChart_MouseLeave;
            secondaryChart.MouseDown += SecondaryChart_MouseDown_Cursor;

            // Ensure charts can receive mouse events
            priceChart.Enabled = true;
            secondaryChart.Enabled = true;
        }

        /// <summary>
        /// Cached range of all historical data points for zoom boundary enforcement.
        /// Prevents zooming out beyond available data range.
        /// </summary>
        private (double xMin, double xMax)? _fullChartDataRange;

        /// <summary>
        /// Timer for deferring expensive chart rendering operations during rapid navigation.
        /// Allows immediate UI updates while batching complex chart operations.
        /// </summary>
        private System.Windows.Forms.Timer _chartUpdateTimer;

        /// <summary>
        /// Counter tracking consecutive navigation actions for progressive speed acceleration.
        /// Resets to zero when navigation pauses, enabling speed ramp-up.
        /// </summary>
        private int _navigationSpeedCounter = 0;

        /// <summary>
        /// Timestamp of the last navigation action for detecting pause periods.
        /// Used to reset speed acceleration when user stops rapid navigation.
        /// </summary>
        private DateTime _lastNavigationTimestamp = DateTime.MinValue;

        /// <summary>
        /// Current navigation step size (1, 2, 5, or 60 snapshots per action).
        /// Increases with consecutive navigations for efficient browsing of large datasets.
        /// </summary>
        private int _currentNavigationStepSize = 1;

        /// <summary>
        /// Interactive cursor line that follows mouse movement on charts.
        /// Provides visual feedback for precise time position identification.
        /// </summary>
        private ScottPlot.Plottable.IPlottable? _mouseCursorLine;


        /// <summary>
        /// Flag indicating whether the main price chart is currently being panned via right-click drag.
        /// </summary>
        private bool _isMainChartPanning = false;

        /// <summary>
        /// Flag indicating whether the secondary chart is currently being panned via right-click drag.
        /// </summary>
        private bool _isSecondaryChartPanning = false;

        /// <summary>
        /// Starting pixel coordinates for main chart panning operation.
        /// Used to calculate drag deltas during right-click panning.
        /// </summary>
        private Point _mainChartPanStartPixel;

        /// <summary>
        /// Starting pixel coordinates for secondary chart panning operation.
        /// Used to calculate drag deltas during right-click panning.
        /// </summary>
        private Point _secondaryChartPanStartPixel;

        /// <summary>
        /// Axis limits of the main chart at the start of panning operation.
        /// Preserved to calculate relative movement during drag operations.
        /// </summary>
        private (double xMin, double xMax, double yMin, double yMax) _mainChartPanStartLimits;

        /// <summary>
        /// Axis limits of the secondary chart at the start of panning operation.
        /// Preserved to calculate relative movement during drag operations.
        /// </summary>
        private (double xMin, double xMax, double yMin, double yMax) _secondaryChartPanStartLimits;

        private void PriceChart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Start panning
                _isMainChartPanning = true;
                _mainChartPanStartPixel = e.Location;
                var lim = priceChart.Plot.GetAxisLimits();
                _mainChartPanStartLimits = (lim.XMin, lim.XMax, lim.YMin, lim.YMax);
                priceChart.Cursor = Cursors.SizeAll;

                // Note: Custom panning logic will override ScottPlot's default right-click behavior
            }
        }

        private void PriceChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMainChartPanning && e.Button == MouseButtons.Right)
            {
                // Compute data-space deltas from pixel movement
                double xNow = priceChart.Plot.GetCoordinateX(e.X);
                double xStart = priceChart.Plot.GetCoordinateX(_mainChartPanStartPixel.X);
                double yNow = priceChart.Plot.GetCoordinateY(e.Y);
                double yStart = priceChart.Plot.GetCoordinateY(_mainChartPanStartPixel.Y);

                double dx = xNow - xStart;
                double dy = yNow - yStart;

                // Use smaller movement threshold for more responsive panning
                if (Math.Abs(dx) > 0.001 || Math.Abs(dy) > 0.001)
                {
                    priceChart.Plot.SetAxisLimits(
                        _mainChartPanStartLimits.xMin - dx,
                        _mainChartPanStartLimits.xMax - dx,
                        _mainChartPanStartLimits.yMin - dy,
                        _mainChartPanStartLimits.yMax - dy);

                    // Single refresh to minimize flicker
                    priceChart.Refresh();
                }
            }
        }

        private void PriceChart_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isMainChartPanning)
            {
                _isMainChartPanning = false;
                priceChart.Cursor = Cursors.Default;
                _mainChartPanStartPixel = Point.Empty;
            }
        }

        private void SecondaryChart_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Start panning
                _isSecondaryChartPanning = true;
                _secondaryChartPanStartPixel = e.Location;
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
                double xStart = secondaryChart.Plot.GetCoordinateX(_secondaryChartPanStartPixel.X);
                double yNow = secondaryChart.Plot.GetCoordinateY(e.Y);
                double yStart = secondaryChart.Plot.GetCoordinateY(_secondaryChartPanStartPixel.Y);

                double dx = xNow - xStart;
                double dy = yNow - yStart;

                // Use smaller movement threshold for more responsive panning
                if (Math.Abs(dx) > 0.001 || Math.Abs(dy) > 0.001)
                {
                    // Secondary chart pans independently - no interference with main chart
                    secondaryChart.Plot.SetAxisLimits(
                        _secondaryChartPanStartLimits.xMin - dx,
                        _secondaryChartPanStartLimits.xMax - dx,
                        _secondaryChartPanStartLimits.yMin - dy,
                        _secondaryChartPanStartLimits.yMax - dy);

                    // Single refresh to minimize flicker
                    secondaryChart.Refresh();
                }
            }
        }

        private void SecondaryChart_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isSecondaryChartPanning)
            {
                _isSecondaryChartPanning = false;
                secondaryChart.Cursor = Cursors.Default;
                _secondaryChartPanStartPixel = Point.Empty;
            }
        }

        // Cursor line and click-to-jump functionality
        private void PriceChart_MouseMove_Cursor(object sender, MouseEventArgs e)
        {
            UpdateCursorLine(priceChart, e);
        }

        private void PriceChart_MouseLeave(object sender, EventArgs e)
        {
            RemoveCursorLine(priceChart);
        }

        private void PriceChart_MouseDown_Cursor(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                JumpToClickedPosition(priceChart, e);
            }
        }

        private void SecondaryChart_MouseMove_Cursor(object sender, MouseEventArgs e)
        {
            UpdateCursorLine(secondaryChart, e);
        }

        private void SecondaryChart_MouseLeave(object sender, EventArgs e)
        {
            RemoveCursorLine(secondaryChart);
        }

        private void SecondaryChart_MouseDown_Cursor(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                JumpToClickedPosition(secondaryChart, e);
            }
        }

        private void UpdateCursorLine(ScottPlot.FormsPlot chart, MouseEventArgs e)
        {
            // Remove existing cursor line
            RemoveCursorLine(chart);

            // Get mouse position in data coordinates
            double xPos = chart.Plot.GetCoordinateX(e.X);

            // Add new cursor line
            _mouseCursorLine = chart.Plot.AddVerticalLine(xPos, Color.LightGray, 1, ScottPlot.LineStyle.Dash);
            chart.Refresh();
        }

        private void RemoveCursorLine(ScottPlot.FormsPlot chart)
        {
            if (_mouseCursorLine != null)
            {
                // Remove the cursor line
                chart.Plot.Remove(_mouseCursorLine);
                _mouseCursorLine = null;
                chart.Refresh();
            }
        }

        /// <summary>
        /// Handles click-to-jump functionality, allowing users to click on chart positions to navigate to specific snapshots.
        /// Finds the closest historical snapshot to the clicked time position and navigates to it.
        /// </summary>
        /// <param name="chart">The chart control that was clicked (primary or secondary).</param>
        /// <param name="e">Mouse event arguments containing click coordinates.</param>
        /// <remarks>
        /// This method provides intuitive time navigation:
        /// - Converts pixel coordinates to data time coordinates
        /// - Finds nearest historical snapshot by timestamp proximity
        /// - Updates current snapshot and navigation state
        /// - Adjusts main chart zoom when clicking secondary chart
        /// - Triggers immediate UI updates for responsive interaction
        /// - Resets navigation speed counters for fresh interaction
        /// </remarks>
        private async void JumpToClickedPosition(ScottPlot.FormsPlot chart, MouseEventArgs e)
        {
            if (historySnapshots == null || historySnapshots.Count == 0) return;

            // Get clicked position in data coordinates
            double clickedX = chart.Plot.GetCoordinateX(e.X);

            // Find the closest snapshot to the clicked position
            MarketSnapshot? closestSnapshot = null;
            double minDistance = double.MaxValue;

            foreach (var snapshot in historySnapshots)
            {
                double distance = Math.Abs(snapshot.Timestamp.ToOADate() - clickedX);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSnapshot = snapshot;
                }
            }

            if (closestSnapshot != null)
            {
                // Jump to the closest snapshot
                int newIndex = historySnapshots.IndexOf(closestSnapshot);
                if (newIndex >= 0 && newIndex < historySnapshots.Count)
                {
                    currentIndex = newIndex;
                    currentSnapshot = closestSnapshot;
                    memoText = memos != null && newIndex < memos.Count ? memos[newIndex] : "";

                    // Update position and average cost for current snapshot
                    _hypotheticalPositionSize = currentSnapshot.PositionSize;
                    _hypotheticalAverageCost = currentSnapshot.BuyinPrice;

                    // If clicking on secondary chart, reset main chart zoom to show the new position
                    if (chart == secondaryChart)
                    {
                        double centerX = currentSnapshot.Timestamp.ToOADate();
                        double spanDays = 2.0 / 24;  // 2 hours
                        priceChart.Plot.SetAxisLimitsX(centerX - spanDays / 2, centerX + spanDays / 2);
                        priceChart.Plot.AxisAutoY();
                        priceChart.Refresh();
                    }

                    // Update UI immediately
                    _ = RefreshUIQuickly();

                    // Reset navigation speed counters
                    _navigationSpeedCounter = 0;
                    _currentNavigationStepSize = 1;
                }
            }
        }

        private void PriceChart_MouseWheel(object sender, MouseEventArgs e)
        {
            if (currentSnapshot == null || !_fullChartDataRange.HasValue) return;

            // Get current limits
            var currentLimits = priceChart.Plot.GetAxisLimits();

            // Calculate zoom factor (smaller steps for finer control)
            double zoomFactor = e.Delta > 0 ? 0.9 : 1.1; // Zoom in on scroll up, out on scroll down

            // Calculate new X range
            double currentSpan = currentLimits.XMax - currentLimits.XMin;
            double newSpan = currentSpan * zoomFactor;

            // Don't allow zooming out past full data range
            double fullDataSpan = _fullChartDataRange.Value.xMax - _fullChartDataRange.Value.xMin;
            if (newSpan > fullDataSpan)
            {
                newSpan = fullDataSpan;
            }

            // Try to center on the snapshot time (black line)
            double snapshotTime = currentSnapshot.Timestamp.ToOADate();
            double desiredXMin = snapshotTime - newSpan / 2;
            double desiredXMax = snapshotTime + newSpan / 2;

            // If centering on snapshot would go outside data bounds, adjust the center
            double newXMin, newXMax;

            if (desiredXMin < _fullChartDataRange.Value.xMin)
            {
                // Would go too far left, so center as far left as possible
                newXMin = _fullChartDataRange.Value.xMin;
                newXMax = newXMin + newSpan;
            }
            else if (desiredXMax > _fullChartDataRange.Value.xMax)
            {
                // Would go too far right, so center as far right as possible
                newXMax = _fullChartDataRange.Value.xMax;
                newXMin = newXMax - newSpan;
            }
            else
            {
                // Can center on snapshot perfectly
                newXMin = desiredXMin;
                newXMax = desiredXMax;
            }

            // Apply new limits
            priceChart.Plot.SetAxisLimitsX(newXMin, newXMax);
            priceChart.Plot.AxisAutoY(); // Keep Y-axis auto-scaled
            priceChart.Refresh();
        }

        private void SecondaryChart_MouseWheel(object sender, MouseEventArgs e)
        {
            if (currentSnapshot == null || !_fullChartDataRange.HasValue) return;

            // Get current limits
            var currentLimits = secondaryChart.Plot.GetAxisLimits();

            // Calculate zoom factor (smaller steps for finer control)
            double zoomFactor = e.Delta > 0 ? 0.9 : 1.1; // Zoom in on scroll up, out on scroll down

            // Calculate new X range
            double currentSpan = currentLimits.XMax - currentLimits.XMin;
            double newSpan = currentSpan * zoomFactor;

            // Don't allow zooming out past full data range
            double fullDataSpan = _fullChartDataRange.Value.xMax - _fullChartDataRange.Value.xMin;
            if (newSpan > fullDataSpan)
            {
                newSpan = fullDataSpan;
            }

            // Try to center on the snapshot time (black line)
            double snapshotTime = currentSnapshot.Timestamp.ToOADate();
            double desiredXMin = snapshotTime - newSpan / 2;
            double desiredXMax = snapshotTime + newSpan / 2;

            // If centering on snapshot would go outside data bounds, adjust the center
            double newXMin, newXMax;

            if (desiredXMin < _fullChartDataRange.Value.xMin)
            {
                // Would go too far left, so center as far left as possible
                newXMin = _fullChartDataRange.Value.xMin;
                newXMax = newXMin + newSpan;
            }
            else if (desiredXMax > _fullChartDataRange.Value.xMax)
            {
                // Would go too far right, so center as far right as possible
                newXMax = _fullChartDataRange.Value.xMax;
                newXMin = newXMax - newSpan;
            }
            else
            {
                // Can center on snapshot perfectly
                newXMin = desiredXMin;
                newXMax = desiredXMax;
            }

            // Apply new limits
            secondaryChart.Plot.SetAxisLimitsX(newXMin, newXMax);
            secondaryChart.Plot.AxisAutoY(); // Keep Y-axis auto-scaled
            secondaryChart.Refresh();
        }

        private void NavigationTimer_Tick(object sender, EventArgs e)
        {
            // Stop the timer and reset navigation flag
            _chartUpdateTimer.Stop();

            // Reset navigation speed counters when user stops
            _navigationSpeedCounter = 0;
            _currentNavigationStepSize = 1;

            // Perform only the expensive chart operations (metrics, secondary chart rendering)
            RefreshChartMetrics();
        }

        private void RefreshChartMetrics()
        {
            if (currentSnapshot == null) return;

            // PRESERVE ZOOM STATE BEFORE CLEARING
            var mainLimits = priceChart.Plot.GetAxisLimits();
            bool hasExistingLimits = mainLimits.XMax > mainLimits.XMin;

            // HIDE CHARTS DURING UPDATE TO PREVENT ANY VISUAL FLICKERING
            bool wasMainVisible = priceChart.Visible;
            bool wasSecondaryVisible = secondaryChart.Visible;
            priceChart.Visible = false;
            if (secondaryChart.Visible)
            {
                secondaryChart.Visible = false;
            }

            try
            {
                // Only update the expensive chart rendering parts
                // Clear and re-render metrics and secondary chart data
                priceChart.Plot.Clear();
                RenderBasePriceData(priceChart);
                RenderY1Metrics(priceChart);
                UpdateChartLegends();

                if (wasSecondaryVisible)
                {
                    secondaryChart.Plot.Clear();
                    RenderSecondaryMetrics(secondaryChart);
                }

                // RESTORE ZOOM STATE AFTER RENDERING
                // CRITICAL FIX: Only restore zoom/pan if user has actually zoomed/panned away from default
                // Don't restore if we're at the default zoom (which would show old timestamps)
                bool isDefaultZoom = !hasExistingLimits ||
                    Math.Abs((mainLimits.XMax - mainLimits.XMin) - (2.0 / 24.0)) < 0.001; // Default is 2 hour span (2/24 days)

                if (hasExistingLimits && !isDefaultZoom)
                {
                    // User has manually zoomed/panned - restore their view
                    // Additional validation: ensure the limits are reasonable (not year 1900)
                    if (mainLimits.XMin > 40000 && mainLimits.XMax > 40000) // OA dates start from ~1900
                    {
                        priceChart.Plot.SetAxisLimits(mainLimits.XMin, mainLimits.XMax, mainLimits.YMin, mainLimits.YMax);
                    }
                }

                // Force layout update
                chartLayout.PerformLayout();
            }
            finally
            {
                // SHOW CHARTS AGAIN - THIS SHOULD BE INSTANT WITH NO FLICKERING
                priceChart.Visible = wasMainVisible;
                secondaryChart.Visible = wasSecondaryVisible;

                // Single refresh after making visible
                priceChart.Refresh();
                if (wasSecondaryVisible)
                {
                    secondaryChart.Refresh();
                }
            }
        }

        /// <summary>
        /// Processes keyboard input for navigation control, enabling arrow key navigation through snapshots.
        /// Provides keyboard shortcuts for efficient browsing of historical market data.
        /// </summary>
        /// <param name="msg">The Windows message containing keyboard input details.</param>
        /// <param name="keyData">The key data identifying which key was pressed.</param>
        /// <returns>True if the key was handled by this control, false to allow further processing.</returns>
        /// <remarks>
        /// This method enables intuitive keyboard navigation:
        /// - Left arrow key: Navigate to previous snapshot
        /// - Right arrow key: Navigate to next snapshot
        /// - Returns true to mark keys as handled, preventing propagation
        /// - Falls back to base implementation for unhandled keys
        /// </remarks>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Left)
            {
                PerformFastNavigation(-1);
                return true;  // Mark as handled to prevent further propagation
            }
            else if (keyData == Keys.Right)
            {
                PerformFastNavigation(1);
                return true;  // Mark as handled
            }
            return base.ProcessCmdKey(ref msg, keyData);  // Allow other keys to pass through
        }

        private void RegisterMouseDownHandlers(Control control)
        {
            control.MouseDown += HandleMouseDown;
            foreach (Control c in control.Controls)
            {
                RegisterMouseDownHandlers(c);
            }
        }

        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            // Only handle right-click for back action if it's not on a chart control
            if (e.Button == MouseButtons.Right && !(sender is ScottPlot.FormsPlot || sender is System.Windows.Forms.PictureBox))
            {
                BackAction?.Invoke();
            }
        }

        private void ConfigureTooltips()
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

            // Patterns tooltip is now handled in RefreshUIQuickly() to ensure currentSnapshot is available

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



        private async void Navigate(int delta)
        {
            // Use the fast navigation approach for better performance
            PerformFastNavigation(delta);
        }

        /// <summary>
        /// Performs efficient navigation through historical snapshots with progressive speed acceleration.
        /// Updates the current snapshot, position data, and triggers UI refresh while deferring expensive chart operations.
        /// </summary>
        /// <param name="delta">Direction and magnitude of navigation (-1 for previous, +1 for next snapshot).</param>
        /// <remarks>
        /// This method implements intelligent navigation features:
        /// - Progressive speed increase with consecutive navigations (1→2→5→60 step increments)
        /// - Boundary checking to prevent out-of-range access
        /// - Immediate UI updates for responsive feel
        /// - Deferred expensive chart rendering via timer to maintain performance
        /// - Automatic speed reset after navigation pauses
        /// </remarks>
        private async void PerformFastNavigation(int delta)
        {
            if (historySnapshots == null || historySnapshots.Count == 0) return;

            // Calculate progressive step size based on consecutive navigations
            int stepSize = DetermineNavigationStepSize();

            // Apply the step size to the delta
            int actualDelta = delta * stepSize;

            int newIndex = currentIndex + actualDelta;
            if (newIndex < 0) newIndex = 0;
            if (newIndex >= historySnapshots.Count) newIndex = historySnapshots.Count - 1;

            // If we're at the boundary, don't count this as a consecutive navigation
            if (newIndex == currentIndex) return;

            // Update the data immediately
            currentIndex = newIndex;
            currentSnapshot = historySnapshots[currentIndex];
            memoText = memos[newIndex];

            // Update position and average cost for current snapshot
            _hypotheticalPositionSize = currentSnapshot.PositionSize;
            _hypotheticalAverageCost = currentSnapshot.BuyinPrice;

            // Update resting orders count for current snapshot
            if (_historicalRestingOrdersDataPoints != null && _historicalRestingOrdersDataPoints.Count > 0)
            {
                var matchingPoint = _historicalRestingOrdersDataPoints.FirstOrDefault(p =>
                    Math.Abs((p.Date - currentSnapshot.Timestamp).TotalSeconds) < 1);
                if (matchingPoint != null)
                {
                    // Update the resting orders count in the snapshot
                    if (currentSnapshot.RestingOrders == null)
                    {
                        currentSnapshot.RestingOrders = new List<(string, string, string, int, int, DateTime?)>();
                    }
                    int targetCount = (int)matchingPoint.Price;
                    while (currentSnapshot.RestingOrders.Count < targetCount)
                    {
                        currentSnapshot.RestingOrders.Add(("buy", "yes", "limit", 1, 50, null));
                    }
                    while (currentSnapshot.RestingOrders.Count > targetCount)
                    {
                        currentSnapshot.RestingOrders.RemoveAt(currentSnapshot.RestingOrders.Count - 1);
                    }
                }
            }

            // Update resting orders count for current snapshot
            if (_historicalRestingOrdersDataPoints != null && _historicalRestingOrdersDataPoints.Count > 0)
            {
                var matchingPoint = _historicalRestingOrdersDataPoints.FirstOrDefault(p =>
                    Math.Abs((p.Date - currentSnapshot.Timestamp).TotalSeconds) < 1);
                if (matchingPoint != null)
                {
                    // Update the resting orders count in the snapshot
                    if (currentSnapshot.RestingOrders == null)
                    {
                        currentSnapshot.RestingOrders = new List<(string, string, string, int, int, DateTime?)>();
                    }
                    int targetCount = (int)matchingPoint.Price;
                    while (currentSnapshot.RestingOrders.Count < targetCount)
                    {
                        currentSnapshot.RestingOrders.Add(("buy", "yes", "limit", 1, 50, null));
                    }
                    while (currentSnapshot.RestingOrders.Count > targetCount)
                    {
                        currentSnapshot.RestingOrders.RemoveAt(currentSnapshot.RestingOrders.Count - 1);
                    }
                }
            }

            // Immediately update all UI elements (fast operations)
            _ = RefreshUIQuickly();

            // Update orderbook from the new snapshot
            PopulateUIFromSnapshot();

            // Start navigation mode and reset timer for expensive operations
            _chartUpdateTimer.Stop();
            _chartUpdateTimer.Start();
        }

        /// <summary>
        /// Calculates the progressive navigation step size based on user interaction patterns.
        /// Implements speed acceleration for efficient browsing of large datasets.
        /// </summary>
        /// <returns>The number of snapshots to skip per navigation action (1, 2, 5, or 60).</returns>
        /// <remarks>
        /// This method provides intelligent navigation acceleration:
        /// - Normal speed (1 snapshot) for initial interactions
        /// - Medium speed (2 snapshots) after 5 consecutive navigations
        /// - Fast speed (5 snapshots) after 15 consecutive navigations
        /// - Rapid speed (60 snapshots) after 60 consecutive navigations
        /// - Automatic reset to normal speed after 500ms pause
        /// - Boundary detection prevents invalid navigation
        /// </remarks>
        private int DetermineNavigationStepSize()
        {
            DateTime now = DateTime.Now;

            // Reset consecutive count if too much time has passed
            if ((now - _lastNavigationTimestamp).TotalMilliseconds > 500)
            {
                _navigationSpeedCounter = 0;
                _currentNavigationStepSize = 1;
            }

            _navigationSpeedCounter++;
            _lastNavigationTimestamp = now;

            // Progressive speed based on consecutive navigations
            if (_navigationSpeedCounter >= 60)
            {
                _currentNavigationStepSize = 60; // Jump 60 bars at a time
            }
            else if (_navigationSpeedCounter >= 15)
            {
                _currentNavigationStepSize = 5; // Jump 5 bars at a time
            }
            else if (_navigationSpeedCounter >= 5)
            {
                _currentNavigationStepSize = 2; // Jump 2 bars at a time
            }
            else
            {
                _currentNavigationStepSize = 1; // Normal speed
            }

            return _currentNavigationStepSize;
        }

        /// <summary>
        /// Performs rapid UI updates for all display elements without expensive chart rendering.
        /// Updates market data, metrics, positions, and patterns while preserving current chart zoom state.
        /// </summary>
        /// <remarks>
        /// This method focuses on fast UI operations to maintain responsive navigation:
        /// - Updates market information and pricing displays
        /// - Refreshes trading metrics and technical indicators
        /// - Updates position and order book information
        /// - Handles pattern detection visualization
        /// - Moves vertical timeline indicator immediately
        /// - Queries database for market titles with fallback to ticker symbols
        /// </remarks>
        private async Task RefreshUIQuickly()
        {
            if (currentSnapshot == null) return;

            // Update strategy output immediately
            strategyOutputTextbox.Text = memoText;

            // Query market title from database
            string marketTitle = currentSnapshot.MarketTicker ?? "--";
            try
            {
                var market = await _context.GetMarketByTicker(currentSnapshot.MarketTicker);
                if (market != null && !string.IsNullOrEmpty(market.title))
                {
                    marketTitle = market.title;
                }
            }
            catch (Exception ex)
            {
                // If database query fails, fall back to ticker
            }

            marketTickerLabel.Text = marketTitle;

            // Update price display elements
            allTimeHighAskPrice.Text = (100 - currentSnapshot.AllTimeHighNo_Bid.Bid).ToString();
            TimeSpan? allTimeHighAskTs = currentSnapshot.AllTimeHighNo_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.AllTimeHighNo_Bid.When : (TimeSpan?)null;
            allTimeHighAskTime.Text = FormatTimeSpan(allTimeHighAskTs);
            allTimeHighBidPrice.Text = currentSnapshot.AllTimeHighYes_Bid.Bid.ToString();
            TimeSpan? allTimeHighBidTs = currentSnapshot.AllTimeHighYes_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.AllTimeHighYes_Bid.When : (TimeSpan?)null;
            allTimeHighBidTime.Text = FormatTimeSpan(allTimeHighBidTs);

            recentHighAskPrice.Text = (100 - currentSnapshot.RecentHighNo_Bid.Bid).ToString();
            TimeSpan? recentHighAskTs = currentSnapshot.RecentHighNo_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.RecentHighNo_Bid.When : (TimeSpan?)null;
            recentHighAskTime.Text = FormatTimeSpan(recentHighAskTs);
            recentHighBidPrice.Text = currentSnapshot.RecentHighYes_Bid.Bid.ToString();
            TimeSpan? recentHighBidTs = currentSnapshot.RecentHighYes_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.RecentHighYes_Bid.When : (TimeSpan?)null;
            recentHighBidTime.Text = FormatTimeSpan(recentHighBidTs);

            currentPriceAsk.Text = currentSnapshot.BestYesAsk.ToString();
            currentPriceBid.Text = currentSnapshot.BestYesBid.ToString();

            recentLowAskPrice.Text = (100 - currentSnapshot.RecentLowNo_Bid.Bid).ToString();
            TimeSpan? recentLowAskTs = currentSnapshot.RecentLowNo_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.RecentLowNo_Bid.When : (TimeSpan?)null;
            recentLowAskTime.Text = FormatTimeSpan(recentLowAskTs);
            recentLowBidPrice.Text = currentSnapshot.RecentLowYes_Bid.Bid.ToString();
            TimeSpan? recentLowBidTs = currentSnapshot.RecentLowYes_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.RecentLowYes_Bid.When : (TimeSpan?)null;
            recentLowBidTime.Text = FormatTimeSpan(recentLowBidTs);

            allTimeLowAskPrice.Text = (100 - currentSnapshot.AllTimeLowNo_Bid.Bid).ToString();
            TimeSpan? allTimeLowAskTs = currentSnapshot.AllTimeLowNo_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.AllTimeLowNo_Bid.When : (TimeSpan?)null;
            allTimeLowAskTime.Text = FormatTimeSpan(allTimeLowAskTs);
            allTimeLowBidPrice.Text = currentSnapshot.AllTimeLowYes_Bid.Bid.ToString();
            TimeSpan? allTimeLowBidTs = currentSnapshot.AllTimeLowYes_Bid.When != DateTime.MinValue ? currentSnapshot.Timestamp - currentSnapshot.AllTimeLowYes_Bid.When : (TimeSpan?)null;
            allTimeLowBidTime.Text = FormatTimeSpan(allTimeLowBidTs);

            // Update trading metrics
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
            // Note: +DI and -DI values are not displayed in UI labels, only plotted on secondary chart
            supportValue.Text = currentSnapshot.AllSupportResistanceLevels != null && currentSnapshot.AllSupportResistanceLevels.Count != 1
                ? currentSnapshot.AllSupportResistanceLevels.Count.ToString()
                : (currentSnapshot.AllSupportResistanceLevels != null && currentSnapshot.AllSupportResistanceLevels.Count == 1
                    ? $"{currentSnapshot.AllSupportResistanceLevels.First().Price}"
                    : "0");

            // Update other info
            chartHeader.Text = marketTitle;
            categoryValue.Text = currentSnapshot.MarketCategory ?? "--";
            timeLeftValue.Text = FormatTimeSpan(currentSnapshot.TimeLeft);
            marketAgeValue.Text = FormatTimeSpan(currentSnapshot.MarketAge);

            // Update flow/momentum values
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

            // Update context values
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
            depthTop4YesValue.Text = (currentSnapshot.DepthAtTop4YesBids / 100.0).ToString("F2");
            depthTop4NoValue.Text = (currentSnapshot.DepthAtTop4NoBids / 100.0).ToString("F2");
            totalDepthYesValue.Text = (currentSnapshot.TotalOrderbookDepth_Yes / 100.0).ToString("F2");
            totalDepthNoValue.Text = (currentSnapshot.TotalOrderbookDepth_No / 100.0).ToString("F2");
            centerMassYesValue.Text = currentSnapshot.YesBidCenterOfMass.ToString("F2");
            centerMassNoValue.Text = currentSnapshot.NoBidCenterOfMass.ToString("F2");
            totalContractsYesValue.Text = (currentSnapshot.TotalBidContracts_Yes / 100.0).ToString("F2");
            totalContractsNoValue.Text = (currentSnapshot.TotalBidContracts_No / 100.0).ToString("F2");

            // Update positions
            positionSizeValue.Text = currentSnapshot.PositionSize.ToString();
            lastTradeValue.Text = currentSnapshot.TotalTraded.ToString();
            positionRoiValue.Text = currentSnapshot.PositionROI.ToString("F2");
            buyinPriceValue.Text = currentSnapshot.BuyinPrice.ToString("F2");  // Use snapshot data for consistency
            positionUpsideValue.Text = currentSnapshot.PositionUpside.ToString("F2");
            positionDownsideValue.Text = currentSnapshot.PositionDownside.ToString("F2");
            // Use simulated resting orders count passed as parameter
            restingOrdersValue.Text = _hypotheticalRestingOrdersCount.ToString();
            simulatedPositionValue.Text = currentSnapshot.PositionSize.ToString();  // Use snapshot data for consistency

            // Update patterns count for CURRENT snapshot only
            int currentSnapshotPatterns = 0;
            if (_historicalPatternDataPoints != null && currentSnapshot != null)
            {
                // Count patterns that match the current snapshot timestamp (within 1 second)
                currentSnapshotPatterns = _historicalPatternDataPoints.Count(p =>
                    Math.Abs((p.Date - currentSnapshot.Timestamp).TotalSeconds) < 1);
            }
            patternsValue.Text = currentSnapshotPatterns.ToString();

            // Update tooltip to show patterns at CURRENT snapshot only
            string patternsTooltip = "Patterns: Detected technical analysis patterns in price data.";
            if (_historicalPatternDataPoints != null && _historicalPatternDataPoints.Count > 0 && currentSnapshot != null)
            {
                // Find patterns that match the current snapshot timestamp (within 1 second)
                var currentSnapshotPatternsList = _historicalPatternDataPoints
                    .Where(p => Math.Abs((p.Date - currentSnapshot.Timestamp).TotalSeconds) < 1)
                    .Select(p => p.Memo?.Replace("Pattern: ", "") ?? "Unknown")
                    .Distinct()
                    .ToList();

                if (currentSnapshotPatternsList.Any())
                {
                    patternsTooltip += $" Current snapshot has {currentSnapshotPatternsList.Count} pattern(s): {string.Join(", ", currentSnapshotPatternsList)}";
                }
                else
                {
                    patternsTooltip += " No patterns detected at current snapshot.";
                }
            }
            else
            {
                patternsTooltip += " No patterns detected yet.";
            }
            toolTip1.SetToolTip(patternsLabel, patternsTooltip);

            // Update support/resistance levels count
            supportValue.Text = currentSnapshot.AllSupportResistanceLevels != null && currentSnapshot.AllSupportResistanceLevels.Count != 1
                ? currentSnapshot.AllSupportResistanceLevels.Count.ToString()
                : (currentSnapshot.AllSupportResistanceLevels != null && currentSnapshot.AllSupportResistanceLevels.Count == 1
                    ? $"{currentSnapshot.AllSupportResistanceLevels.First().Price}"
                    : "0");

            // Immediately move the vertical line (fast operation)
            MoveVerticalLineImmediately();
        }

        private void MoveVerticalLineImmediately()
        {
            if (currentSnapshot == null) return;

            double centerX = currentSnapshot.Timestamp.ToOADate();

            // Remove existing vertical lines immediately
            var mainPlottables = priceChart.Plot.GetPlottables().ToList();
            for (int i = mainPlottables.Count - 1; i >= 0; i--)
            {
                var plottable = mainPlottables[i];
                if (plottable != null)
                {
                    // Check for ScottPlot vertical line types
                    if (plottable.GetType().Name.Contains("VerticalLine") ||
                        plottable.GetType().FullName.Contains("VerticalLine") ||
                        plottable.ToString().Contains("Vertical"))
                    {
                        priceChart.Plot.Remove(plottable);
                    }
                }
            }

            // Add fresh vertical line
            var verticalLine = priceChart.Plot.AddVerticalLine(centerX, Color.Black, 2);

            // Same for secondary chart
            if (secondaryChart.Visible)
            {
                var secondaryPlottables = secondaryChart.Plot.GetPlottables().ToList();
                for (int i = secondaryPlottables.Count - 1; i >= 0; i--)
                {
                    var plottable = secondaryPlottables[i];
                    if (plottable != null)
                    {
                        if (plottable.GetType().Name.Contains("VerticalLine") ||
                            plottable.GetType().FullName.Contains("VerticalLine") ||
                            plottable.ToString().Contains("Vertical"))
                        {
                            secondaryChart.Plot.Remove(plottable);
                        }
                    }
                }
                var secondaryVerticalLine = secondaryChart.Plot.AddVerticalLine(centerX, Color.Black, 2);
            }

            // Fast refresh
            priceChart.Refresh();
            if (secondaryChart.Visible)
            {
                secondaryChart.Refresh();
            }
        }

        /// <summary>
        /// Navigates through the historical snapshots by a specified number of steps.
        /// Provides programmatic control over snapshot navigation for automated analysis.
        /// </summary>
        /// <param name="delta">Number of snapshots to move forward (positive) or backward (negative) in time.</param>
        /// <remarks>
        /// This method delegates to PerformFastNavigation for efficient UI updates.
        /// Navigation respects boundaries and provides smooth user experience during analysis.
        /// </remarks>
        public async void NavigateToSnapshot(int delta)
        {
            // Use fast navigation for better performance
            PerformFastNavigation(delta);
        }

        /// <summary>
        /// Populates the SnapshotViewer with market data for analysis and visualization.
        /// Initializes the control with a specific snapshot, historical context, and simulation parameters.
        /// </summary>
        /// <param name="snapshot">The initial market snapshot to display and analyze.</param>
        /// <param name="history">Complete chronological sequence of market snapshots for navigation and trend analysis.</param>
        /// <param name="memosList">Strategy memos corresponding to each historical snapshot, providing trading rationale.</param>
        /// <param name="simulatedPosition">Hypothetical position size for backtesting scenarios (default: 0).</param>
        /// <param name="averageCost">Hypothetical average cost basis for the simulated position (default: 0.0).</param>
        /// <param name="simulatedRestingOrders">Count of hypothetical resting orders in the strategy (default: 0).</param>
        /// <param name="positionPoints">Time series data for position size changes during simulation (optional).</param>
        /// <param name="averageCostPoints">Time series data for average cost evolution (optional).</param>
        /// <param name="restingOrdersPoints">Time series data for resting orders count (optional).</param>
        /// <param name="patternPoints">Time series data for detected technical patterns (optional).</param>
        /// <remarks>
        /// This method performs comprehensive data initialization:
        /// - Validates and sets the current snapshot with fallback logic
        /// - Orders historical data chronologically
        /// - Maps simulation data to individual snapshots
        /// - Updates the UI to reflect the new data context
        /// - Handles edge cases like invalid timestamps gracefully
        /// </remarks>
        public void Populate(MarketSnapshot snapshot, List<MarketSnapshot> history, List<string> memosList, int simulatedPosition = 0, double averageCost = 0.0, int simulatedRestingOrders = 0, List<PricePoint>? positionPoints = null, List<PricePoint>? averageCostPoints = null, List<PricePoint>? restingOrdersPoints = null, List<PricePoint>? patternPoints = null)
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

            // Store simulated position and average cost
            _hypotheticalPositionSize = simulatedPosition;
            _hypotheticalAverageCost = averageCost;
            _hypotheticalRestingOrdersCount = simulatedRestingOrders;
            _historicalPositionDataPoints = positionPoints ?? new List<PricePoint>();
            _historicalAverageCostDataPoints = averageCostPoints ?? new List<PricePoint>();
            _historicalRestingOrdersDataPoints = restingOrdersPoints ?? new List<PricePoint>();
            _historicalPatternDataPoints = patternPoints ?? new List<PricePoint>();
            simulatedPositionValue.Text = simulatedPosition.ToString();

            // Map position data to individual snapshots
            if (_historicalPositionDataPoints != null && _historicalPositionDataPoints.Count > 0)
            {
                foreach (var histSnapshot in historySnapshots)
                {
                    var matchingPoint = _historicalPositionDataPoints.FirstOrDefault(p =>
                        Math.Abs((p.Date - histSnapshot.Timestamp).TotalSeconds) < 1); // Match within 1 second
                    if (matchingPoint != null)
                    {
                        histSnapshot.PositionSize = (int)matchingPoint.Price;
                    }
                }
            }

            // Map average cost data to individual snapshots
            if (_historicalAverageCostDataPoints != null && _historicalAverageCostDataPoints.Count > 0)
            {
                foreach (var histSnapshot in historySnapshots)
                {
                    var matchingPoint = _historicalAverageCostDataPoints.FirstOrDefault(p =>
                        Math.Abs((p.Date - histSnapshot.Timestamp).TotalSeconds) < 1); // Match within 1 second
                    if (matchingPoint != null)
                    {
                        histSnapshot.BuyinPrice = matchingPoint.Price;
                    }
                }
            }

            // Map resting orders data to individual snapshots
            if (_historicalRestingOrdersDataPoints != null && _historicalRestingOrdersDataPoints.Count > 0)
            {
                foreach (var histSnapshot in historySnapshots)
                {
                    var matchingPoint = _historicalRestingOrdersDataPoints.FirstOrDefault(p =>
                        Math.Abs((p.Date - histSnapshot.Timestamp).TotalSeconds) < 1); // Match within 1 second
                    if (matchingPoint != null)
                    {
                        // Store resting orders count in a way that can be accessed by the UI
                        // We'll use the existing RestingOrders property or create a simulated count
                        if (histSnapshot.RestingOrders == null)
                        {
                            histSnapshot.RestingOrders = new List<(string, string, string, int, int, DateTime?)>();
                        }
                        // Add dummy resting orders to match the count from simulation
                        int targetCount = (int)matchingPoint.Price;
                        while (histSnapshot.RestingOrders.Count < targetCount)
                        {
                            histSnapshot.RestingOrders.Add(("buy", "yes", "limit", 1, 50, null));
                        }
                    }
                }
            }

            PopulateUIFromSnapshot();  // Full UI update
        }

        private string FormatTimeSpan(TimeSpan? ts)
        {
            return ts.HasValue ? $"{ts.Value.Days} days, {ts.Value.Hours} hours" : "--";
        }

        /// <summary>
        /// Populates the order book display with current bid and ask data from the snapshot.
        /// Organizes and visualizes market depth with proper sorting and color coding.
        /// </summary>
        /// <remarks>
        /// This method handles order book visualization with:
        /// - Separation of bids and asks with distinct color schemes
        /// - Proper sorting (asks descending, bids ascending for display)
        /// - Price filtering to remove noise at extremes
        /// - Visual spacer between bid and ask sides
        /// - Automatic scrolling to center on the current spread
        /// - Size calculations showing both quantity and value
        /// </remarks>
        private async void PopulateUIFromSnapshot()
        {
            // Clear and repopulate order book with bids and asks
            orderbookGrid.Rows.Clear();
            if (currentSnapshot != null)
            {
                // Get yes bids and no bids
                var yesBids = currentSnapshot.GetYesBids();
                var noBids = currentSnapshot.GetNoBids();

                // Track the row index for the spacer
                int spacerRowIndex = -1;

                // Create separate lists for bids and asks
                var bidList = new List<(int price, int size)>();
                var askList = new List<(int price, int size)>();

                // Add asks first (100 - no bid prices) - these will be on top in red
                foreach (var bid in noBids)
                {
                    int askPrice = 100 - bid.Key;
                    // Filter out very low/high prices that might be noise
                    if (askPrice >= 1 && askPrice <= 99)
                    {
                        askList.Add((askPrice, bid.Value));
                    }
                }

                // Add yes bids (keep original prices) - these will be on bottom in teal
                foreach (var bid in yesBids)
                {
                    // Filter out very low prices that might be noise
                    if (bid.Key >= 1 && bid.Key <= 99)
                    {
                        bidList.Add((bid.Key, bid.Value));
                    }
                }

                // Sort bids ascending (lowest to highest) - for display purposes
                bidList = bidList.OrderByDescending(b => b.price).ToList();

                // Sort asks descending (highest to lowest) - for display purposes
                askList = askList.OrderByDescending(a => a.price).ToList();

                // Add all asks
                foreach (var ask in askList)
                {
                    int rowIdx = orderbookGrid.Rows.Add(ask.price.ToString(), ask.size, ((ask.price * ask.size) / 100.0).ToString("F2"));
                    orderbookGrid.Rows[rowIdx].Cells[0].Style.ForeColor = Color.OrangeRed;  // Red for asks
                    orderbookGrid.Rows[rowIdx].Cells[1].Style.ForeColor = Color.OrangeRed;
                    orderbookGrid.Rows[rowIdx].Cells[2].Style.ForeColor = Color.OrangeRed;
                }

                // Add spacer row
                spacerRowIndex = orderbookGrid.Rows.Add("--", "--", "--");
                orderbookGrid.Rows[spacerRowIndex].Cells[0].Style.ForeColor = Color.Gray;
                orderbookGrid.Rows[spacerRowIndex].Cells[1].Style.ForeColor = Color.Gray;
                orderbookGrid.Rows[spacerRowIndex].Cells[2].Style.ForeColor = Color.Gray;
                orderbookGrid.Rows[spacerRowIndex].DefaultCellStyle.BackColor = Color.LightGray;

                // Add all bids first
                foreach (var bid in bidList)
                {
                    int rowIdx = orderbookGrid.Rows.Add(bid.price.ToString(), bid.size, ((bid.price * bid.size) / 100.0).ToString("F2"));
                    orderbookGrid.Rows[rowIdx].Cells[0].Style.ForeColor = Color.FromArgb(0, 206, 209);  // Teal for bids
                    orderbookGrid.Rows[rowIdx].Cells[1].Style.ForeColor = Color.FromArgb(0, 206, 209);
                    orderbookGrid.Rows[rowIdx].Cells[2].Style.ForeColor = Color.FromArgb(0, 206, 209);
                }

                // Scroll to center on the spread area (spacer row)
                if (spacerRowIndex >= 0 && orderbookGrid.Rows.Count > 0)
                {
                    // Force a layout update to ensure DisplayedRowCount is accurate
                    orderbookGrid.PerformLayout();

                    int totalRows = orderbookGrid.Rows.Count;
                    int visibleRows = orderbookGrid.DisplayedRowCount(false);
                    if (visibleRows <= 0) visibleRows = 10; // Default if not yet calculated

                    // Calculate the percentage position of the spacer row
                    double spacerPercentage = (double)spacerRowIndex / Math.Max(1, totalRows - 1);

                    // Calculate the row to scroll to so that the spacer is centered
                    int scrollToRow = (int)(spacerRowIndex - (visibleRows / 2.0));

                    // Ensure we don't scroll past the bounds
                    scrollToRow = Math.Max(0, scrollToRow);
                    int maxScrollRow = Math.Max(0, totalRows - visibleRows);
                    scrollToRow = Math.Min(scrollToRow, maxScrollRow);

                    // Set the scroll position
                    orderbookGrid.FirstDisplayedScrollingRowIndex = scrollToRow;
                }
            }

            // Update chart (moves vertical line and zooms) - but only if not currently evaluating chart filters
            if (!_isEvaluatingChartFilters)
            {
                UpdateChart();
            }

            // Update all UI elements (fast operations)
            _ = RefreshUIQuickly();
        }

        /// <summary>
        /// Updates the main price chart with current market data and technical indicators.
        /// Handles chart rendering, layout adjustments, and zoom state preservation.
        /// </summary>
        /// <remarks>
        /// This method manages comprehensive chart updates including:
        /// - Clearing and re-rendering base price data
        /// - Applying selected technical indicators (RSI, MACD, EMA, etc.)
        /// - Managing secondary chart visibility based on metric selection
        /// - Preserving user zoom/pan state when appropriate
        /// - Updating chart legends and layout
        /// - Handling both primary and secondary chart synchronization
        /// </remarks>
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

                    // Secondary chart maintains independent view
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

        /// <summary>
        /// Handles control resize events to maintain proper typography scaling and chart proportions.
        /// Applies responsive font scaling based on control dimensions and DPI settings.
        /// </summary>
        /// <param name="sender">The source of the resize event.</param>
        /// <param name="e">Event arguments (unused).</param>
        /// <remarks>
        /// This method ensures consistent visual appearance across different screen sizes:
        /// - Calculates scaling factors based on width/height ratios
        /// - Applies conservative DPI-aware typography scaling
        /// - Updates chart axis labels and tick marks for readability
        /// - Only applies scaling when changes are significant (>10% difference)
        /// - Refreshes both primary and secondary charts after scaling
        /// </remarks>
        private void SnapshotViewer_ResizeEnd(object sender, EventArgs e)
        {
            // Calculate isotropic scale factor with DPI awareness (more conservative)
            float widthScale = (float)this.Width / OriginalWidth;
            float heightScale = (float)this.Height / OriginalHeight;
            float baseScale = Math.Clamp(Math.Min(widthScale, heightScale), MinScale, MaxScale);
            float dpiScale = TypographyManager.Instance.GetTypographyScale();

            // Use more conservative scaling to prevent layout issues
            float scaleFactor = Math.Min(baseScale, dpiScale);

            // Only apply typography scaling if it's significantly different from 1.0
            if (Math.Abs(scaleFactor - 1.0f) > 0.1f)
            {
                // Apply enhanced typography with scaling
                TypographyManager.Instance.ApplyTypography(this, scaleFactor);

                // Special handling for ScottPlot charts (typography manager skips them)
                var plot = priceChart.Plot;
                plot.XAxis.LabelStyle(fontSize: 12f * scaleFactor);
                plot.YAxis.LabelStyle(fontSize: 12f * scaleFactor);
                plot.XAxis.TickLabelStyle(fontSize: 10f * scaleFactor);
                plot.YAxis.TickLabelStyle(fontSize: 10f * scaleFactor);

                var secondaryPlot = secondaryChart.Plot;
                secondaryPlot.XAxis.LabelStyle(fontSize: 12f * scaleFactor);
                secondaryPlot.YAxis.LabelStyle(fontSize: 12f * scaleFactor);
                secondaryPlot.XAxis.TickLabelStyle(fontSize: 10f * scaleFactor);
                secondaryPlot.YAxis.TickLabelStyle(fontSize: 10f * scaleFactor);

                // Refresh both charts to apply changes
                priceChart.Refresh();
                secondaryChart.Refresh();
            }
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

        private void ApplyInitialTypography()
        {
            // Apply typography on initial load with conservative DPI scaling
            float dpiScale = TypographyManager.Instance.GetTypographyScale();

            // Use more conservative scaling for initial load to prevent layout issues
            float scaleFactor = Math.Min(dpiScale, 1.2f);

            if (Math.Abs(scaleFactor - 1.0f) > 0.1f)
            {
                TypographyManager.Instance.ApplyTypography(this, scaleFactor);
            }
        }

        /// <summary>
        /// Evaluates and applies chart filter changes with comprehensive re-rendering.
        /// Handles technical indicator toggling and chart layout adjustments with full context updates.
        /// </summary>
        /// <remarks>
        /// This method provides complete chart refresh functionality:
        /// - Temporarily hides charts to prevent visual flickering
        /// - Re-renders all base data and selected indicators
        /// - Adjusts chart layout based on secondary metric visibility
        /// - Preserves appropriate zoom states
        /// - Forces layout updates for proper sizing
        /// - Includes multiple refresh cycles for stability
        /// - Thread-safe with evaluation flag protection
        /// </remarks>
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

                    // Secondary chart maintains independent view
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



        /// <summary>
        /// Renders the fundamental price data (bids, asks, buy/sell signals) on the specified chart.
        /// Attempts to use cached chart data for performance, falling back to snapshot-based rendering.
        /// </summary>
        /// <param name="chart">The ScottPlot chart control to render price data on.</param>
        /// <remarks>
        /// This method implements a two-tier rendering approach:
        /// - Primary: Uses cached chart images from the CacheDir for instant loading
        /// - Fallback: Generates price data directly from market snapshots
        /// Includes vertical line marker at current snapshot time and initial zoom setup.
        /// Handles both cached and dynamic rendering with proper error recovery.
        /// </remarks>
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
                catch (Exception)
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
            var snapshotLine = chart.Plot.AddVerticalLine(centerX, Color.Black, 2);

            // Initially zoom in on the snapshot (e.g., +/- 1 hour, total 2 hour span)
            double spanDays = 2.0 / 24;  // 2 hours
            chart.Plot.SetAxisLimitsX(centerX - spanDays / 2, centerX + spanDays / 2);
            chart.Plot.AxisAutoY();  // Auto-scale Y-axis
            chart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);

            // Store full data range for zoom bounds
            if (!_fullChartDataRange.HasValue && historySnapshots != null && historySnapshots.Count > 0)
            {
                double minTime = historySnapshots.Min(s => s.Timestamp.ToOADate());
                double maxTime = historySnapshots.Max(s => s.Timestamp.ToOADate());
                _fullChartDataRange = (minTime, maxTime);
            }
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

        /// <summary>
        /// Renders primary chart technical indicators and overlays on the main price chart.
        /// Includes EMA, Bollinger Bands, VWAP, PSAR, support/resistance levels, and pattern markers.
        /// </summary>
        /// <param name="chart">The ScottPlot chart control to render technical indicators on.</param>
        /// <remarks>
        /// This method handles comprehensive technical analysis visualization:
        /// - Moving averages (EMA) for trend identification
        /// - Volatility bands (Bollinger) for overbought/oversold signals
        /// - Volume-weighted pricing (VWAP) for institutional benchmarks
        /// - Trend following (PSAR) for stop-loss placement
        /// - Historical support/resistance levels with visual markers
        /// - Technical pattern detection with interactive tooltips
        /// - Dynamic legend generation based on active indicators
        /// </remarks>
        private void RenderY1Metrics(ScottPlot.FormsPlot chart)
        {
            var legendItems = new List<string>();

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

            // Patterns - Plot pattern occurrences as markers with tooltips on PRIMARY chart
            if (patternsLabel.Checked && _historicalPatternDataPoints != null && _historicalPatternDataPoints.Count > 0)
            {
                var patternTimePoints = new List<double>();
                var patternValuePoints = new List<double>();
                var patternTooltips = new List<string>();

                foreach (var patternPoint in _historicalPatternDataPoints)
                {
                    patternTimePoints.Add(patternPoint.Date.ToOADate());
                    patternValuePoints.Add(patternPoint.Price);

                    // Create tooltip with pattern details from Memo field
                    string tooltip = patternPoint.Memo ?? "Pattern";
                    if (tooltip.Contains("Pattern: "))
                    {
                        // Extract pattern name from the memo
                        string patternName = tooltip.Replace("Pattern: ", "");
                        tooltip = $"Pattern: {patternName}";
                    }
                    patternTooltips.Add(tooltip);
                }

                if (patternTimePoints.Count > 0)
                {
                    // Plot patterns as diamond markers on the PRIMARY chart
                    var patternScatter = chart.Plot.AddScatter(patternTimePoints.ToArray(), patternValuePoints.ToArray(),
                        Color.Red, 8, markerShape: ScottPlot.MarkerShape.filledDiamond);
                    patternScatter.Label = "Patterns";
                    legendItems.Add("Patterns");

                    // Add text labels above pattern markers for better visibility
                    for (int i = 0; i < patternTimePoints.Count; i++)
                    {
                        // Add a small text label above each pattern marker
                        var text = chart.Plot.AddText(patternTooltips[i], patternTimePoints[i], patternValuePoints[i] + 0.3,
                            8, Color.Red);
                        text.IsVisible = true; // Always visible for better pattern identification
                    }
                }
            }

            // Add legend if there are items
            if (legendItems.Any())
            {
                chart.Plot.Legend(location: ScottPlot.Alignment.UpperLeft);
            }
        }

        /// <summary>
        /// Renders advanced technical indicators and market metrics on the secondary chart.
        /// Displays MACD, RSI, ATR, ADX, Stochastic, OBV, and various market flow indicators.
        /// </summary>
        /// <param name="chart">The ScottPlot chart control for the secondary chart display.</param>
        /// <remarks>
        /// This method provides comprehensive secondary analysis including:
        /// - Momentum indicators (MACD, RSI, Stochastic) for entry/exit signals
        /// - Volatility measures (ATR) for risk management
        /// - Trend strength (ADX) with directional components
        /// - Volume analysis (OBV) for trend confirmation
        /// - Market microstructure (velocity, order flow, depth) for advanced analysis
        /// - Position and performance metrics for strategy evaluation
        /// - Automatic legend generation and full time range display
        /// </remarks>
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
            var secondarySnapshotLine = chart.Plot.AddVerticalLine(centerX, Color.Black, 2);

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

            // Slope - Plot both Yes and No
            if (slopeCB.Checked)
            {
                var slopeYesPoints = new List<double>();
                var slopeNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    slopeYesPoints.Add(snapshot.YesBidSlopePerMinute_Short);
                    slopeNoPoints.Add(snapshot.NoBidSlopePerMinute_Short);
                }

                if (slopeYesPoints.Count > 1)
                {
                    var slopeYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), slopeYesPoints.ToArray(), Color.Teal, 2);
                    slopeYesScatter.Label = "Slope Yes";
                    var slopeNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), slopeNoPoints.ToArray(), Color.DarkCyan, 2);
                    slopeNoScatter.Label = "Slope No";
                    legendItems.Add("Slope");
                }
            }

            // ADX - Plot over time
            if (adxLabel.Checked)
            {
                var adxPoints = new List<double>();
                var plusDIPoints = new List<double>();
                var minusDIPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    if (snapshot.ADX.HasValue)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        adxPoints.Add(snapshot.ADX.Value);
                        plusDIPoints.Add(snapshot.PlusDI.GetValueOrDefault());
                        minusDIPoints.Add(snapshot.MinusDI.GetValueOrDefault());
                    }
                }

                if (adxPoints.Count > 1)
                {
                    var adxScatter = chart.Plot.AddScatter(timePoints.ToArray(), adxPoints.ToArray(), Color.Brown, 2);
                    adxScatter.Label = "ADX";
                    var plusDIScatter = chart.Plot.AddScatter(timePoints.ToArray(), plusDIPoints.ToArray(), Color.DarkGreen, 2);
                    plusDIScatter.Label = "+DI";
                    var minusDIScatter = chart.Plot.AddScatter(timePoints.ToArray(), minusDIPoints.ToArray(), Color.DarkRed, 2);
                    minusDIScatter.Label = "-DI";
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

            // Top Velocity - Plot both Yes and No
            if (topVelocityCB.Checked)
            {
                var topVelYesPoints = new List<double>();
                var topVelNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    topVelYesPoints.Add(snapshot.VelocityPerMinute_Top_Yes_Bid);
                    topVelNoPoints.Add(snapshot.VelocityPerMinute_Top_No_Bid);
                }

                if (topVelYesPoints.Count > 1)
                {
                    var topVelYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), topVelYesPoints.ToArray(), Color.DarkGreen, 2);
                    topVelYesScatter.Label = "Top Velocity Yes";
                    var topVelNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), topVelNoPoints.ToArray(), Color.LightGreen, 2);
                    topVelNoScatter.Label = "Top Velocity No";
                    legendItems.Add("Top Velocity");
                }
            }

            // Bottom Velocity - Plot both Yes and No
            if (bottomVelocityCB.Checked)
            {
                var bottomVelYesPoints = new List<double>();
                var bottomVelNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    bottomVelYesPoints.Add(snapshot.VelocityPerMinute_Bottom_Yes_Bid);
                    bottomVelNoPoints.Add(snapshot.VelocityPerMinute_Bottom_No_Bid);
                }

                if (bottomVelYesPoints.Count > 1)
                {
                    var bottomVelYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), bottomVelYesPoints.ToArray(), Color.DarkRed, 2);
                    bottomVelYesScatter.Label = "Bottom Velocity Yes";
                    var bottomVelNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), bottomVelNoPoints.ToArray(), Color.LightCoral, 2);
                    bottomVelNoScatter.Label = "Bottom Velocity No";
                    legendItems.Add("Bottom Velocity");
                }
            }

            // Net Order Rate - Plot both Yes and No
            if (netOrderRateCB.Checked)
            {
                var netOrderYesPoints = new List<double>();
                var netOrderNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    netOrderYesPoints.Add(snapshot.TradeRatePerMinute_Yes);
                    netOrderNoPoints.Add(snapshot.TradeRatePerMinute_No);
                }

                if (netOrderYesPoints.Count > 1)
                {
                    var netOrderYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), netOrderYesPoints.ToArray(), Color.Purple, 2);
                    netOrderYesScatter.Label = "Net Order Rate Yes";
                    var netOrderNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), netOrderNoPoints.ToArray(), Color.MediumPurple, 2);
                    netOrderNoScatter.Label = "Net Order Rate No";
                    legendItems.Add("Net Order Rate");
                }
            }

            // Trade Volume - Plot both Yes and No
            if (tradeVolumeCB.Checked)
            {
                var tradeVolYesPoints = new List<double>();
                var tradeVolNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    tradeVolYesPoints.Add(snapshot.TradeVolumePerMinute_Yes);
                    tradeVolNoPoints.Add(snapshot.TradeVolumePerMinute_No);
                }

                if (tradeVolYesPoints.Count > 1)
                {
                    var tradeVolYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), tradeVolYesPoints.ToArray(), Color.Orange, 2);
                    tradeVolYesScatter.Label = "Trade Volume Yes";
                    var tradeVolNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), tradeVolNoPoints.ToArray(), Color.DarkOrange, 2);
                    tradeVolNoScatter.Label = "Trade Volume No";
                    legendItems.Add("Trade Volume");
                }
            }

            // Average Trade Size - Plot both Yes and No
            if (avgTradeSizeCB.Checked)
            {
                var avgSizeYesPoints = new List<double>();
                var avgSizeNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    avgSizeYesPoints.Add(snapshot.AverageTradeSize_Yes);
                    avgSizeNoPoints.Add(snapshot.AverageTradeSize_No);
                }

                if (avgSizeYesPoints.Count > 1)
                {
                    var avgSizeYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), avgSizeYesPoints.ToArray(), Color.Pink, 2);
                    avgSizeYesScatter.Label = "Avg Trade Size Yes";
                    var avgSizeNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), avgSizeNoPoints.ToArray(), Color.LightPink, 2);
                    avgSizeNoScatter.Label = "Avg Trade Size No";
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

            // Depth Top 4 - Plot both Yes and No
            if (depthTop4CB.Checked)
            {
                var depthYesPoints = new List<double>();
                var depthNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    depthYesPoints.Add(snapshot.DepthAtTop4YesBids);
                    depthNoPoints.Add(snapshot.DepthAtTop4NoBids);
                }

                if (depthYesPoints.Count > 1)
                {
                    var depthYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), depthYesPoints.ToArray(), Color.Maroon, 2);
                    depthYesScatter.Label = "Depth Top 4 Yes";
                    var depthNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), depthNoPoints.ToArray(), Color.IndianRed, 2);
                    depthNoScatter.Label = "Depth Top 4 No";
                    legendItems.Add("Depth Top 4");
                }
            }

            // Center Mass - Plot both Yes and No
            if (centerMassCB.Checked)
            {
                var centerMassYesPoints = new List<double>();
                var centerMassNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    centerMassYesPoints.Add(snapshot.YesBidCenterOfMass);
                    centerMassNoPoints.Add(snapshot.NoBidCenterOfMass);
                }

                if (centerMassYesPoints.Count > 1)
                {
                    var centerMassYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), centerMassYesPoints.ToArray(), Color.Olive, 2);
                    centerMassYesScatter.Label = "Center Mass Yes";
                    var centerMassNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), centerMassNoPoints.ToArray(), Color.OliveDrab, 2);
                    centerMassNoScatter.Label = "Center Mass No";
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

            // Total Depth - Plot both Yes and No
            if (totalDepthCB.Checked)
            {
                var depthYesPoints = new List<double>();
                var depthNoPoints = new List<double>();
                var timePoints = new List<double>();

                foreach (var snapshot in historySnapshots)
                {
                    timePoints.Add(snapshot.Timestamp.ToOADate());
                    depthYesPoints.Add(snapshot.TotalOrderbookDepth_Yes);
                    depthNoPoints.Add(snapshot.TotalOrderbookDepth_No);
                }

                if (depthYesPoints.Count > 1)
                {
                    var depthYesScatter = chart.Plot.AddScatter(timePoints.ToArray(), depthYesPoints.ToArray(), Color.Gold, 2);
                    depthYesScatter.Label = "Total Depth Yes";
                    var depthNoScatter = chart.Plot.AddScatter(timePoints.ToArray(), depthNoPoints.ToArray(), Color.Yellow, 2);
                    depthNoScatter.Label = "Total Depth No";
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

                // Use cached position data if available, otherwise fall back to snapshot data
                if (_historicalPositionDataPoints != null && _historicalPositionDataPoints.Count > 0)
                {
                    foreach (var point in _historicalPositionDataPoints)
                    {
                        timePoints.Add(point.Date.ToOADate());
                        simulatedPosPoints.Add(point.Price);
                    }
                }
                else
                {
                    foreach (var snapshot in historySnapshots)
                    {
                        timePoints.Add(snapshot.Timestamp.ToOADate());
                        simulatedPosPoints.Add(snapshot.PositionSize); // Using position size as proxy
                    }
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
                chart.Plot.Legend(location: ScottPlot.Alignment.UpperLeft);
            }

            // Set up secondary chart axes - show full context, don't zoom to current snapshot
            chart.Plot.XAxis.TickLabelFormat("yyyy-MM-dd HH:mm", dateTimeFormat: true);
            chart.Plot.AxisAutoX(); // Auto-scale X-axis to show full context
            chart.Plot.AxisAutoY(); // Auto-scale Y-axis

            // Store full data range for zoom constraints (only if not already set)
            if (!_fullChartDataRange.HasValue && historySnapshots != null && historySnapshots.Count > 0)
            {
                double minTime = historySnapshots.Min(s => s.Timestamp.ToOADate());
                double maxTime = historySnapshots.Max(s => s.Timestamp.ToOADate());
                _fullChartDataRange = (minTime, maxTime);
            }
        }

        /// <summary>
        /// Determines whether any secondary chart metrics are currently enabled for display.
        /// Used to control secondary chart visibility and layout adjustments.
        /// </summary>
        /// <returns>True if any secondary metrics checkboxes are checked, false otherwise.</returns>
        /// <remarks>
        /// This method checks all secondary chart metric controls including:
        /// - Technical indicators (MACD, RSI, ATR, ADX, Stochastic, OBV)
        /// - Market flow metrics (velocity, order rates, trade volume, size)
        /// - Order book analysis (imbalance, depth, center mass, contracts)
        /// - Position metrics (size, ROI, resting orders)
        /// - Pattern detection
        /// Returns true if any are enabled, triggering secondary chart display.
        /// </remarks>
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
                   restingOrdersLabel.Checked ||
                   rsiLabel.Checked ||
                   patternsLabel.Checked;
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

        private void patternsLabel_CheckedChanged(object sender, EventArgs e)
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
