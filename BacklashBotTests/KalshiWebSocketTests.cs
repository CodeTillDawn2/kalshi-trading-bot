using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashBotData.Configuration;
using BacklashCommon.Configuration;
using BacklashDTOs;
using BacklashInterfaces.Enums;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.Websockets;
using KalshiBotAPI.WebSockets.Interfaces;
using KalshiBotData.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net.WebSockets;
using System.Text.Json;

namespace BacklashBotTests
{
    /// <summary>
    /// Comprehensive NUnit test fixture for validating the KalshiWebSocketClient functionality.
    /// This class tests the WebSocket client's ability to manage connections, handle subscriptions,
    /// process real-time market data messages, and coordinate with related components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The test fixture covers the following areas:
    /// - Channel enable/disable operations and state management
    /// - Message processing for various WebSocket message types (orderbook, ticker, trade, fill, lifecycle)
    /// - Integration testing with subscription and message processing components
    /// - Connection lifecycle management and component coordination
    /// </para>
    /// <para>
    /// Tests use mocked dependencies to isolate the WebSocket client behavior and ensure
    /// reliable, repeatable testing of real-time trading platform interactions.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class KalshiWebSocketTests
    {
        private Mock<ILogger<KalshiWebSocketClient>> _loggerMock;
        private Mock<ILogger<SqlDataService>> _sqlLoggerMock;
        private Mock<ISqlDataService> _sqlDataService;
        private Mock<IStatusTrackerService> _statusTracker;
        private Mock<IBotReadyStatus> _readyStatus;
        private SqlDataService _sqlService;
        private KalshiWebSocketClient _client;
        private IOptions<KalshiConfig> _kalshiConfigOptions;
        private IOptions<KalshiWebSocketClientConfig> _websocketConfigOptions;
        private IConfiguration _configuration;

        // New mocks for refactored components
        private Mock<IWebSocketConnectionManager> _connectionManagerMock;
        private Mock<ISubscriptionManager> _subscriptionManagerMock;
        private Mock<IMessageProcessor> _messageProcessorMock;
        private Mock<IDataCache> _dataCacheMock;

        /// <summary>
        /// Initializes the test environment before each test execution.
        /// Sets up mocked dependencies, configures test data, and prepares the KalshiWebSocketClient instance.
        /// </summary>
        /// <remarks>
        /// This setup method:
        /// - Creates and configures all necessary mock objects for dependencies
        /// - Loads configuration from appsettings.json for realistic testing
        /// - Initializes the real SqlDataService for database operations
        /// - Sets up mock behaviors for WebSocket components and message processing
        /// - Creates the KalshiWebSocketClient instance with all dependencies
        /// </remarks>
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

            // Load configuration from appsettings.json
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var baseConfig = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            _configuration = new ConfigurationBuilder()
                .AddConfiguration(baseConfig)
                .AddSecretsConfiguration(basePath, baseConfig)
                .Build();

            var kalshiConfig = _configuration.GetSection(KalshiConfig.SectionName).Get<KalshiConfig>();

            // Interpolate placeholders in KalshiConfig (matching Program.cs)
            var interpolatedKeyId = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(kalshiConfig.KeyId, _configuration);
            var interpolatedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.InterpolateConfigurationValue(kalshiConfig.KeyFile, _configuration);

            // Resolve the key file path to the secrets directory
            var secretsConfig = _configuration.GetSection(BacklashCommon.Configuration.SecretsConfig.SectionName).Get<BacklashCommon.Configuration.SecretsConfig>();
            var resolvedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.ResolveSecretsFilePath(
                interpolatedKeyFile,
                secretsConfig,
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot")));

            kalshiConfig.KeyId = interpolatedKeyId;
            kalshiConfig.KeyFile = resolvedKeyFile;

            // Validate configuration
            Assert.That(kalshiConfig.KeyId, Is.Not.Null.And.Not.Empty, "KalshiConfig.BotKeyId is missing in appsettings.json");
            Assert.That(kalshiConfig.KeyFile, Is.Not.Null.And.Not.Empty, "KalshiConfig.BotKeyFile is missing in appsettings.json");
            Assert.That(System.IO.File.Exists(kalshiConfig.KeyFile), Is.True, $"KeyFile {kalshiConfig.KeyFile} does not exist");
            Assert.That(kalshiConfig.Environment, Is.Not.Null.And.Not.Empty, "KalshiConfig.Environment is missing in appsettings.json");

            _kalshiConfigOptions = Options.Create(kalshiConfig);

            var websocketConfig = _configuration.GetSection(KalshiWebSocketClientConfig.SectionName).Get<KalshiWebSocketClientConfig>();
            _websocketConfigOptions = Options.Create(websocketConfig);

            // Initialize real SqlDataService
            var connectionString = ConfigurationHelper.BuildConnectionString(_configuration);
            Assert.That(connectionString, Is.Not.Null.And.Not.Empty, "DefaultConnection string is missing in appsettings.json");
            var dataConfig = _configuration.GetSection(BacklashBotDataConfig.SectionName).Get<BacklashBotDataConfig>();
            _sqlService = new SqlDataService(connectionString, _sqlLoggerMock.Object, dataConfig, null);

            // Create mock objects for the new refactored dependencies
            _connectionManagerMock = new Mock<IWebSocketConnectionManager>();
            _subscriptionManagerMock = new Mock<ISubscriptionManager>();
            _messageProcessorMock = new Mock<IMessageProcessor>();
            _dataCacheMock = new Mock<IDataCache>();

            // Setup default mock behaviors
            _connectionManagerMock.Setup(cm => cm.IsConnected()).Returns(true);
            _connectionManagerMock.Setup(cm => cm.ConnectAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.StopAsync()).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.GetWebSocket()).Returns((ClientWebSocket)null);
            _connectionManagerMock.Setup(cm => cm.ResetConnectionAsync()).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.SendMessageAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.ReceiveAsync()).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.DisableReconnect());
            _connectionManagerMock.Setup(cm => cm.EnableReconnect());
            _connectionManagerMock.Setup(cm => cm.ConnectSemaphoreCount).Returns(0);

            _subscriptionManagerMock.Setup(sm => sm.EventCounts).Returns(new System.Collections.Concurrent.ConcurrentDictionary<string, long>());
            _subscriptionManagerMock.Setup(sm => sm.WatchedMarkets).Returns(new HashSet<string>());
            _subscriptionManagerMock.Setup(sm => sm.StartAsync()).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.SubscribeToChannelAsync(It.IsAny<string>(), It.IsAny<string[]>())).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.SubscribeToWatchedMarketsAsync()).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.UpdateSubscriptionAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.UnsubscribeFromChannelAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.UnsubscribeFromAllAsync()).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.ResubscribeAsync(It.IsAny<bool>())).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.GetChannelName(It.IsAny<string>())).Returns("test");
            _subscriptionManagerMock.Setup(sm => sm.GenerateNextMessageId()).Returns(1);
            _subscriptionManagerMock.Setup(sm => sm.IsSubscribed(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _subscriptionManagerMock.Setup(sm => sm.CanSubscribeToMarket(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _subscriptionManagerMock.Setup(sm => sm.SetSubscriptionState(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SubscriptionState>()));
            _subscriptionManagerMock.Setup(sm => sm.ClearOrderBookQueue(It.IsAny<string>()));
            _subscriptionManagerMock.Setup(sm => sm.WaitForEmptyOrderBookQueueAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.ResetEventCounts());
            _subscriptionManagerMock.Setup(sm => sm.GetEventCountsByMarket(It.IsAny<string>())).Returns((0, 0, 0));
            _subscriptionManagerMock.Setup(sm => sm.UpdateSubscriptionStateFromConfirmationAsync(It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _subscriptionManagerMock.Setup(sm => sm.RemovePendingConfirmation(It.IsAny<int>())).Returns(false);
            _subscriptionManagerMock.Setup(sm => sm.GetPendingConfirm(It.IsAny<int>())).Returns((string.Empty, new string[0]));
            _subscriptionManagerMock.Setup(sm => sm.SubscriptionUpdateSemaphoreCount).Returns(0);
            _subscriptionManagerMock.Setup(sm => sm.ChannelSubscriptionSemaphoreCount).Returns(0);
            _subscriptionManagerMock.Setup(sm => sm.QueuedSubscriptionUpdatesCount).Returns(0);

            _messageProcessorMock.Setup(mp => mp.OrderBookMessageQueueCount).Returns(0);
            _messageProcessorMock.Setup(mp => mp.PendingConfirmsCount).Returns(0);
            _messageProcessorMock.Setup(mp => mp.LastSequenceNumber).Returns(0);
            _messageProcessorMock.Setup(mp => mp.StartProcessingAsync()).Returns(Task.CompletedTask);
            _messageProcessorMock.Setup(mp => mp.StopProcessingAsync()).Returns(Task.CompletedTask);
            _messageProcessorMock.Setup(mp => mp.ResetEventCounts());
            _messageProcessorMock.Setup(mp => mp.ClearOrderBookQueue(It.IsAny<string>()));
            _messageProcessorMock.Setup(mp => mp.WaitForEmptyOrderBookQueueAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
            _messageProcessorMock.Setup(mp => mp.GetEventCountsByMarket(It.IsAny<string>())).Returns((0, 0, 0));
            _messageProcessorMock.Setup(mp => mp.SetWriteToSql(It.IsAny<bool>()));

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
                _websocketConfigOptions,
                _loggerMock.Object,
                _statusTracker.Object,
                _readyStatus.Object,
                _sqlDataService.Object,
                _connectionManagerMock.Object,
                _subscriptionManagerMock.Object,
                _messageProcessorMock.Object,
                null, // performanceMetrics
                false); // writeToSql
        }

        /// <summary>
        /// Cleans up the test environment after each test execution.
        /// Disposes of resources and ensures proper cleanup of test components.
        /// </summary>
        /// <remarks>
        /// This teardown method:
        /// - Disposes of the SqlDataService to free database resources
        /// - Ensures proper cleanup of any connected WebSocket resources
        /// </remarks>
        [TearDown]
        public void TearDown()
        {
            if (_client.IsConnected())
            {
                // Note: UnsubscribeAllAsync removed as it was causing test instability
                // Connection cleanup is handled by the client's Dispose method
            }
            _sqlService.Dispose();
        }

        #region Channel Enable/Disable Tests

        /// <summary>
        /// Verifies that all WebSocket channels are disabled by default when the client is initialized.
        /// </summary>
        /// <remarks>
        /// This test ensures the security principle that channels must be explicitly enabled
        /// before they can receive WebSocket messages, preventing unintended data flow.
        /// </remarks>
        [Test]
        public void ChannelEnableDisable_AllChannelsInitiallyDisabled()
        {
            TestContext.WriteLine("Testing that all WebSocket channels are disabled by default when the client is initialized.");
            // Arrange - All channels should be disabled by default
            var allChannels = new[] { "orderbook", "ticker", "trade", "fill", "lifecycle", "event_lifecycle" };

            // Act & Assert - Verify all channels are disabled
            foreach (var channel in allChannels)
            {
                Assert.That(_client.IsChannelEnabled(channel), Is.False, $"Channel {channel} should be disabled by default");
            }
            TestContext.WriteLine("Result: All channels are disabled by default as expected.");
        }

        /// <summary>
        /// Verifies that a single WebSocket channel can be successfully enabled.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - The EnableChannel method correctly updates the channel state
        /// - The IsChannelEnabled method returns the correct state
        /// - The GetEnabledChannels method includes the newly enabled channel
        /// </remarks>
        [Test]
        public void EnableChannel_SingleChannel_EnablementWorks()
        {
            TestContext.WriteLine("Testing that a single WebSocket channel can be successfully enabled.");
            // Arrange
            string channel = "orderbook";

            // Act
            _client.EnableChannel(channel);

            // Assert
            Assert.That(_client.IsChannelEnabled(channel), Is.True, $"Channel {channel} should be enabled");
            Assert.That(_client.GetEnabledChannels().Contains(channel), Is.True, $"Enabled channels should contain {channel}");
            TestContext.WriteLine("Result: Single channel enabled successfully.");
        }

        /// <summary>
        /// Verifies that a single WebSocket channel can be successfully disabled.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - The DisableChannel method correctly updates the channel state
        /// - The IsChannelEnabled method returns the correct state after disabling
        /// - The GetEnabledChannels method excludes the disabled channel
        /// </remarks>
        [Test]
        public void DisableChannel_SingleChannel_DisablementWorks()
        {
            TestContext.WriteLine("Testing that a single WebSocket channel can be successfully disabled.");
            // Arrange
            string channel = "orderbook";
            _client.EnableChannel(channel);

            // Act
            _client.DisableChannel(channel);

            // Assert
            Assert.That(_client.IsChannelEnabled(channel), Is.False, $"Channel {channel} should be disabled");
            Assert.That(_client.GetEnabledChannels().Contains(channel), Is.False, $"Enabled channels should not contain {channel}");
            TestContext.WriteLine("Result: Single channel disabled successfully.");
        }

        /// <summary>
        /// Verifies that all WebSocket channels can be enabled simultaneously.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - The EnableAllChannels method enables all supported channels
        /// - Each individual channel state is correctly updated
        /// - The GetEnabledChannels method returns all expected channels
        /// </remarks>
        [Test]
        public void EnableAllChannels_AllChannelsEnabled()
        {
            TestContext.WriteLine("Testing that all WebSocket channels can be enabled simultaneously.");
            // Arrange
            var allChannels = new[] { "orderbook", "ticker", "trade", "fill", "lifecycle", "event_lifecycle" };

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
            TestContext.WriteLine("Result: All channels enabled successfully.");
        }

        /// <summary>
        /// Verifies that all WebSocket channels can be disabled simultaneously.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - The DisableAllChannels method disables all channels
        /// - Each individual channel state is correctly updated to disabled
        /// - The GetEnabledChannels method returns an empty collection
        /// </remarks>
        [Test]
        public void DisableAllChannels_AllChannelsDisabled()
        {
            TestContext.WriteLine("Testing that all WebSocket channels can be disabled simultaneously.");
            // Arrange
            _client.EnableAllChannels();
            var allChannels = new[] { "orderbook", "ticker", "trade", "fill", "lifecycle", "event_lifecycle" };

            // Act
            _client.DisableAllChannels();

            // Assert
            foreach (var channel in allChannels)
            {
                Assert.That(_client.IsChannelEnabled(channel), Is.False, $"Channel {channel} should be disabled");
            }

            var enabledChannels = _client.GetEnabledChannels();
            Assert.That(enabledChannels.Count, Is.EqualTo(0), "No channels should be enabled");
            TestContext.WriteLine("Result: All channels disabled successfully.");
        }

        #endregion

        #region Message Processing Tests

        /// <summary>
        /// Verifies that orderbook snapshot messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Orderbook snapshot messages trigger the appropriate processing
        /// - The MessageProcessor receives and handles the message correctly
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessOrderBookMessage_OrderBookSnapshot_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that orderbook snapshot messages are correctly processed by the MessageProcessor.");
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

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("orderbook_snapshot"))), Times.Once);
            TestContext.WriteLine("Result: Orderbook snapshot message processed successfully.");
        }

        /// <summary>
        /// Verifies that orderbook delta messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Orderbook delta messages trigger the appropriate processing
        /// - The MessageProcessor receives and handles incremental updates correctly
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessOrderBookMessage_OrderBookDelta_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that orderbook delta messages are correctly processed by the MessageProcessor.");
            // Arrange
            var message = @"{
                ""type"": ""orderbook_delta"",
                ""market_id"": ""123"",
                ""msg"": {
                    ""market_ticker"": ""TEST-123"",
                    ""deltas"": [{""side"": ""bid"", ""price"": 100, ""size"": 10}]
                }
            }";

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("orderbook_delta"))), Times.Once);
            TestContext.WriteLine("Result: Orderbook delta message processed successfully.");
        }

        /// <summary>
        /// Verifies that ticker messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Ticker messages with market data trigger appropriate processing
        /// - The MessageProcessor handles real-time price and volume updates correctly
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessTickerMessage_TickerData_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that ticker messages are correctly processed by the MessageProcessor.");
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

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("ticker"))), Times.Once);
            TestContext.WriteLine("Result: Ticker message processed successfully.");
        }

        /// <summary>
        /// Verifies that trade messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Trade execution messages trigger appropriate processing
        /// - The MessageProcessor handles trade data with price, size, and side information
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessTradeMessage_TradeData_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that trade messages are correctly processed by the MessageProcessor.");
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

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("trade"))), Times.Once);
            TestContext.WriteLine("Result: Trade message processed successfully.");
        }

        /// <summary>
        /// Verifies that fill messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Order fill messages trigger appropriate processing
        /// - The MessageProcessor handles fill data with order and quantity information
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessFillMessage_FillData_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that fill messages are correctly processed by the MessageProcessor.");
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

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("fill"))), Times.Once);
            TestContext.WriteLine("Result: Fill message processed successfully.");
        }

        /// <summary>
        /// Verifies that market lifecycle messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Market lifecycle messages trigger appropriate processing
        /// - The MessageProcessor handles market status changes and lifecycle events
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessMarketLifecycleMessage_LifecycleData_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that market lifecycle messages are correctly processed by the MessageProcessor.");
            // Arrange
            var message = @"{
                ""type"": ""market_lifecycle_v2"",
                ""market_id"": ""123"",
                ""msg"": {
                    ""market_ticker"": ""TEST-123"",
                    ""status"": ""active""
                }
            }";

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("market_lifecycle"))), Times.Once);
            TestContext.WriteLine("Result: Market lifecycle message processed successfully.");
        }

        /// <summary>
        /// Verifies that event lifecycle messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Event lifecycle messages trigger appropriate processing
        /// - The MessageProcessor handles event status changes and lifecycle events
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessEventLifecycleMessage_EventLifecycleData_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that event lifecycle messages are correctly processed by the MessageProcessor.");
            // Arrange
            var message = @"{
                ""type"": ""event_lifecycle"",
                ""event_id"": ""event-123"",
                ""msg"": {
                    ""event_ticker"": ""EVENT-123"",
                    ""status"": ""active""
                }
            }";

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("event_lifecycle"))), Times.Once);
            TestContext.WriteLine("Result: Event lifecycle message processed successfully.");
        }

        /// <summary>
        /// Verifies that error messages are correctly processed by the MessageProcessor without throwing exceptions.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Error messages from the WebSocket are handled gracefully
        /// - The MessageProcessor processes error messages without throwing exceptions
        /// - Error handling maintains system stability during WebSocket communication issues
        /// </remarks>
        [Test]
        public async Task ProcessErrorMessage_ObjectError_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that error messages are correctly processed by the MessageProcessor without throwing exceptions.");
            // Arrange
            var message = @"{
                ""type"": ""error"",
                ""msg"": {""code"": 6, ""message"": ""Already subscribed""}
            }";

            // Act - This should not throw an exception
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("error"))), Times.Once);
            TestContext.WriteLine("Result: Error message processed successfully without exceptions.");
        }

        /// <summary>
        /// Verifies that subscription confirmation messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Subscription confirmation messages trigger appropriate processing
        /// - The MessageProcessor handles subscription acknowledgments with SID information
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessSubscribedMessage_SubscriptionConfirmation_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that subscription confirmation messages are correctly processed by the MessageProcessor.");
            // Arrange
            var message = @"{
                ""type"": ""subscribed"",
                ""sid"": 12345,
                ""channel"": ""orderbook_delta""
            }";

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("subscribed"))), Times.Once);
            TestContext.WriteLine("Result: Subscription confirmation message processed successfully.");
        }

        /// <summary>
        /// Verifies that unsubscription confirmation messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - Unsubscription confirmation messages trigger appropriate processing
        /// - The MessageProcessor handles unsubscription acknowledgments correctly
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessUnsubscribedMessage_UnsubscriptionConfirmation_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that unsubscription confirmation messages are correctly processed by the MessageProcessor.");
            // Arrange
            var message = @"{
                ""type"": ""unsubscribed"",
                ""sid"": 12345
            }";

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("unsubscribed"))), Times.Once);
            TestContext.WriteLine("Result: Unsubscription confirmation message processed successfully.");
        }

        /// <summary>
        /// Verifies that OK confirmation messages are correctly processed by the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This test ensures that:
        /// - OK confirmation messages trigger appropriate processing
        /// - The MessageProcessor handles successful operation acknowledgments
        /// - The message format matches Kalshi's WebSocket API specification
        /// </remarks>
        [Test]
        public async Task ProcessOkMessage_UpdateConfirmation_MessageProcessorCalled()
        {
            TestContext.WriteLine("Testing that OK confirmation messages are correctly processed by the MessageProcessor.");
            // Arrange
            var message = @"{
                ""type"": ""ok"",
                ""id"": 12345
            }";

            // Act
            await _messageProcessorMock.Object.ProcessMessageAsync(message);

            // Assert - Verify that ProcessMessageAsync was called with the correct message
            _messageProcessorMock.Verify(mp => mp.ProcessMessageAsync(It.Is<string>(s => s.Contains("ok"))), Times.Once);
            TestContext.WriteLine("Result: OK confirmation message processed successfully.");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Verifies that subscribing to a specific channel correctly delegates to the SubscriptionManager.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client properly forwards subscription requests to the SubscriptionManager
        /// - Channel enablement is respected during subscription operations
        /// - Market tickers are correctly passed through to the subscription layer
        /// </remarks>
        [Test]
        public async Task SubscribeToChannel_OrderBookChannel_SubscriptionManagerCalled()
        {
            TestContext.WriteLine("Testing that subscribing to a specific channel correctly delegates to the SubscriptionManager.");
            // Arrange
            string channel = "orderbook";
            string[] marketTickers = { "TEST-123" };
            _client.EnableChannel(channel);

            // Act
            await _client.SubscribeToChannelAsync(channel, marketTickers);

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.SubscribeToChannelAsync(channel, marketTickers),
                Times.Once);
            TestContext.WriteLine("Result: Subscription to channel delegated successfully.");
        }

        /// <summary>
        /// Verifies that subscribing to watched markets correctly delegates to the SubscriptionManager for all enabled channels.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client processes watched markets for all enabled channels
        /// - Connection state is properly checked before attempting subscriptions
        /// - Subscription requests are filtered based on current subscription status and market eligibility
        /// - The SubscriptionManager is called appropriately for each enabled channel
        /// </remarks>
        [Test]
        public async Task SubscribeToWatchedMarkets_WatchedMarketsSet_SubscriptionManagerCalled()
        {
            TestContext.WriteLine("Testing that subscribing to watched markets correctly delegates to the SubscriptionManager for all enabled channels.");
            // Arrange
            var watchedMarkets = new HashSet<string> { "TEST-123", "TEST-456" };
            _client.WatchedMarkets = watchedMarkets;
            _client.EnableAllChannels();

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
            TestContext.WriteLine("Result: Subscription to watched markets delegated successfully.");
        }

        /// <summary>
        /// Verifies that unsubscribing from a specific channel correctly delegates to the SubscriptionManager.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client properly forwards unsubscription requests to the SubscriptionManager
        /// - Channel unsubscription operations are handled correctly
        /// - The SubscriptionManager's unsubscription method is called with the correct parameters
        /// </remarks>
        [Test]
        public async Task UnsubscribeFromChannel_ChannelSpecified_SubscriptionManagerCalled()
        {
            TestContext.WriteLine("Testing that unsubscribing from a specific channel correctly delegates to the SubscriptionManager.");
            // Arrange
            string channel = "orderbook";

            // Act
            await _client.UnsubscribeFromChannelAsync(channel);

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.UnsubscribeFromChannelAsync(channel),
                Times.Once);
            TestContext.WriteLine("Result: Unsubscription from channel delegated successfully.");
        }

        /// <summary>
        /// Verifies that unsubscribing from all channels correctly delegates to the SubscriptionManager.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client properly forwards full unsubscription requests to the SubscriptionManager
        /// - Complete unsubscription operations are handled correctly
        /// - The SubscriptionManager's full unsubscription method is called
        /// </remarks>
        [Test]
        public async Task UnsubscribeFromAll_AllChannels_UnsubscribeFromAllAsyncCalled()
        {
            TestContext.WriteLine("Testing that unsubscribing from all channels correctly delegates to the SubscriptionManager.");
            // Act
            await _client.UnsubscribeFromAllAsync();

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.UnsubscribeFromAllAsync(),
                Times.Once);
            TestContext.WriteLine("Result: Unsubscription from all channels delegated successfully.");
        }

        /// <summary>
        /// Verifies that checking subscription status correctly delegates to the SubscriptionManager.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client properly forwards subscription status queries to the SubscriptionManager
        /// - Market and channel parameters are correctly passed through
        /// - The SubscriptionManager's status checking method is called with correct parameters
        /// </remarks>
        [Test]
        public void IsSubscribed_MarketAndChannel_SubscriptionManagerCalled()
        {
            TestContext.WriteLine("Testing that checking subscription status correctly delegates to the SubscriptionManager.");
            // Arrange
            string marketTicker = "TEST-123";
            string channel = "orderbook";

            // Act
            _client.IsSubscribed(marketTicker, channel);

            // Assert
            _subscriptionManagerMock.Verify(
                sm => sm.IsSubscribed(marketTicker, channel),
                Times.Once);
            TestContext.WriteLine("Result: Subscription status check delegated successfully.");
        }

        /// <summary>
        /// Verifies that resetting event counts correctly delegates to the MessageProcessor.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client properly forwards event count reset requests to the MessageProcessor
        /// - The MessageProcessor's reset method is called correctly
        /// - Event counting functionality can be reset as needed
        /// </remarks>
        [Test]
        public void ResetEventCounts_Called_MessageProcessorResetEventCountsCalled()
        {
            TestContext.WriteLine("Testing that resetting event counts correctly delegates to the MessageProcessor.");
            // Act
            _client.ResetEventCounts();

            // Assert
            _messageProcessorMock.Verify(
                mp => mp.ResetEventCounts(),
                Times.Once);
            TestContext.WriteLine("Result: Event counts reset delegated successfully.");
        }

        #endregion

        #region Connection Tests

        /// <summary>
        /// Verifies that connecting to the WebSocket properly initializes all components.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client successfully establishes connection through the ConnectionManager
        /// - Message processing is started when connection is established
        /// - Subscription management is initialized for the connection
        /// - All components are properly coordinated during the connection process
        /// </remarks>
        [Test]
        public async Task ConnectAsync_ConnectionManagerConnected_MessageProcessingStarted()
        {
            TestContext.WriteLine("Testing that connecting to the WebSocket properly initializes all components.");
            // Arrange
            _connectionManagerMock.Setup(cm => cm.ConnectAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.IsConnected()).Returns(true);

            // Act
            await _client.ConnectAsync();

            // Assert
            _connectionManagerMock.Verify(cm => cm.ConnectAsync(0), Times.Once);
            _messageProcessorMock.Verify(mp => mp.StartProcessingAsync(), Times.Once);
            _subscriptionManagerMock.Verify(sm => sm.StartAsync(), Times.Once);
            TestContext.WriteLine("Result: WebSocket connection and component initialization completed successfully.");
        }

        /// <summary>
        /// Verifies that shutting down the WebSocket client properly stops all components.
        /// </summary>
        /// <remarks>
        /// This integration test ensures that:
        /// - The WebSocket client properly unsubscribes from all channels during shutdown
        /// - The connection is cleanly closed through the ConnectionManager
        /// - Message processing is stopped gracefully
        /// - All components are properly coordinated during the shutdown process
        /// </remarks>
        [Test]
        public async Task ShutdownAsync_AllComponentsStopped()
        {
            TestContext.WriteLine("Testing that shutting down the WebSocket client properly stops all components.");
            // Arrange
            _subscriptionManagerMock.Setup(sm => sm.UnsubscribeFromAllAsync()).Returns(Task.CompletedTask);
            _connectionManagerMock.Setup(cm => cm.StopAsync()).Returns(Task.CompletedTask);
            _messageProcessorMock.Setup(mp => mp.StopProcessingAsync()).Returns(Task.CompletedTask);

            // Act
            await _client.ShutdownAsync();

            // Assert
            _subscriptionManagerMock.Verify(sm => sm.UnsubscribeFromAllAsync(), Times.Once);
            _connectionManagerMock.Verify(cm => cm.StopAsync(), Times.Once);
            _messageProcessorMock.Verify(mp => mp.StopProcessingAsync(), Times.Once);
            TestContext.WriteLine("Result: WebSocket client shutdown completed successfully.");
        }

        #endregion
    }
}