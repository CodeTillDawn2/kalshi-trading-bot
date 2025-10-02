using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashDTOs.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BacklashInterfaces.PerformanceMetrics;
using BacklashCommon.Configuration;
using System.Data.SqlClient;

namespace MentionMarketBingo;

public partial class Form1 : Form
{
    private BacklashBotContext? _context;
    private List<EventDTO> _events = new();
    private List<MarketDTO> _markets = new();

    public Form1()
    {
        InitializeComponent();
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            // Load configuration using ConfigurationHelper
            var configBuilder = ConfigurationHelper.CreateConfigurationBuilder(AppDomain.CurrentDomain.BaseDirectory, Array.Empty<string>());
            var configuration = configBuilder.Build();

            // Build connection string with secrets interpolation
            string connectionString = ConfigurationHelper.BuildConnectionString(configuration);

            // Get BacklashBotData configuration
            var dataConfig = configuration.GetSection("BacklashBotData").Get<BacklashBotDataConfig>();
            if (dataConfig == null)
            {
                throw new InvalidOperationException("BacklashBotData configuration section is missing or invalid");
            }

            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<BacklashBotContext>();
            var performanceMonitor = new MockPerformanceMonitor();

            _context = new BacklashBotContext(connectionString, logger, dataConfig, performanceMonitor);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize database: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        if (_context == null) return;

        try
        {
            // Load events with "mention" in ticker
            _events = await _context.GetEvents(tickerWildcard: "%mention%");

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
        if (_context == null || eventComboBox.SelectedItem == null) return;

        var selectedEvent = (EventDTO)eventComboBox.SelectedItem;

        try
        {
            // Load markets for the selected event
            var activeStatuses = new HashSet<string> { "active" };
            _markets = await _context.GetMarkets(includedStatuses: activeStatuses, eventTicker: selectedEvent.event_ticker);

            // Clear existing buttons
            bingoPanel.Controls.Clear();

            // Create buttons for each market
            foreach (var market in _markets)
            {
                var button = new Button
                {
                    Text = market.title ?? market.market_ticker ?? "Unknown",
                    Size = new Size(120, 80),
                    Margin = new Padding(5),
                    Tag = market
                };
                button.Click += MarketButton_Click;
                bingoPanel.Controls.Add(button);
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
        var market = (MarketDTO)button.Tag;

        MessageBox.Show($"Selected market: {market.title}\nTicker: {market.market_ticker}", "Market Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

// Mock performance monitor for simplicity
public class MockPerformanceMonitor : IPerformanceMonitor
{
    public void RecordSpeedDialMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null)
    {
        // Do nothing
    }

    public void RecordProgressBarMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null)
    {
        // Do nothing
    }

    public void RecordCounterMetric(string className, string id, string name, string description, double value, string unit, string category)
    {
        // Do nothing
    }

    public void RecordTrafficLightMetric(string className, string id, string name, string description, double value, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null)
    {
        // Do nothing
    }

    public void RecordPieChartMetric(string className, string id, string name, string description, double value, double? secondaryValue, string unit, string category, double? minThreshold = null, double? warningThreshold = null, double? criticalThreshold = null)
    {
        // Do nothing
    }

    public void RecordNumericDisplayMetric(string className, string id, string name, string description, double value, string unit, string category)
    {
        // Do nothing
    }

    public void RecordBadgeMetric(string className, string id, string name, string description, double value, string unit, string category)
    {
        // Do nothing
    }

    public void RecordDisabledMetric(string className, string id, string name, string description, double value, string unit, string category)
    {
        // Do nothing
    }
}
