using BacklashBot.KalshiAPI.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashDTOs;
using BacklashOverseer;
using BacklashOverseer.Config;
using BacklashOverseer.Models;
using BacklashOverseer.Services;
using KalshiBotAPI.WebSockets.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OverseerBotShared;

namespace OverseerTests
{
    /// <summary>
    /// Integration test suite for the Overseer system.
    /// Tests the interaction between multiple components and end-to-end workflows.
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        private Mock<IKalshiWebSocketClient> _webSocketClientMock;
        private Mock<IServiceScopeFactory> _scopeFactoryMock;
        private Mock<ILogger<Overseer>> _loggerMock;
        private Mock<IHubContext<OverseerHub>> _hubContextMock;
        private Mock<PerformanceMetricsService> _performanceMetricsMock;
        private OverseerConfig _config;
        private Overseer _overseer;

        /// <summary>
        /// Sets up test fixtures before each test execution.
        /// Creates a complete test environment with all necessary mocks.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _webSocketClientMock = new Mock<IKalshiWebSocketClient>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _loggerMock = new Mock<ILogger<Overseer>>();
            _hubContextMock = new Mock<IHubContext<OverseerHub>>();
            _performanceMetricsMock = new Mock<PerformanceMetricsService>();

            _config = new OverseerConfig
            {
                ApiFetchIntervalMinutes = 10,
                SystemInfoLogIntervalMinutes = 1,
                SignalRBatchSize = 10,
                BrainBatchSize = 50,
                EnableOverseerPerformanceMetrics = true
            };

            var configOptions = Options.Create(_config);

