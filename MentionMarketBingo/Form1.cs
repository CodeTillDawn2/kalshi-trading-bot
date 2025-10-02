using BacklashBot.KalshiAPI.Interfaces;
using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
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
using BacklashDTOs.KalshiAPI;

namespace MentionMarketBingo;

public partial class Form1 : Form
{
    private readonly IServiceProvider _serviceProvider;
    private List<EventDTO> _events = new();

    public Form1(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        InitializeComponent();
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        // Load config
        LoadConfig();

        await InitializeAsync();
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

                var subtitleLabel = new Label
                {
                    Text = market.YesSubTitle ?? market.Ticker ?? "Unknown",
                    Font = new Font(DefaultFont.FontFamily, DefaultFont.Size + 2, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = 40
                };

                var yesLabel = new Label
                {
                    Text = $"Yes: {market.YesBid}¢",
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = 20
                };

                var noLabel = new Label
                {
                    Text = $"No: {market.NoBid}¢",
                    Dock = DockStyle.Top,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Height = 20
                };

                panel.Controls.Add(noLabel);
                panel.Controls.Add(yesLabel);
                panel.Controls.Add(subtitleLabel);

                panel.Click += (s, e) => MarketPanel_Click(panel, market);
                subtitleLabel.Click += (s, e) => MarketPanel_Click(panel, market);
                yesLabel.Click += (s, e) => MarketPanel_Click(panel, market);
                noLabel.Click += (s, e) => MarketPanel_Click(panel, market);

                // Calculate row and column
                int row = i / 5;
                int col = i % 5;
                bingoPanel.Controls.Add(panel, col, row);
            }
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

            var response = await apiService.CreateOrderAsync(market.Ticker, orderRequest);

            if (response != null)
            {
                MessageBox.Show($"Order placed successfully!\nOrder ID: {response.Order.OrderId}\nCount: {count}\nPrice: ${maxYesPriceDollars:F2}", "Order Placed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Keep disabled
            }
            else
            {
                MessageBox.Show("Failed to place order.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                panel.Enabled = true; // Re-enable on failure
            }
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
}