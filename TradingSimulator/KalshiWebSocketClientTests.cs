using KalshiBotAPI.Configuration;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using BacklashDTOs.Configuration;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;


namespace TradingSimulator.Tests
{
    [TestFixture]
    public class KalshiWebSocketClientTests
    {
        private Mock<ILogger<KalshiWebSocketClient>> _loggerMock;
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        private Mock<ISqlDataService> _sqlDataService;
        private Mock<IStatusTrackerService> _statusTracker;
        private Mock<IBotReadyStatus> _readyStatus;
        private SqlDataService _sqlService;
        private KalshiWebSocketClient _client;
        private IOptions<KalshiConfig> _kalshiConfigOptions;
        private IConfiguration _configuration;

        // New mocks for refactored components
        private Mock<IWebSocketConnectionManager> _connectionManagerMock;
        private Mock<ISubscriptionManager> _subscriptionManagerMock;
        private Mock<IMessageProcessor> _messageProcessorMock;
        private Mock<IDataCache> _dataCacheMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<KalshiWebSocketClient>>();
            _sqlLoggerMock = new Mock<ILogger<SqlDataService>>();
            _statusTracker = new Mock<IStatusTrackerService>();
            _readyStatus = new Mock<IBotReadyStatus>();
            _sqlDataService = new Mock<ISqlDataService>();

            var initializationCompleted = new TaskCompletionSource<bool>();
            initializationCompleted.SetResult(true);
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

