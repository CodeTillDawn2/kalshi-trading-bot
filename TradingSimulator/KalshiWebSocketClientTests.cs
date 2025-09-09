using KalshiBotAPI.Configuration;
using KalshiBotAPI.Websockets;
using KalshiBotData.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BacklashBot.Configuration;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using System.Net.WebSockets;
using System.Text;


namespace TradingSimulator.Tests
{
    [TestFixture]
    public class KalshiWebSocketClientTests
    {
        private Mock<ILogger<KalshiWebSocketClient>> _loggerMock;
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        private Mock<ISqlDataService> _sqlDataService;
        private Mock<IServiceScopeFactory> _scopeFactoryMock;
        private Mock<IStatusTrackerService> _statusTracker;
        private Mock<IBotReadyStatus> _readyStatus;
        private Mock<IServiceFactory> _serviceFactoryMock;
        private SqlDataService _sqlService;
        private KalshiWebSocketClient _client;
        private IOptions<KalshiConfig> _kalshiConfigOptions;
        private IOptions<LoggingConfig> _loggingConfigOptions;
        private IConfiguration _configuration;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<KalshiWebSocketClient>>();
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _statusTracker = new Mock<IStatusTrackerService>();
            _readyStatus = new Mock<IBotReadyStatus>();
            _sqlDataService = new Mock<ISqlDataService>();

            var initializationCompleted = new TaskCompletionSource<bool>();
            initializationCompleted.SetResult(true); // Simulate initialization completed
            _readyStatus.SetupGet(st => st.InitializationCompleted).Returns(initializationCompleted);

            var cts = new CancellationTokenSource();
            _statusTracker.Setup(st => st.GetCancellationToken()).Returns(cts.Token);

            // Load configuration from appsettings.local.json
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            _configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: false)
                .Build();

            var kalshiConfig = new KalshiConfig();
            _configuration.GetSection("Kalshi").Bind(kalshiConfig);

            var loggingConfig = new LoggingConfig();
            _configuration.GetSection("Logging").Bind(loggingConfig);

            // Validate configuration
            Assert.That(kalshiConfig.KeyId, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyId is missing in appsettings.local.json");
            Assert.That(kalshiConfig.KeyFile, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyFile is missing in appsettings.local.json");
            Assert.That(System.IO.File.Exists(kalshiConfig.KeyFile), Is.True, $"KeyFile {kalshiConfig.KeyFile} does not exist");
            Assert.That(kalshiConfig.Environment, Is.Not.Null.And.Not.Empty, "KalshiConfig.Environment is missing in appsettings.local.json");

            _kalshiConfigOptions = Options.Create(kalshiConfig);
            _loggingConfigOptions = Options.Create(loggingConfig);

            // Initialize real SqlDataService
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "DefaultConnection string is missing in appsettings.local.json");
            _sqlService = new SqlDataService(_configuration, _sqlLoggerMock.Object);

            _client = new KalshiWebSocketClient(_kalshiConfigOptions, _loggerMock.Object, _statusTracker.Object, _readyStatus.Object, _sqlDataService.Object, false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_client.IsConnected())
            {
                //await _client.UnsubscribeAllAsync();
            }
            _sqlService.Dispose();
        }

        [Test]
        public async Task SubscribeAllChannels_ConfirmSubscription_Unsubscribe_ConfirmUnsubscription()
        {
            // Arrange
            string[] channels = { "orderbook", "ticker", "trade", "fill", "lifecycle" };
            string marketTicker = "KXWTO-26"; // Replace with an active market ticker
            bool[] receivedConfirmations = new bool[channels.Length];
            bool messageReceived = false;
            var cts = new CancellationTokenSource();

            try
            {
                // Connect to the real WebSocket server
                await _client.ConnectAsync();

                // Track messages to confirm reception
                _client.MessageReceived += (sender, timestamp) =>
                {
                    messageReceived = true;
                    _loggerMock.Object.Log(LogLevel.Information, "Received WebSocket message at {Timestamp}", timestamp);
                };


                // Act: Subscribe to all channels
                _client.WatchedMarkets = new HashSet<string> { marketTicker }; // Ensure subscriptions are maintained
                foreach (var channel in channels)
                {
                    if (!(channel.Contains("fill") || channel.Contains("lifecycle")))
                    {
                        await _client.SubscribeToChannelAsync(channel, new[] { marketTicker });
                    }
                }

                // Assert: Verify subscription state
                foreach (var channel in channels)
                {
                    if (!(channel.Contains("fill") || channel.Contains("lifecycle")))
                    {
                        Assert.That(_client.IsSubscribed(marketTicker, channel), Is.True, $"Failed to subscribe to {channel}");
                    }
                    else
                    {
                        Assert.That(_client.IsSubscribed("", channel), Is.True, $"Failed to subscribe to {channel}");
                    }
                }

                // Wait 30 seconds for messages
                await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);


                // Assert: Check if messages were received
                int OrderBookCount = (int)_client.EventCounts.Where(x => x.Key == "OrderBook").Select(x => x.Value).FirstOrDefault();
                int TickerCount = (int)_client.EventCounts.Where(x => x.Key == "Ticker").Select(x => x.Value).FirstOrDefault();
                int TradeCount = (int)_client.EventCounts.Where(x => x.Key == "Trade").Select(x => x.Value).FirstOrDefault();
                int FillCount = (int)_client.EventCounts.Where(x => x.Key == "Fill").Select(x => x.Value).FirstOrDefault();
                int MarketLifecycleCount = (int)_client.EventCounts.Where(x => x.Key == "MarketLifecycle").Select(x => x.Value).FirstOrDefault();
                int EventLifecycleCount = (int)_client.EventCounts.Where(x => x.Key == "EventLifecycle").Select(x => x.Value).FirstOrDefault();
                int SubscribedCount = (int)_client.EventCounts.Where(x => x.Key == "Subscribed").Select(x => x.Value).FirstOrDefault();
                int UnsubscribeCount = (int)_client.EventCounts.Where(x => x.Key == "Unsubscribe").Select(x => x.Value).FirstOrDefault();
                int OkCount = (int)_client.EventCounts.Where(x => x.Key == "Ok").Select(x => x.Value).FirstOrDefault();
                int ErrorCount = (int)_client.EventCounts.Where(x => x.Key == "Error").Select(x => x.Value).FirstOrDefault();
                int UnknownCount = (int)_client.EventCounts.Where(x => x.Key == "Unknown").Select(x => x.Value).FirstOrDefault();

                // Act: Unsubscribe from all channels
                foreach (var channel in channels)
                {
                    await _client.UnsubscribeFromChannelAsync(channel);
                }

                // Wait 5 seconds
                await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);

