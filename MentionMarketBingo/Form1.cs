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

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var apiService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();

            // Test API call, offloaded to thread pool to avoid sync context deadlock
            try
            {
                var testResponse = await Task.Run(() => apiService.GetExchangeStatusAsync()).ConfigureAwait(false);
                if (testResponse != null)
                {
                    MessageBox.Show($"Test API call succeeded. Exchange status: {testResponse.exchange_active}", "Test Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Test API call returned null", "Test Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception testEx)
            {
                MessageBox.Show($"Test API call failed: {testEx.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Stop if test fails
            }

            // Fetch all open events from API using pagination, with offloaded calls
            var allEvents = new List<KalshiEvent>();
            string? cursor = null;

            do
            {
                var eventsResponse = await Task.Run(() => apiService.GetEventsAsync(
                    limit: 50,
                    status: KalshiConstants.Status_Open,
                    cursor: cursor,
                    withNestedMarkets: false
                )).ConfigureAwait(false);

                if (eventsResponse?.Events != null)
                {
                    allEvents.AddRange(eventsResponse.Events);
                    cursor = eventsResponse.Cursor;
                }
                else
                {
                    cursor = null; // No more pages
                }

                // Add rate limiting delay between requests to avoid API throttling
                if (!string.IsNullOrEmpty(cursor))
                {
                    await Task.Delay(250).ConfigureAwait(false); // 250ms delay
                }
            } while (!string.IsNullOrEmpty(cursor));

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

            var eventResponse = await Task.Run(() => apiService.FetchEventAsync(selectedEvent.event_ticker, withNestedMarkets: true)).ConfigureAwait(false);
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

            // Create buttons for each market, arranged in a 5x5 grid
            int maxButtons = Math.Min(activeMarkets.Count, 25); // Limit to 25 for bingo board
            for (int i = 0; i < maxButtons; i++)
            {
                var market = activeMarkets[i];
                var button = new Button
                {
                    Text = market.Title ?? market.Ticker ?? "Unknown",
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    Tag = market
                };
                button.Click += MarketButton_Click;

                // Calculate row and column for 5x5 grid
                int row = i / 5;
                int col = i % 5;
                bingoPanel.Controls.Add(button, col, row);
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
}