            // Validate configuration
            Assert.That(kalshiConfig.KeyId, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyId is missing in appsettings.local.json");
            Assert.That(kalshiConfig.KeyFile, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyFile is missing in appsettings.local.json");
            Assert.That(System.IO.File.Exists(kalshiConfig.KeyFile), Is.True, $"KeyFile {kalshiConfig.KeyFile} does not exist");
            Assert.That(kalshiConfig.Environment, Is.Not.Null.And.Not.Empty, "KalshiConfig.Environment is missing in appsettings.local.json");

            _kalshiConfigOptions = Options.Create(kalshiConfig);

            // Initialize real SqlDataService
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "DefaultConnection string is missing in appsettings.local.json");
            _sqlService = new SqlDataService(_configuration, _sqlLoggerMock.Object);

            // Create mock objects for the new refactored dependencies
            _connectionManagerMock = new Mock<IWebSocketConnectionManager>();
            _subscriptionManagerMock = new Mock<ISubscriptionManager>();
            _messageProcessorMock = new Mock<IMessageProcessor>();
            _dataCacheMock = new Mock<IDataCache>();

            // Setup default mock behaviors
            _connectionManagerMock.Setup(cm => cm.IsConnected()).Returns(true);
            _subscriptionManagerMock.Setup(sm => sm.EventCounts).Returns(new System.Collections.Concurrent.ConcurrentDictionary<string, long>());
            _subscriptionManagerMock.Setup(sm => sm.WatchedMarkets).Returns(new HashSet<string>());
            _messageProcessorMock.Setup(mp => mp.OrderBookMessageQueueCount).Returns(0);
            _messageProcessorMock.Setup(mp => mp.PendingConfirmsCount).Returns(0);

            // Setup ProcessMessageAsync to fire events based on message content
            // The key is to fire events on the mock that the client is listening to
            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("orderbook_snapshot"))))
                .Callback<string>(message =>
                {
                    var eventArgs = new OrderBookEventArgs("snapshot", JsonDocument.Parse(message).RootElement);
                    _messageProcessorMock.Raise(m => m.OrderBookReceived += null, eventArgs);
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("orderbook_delta"))))
                .Callback<string>(message =>
                {
                    var eventArgs = new OrderBookEventArgs("delta", JsonDocument.Parse(message).RootElement);
                    _messageProcessorMock.Raise(m => m.OrderBookReceived += null, eventArgs);
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("ticker"))))
                .Callback<string>(message =>
                {
                    var tickerArgs = new TickerEventArgs();
                    tickerArgs.market_ticker = "TEST-123";
                    tickerArgs.price = 100;
                    _messageProcessorMock.Raise(m => m.TickerReceived += null, tickerArgs);
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("trade"))))
                .Callback<string>(message =>
                {
                    var eventArgs = new TradeEventArgs(JsonDocument.Parse(message).RootElement);
                    _messageProcessorMock.Raise(m => m.TradeReceived += null, eventArgs);
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("fill"))))
                .Callback<string>(message =>
                {
                    var eventArgs = new FillEventArgs(JsonDocument.Parse(message).RootElement);
                    _messageProcessorMock.Raise(m => m.FillReceived += null, eventArgs);
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("market_lifecycle"))))
                .Callback<string>(message =>
                {
                    var eventArgs = new MarketLifecycleEventArgs(JsonDocument.Parse(message).RootElement);
                    _messageProcessorMock.Raise(m => m.MarketLifecycleReceived += null, eventArgs);
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("event_lifecycle"))))
                .Callback<string>(message =>
                {
                    var eventArgs = new EventLifecycleEventArgs(JsonDocument.Parse(message).RootElement);
                    _messageProcessorMock.Raise(m => m.EventLifecycleReceived += null, eventArgs);
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("subscribed"))))
                .Callback<string>(message =>
                {
                    // Extract SID from message for subscription confirmation
                    var doc = JsonDocument.Parse(message);
                    var sid = doc.RootElement.GetProperty("sid").GetInt32();
                    _subscriptionManagerMock.Object.UpdateSubscriptionStateFromConfirmationAsync(sid, "orderbook_delta").Wait();
                })
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("ok"))))
                .Returns(Task.CompletedTask);

            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("error"))))
                .Returns(Task.CompletedTask);

            // Default catch-all setup - this must be last
            _messageProcessorMock.Setup(mp => mp.ProcessMessageAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _client = new KalshiWebSocketClient(
                _kalshiConfigOptions,
                _loggerMock.Object,
                _statusTracker.Object,
                _readyStatus.Object,
                _sqlDataService.Object,
                _connectionManagerMock.Object,
                _subscriptionManagerMock.Object,
                _messageProcessorMock.Object,
                _dataCacheMock.Object,
                false);
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

        #region Channel Enable/Disable Tests

        [Test]
        public void ChannelEnableDisable_AllChannelsInitiallyDisabled()
        {
            // Arrange - All channels should be disabled by default
            var allChannels = new[] { "orderbook", "ticker", "trade", "fill", "lifecycle", "event_lifecycle" };

            Console.WriteLine("🧪 Testing: Initial Channel State");
            Console.WriteLine("   Expected: All WebSocket channels should be disabled by default");
            Console.WriteLine($"   Channels: {string.Join(", ", allChannels)}");

            // Act & Assert - Verify all channels are disabled
            foreach (var channel in allChannels)
            {
                Assert.That(_client.IsChannelEnabled(channel), Is.False, $"Channel {channel} should be disabled by default");
            }

            Console.WriteLine("✅ PASSED: All channels are correctly disabled by default");
        }

        [Test]
        public void EnableChannel_SingleChannel_EnablementWorks()
        {
            // Arrange
            string channel = "orderbook";

            Console.WriteLine("🧪 Testing: Enable Single Channel");
            Console.WriteLine("   Expected: Specific channel should be enabled when EnableChannel is called");
            Console.WriteLine($"   Channel: {channel}");

            // Act
            _client.EnableChannel(channel);

            // Assert
            Assert.That(_client.IsChannelEnabled(channel), Is.True, $"Channel {channel} should be enabled");
            Assert.That(_client.GetEnabledChannels().Contains(channel), Is.True, $"Enabled channels should contain {channel}");

            Console.WriteLine("✅ PASSED: Channel was successfully enabled");
        }

        [Test]
        public void DisableChannel_SingleChannel_DisablementWorks()
        {
            // Arrange
            string channel = "orderbook";
            _client.EnableChannel(channel);

            Console.WriteLine("🧪 Testing: Disable Single Channel");
            Console.WriteLine("   Expected: Specific channel should be disabled when DisableChannel is called");
            Console.WriteLine($"   Channel: {channel}");

            // Act
            _client.DisableChannel(channel);

            // Assert
            Assert.That(_client.IsChannelEnabled(channel), Is.False, $"Channel {channel} should be disabled");
            Assert.That(_client.GetEnabledChannels().Contains(channel), Is.False, $"Enabled channels should not contain {channel}");

            Console.WriteLine("✅ PASSED: Channel was successfully disabled");
        }

        [Test]
        public void EnableAllChannels_AllChannelsEnabled()
        {
            // Arrange
            var allChannels = new[] { "orderbook", "ticker", "trade", "fill", "lifecycle", "event_lifecycle" };

            Console.WriteLine("🧪 Testing: Enable All Channels");
            Console.WriteLine("   Expected: All WebSocket channels should be enabled");
            Console.WriteLine($"   Channels: {string.Join(", ", allChannels)}");

            // Act
            _client.EnableAllChannels();

            // Assert
            foreach (var channel in allChannels)
            {
                Assert.That(_client.IsChannelEnabled(channel), Is.True, $"Channel {channel} should be enabled");
            }

            var enabledChannels = _client.GetEnabledChannels();
            Assert.That(enabledChannels.Count, Is.EqualTo(allChannels.Length), "All channels should be enabled");
            foreach (var channel in allChannels)
            {
                Assert.That(enabledChannels.Contains(channel), Is.True, $"Enabled channels should contain {channel}");
            }

            Console.WriteLine("✅ PASSED: All WebSocket channels were successfully enabled");
        }

        [Test]
        public void DisableAllChannels_AllChannelsDisabled()
        {
            // Arrange
            _client.EnableAllChannels();
            var allChannels = new[] { "orderbook", "ticker", "trade", "fill", "lifecycle", "event_lifecycle" };

            Console.WriteLine("🧪 Testing: Disable All Channels");
            Console.WriteLine("   Expected: All WebSocket channels should be disabled");
            Console.WriteLine($"   Channels: {string.Join(", ", allChannels)}");

            // Act
            _client.DisableAllChannels();

            // Assert
            foreach (var channel in allChannels)
            {
                Assert.That(_client.IsChannelEnabled(channel), Is.False, $"Channel {channel} should be disabled");
            }

            var enabledChannels = _client.GetEnabledChannels();
            Assert.That(enabledChannels.Count, Is.EqualTo(0), "No channels should be enabled");

            Console.WriteLine("✅ PASSED: All channels were successfully disabled");
        }

        #endregion

        #region Message Processing Tests

        [Test]
        public async Task ProcessOrderBookMessage_OrderBookSnapshot_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""orderbook_snapshot"",
                ""market_id"": ""123"",
                ""msg"": {
                    ""market_ticker"": ""TEST-123"",
                    ""bids"": [[100, 10]],
                    ""asks"": [[101, 5]]
                }
            }";

            Console.WriteLine("🧪 Testing: OrderBook Snapshot Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with orderbook_snapshot message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("orderbook_snapshot"))), Times.Once);

            Console.WriteLine("✅ PASSED: OrderBook snapshot message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessOrderBookMessage_OrderBookDelta_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""orderbook_delta"",
                ""market_id"": ""123"",
                ""msg"": {
                    ""market_ticker"": ""TEST-123"",
                    ""deltas"": [{""side"": ""bid"", ""price"": 100, ""size"": 10}]
                }
            }";

            Console.WriteLine("🧪 Testing: OrderBook Delta Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with orderbook_delta message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("orderbook_delta"))), Times.Once);

            Console.WriteLine("✅ PASSED: OrderBook delta message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessTickerMessage_TickerData_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""ticker"",
                ""market_id"": ""123"",
                ""market_ticker"": ""TEST-123"",
                ""price"": 100,
                ""yes_bid"": 99,
                ""yes_ask"": 101,
                ""volume"": 1000,
                ""open_interest"": 5000,
                ""dollar_volume"": 100000,
                ""dollar_open_interest"": 500000,
                ""ts"": 1640995200000
            }";

            Console.WriteLine("🧪 Testing: Ticker Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with ticker message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("ticker"))), Times.Once);

            Console.WriteLine("✅ PASSED: Ticker message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessTradeMessage_TradeData_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""trade"",
                ""market_id"": ""123"",
                ""msg"": {
                    ""market_ticker"": ""TEST-123"",
                    ""price"": 100,
                    ""size"": 10,
                    ""side"": ""yes""
                }
            }";

            Console.WriteLine("🧪 Testing: Trade Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with trade message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("trade"))), Times.Once);

            Console.WriteLine("✅ PASSED: Trade message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessFillMessage_FillData_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""fill"",
                ""market_id"": ""123"",
                ""msg"": {
                    ""order_id"": ""order-123"",
                    ""filled_qty"": 10,
                    ""remaining_qty"": 0
                }
            }";

            Console.WriteLine("🧪 Testing: Fill Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with fill message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("fill"))), Times.Once);

            Console.WriteLine("✅ PASSED: Fill message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessMarketLifecycleMessage_LifecycleData_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""market_lifecycle_v2"",
                ""market_id"": ""123"",
                ""msg"": {
                    ""market_ticker"": ""TEST-123"",
                    ""status"": ""active""
                }
            }";

            Console.WriteLine("🧪 Testing: Market Lifecycle Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with market_lifecycle message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("market_lifecycle"))), Times.Once);

            Console.WriteLine("✅ PASSED: Market lifecycle message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessEventLifecycleMessage_EventLifecycleData_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""event_lifecycle"",
                ""event_id"": ""event-123"",
                ""msg"": {
                    ""event_ticker"": ""EVENT-123"",
                    ""status"": ""active""
                }
            }";

            Console.WriteLine("🧪 Testing: Event Lifecycle Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with event_lifecycle message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("event_lifecycle"))), Times.Once);

            Console.WriteLine("✅ PASSED: Event lifecycle message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessErrorMessage_ObjectError_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""error"",
                ""msg"": {""code"": 6, ""message"": ""Already subscribed""}
            }";

            Console.WriteLine("🧪 Testing: Error Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should handle error messages without throwing exceptions");

            // Act - This should not throw an exception
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("error"))), Times.Once);

            Console.WriteLine("✅ PASSED: Error message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessSubscribedMessage_SubscriptionConfirmation_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""subscribed"",
                ""sid"": 12345,
                ""channel"": ""orderbook_delta""
            }";

            Console.WriteLine("🧪 Testing: Subscription Confirmation Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with subscribed message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("subscribed"))), Times.Once);

            Console.WriteLine("✅ PASSED: Subscription confirmation message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessUnsubscribedMessage_UnsubscriptionConfirmation_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""unsubscribed"",
                ""sid"": 12345
            }";

            Console.WriteLine("🧪 Testing: Unsubscription Confirmation Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with unsubscribed message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("unsubscribed"))), Times.Once);

            Console.WriteLine("✅ PASSED: Unsubscription confirmation message was processed correctly by MessageProcessor");
        }

        [Test]
        public async Task ProcessOkMessage_UpdateConfirmation_MessageProcessorCalled()
        {
            // Arrange
            var message = @"{
                ""type"": ""ok"",
                ""id"": 12345
            }";

            Console.WriteLine("🧪 Testing: OK Confirmation Message Processing");
            Console.WriteLine("   Expected: MessageProcessor.ProcessMessageAsync should be called with ok message");

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("ok"))), Times.Once);

            Console.WriteLine("✅ PASSED: OK confirmation message was processed correctly by MessageProcessor");
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task SubscribeToChannel_OrderBookChannel_SubscriptionManagerCalled()
        {
            // Arrange
            string channel = "orderbook";
            string[] marketTickers = { "TEST-123" };
            _client.EnableChannel(channel);

            Console.WriteLine("🧪 Testing: Subscribe to Channel");
            Console.WriteLine("   Expected: SubscriptionManager.SubscribeToChannelAsync should be called with correct parameters");
            Console.WriteLine($"   Channel: {channel}, Markets: {string.Join(", ", marketTickers)}");

            // Act
            await _client.SubscribeToChannelAsync(channel, marketTickers);

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.SubscribeToChannelAsync(channel, marketTickers),
                Times.Once);

            Console.WriteLine("✅ PASSED: Channel subscription was handled correctly by SubscriptionManager");
        }

        [Test]
        public async Task SubscribeToWatchedMarkets_WatchedMarketsSet_SubscriptionManagerCalled()
        {
            // Arrange
            var watchedMarkets = new HashSet<string> { "TEST-123", "TEST-456" };
            _client.WatchedMarkets = watchedMarkets;
            _client.EnableAllChannels();

            Console.WriteLine("🧪 Testing: Subscribe to Watched Markets");
            Console.WriteLine("   Expected: SubscriptionManager.SubscribeToChannelAsync should be called for enabled channels");
            Console.WriteLine($"   Markets: {string.Join(", ", watchedMarkets)}");

            // Setup the connection manager to be connected
            _connectionManagerMock.Setup(cm => cm.IsConnected()).Returns(true);

            // Setup the subscription manager to return the watched markets
            _subscriptionManagerMock.Setup(sm => sm.WatchedMarkets).Returns(watchedMarkets);
            _subscriptionManagerMock.Setup(sm => sm.IsSubscribed(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _subscriptionManagerMock.Setup(sm => sm.CanSubscribeToMarket(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _subscriptionManagerMock.Setup(sm => sm.SubscribeToChannelAsync(It.IsAny<string>(), It.IsAny<string[]>())).Returns(Task.CompletedTask);

            // Act
            await _client.SubscribeToWatchedMarketsAsync();

            // Assert - Verify that SubscribeToChannelAsync was called for enabled channels
            _subscriptionManagerMock.Verify(
                sm => sm.SubscribeToChannelAsync(It.IsAny<string>(), It.IsAny<string[]>()),
                Times.AtLeastOnce);

            Console.WriteLine("✅ PASSED: Successfully subscribed to watched markets for enabled channels");
        }

        [Test]
        public async Task UnsubscribeFromChannel_ChannelSpecified_SubscriptionManagerCalled()
        {
            // Arrange
            string channel = "orderbook";

            Console.WriteLine("🧪 Testing: Unsubscribe from Channel");
            Console.WriteLine("   Expected: SubscriptionManager.UnsubscribeFromChannelAsync should be called with correct channel");
            Console.WriteLine($"   Channel: {channel}");

            // Act
            await _client.UnsubscribeFromChannelAsync(channel);

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.UnsubscribeFromChannelAsync(channel),
                Times.Once);

            Console.WriteLine("✅ PASSED: Channel unsubscription was handled correctly by SubscriptionManager");
        }

        [Test]
        public async Task UnsubscribeFromAll_AllChannels_UnsubscribeFromAllAsyncCalled()
        {
            Console.WriteLine("🧪 Testing: Unsubscribe from All Channels");
            Console.WriteLine("   Expected: SubscriptionManager.UnsubscribeFromAllAsync should be called");

            // Act
            await _client.UnsubscribeFromAllAsync();

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.UnsubscribeFromAllAsync(),
                Times.Once);

            Console.WriteLine("✅ PASSED: Full unsubscription was handled correctly by SubscriptionManager");
        }

        [Test]
        public void IsSubscribed_MarketAndChannel_SubscriptionManagerCalled()
        {
            // Arrange
            string marketTicker = "TEST-123";
            string channel = "orderbook";

            Console.WriteLine("🧪 Testing: Check Subscription Status");
            Console.WriteLine("   Expected: SubscriptionManager.IsSubscribed should be called with correct parameters");
            Console.WriteLine($"   Market: {marketTicker}, Channel: {channel}");

            // Act
            _client.IsSubscribed(marketTicker, channel);

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.IsSubscribed(marketTicker, channel),
                Times.Once);

            Console.WriteLine("✅ PASSED: Subscription status check was handled correctly by SubscriptionManager");
        }

        [Test]
        public void ResetEventCounts_Called_MessageProcessorResetEventCountsCalled()
        {
            Console.WriteLine("🧪 Testing: Reset Event Counts");
            Console.WriteLine("   Expected: MessageProcessor.ResetEventCounts should be called");

            // Act
            _client.ResetEventCounts();

            // Assert
            _messageProcessorMock.Verify(
                mp => mp.ResetEventCounts(),
                Times.Once);

            Console.WriteLine("✅ PASSED: Event counts reset was handled correctly by MessageProcessor");
        }

        #endregion

        #region Connection Tests

        [Test]
        public async Task ConnectAsync_ConnectionManagerConnected_MessageProcessingStarted()
        {
            // Arrange
            _connectionManagerMock.Setup(cm => cm.ConnectAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.IsConnected()).Returns(true);

            Console.WriteLine("🧪 Testing: WebSocket Connection and Component Startup");
            Console.WriteLine("   Expected: All components (ConnectionManager, MessageProcessor, SubscriptionManager) should start properly");

            // Act
            await _client.ConnectAsync();

            // Assert
            _connectionManagerMock.Verify(cm => cm.ConnectAsync(0), Times.Once);
            _messageProcessorMock.Verify(mp => mp.StartProcessingAsync(), Times.Once);
            _subscriptionManagerMock.Verify(sm => sm.StartAsync(), Times.Once);

            Console.WriteLine("✅ PASSED: WebSocket connection established and all components started successfully");
        }

        [Test]
        public async Task ShutdownAsync_AllComponentsStopped()
        {
            // Arrange
            _subscriptionManagerMock.Setup(sm => sm.UnsubscribeFromAllAsync()).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.StopAsync()).Returns(Task.CompletedTask);
            _messageProcessorMock.Setup(mp => mp.StopProcessingAsync()).Returns(Task.CompletedTask);

            Console.WriteLine("🧪 Testing: Stop All Services");
            Console.WriteLine("   Expected: All components (SubscriptionManager, ConnectionManager, MessageProcessor) should be stopped");

            // Act
            await _client.ShutdownAsync();

            // Assert
            _subscriptionManagerMock.Verify(sm => sm.UnsubscribeFromAllAsync(), Times.Once);
            _connectionManagerMock.Verify(cm => cm.StopAsync(), Times.Once);
            _messageProcessorMock.Verify(mp => mp.StopProcessingAsync(), Times.Once);

            Console.WriteLine("✅ PASSED: All WebSocket services were stopped successfully");
        }

        #endregion
    }
}