                // Assert: Confirm unsubscribed
                foreach (var channel in channels)
                {
                    Assert.That(_client.IsSubscribed(marketTicker, channel), Is.False, $"Failed to unsubscribe from {channel}");
                }

                _client.ResetEventCounts();

                await Task.Delay(TimeSpan.FromSeconds(60), cts.Token);
                OrderBookCount = (int)_client.EventCounts.Where(x => x.Key == "OrderBook").Select(x => x.Value).FirstOrDefault();
                TickerCount = (int)_client.EventCounts.Where(x => x.Key == "Ticker").Select(x => x.Value).FirstOrDefault();
                TradeCount = (int)_client.EventCounts.Where(x => x.Key == "Trade").Select(x => x.Value).FirstOrDefault();
                FillCount = (int)_client.EventCounts.Where(x => x.Key == "Fill").Select(x => x.Value).FirstOrDefault();
                MarketLifecycleCount = (int)_client.EventCounts.Where(x => x.Key == "MarketLifecycle").Select(x => x.Value).FirstOrDefault();
                EventLifecycleCount = (int)_client.EventCounts.Where(x => x.Key == "EventLifecycle").Select(x => x.Value).FirstOrDefault();
                SubscribedCount = (int)_client.EventCounts.Where(x => x.Key == "Subscribed").Select(x => x.Value).FirstOrDefault();
                UnsubscribeCount = (int)_client.EventCounts.Where(x => x.Key == "Unsubscribe").Select(x => x.Value).FirstOrDefault();
                OkCount = (int)_client.EventCounts.Where(x => x.Key == "Ok").Select(x => x.Value).FirstOrDefault();
                ErrorCount = (int)_client.EventCounts.Where(x => x.Key == "Error").Select(x => x.Value).FirstOrDefault();
                UnknownCount = (int)_client.EventCounts.Where(x => x.Key == "Unknown").Select(x => x.Value).FirstOrDefault();

                if (OrderBookCount > 0 || TickerCount > 0 || TradeCount > 0 || FillCount > 0 ||
                    MarketLifecycleCount > 0 || EventLifecycleCount > 0 || SubscribedCount > 0 ||
                    UnsubscribeCount > 0 || OkCount > 0 || ErrorCount > 0 || UnknownCount > 0)
                {
                    _loggerMock.Object.Log(LogLevel.Information, "Event counts after unsubscription: OrderBook={OrderBook}, Ticker={Ticker}, Trade={Trade}, Fill={Fill}, MarketLifecycle={MarketLifecycle}, EventLifecycle={EventLifecycle}, Subscribed={Subscribed}, Unsubscribe={Unsubscribe}, Ok={Ok}, Error={Error}, Unknown={Unknown}",
                        OrderBookCount, TickerCount, TradeCount, FillCount, MarketLifecycleCount, EventLifecycleCount, SubscribedCount, UnsubscribeCount, OkCount, ErrorCount, UnknownCount);
                    Assert.Fail("Events were still received after unsubscription. This indicates a potential issue with the unsubscribe logic or the WebSocket connection.");
                }

            }
            catch (WebSocketException ex)
            {
                Assert.Fail($"WebSocket connection failed: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                Assert.Fail($"WebSocket operation failed: {ex.Message}");
            }
            finally
            {
                cts.Cancel();
                if (_client.IsConnected())
                {
                    //await _client.UnsubscribeAllAsync();
                }
            }
        }

        // Helper method to receive messages for subscription confirmations
        private async Task<string> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                var webSocketField = typeof(KalshiWebSocketClient)
                    .GetField("_webSocket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                ClientWebSocket? webSocket = (ClientWebSocket?)webSocketField.GetValue(_client);
                if (webSocket == null || webSocket.State != WebSocketState.Open)
                {
                    _loggerMock.Object.Log(LogLevel.Error, "WebSocket is not connected");
                    return null;
                }

                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    return Encoding.UTF8.GetString(buffer, 0, result.Count);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    _loggerMock.Object.Log(LogLevel.Warning, "WebSocket closed by server: {Code}, {Reason}",
                        result.CloseStatus, result.CloseStatusDescription);
                }
            }
            catch (Exception ex)
            {
                _loggerMock.Object.Log(LogLevel.Error, ex, "Error receiving WebSocket message");
            }
            return null;
        }
    }
}