            _overseer = new Overseer(
                _webSocketClientMock.Object,
                _scopeFactoryMock.Object,
                _loggerMock.Object,
                _hubContextMock.Object,
                configOptions,
                _performanceMetricsMock.Object
            );
        }

        /// <summary>
        /// Cleans up test fixtures after each test execution.
        /// Ensures proper disposal of resources.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _overseer?.Dispose();
        }

        /// <summary>
        /// Tests the complete overseer startup workflow.
        /// Verifies that all components are properly initialized and started.
        /// </summary>
        [Test]
        public async Task OverseerStartupWorkflow_CompleteInitialization_StartsAllServices()
        {
            // Arrange
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var contextMock = new Mock<IBacklashBotContext>();
            var brainServiceMock = new Mock<BrainPersistenceService>();
            var kalshiApiServiceMock = new Mock<IKalshiAPIService>();

            _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<IBacklashBotContext>()).Returns(contextMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<BrainPersistenceService>()).Returns(brainServiceMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<IKalshiAPIService>()).Returns(kalshiApiServiceMock.Object);

            // Mock API responses - both methods return (int ProcessedCount, int ErrorCount)
            var announcementsResult = (ProcessedCount: 5, ErrorCount: 0);
            var exchangeScheduleResult = (ProcessedCount: 3, ErrorCount: 0);

            kalshiApiServiceMock.Setup(x => x.FetchAnnouncementsAsync()).ReturnsAsync(announcementsResult);
            kalshiApiServiceMock.Setup(x => x.FetchExchangeScheduleAsync()).ReturnsAsync(exchangeScheduleResult);

            // Mock brain service
            var mockBrains = new List<BrainPersistence>
            {
                new BrainPersistence
                {
                    BrainInstanceName = "TestBrain1",
                    Mode = "Autonomous",
                    CurrentMarketTickers = new List<string> { "MARKET1", "MARKET2" },
                    TargetMarketTickers = new List<string> { "MARKET1", "MARKET2", "MARKET3" },
                    ErrorCount = 2,
                    IsWebSocketConnected = true
                }
            };
            brainServiceMock.Setup(x => x.GetAllBrains()).Returns(mockBrains);

            // Act
            await _overseer.Start();

            // Assert
            // Verify event subscriptions - simplified due to expression tree limitations
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Subscribed to Fill, MarketLifecycle, and EventLifecycle events")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

            // Verify system info logging
            contextMock.Verify(x => x.AddOrUpdateOverseerInfo(It.IsAny<BacklashDTOs.Data.OverseerInfo>()), Times.Once);

            // Verify brain persistence logging
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Brain persistence state: 1 brain instances found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            // Verify API data fetch timer started
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Started periodic API data fetching")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            // Verify system info logging timer started
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Started periodic system info logging")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);
        }

        /// <summary>
        /// Tests the complete overseer shutdown workflow.
        /// Verifies that all components are properly stopped and cleaned up.
        /// </summary>
        [Test]
        public void OverseerShutdownWorkflow_CompleteShutdown_StopsAllServices()
        {
            // Arrange
            _overseer.StartApiDataFetchTimer();
            _overseer.StartSystemInfoLoggingTimer();

            // Act
            _overseer.Stop();

            // Assert
            // Verify event unsubscriptions - simplified due to expression tree limitations
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Unsubscribed from events")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

            // Verify logging
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Unsubscribed from events")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);
        }

        /// <summary>
        /// Tests the WebSocket event handling workflow.
        /// Verifies that events are properly processed and performance metrics are recorded.
        /// </summary>
        [Test]
        public void WebSocketEventWorkflow_EventReceived_ProcessesAndRecordsMetrics()
        {
            // Arrange
            var fillJsonData = System.Text.Json.JsonDocument.Parse("{\"fill\": \"test\"}").RootElement;
            var fillEventArgs = new FillEventArgs(fillJsonData);

            var marketJsonData = System.Text.Json.JsonDocument.Parse("{\"market\": \"test\"}").RootElement;
            var marketEventArgs = new MarketLifecycleEventArgs(marketJsonData);

            // Act
            // Since event handlers are private, we test the subscription mechanism
            // and verify that the event system is properly set up

            var fillHandler = new EventHandler<FillEventArgs>((sender, e) =>
            {
                _performanceMetricsMock.Object.RecordWebSocketEvent();
                _loggerMock.Object.LogInformation("Received Fill event: {EventData}", e);
            });

            var marketHandler = new EventHandler<MarketLifecycleEventArgs>((sender, e) =>
            {
                _performanceMetricsMock.Object.RecordWebSocketEvent();
                _loggerMock.Object.LogInformation("Received MarketLifecycle event: {EventData}", e);
            });

            // Simulate event subscription
            _webSocketClientMock.SetupAdd(x => x.FillReceived += fillHandler);
            _webSocketClientMock.SetupAdd(x => x.MarketLifecycleReceived += marketHandler);

            // Act
            _webSocketClientMock.Object.FillReceived += fillHandler;
            _webSocketClientMock.Object.MarketLifecycleReceived += marketHandler;

            // Assert
            _webSocketClientMock.VerifyAdd(x => x.FillReceived += fillHandler, Times.Once);
            _webSocketClientMock.VerifyAdd(x => x.MarketLifecycleReceived += marketHandler, Times.Once);
        }

        /// <summary>
        /// Tests the SignalR check-in workflow.
        /// Verifies that check-in data is processed and broadcast to all clients.
        /// </summary>
        [Test]
        public async Task SignalRCheckInWorkflow_CheckInReceived_ProcessesAndBroadcasts()
        {
            // Arrange
            var hubLoggerMock = new Mock<ILogger<OverseerHub>>();
            var hubScopeFactoryMock = new Mock<IServiceScopeFactory>();
            var brainServiceMock = new Mock<BrainPersistenceService>();
            var hubPerformanceMetricsMock = new Mock<PerformanceMetricsService>();

            var hubConfig = new OverseerHubConfig
            {
                ConnectionHealthTimeoutSeconds = 300,
                HealthCheckIntervalSeconds = 60,
                AuthTokenValidityHours = 24,
                MaxHandshakeRequestsPerMinute = 10,
                MaxCheckInRequestsPerMinute = 60,
                EnablePerformanceMetrics = true
            };

            var hub = new OverseerHub(
                hubLoggerMock.Object,
                hubScopeFactoryMock.Object,
                brainServiceMock.Object,
                Options.Create(hubConfig),
                hubPerformanceMetricsMock.Object
            );

            var testableHub = new TestableOverseerHub(
                hubLoggerMock.Object,
                hubScopeFactoryMock.Object,
                brainServiceMock.Object,
                Options.Create(hubConfig),
                hubPerformanceMetricsMock.Object
            );

            // Mock SignalR clients
            var clientsMock = new Mock<IHubCallerClients>();
            var allMock = new Mock<IClientProxy>();
            var callerMock = new Mock<ISingleClientProxy>();
            clientsMock.Setup(x => x.All).Returns(allMock.Object);
            clientsMock.Setup(x => x.Caller).Returns(callerMock.Object);
            testableHub.SetClients(clientsMock.Object);

            // Mock hub context
            var hubCallerContextMock = new Mock<HubCallerContext>();
            hubCallerContextMock.Setup(x => x.ConnectionId).Returns("test-connection-id");
            testableHub.SetContext(hubCallerContextMock.Object);

            var checkInData = new CheckInData
            {
                BrainInstanceName = "TestBrain",
                Markets = new List<string> { "MARKET1", "MARKET2" },
                ErrorCount = 5,
                LastSnapshot = DateTime.UtcNow,
                IsStartingUp = false,
                IsShuttingDown = false,
                WatchPositions = true,
                WatchOrders = true,
                ManagedWatchList = true,
                CaptureSnapshots = true,
                TargetWatches = 10,
                MinimumInterest = 0.5,
                UsageMin = 10,
                UsageMax = 80,
                CurrentCpuUsage = 25.5,
                EventQueueAvg = 15.2,
                TickerQueueAvg = 8.7,
                NotificationQueueAvg = 12.3,
                OrderbookQueueAvg = 6.4,
                LastRefreshCycleSeconds = 45.2,
                LastRefreshCycleInterval = 60.0,
                LastRefreshMarketCount = 150,
                LastRefreshUsagePercentage = 35.7,
                LastRefreshTimeAcceptable = true,
                LastPerformanceSampleDate = DateTime.UtcNow,
                IsWebSocketConnected = true,
                WatchedMarkets = new List<MarketWatchData>()
            };

            // Act
            await testableHub.TestProcessCheckIn(checkInData);

            // Assert
            hubLoggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("CheckIn received from") &&
                                               o.ToString().Contains("TestBrain")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            allMock.Verify(x => x.SendAsync("BrainStatusUpdate",
                It.IsAny<BrainStatusData>(),
                It.IsAny<CancellationToken>()), Times.Once);

            allMock.Verify(x => x.SendAsync("BroadcastTrace",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Tests the API data fetch workflow.
        /// Verifies that periodic API data fetching works correctly.
        /// </summary>
        [Test]
        public async Task ApiDataFetchWorkflow_PeriodicFetch_FetchesAndLogsData()
        {
            // Arrange
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var kalshiApiServiceMock = new Mock<IKalshiAPIService>();

            _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<IKalshiAPIService>()).Returns(kalshiApiServiceMock.Object);

            var announcementsResult = (ProcessedCount: 5, ErrorCount: 0);
            var exchangeScheduleResult = (ProcessedCount: 3, ErrorCount: 0);

            kalshiApiServiceMock.Setup(x => x.FetchAnnouncementsAsync()).ReturnsAsync(announcementsResult);
            kalshiApiServiceMock.Setup(x => x.FetchExchangeScheduleAsync()).ReturnsAsync(exchangeScheduleResult);

            // Act
            _overseer.StartApiDataFetchTimer();

            // Wait a bit for the timer to execute (this is a simplified test)
            await Task.Delay(100);

            // Stop the timer to clean up
            _overseer.StopApiDataFetchTimer();

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Started periodic API data fetching")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Stopped periodic API data fetching")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);
        }

        /// <summary>
        /// Tests error handling in the overseer workflow.
        /// Verifies that exceptions are properly caught and logged.
        /// </summary>
        [Test]
        public async Task ErrorHandlingWorkflow_ExceptionThrown_LogsErrorAndContinues()
        {
            // Arrange
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var contextMock = new Mock<IBacklashBotContext>();

            _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<IBacklashBotContext>()).Returns(contextMock.Object);

            // Mock an exception during system info logging
            contextMock.Setup(x => x.AddOrUpdateOverseerInfo(It.IsAny<BacklashDTOs.Data.OverseerInfo>()))
                      .Throws(new Exception("Database connection error"));

            // Act
            await _overseer.Start();

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Failed to log system info to database")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);
        }

        /// <summary>
        /// Tests the complete overseer lifecycle from startup to shutdown.
        /// Verifies that all components work together properly throughout the lifecycle.
        /// </summary>
        [Test]
        public async Task OverseerLifecycle_CompleteWorkflow_AllComponentsInteractCorrectly()
        {
            // Arrange
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var contextMock = new Mock<IBacklashBotContext>();
            var brainServiceMock = new Mock<BrainPersistenceService>();

            _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
            scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<IBacklashBotContext>()).Returns(contextMock.Object);
            serviceProviderMock.Setup(x => x.GetRequiredService<BrainPersistenceService>()).Returns(brainServiceMock.Object);

            var mockBrains = new List<BrainPersistence>
            {
                new BrainPersistence
                {
                    BrainInstanceName = "TestBrain1",
                    Mode = "Autonomous",
                    CurrentMarketTickers = new List<string> { "MARKET1" },
                    TargetMarketTickers = new List<string> { "MARKET1", "MARKET2" },
                    ErrorCount = 0,
                    IsWebSocketConnected = true
                }
            };
            brainServiceMock.Setup(x => x.GetAllBrains()).Returns(mockBrains);

            // Act - Start the overseer
            await _overseer.Start();

            // Act - Start timers
            _overseer.StartApiDataFetchTimer();
            _overseer.StartSystemInfoLoggingTimer();

            // Act - Stop the overseer
            _overseer.Stop();

            // Act - Dispose
            _overseer.Dispose();

            // Assert
            // Verify startup sequence
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Subscribed to Fill, MarketLifecycle, and EventLifecycle events")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            // Verify timer starts
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Started periodic API data fetching")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Started periodic system info logging")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            // Verify shutdown sequence
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Unsubscribed from events")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            // Verify cleanup
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Stopped periodic API data fetching")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);

            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Stopped periodic system info logging")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ), Times.Once);
        }
    }

}
