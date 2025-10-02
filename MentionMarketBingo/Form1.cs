using BacklashBot.KalshiAPI.Interfaces;
using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashDTOs.KalshiAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BacklashInterfaces.PerformanceMetrics;
using BacklashCommon.Configuration;
using System.Data.SqlClient;
using BacklashInterfaces.Constants;
using System.IO;

namespace MentionMarketBingo;

public partial class Form1 : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MentionMarketBingoOrderBookService _orderBookService;
    private readonly MentionMarketBingoWebSocketMonitorService _webSocketMonitorService;
    private List<EventDTO> _events = new();
    private List<string> _currentMarketTickers = new();

    public Form1(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _orderBookService = serviceProvider.GetRequiredService<MentionMarketBingoOrderBookService>();
        _webSocketMonitorService = serviceProvider.GetRequiredService<MentionMarketBingoWebSocketMonitorService>();
        InitializeComponent();
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        // Load config
        LoadConfig();

        await InitializeAsync();

        // Start the orderbook service
        await _orderBookService.StartAsync();

        // Start the WebSocket monitor service
        await _webSocketMonitorService.StartAsync();

        // Subscribe to orderbook updates
        _orderBookService.OrderBookUpdated += OnOrderBookUpdated;
    }

    private async Task InitializeAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();


            // Fetch all open events from API (pagination handled internally)
            var eventsResponse = await Task.Run(() => apiService.GetEventsAsync(
                status: KalshiConstants.Status_Open,
                limit: 200,
                withNestedMarkets: false
            ));

            var allEvents = eventsResponse?.Events ?? new List<KalshiEvent>();

            // Filter events that contain "mention" in the ticker
            var mentionEvents = allEvents
                .Where(e => e.EventTicker?.Contains("mention", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            // Convert to EventDTO for the UI
            _events = mentionEvents.Select(apiEvent => new EventDTO
            {
                event_ticker = apiEvent.EventTicker,
                title = apiEvent.Title,
                sub_title = apiEvent.SubTitle,
                category = apiEvent.Category,
                series_ticker = apiEvent.SeriesTicker,
                mutually_exclusive = apiEvent.MutuallyExclusive,
                collateral_return_type = apiEvent.CollateralReturnType
            }).ToList();

            if (_events.Count == 0)
            {
                MessageBox.Show("No events with 'mention' in the ticker could be found.", "No Events Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            eventComboBox.DataSource = _events;
            eventComboBox.DisplayMember = "title";
            eventComboBox.ValueMember = "event_ticker";

            // Load markets for the first event
            if (_events.Count > 0)
            {
                eventComboBox.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load events: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void eventComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (eventComboBox.SelectedItem == null) return;

        var selectedEvent = (EventDTO)eventComboBox.SelectedItem;

        try
        {
            // Unsubscribe from previous markets
            foreach (var ticker in _currentMarketTickers)
            {
                await _orderBookService.UnsubscribeFromMarket(ticker);
            }
            _currentMarketTickers.Clear();

            // Fetch markets for the selected event from API with nested markets, offloaded to thread pool
            using var scope = _serviceProvider.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            var eventResponse = await Task.Run(() => apiService.FetchEventAsync(selectedEvent.event_ticker, withNestedMarkets: true));
            if (eventResponse?.Event?.Markets == null)
            {
                MessageBox.Show($"No markets found for event '{selectedEvent.title}' (ticker: {selectedEvent.event_ticker}).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var apiMarkets = eventResponse.Event.Markets;

            // Filter for active markets only
            var activeMarkets = apiMarkets.Where(m => m.Status == "active").ToList();

            if (activeMarkets.Count == 0)
            {
                MessageBox.Show($"No active markets found for event '{selectedEvent.title}' (ticker: {selectedEvent.event_ticker}). Loading all markets for this event.", "Debug Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                activeMarkets = apiMarkets; // Use all markets if no active ones
            }

            // Clear existing buttons
            bingoPanel.Controls.Clear();
            bingoPanel.ColumnCount = 5;
            int rows = (activeMarkets.Count + 4) / 5;
            bingoPanel.RowCount = rows;

            // Create panels for each market, arranged in a grid
            for (int i = 0; i < activeMarkets.Count; i++)
            {
                var market = activeMarkets[i];
                var panel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    Tag = market,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.LightGray
                };

                // Create table layout for organized structure
                var tableLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    RowCount = 4,
                    ColumnCount = 3,
                    Padding = new Padding(2)
                };
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

                // Row 0: Title
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
                var subtitleLabel = new Label
                {
                    Text = market.YesSubTitle ?? market.Ticker ?? "Unknown",
                    Font = new Font(DefaultFont.FontFamily, DefaultFont.Size + 2, FontStyle.Bold),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tableLayout.Controls.Add(subtitleLabel, 0, 0);
                tableLayout.SetColumnSpan(subtitleLabel, 3);

                // Row 1: Price labels
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                var yesLabel = new Label
                {
                    Text = $"Yes: {market.YesBid}¢",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var noLabel = new Label
                {
                    Text = $"No: {market.NoBid}¢",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                tableLayout.Controls.Add(yesLabel, 0, 1);
                tableLayout.Controls.Add(noLabel, 2, 1);

                // Row 2: Dollar amount labels
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
                var yesDollarLabel = new Label
                {
                    Text = "$0.00",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(DefaultFont.FontFamily, DefaultFont.Size - 1),
                    ForeColor = Color.DarkGreen
                };
                var noDollarLabel = new Label
                {
                    Text = "$0.00",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(DefaultFont.FontFamily, DefaultFont.Size - 1),
                    ForeColor = Color.DarkRed
                };
                tableLayout.Controls.Add(yesDollarLabel, 0, 2);
                tableLayout.Controls.Add(noDollarLabel, 2, 2);

                // Row 3: Progress bars
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
                var yesProgressBar = new ProgressBar
                {
                    Dock = DockStyle.Fill,
                    Style = ProgressBarStyle.Continuous,
                    Maximum = 100,
                    Value = 0,
                    Tag = "yes"
                };
                var noProgressBar = new ProgressBar
                {
                    Dock = DockStyle.Fill,
                    Style = ProgressBarStyle.Continuous,
                    Maximum = 100,
                    Value = 0,
                    Tag = "no"
                };

                // Custom paint for green/red colors
                yesProgressBar.Paint += (s, e) =>
                {
                    var pb = (ProgressBar)s;
                    var rect = new Rectangle(0, 0, pb.Width, pb.Height);
                    var fillRect = new Rectangle(0, 0, (int)(pb.Width * pb.Value / (double)pb.Maximum), pb.Height);

                    // Draw background
                    e.Graphics.FillRectangle(Brushes.LightGray, rect);
                    // Draw filled portion in green
                    if (fillRect.Width > 0)
                        e.Graphics.FillRectangle(Brushes.Green, fillRect);
                };

                noProgressBar.Paint += (s, e) =>
                {
                    var pb = (ProgressBar)s;
                    var rect = new Rectangle(0, 0, pb.Width, pb.Height);
                    var fillRect = new Rectangle(0, 0, (int)(pb.Width * pb.Value / (double)pb.Maximum), pb.Height);

                    // Draw background
                    e.Graphics.FillRectangle(Brushes.LightGray, rect);
                    // Draw filled portion in red
                    if (fillRect.Width > 0)
                        e.Graphics.FillRectangle(Brushes.Red, fillRect);
                };

                tableLayout.Controls.Add(yesProgressBar, 0, 3);
                tableLayout.Controls.Add(noProgressBar, 2, 3);

                // Store references for later updates
                panel.Controls.Add(tableLayout);

                panel.Click += (s, e) => MarketPanel_Click(panel, market);
                subtitleLabel.Click += (s, e) => MarketPanel_Click(panel, market);
                yesLabel.Click += (s, e) => MarketPanel_Click(panel, market);
                noLabel.Click += (s, e) => MarketPanel_Click(panel, market);

                // Calculate row and column
                int row = i / 5;
                int col = i % 5;
                bingoPanel.Controls.Add(panel, col, row);
            }

            // Subscribe to new markets
            _currentMarketTickers = activeMarkets.Select(m => m.Ticker).ToList();

            // Subscribe to all markets at once for faster orderbook loading
            await _orderBookService.SubscribeToMarkets(_currentMarketTickers.ToArray());

            // Track markets for monitoring purposes
            await _webSocketMonitorService.SubscribeToEventMarketsAsync(selectedEvent.event_ticker, _currentMarketTickers.ToArray());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load markets: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MarketButton_Click(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var market = (KalshiMarket)button.Tag;

        MessageBox.Show($"Selected market: {market.Title}\nTicker: {market.Ticker}", "Market Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async void MarketPanel_Click(Panel panel, KalshiMarket market)
    {
        panel.Enabled = false; // Disable to prevent multiple clicks
        try
        {
            if (!decimal.TryParse(maxExposureTextBox.Text, out decimal maxExposureDollars) || maxExposureDollars <= 0)
            {
                MessageBox.Show("Invalid Max Exposure value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!decimal.TryParse(maxYesPriceTextBox.Text, out decimal maxYesPriceDollars) || maxYesPriceDollars <= 0)
            {
                MessageBox.Show("Invalid Max Yes Price value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int maxExposureCents = (int)(maxExposureDollars * 100);
            int yesPriceCents = (int)(maxYesPriceDollars * 100);
            int count = maxExposureCents / yesPriceCents;

            if (count <= 0)
            {
                MessageBox.Show("Max Exposure too low for the given Max Yes Price.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            var orderRequest = new CreateOrderRequest
            {
                Action = "buy",
                Type = "limit",
                Ticker = market.Ticker,
                Side = "yes",
                Count = count,
                YesPrice = yesPriceCents,
                ClientOrderId = Guid.NewGuid().ToString()
            };

            //var response = await apiService.CreateOrderAsync(market.Ticker, orderRequest);

            //if (response != null)
            //{
            //    MessageBox.Show($"Order placed successfully!\nOrder ID: {response.Order.OrderId}\nCount: {count}\nPrice: ${maxYesPriceDollars:F2}", "Order Placed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    // Keep disabled
            //}
            //else
            //{
            //    MessageBox.Show("Failed to place order.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    panel.Enabled = true; // Re-enable on failure
            //}
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error placing order: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            panel.Enabled = true; // Re-enable on error
        }
    }

    private async void RefreshButton_Click(object sender, EventArgs e)
    {
        await InitializeAsync();
    }

    private void BuyNosButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show("Event finished - buying No positions for all active markets.", "Buy Nos", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void maxExposureTextBox_TextChanged(object sender, EventArgs e)
    {
        SaveConfig();
    }

    private void maxNoPriceTextBox_TextChanged(object sender, EventArgs e)
    {
        SaveConfig();
    }

    private void maxYesPriceTextBox_TextChanged(object sender, EventArgs e)
    {
        SaveConfig();
    }

    private async void refreshButton_Click(object sender, EventArgs e)
    {
        await InitializeAsync();
    }

    private void buyNosButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show("Event finished - buying No positions for all active markets.", "Buy Nos", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadConfig()
    {
        if (File.Exists("config.txt"))
        {
            var lines = File.ReadAllLines("config.txt");
            if (lines.Length >= 3)
            {
                maxExposureTextBox.Text = lines[0];
                maxNoPriceTextBox.Text = lines[1];
                maxYesPriceTextBox.Text = lines[2];
            }
        }
    }

    private void SaveConfig()
    {
        File.WriteAllLines("config.txt", new[] { maxExposureTextBox.Text, maxNoPriceTextBox.Text, maxYesPriceTextBox.Text });
    }

    private void OnOrderBookUpdated(object? sender, string ticker)
    {
        // Find the panel for this ticker
        var panel = bingoPanel.Controls.OfType<Panel>().FirstOrDefault(p => ((KalshiMarket)p.Tag).Ticker == ticker);
        if (panel == null) return;

        var orderBook = _orderBookService.GetOrderBook(ticker);
        var yesBid = orderBook.LastOrDefault(o => o.Side == "yes")?.Price ?? 0;
        var noBid = orderBook.LastOrDefault(o => o.Side == "no")?.Price ?? 0;

        // Calculate dollar values for resting orders
        var (yesDollarValue, noDollarValue) = CalculateOrderBookDollarValues(orderBook);

        // Update labels and bars on UI thread
        if (panel.InvokeRequired)
        {
            panel.Invoke(() => UpdatePanelLabelsAndBars(panel, yesBid, noBid, yesDollarValue, noDollarValue));
        }
        else
        {
            UpdatePanelLabelsAndBars(panel, yesBid, noBid, yesDollarValue, noDollarValue);
        }

        // Update all progress bars to maintain relative scaling
        UpdateAllProgressBars();
    }

    private (decimal yesDollarValue, decimal noDollarValue) CalculateOrderBookDollarValues(List<OrderbookData> orderBook)
    {
        decimal yesTotal = 0;
        decimal noTotal = 0;

        foreach (var order in orderBook)
        {
            // Price is in cents, convert to dollars and multiply by resting contracts
            decimal priceInDollars = order.Price / 100.0m;
            decimal orderValue = priceInDollars * order.RestingContracts;

            if (order.Side == "yes")
            {
                yesTotal += orderValue;
            }
            else if (order.Side == "no")
            {
                noTotal += orderValue;
            }
        }

        return (yesTotal, noTotal);
    }

    private void UpdatePanelLabelsAndBars(Panel panel, int yesBid, int noBid, decimal yesDollarValue, decimal noDollarValue)
    {
        // Find the table layout panel
        var tableLayout = panel.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
        if (tableLayout == null) return;

        // Update price labels (row 1)
        var yesLabel = tableLayout.GetControlFromPosition(0, 1) as Label;
        var noLabel = tableLayout.GetControlFromPosition(2, 1) as Label;
        if (yesLabel != null) yesLabel.Text = $"Yes: {yesBid}¢";
        if (noLabel != null) noLabel.Text = $"No: {noBid}¢";

        // Update dollar amount labels (row 2)
        var yesDollarLabel = tableLayout.GetControlFromPosition(0, 2) as Label;
        var noDollarLabel = tableLayout.GetControlFromPosition(2, 2) as Label;
        if (yesDollarLabel != null) yesDollarLabel.Text = $"${yesDollarValue:F2}";
        if (noDollarLabel != null) noDollarLabel.Text = $"${noDollarValue:F2}";
    }

    private void UpdateAllProgressBars()
    {
        // Calculate max dollar values across all markets for relative scaling
        decimal maxYesValue = 0;
        decimal maxNoValue = 0;

        foreach (var panel in bingoPanel.Controls.OfType<Panel>())
        {
            var market = (KalshiMarket)panel.Tag;
            var orderBook = _orderBookService.GetOrderBook(market.Ticker);
            var (yesValue, noValue) = CalculateOrderBookDollarValues(orderBook);

            maxYesValue = Math.Max(maxYesValue, yesValue);
            maxNoValue = Math.Max(maxNoValue, noValue);
        }

        // Update all progress bars with relative scaling
        foreach (var panel in bingoPanel.Controls.OfType<Panel>())
        {
            var market = (KalshiMarket)panel.Tag;
            var orderBook = _orderBookService.GetOrderBook(market.Ticker);
            var (yesValue, noValue) = CalculateOrderBookDollarValues(orderBook);

            var tableLayout = panel.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
            if (tableLayout == null) continue;

            // Update progress bars (row 3)
            var yesProgressBar = tableLayout.GetControlFromPosition(0, 3) as ProgressBar;
            var noProgressBar = tableLayout.GetControlFromPosition(2, 3) as ProgressBar;

            if (yesProgressBar != null)
            {
                int yesPercentage = maxYesValue > 0 ? (int)((yesValue / maxYesValue) * 100) : 0;
                yesProgressBar.Value = Math.Min(yesPercentage, 100);
            }

            if (noProgressBar != null)
            {
                int noPercentage = maxNoValue > 0 ? (int)((noValue / maxNoValue) * 100) : 0;
                noProgressBar.Value = Math.Min(noPercentage, 100);
            }
        }
    }
}