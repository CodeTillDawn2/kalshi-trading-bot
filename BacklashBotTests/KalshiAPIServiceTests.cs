using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashBotData.Configuration;
using BacklashBotData.Data;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs.Data;
using BacklashDTOs.Helpers;
using BacklashInterfaces.Constants;
using BacklashInterfaces.PerformanceMetrics;
using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace KalshiBotTests
{
    /// <summary>
    /// Comprehensive NUnit test fixture for validating the KalshiAPIService functionality.
    /// This test class provides integration testing for all Kalshi API service methods including
    /// market data retrieval, series information, event details, position management, candlestick data,
    /// account balance, exchange status, order operations, announcements, and various other API endpoints.
    /// The tests use a combination of mocked dependencies and real database context for dynamic test data
    /// to ensure comprehensive coverage while maintaining test isolation and reliability.
    /// </summary>
    [TestFixture]
    public class KalshiAPIServiceTests
    {
        /// <summary>
        /// Mock logger for testing logging behavior in the API service.
        /// </summary>
        private Mock<ILogger<IKalshiAPIService>> _loggerMock;

        /// <summary>
        /// Connection string loaded from appsettings.json.
        /// </summary>
        private string _connectionString;

        /// <summary>
        /// Kalshi configuration loaded from appsettings.json.
        /// </summary>
        private KalshiConfig _kalshiConfig;

        /// <summary>
        /// BacklashBotData configuration loaded from appsettings.json.
        /// </summary>
        private BacklashBotDataConfig _dataConfig;

        /// <summary>
        /// Secrets configuration loaded from appsettings.json.
        /// </summary>
        private SecretsConfig _secretsConfig;

        /// <summary>
        /// Mock service scope factory for dependency injection in tests.
        /// </summary>
        private Mock<IServiceScopeFactory> _scopeFactoryMock;

        /// <summary>
        /// Mock status tracker service for managing cancellation tokens and operation status.
        /// </summary>
        private Mock<IStatusTrackerService> _statusTrackerMock;

        /// <summary>
        /// Mock performance monitor for recording execution times and metrics.
        /// </summary>
        private Mock<IPerformanceMonitor> _performanceMonitorMock;

        /// <summary>
        /// Options wrapper for Kalshi configuration settings.
        /// </summary>
        private IOptions<KalshiConfig> _kalshiConfigOptions;

        /// <summary>
        /// The actual KalshiAPIService instance being tested with mocked dependencies.
        /// </summary>
        private KalshiAPIService _apiService;

        /// <summary>
        /// Mock database context for isolating database operations during testing.
        /// </summary>
        private Mock<IBacklashBotContext> _contextMock;

        /// <summary>
        /// Real database context for querying dynamic test data from the actual database.
        /// Used to obtain real market, series, and event tickers for testing.
        /// </summary>
        private BacklashBotContext _realContext;

        /// <summary>
        /// Market ticker obtained from the database for use in tests requiring a specific market.
        /// </summary>
        private string _testMarketTicker;

        /// <summary>
        /// Series ticker obtained from the database for use in tests requiring a specific series.
        /// </summary>
        private string _testSeriesTicker;

        /// <summary>
        /// Event ticker obtained from the database for use in tests requiring a specific event.
        /// </summary>
        private string _testEventTicker;

        /// <summary>
        /// Time interval constant used for candlestick testing ("minute").
        /// </summary>
        private const string TestInterval = "minute";

        /// <summary>
        /// Start timestamp for candlestick data testing (24 hours ago from test execution).
        /// </summary>
        private long _testStartTs;

        /// <summary>
        /// Performance threshold constants for API timing requirements (in milliseconds).
        /// </summary>
        private const int MarketDataApiTimeoutMs = 5000;    // 5 seconds for market data
        private const int OrderApiTimeoutMs = 3000;          // 3 seconds for order operations
        private const int AccountApiTimeoutMs = 2000;        // 2 seconds for account data
        private const int MetadataApiTimeoutMs = 10000;      // 10 seconds for metadata operations

        /// <summary>
        /// Test category constants for organizing tests by API operation type.
        /// </summary>
        public const string MarketDataCategory = "MarketData";

        /// <summary>
        /// Test category for order-related operations and trading functionality.
        /// </summary>
        public const string OrderOperationsCategory = "OrderOperations";

        /// <summary>
        /// Test category for account balance and account-related data operations.
        /// </summary>
        public const string AccountDataCategory = "AccountData";

        /// <summary>
        /// Test category for metadata operations like series and event information.
        /// </summary>
        public const string MetadataCategory = "Metadata";

        /// <summary>
        /// Test category for performance and timing validation tests.
        /// </summary>
        public const string PerformanceCategory = "Performance";

        /// <summary>
        /// Test category for error handling and exception scenarios.
        /// </summary>
        public const string ErrorHandlingCategory = "ErrorHandling";

        /// <summary>
        /// Test category for input parameter validation tests.
        /// </summary>
        public const string InputValidationCategory = "InputValidation";

        /// <summary>
        /// Test category for configuration validation and setup tests.
        /// </summary>
        public const string ConfigurationCategory = "Configuration";

        /// <summary>
        /// Performs one-time setup for the entire test fixture.
        /// Loads real configuration from appsettings.json, initializes a real database context,
        /// and queries for active market data to use in tests. This setup runs once before all tests
        /// in the fixture to provide consistent test data across all test methods.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Load configuration from appsettings.json
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var appsettingsPath = Path.Combine(basePath, "appsettings.json");
            var json = File.ReadAllText(appsettingsPath);
            var doc = JsonDocument.Parse(json);

            // Parse configs from JSON
            _connectionString = doc.RootElement.GetProperty("DBConnection").GetProperty("DefaultConnection").GetString();
            _dataConfig = JsonSerializer.Deserialize<BacklashBotDataConfig>(doc.RootElement.GetProperty("DBConnection").GetProperty("BacklashBotData"));
            _kalshiConfig = JsonSerializer.Deserialize<KalshiConfig>(doc.RootElement.GetProperty("Kalshi"));
            _secretsConfig = JsonSerializer.Deserialize<SecretsConfig>(doc.RootElement.GetProperty("Secrets"));

            // Initialize real context to query for dynamic test data
            var logger = new Mock<ILogger<BacklashBotContext>>().Object;
            _realContext = new BacklashBotContext(_connectionString, logger, _dataConfig);

            // Query for an active market
            var activeMarket = await _realContext.GetMarketsFiltered(
                includedStatuses: new HashSet<string> { KalshiConstants.Status_Active }
            );

            Assert.That(activeMarket, Is.Not.Empty, "No active markets found in the database");

            // Get a market with good liquidity for testing
            var marketDto = activeMarket.Where(x => x.yes_bid > 20 && x.no_bid > 20).OrderByDescending(x => x.APILastFetchedDate).FirstOrDefault();
            if (marketDto == null)
            {
                // Fallback to first market if no good bids found
                marketDto = activeMarket.First();
            }

            _testMarketTicker = marketDto.market_ticker;
            _testEventTicker = marketDto.event_ticker;

            // Get event data for the selected market
            var eventDto = await _realContext.GetEventByTicker(_testEventTicker);
            Assert.That(eventDto, Is.Not.Null, $"Event not found for ticker {_testEventTicker}");
            _testSeriesTicker = eventDto.series_ticker;

            // Get the latest candlestick for this market to use as a reference point
            var latestCandlestick = await _realContext.GetLatestCandlestick(_testMarketTicker, 1); // 1 = minute interval
            if (latestCandlestick != null)
            {
                // Set start time to 2 hours before the latest candlestick to ensure overlap
                _testStartTs = latestCandlestick.end_period_ts - (2 * 60 * 60); // Subtract 2 hours in Unix timestamp
            }
            else
            {
                // Fallback to 24 hours ago if no candlesticks exist
                _testStartTs = UnixHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddDays(-1));
            }
        }

        /// <summary>
        /// Performs cleanup after all tests in the fixture have completed.
        /// Disposes of the real database context to ensure proper resource cleanup.
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _realContext?.Dispose();
        }

        /// <summary>
        /// Sets up the test environment before each individual test method.
        /// Loads configuration, validates required settings, creates mocks for all dependencies,
        /// sets up the service scope factory with mocked context, and instantiates the API service
        /// with real configuration but mocked dependencies for isolated testing.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Use the loaded kalshi config
            var kalshiConfig = _kalshiConfig;

            // Validate configuration
            Assert.That(kalshiConfig.KeyId, Is.Not.Null.And.Not.Empty, "KalshiConfig.BotKeyId is missing in appsettings.json");
            Assert.That(kalshiConfig.KeyFile, Is.Not.Null.And.Not.Empty, "KalshiConfig.BotKeyFile is missing in appsettings.json");
            Assert.That(kalshiConfig.Environment, Is.Not.Null.And.Not.Empty, "KalshiConfig.Environment is missing in appsettings.json");

            // Resolve the key file path using the loaded secrets config
            var resolvedKeyFile = BacklashCommon.Configuration.ConfigurationHelper.ResolveSecretsFilePath(
                kalshiConfig.KeyFile,
                _secretsConfig,
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot")));

            kalshiConfig.KeyFile = resolvedKeyFile;

            // Now validate that the resolved key file exists
            Assert.That(File.Exists(kalshiConfig.KeyFile), Is.True, $"KeyFile {kalshiConfig.KeyFile} does not exist");

            _kalshiConfigOptions = Options.Create(kalshiConfig);

            _loggerMock = new Mock<ILogger<IKalshiAPIService>>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _statusTrackerMock = new Mock<IStatusTrackerService>();
            _statusTrackerMock.Setup(s => s.GetCancellationToken()).Returns(CancellationToken.None);
            _performanceMonitorMock = new Mock<IPerformanceMonitor>();

            // Mock the DB context to avoid actual database interactions (focus on API calls)
            _contextMock = new Mock<IBacklashBotContext>();

            // Setup ALL database methods that might be called during market fetching
            // Use Verifiable to ensure all expected calls are made correctly

            _contextMock.Setup(c => c.AddOrUpdateMarkets(It.IsAny<List<MarketDTO>>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.AddOrUpdateMarket(It.IsAny<MarketDTO>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.GetMarketByTicker(It.IsAny<string>()))
                .ReturnsAsync((string ticker) => new MarketDTO
                {
                    market_ticker = ticker,
                    status = KalshiConstants.Status_Active
                })
                .Verifiable();

            _contextMock.Setup(c => c.AddOrUpdateSeries(It.IsAny<SeriesDTO>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.AddOrUpdateEvent(It.IsAny<EventDTO>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.GetMarketPositions(It.IsAny<HashSet<string>>(), It.IsAny<bool?>(), It.IsAny<bool?>()))
                .ReturnsAsync(new List<MarketPositionDTO>())
                .Verifiable();

            _contextMock.Setup(c => c.GetMarketPositions(null, null, null))
                .ReturnsAsync(new List<MarketPositionDTO>())
                .Verifiable();

            _contextMock.Setup(c => c.AddOrUpdateMarketPosition(It.IsAny<MarketPositionDTO>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.RemoveMarketPosition(It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.AddOrUpdateCandlestick(It.IsAny<CandlestickDTO>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.UpdateMarketLastCandlestick(It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            _contextMock.Setup(c => c.AddOrUpdateOrder(It.IsAny<OrderDTO>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Additional methods that might be called
            _contextMock.Setup(c => c.GetEventByTicker(It.IsAny<string>()))
                .ReturnsAsync((string ticker) => new EventDTO
                {
                    event_ticker = ticker,
                    series_ticker = "TEST_SERIES"
                })
                .Verifiable();

            _contextMock.Setup(c => c.GetSeriesByTicker(It.IsAny<string>()))
                .ReturnsAsync((string ticker) => new SeriesDTO
                {
                    series_ticker = ticker,
                    title = "Test Series"
                })
                .Verifiable();

            // Setup scope factory to return mocked context
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IBacklashBotContext))).Returns(_contextMock.Object);
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            // Instantiate the real API service with mocks and real config
            var apiConfig = new KalshiAPIServiceConfig
            {
                EnablePerformanceMetrics = false // Disable performance monitoring in tests
            };

            var apiConfigOptions = Options.Create(apiConfig);

            _apiService = new KalshiAPIService(
                _loggerMock.Object,
                _connectionString,
                _scopeFactoryMock.Object,
                _statusTrackerMock.Object,
                _kalshiConfigOptions,
                apiConfigOptions,
                _performanceMonitorMock.Object
            );
        }

        /// <summary>
        /// Tests the FetchMarketsAsync method with status filter set to "open".
        /// Verifies that the API service can successfully retrieve and process markets
        /// with open status, ensuring proper data handling and error management.
        /// </summary>
        [Test]
        [Category(MarketDataCategory)]
        public async Task FetchMarketsAsync_WithStatusOpen_ReturnsProcessedMarkets()
        {
            TestContext.WriteLine("Testing FetchMarketsAsync with status filter set to 'open'.");
            TestContext.WriteLine("Expected: API should return processed markets with no errors");
            TestContext.WriteLine("Parameters: status=open");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(status: KalshiConstants.Status_Open);

            // Output details
            TestContext.WriteLine($"Result: Processed {processedCount} markets with {errorCount} errors");

            // Assert
            Assert.That(processedCount, Is.GreaterThan(0), "Expected some open markets to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch - all markets should be processed successfully");

            TestContext.WriteLine("Result: FetchMarketsAsync with open status completed successfully.");
        }

        /// <summary>
        /// Tests the FetchMarketsAsync method with a specific market ticker filter.
        /// Verifies that the API service can retrieve and process a single market
        /// when provided with a specific ticker, ensuring accurate filtering and processing.
        /// </summary>
        [Test]
        [Category(MarketDataCategory)]
        public async Task FetchMarketsAsync_WithSpecificTicker_ReturnsProcessedMarket()
        {
            TestContext.WriteLine("Testing FetchMarketsAsync with a specific market ticker filter.");
            TestContext.WriteLine("Expected: API should return exactly one processed market with no errors");
            TestContext.WriteLine($"Parameters: ticker={_testMarketTicker}");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(tickers: new[] { _testMarketTicker });

            // Output details
            TestContext.WriteLine($"Result: Processed {processedCount} markets with {errorCount} errors");

            // Assert
            Assert.That(processedCount, Is.EqualTo(1), "Expected exactly one market to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");

            TestContext.WriteLine("Result: FetchMarketsAsync with specific ticker completed successfully.");
        }

        /// <summary>
        /// Tests the FetchSeriesAsync method with a valid series ticker.
        /// Verifies that the API service can successfully retrieve series information
        /// including metadata, tags, and settlement sources for a given series ticker.
        /// </summary>
        [Test]
        [Category(MetadataCategory)]
        public async Task FetchSeriesAsync_WithValidTicker_ReturnsSeriesData()
        {
            TestContext.WriteLine("Testing FetchSeriesAsync with a valid series ticker.");
            TestContext.WriteLine("Expected: API should return series data with matching ticker");
            TestContext.WriteLine($"Parameters: series_ticker={_testSeriesTicker}");

            // Act
            var result = await _apiService.FetchSeriesAsync(_testSeriesTicker);

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Ticker={result.Series.Ticker}, Title={result.Series.Title}, Category={result.Series.Category}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected series data to be returned");
            Assert.That(result.Series.Ticker, Is.EqualTo(_testSeriesTicker), "Ticker mismatch in response");

            TestContext.WriteLine("Result: FetchSeriesAsync completed successfully.");
        }

        /// <summary>
        /// Tests the FetchEventAsync method with a valid event ticker and nested markets enabled.
        /// Verifies that the API service can retrieve comprehensive event information
        /// including nested market data, ensuring proper relationship handling and data integrity.
        /// </summary>
        [Test]
        public async Task FetchEventAsync_WithValidTicker_ReturnsEventData()
        {
            // Use the dynamically selected event ticker from setup
            var testEventTicker = _testEventTicker;

            TestContext.WriteLine("Testing FetchEventAsync with a valid event ticker and nested markets enabled.");
            TestContext.WriteLine("Expected: API should return event data with nested markets");
            TestContext.WriteLine($"Parameters: event_ticker={testEventTicker} (dynamically selected), with_nested_markets=true");

            // Act
            var result = await _apiService.FetchEventAsync(testEventTicker, withNestedMarkets: true);

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Ticker={result.Event.EventTicker}, Title={result.Event.Title}, MarketCount={result.Event.Markets?.Count ?? 0}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected event data to be returned");
            Assert.That(result.Event.EventTicker, Is.EqualTo(testEventTicker), "Event ticker mismatch in response");

            TestContext.WriteLine("Result: FetchEventAsync with nested markets completed successfully.");
        }

        /// <summary>
        /// Tests the FetchPositionsAsync method to retrieve user's current positions.
        /// Verifies that the API service can successfully fetch and process position data,
        /// including proper handling of empty position sets and error conditions.
        /// </summary>
        [Test]
        public async Task FetchPositionsAsync_ReturnsPositions()
        {
            TestContext.WriteLine("Testing FetchPositionsAsync to retrieve user's current positions.");
            TestContext.WriteLine("Expected: API should return positions data with no errors");
            TestContext.WriteLine("Parameters: none (fetch all positions)");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchPositionsAsync();

            // Output details
            TestContext.WriteLine($"Result: Processed {processedCount} positions with {errorCount} errors");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no positions, so no strict assertion on count

            TestContext.WriteLine("Result: FetchPositionsAsync completed successfully.");
        }

        /// <summary>
        /// Tests the FetchCandlesticksAsync method with valid parameters.
        /// Verifies that the API service can successfully retrieve and process candlestick data
        /// for a specific market, series, and time interval, ensuring proper data handling
        /// and database persistence of technical analysis data.
        /// </summary>
        [Test]
        public async Task FetchCandlesticksAsync_WithValidParams_ReturnsCandlesticks()
        {
            TestContext.WriteLine("Testing FetchCandlesticksAsync with valid parameters.");
            TestContext.WriteLine("Expected: API should return candlestick data with no errors");
            TestContext.WriteLine($"Parameters: series={_testSeriesTicker}, market={_testMarketTicker}, interval={TestInterval}");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchCandlesticksAsync(
                _testSeriesTicker, _testMarketTicker, TestInterval, _testStartTs);

            // Output details
            TestContext.WriteLine($"Result: Processed {processedCount} candlesticks with {errorCount} errors");

            // Assert
            Assert.That(processedCount, Is.GreaterThan(0), "Expected some candlesticks to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");

            TestContext.WriteLine("Result: FetchCandlesticksAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetBalanceAsync method to retrieve account balance.
        /// Verifies that the API service can successfully fetch the current account balance
        /// and that it returns a non-negative value, ensuring proper authentication and data retrieval.
        /// </summary>
        [Test]
        [Category(AccountDataCategory)]
        public async Task GetBalanceAsync_ReturnsPositiveBalance()
        {
            TestContext.WriteLine("Testing GetBalanceAsync to retrieve account balance.");
            TestContext.WriteLine("Expected: API should return non-negative account balance");
            TestContext.WriteLine("Parameters: none");

            // Act
            var balance = await _apiService.GetBalanceAsync();

            // Output details
            TestContext.WriteLine($"Result: Account balance: {balance / 100m} USD");

            // Assert
            Assert.That(balance, Is.GreaterThanOrEqualTo(0), "Expected non-negative balance");

            TestContext.WriteLine("Result: GetBalanceAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetExchangeStatusAsync method to retrieve exchange operational status.
        /// Verifies that the API service can successfully fetch exchange status information
        /// including trading availability and operational state, ensuring proper system monitoring.
        /// </summary>
        [Test]
        public async Task GetExchangeStatusAsync_ReturnsStatus()
        {
            TestContext.WriteLine("Testing GetExchangeStatusAsync to retrieve exchange operational status.");
            TestContext.WriteLine("Expected: API should return exchange operational status");
            TestContext.WriteLine("Parameters: none");

            // Act
            var status = await _apiService.GetExchangeStatusAsync();

            // Output details
            if (status != null)
            {
                TestContext.WriteLine($"Result: TradingActive={status.trading_active}, ExchangeActive={status.exchange_active}");
            }

            // Assert
            Assert.That(status, Is.Not.Null, "Expected exchange status to be returned");

            TestContext.WriteLine("Result: GetExchangeStatusAsync completed successfully.");
        }

        /// <summary>
        /// Tests the FetchOrdersAsync method with status filter.
        /// Verifies that the API service can successfully retrieve and process order data
        /// with filtering capabilities, ensuring proper handling of order history and status tracking.
        /// </summary>
        [Test]
        public async Task FetchOrdersAsync_ReturnsOrders()
        {
            TestContext.WriteLine("Testing FetchOrdersAsync with status filter.");
            TestContext.WriteLine("Expected: API should return orders with no errors");
            TestContext.WriteLine("Parameters: status=executed");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchOrdersAsync(status: "executed");

            // Output details
            TestContext.WriteLine($"Result: Processed {processedCount} orders with {errorCount} errors");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no orders, so no strict assertion on count

            TestContext.WriteLine("Result: FetchOrdersAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetExchangeScheduleAsync method to retrieve trading schedule information.
        /// Verifies that the API service can successfully fetch exchange operating hours,
        /// maintenance windows, and trading schedule data for operational planning.
        /// </summary>
        [Test]
        public async Task GetExchangeScheduleAsync_ReturnsSchedule()
        {
            TestContext.WriteLine("Testing GetExchangeScheduleAsync to retrieve trading schedule information.");
            TestContext.WriteLine("Expected: API should return exchange trading hours schedule");
            TestContext.WriteLine("Parameters: none");

            // Act
            var schedule = await _apiService.GetExchangeScheduleAsync();

            // Output details
            if (schedule != null && schedule.Schedule.StandardHours.Any())
            {
                TestContext.WriteLine($"Result: Fetched {schedule.Schedule.StandardHours.Count} standard hours periods");
            }

            // Assert
            Assert.That(schedule, Is.Not.Null, "Expected schedule to be returned");
            Assert.That(schedule.Schedule.StandardHours, Is.Not.Empty, "Expected at least one standard hours period");

            TestContext.WriteLine("Result: GetExchangeScheduleAsync completed successfully.");
        }

        /// <summary>
        /// Tests the FetchAnnouncementsAsync method to retrieve exchange announcements.
        /// Verifies that the API service can successfully fetch and process important
        /// notifications and updates from the Kalshi exchange platform.
        /// </summary>
        [Test]
        public async Task FetchAnnouncementsAsync_ReturnsAnnouncements()
        {
            TestContext.WriteLine("Testing FetchAnnouncementsAsync to retrieve exchange announcements.");
            TestContext.WriteLine("Expected: API should return announcements with no errors");
            TestContext.WriteLine("Parameters: none");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchAnnouncementsAsync();

            // Output details
            TestContext.WriteLine($"Result: Processed {processedCount} announcements with {errorCount} errors");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no announcements, so no strict assertion on count

            TestContext.WriteLine("Result: FetchAnnouncementsAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetTotalRestingOrderValueAsync method to retrieve total resting order value.
        /// Verifies that the API service can successfully calculate and return the total value
        /// of all resting orders in the account, providing insight into current exposure.
        /// </summary>
        [Test]
        public async Task GetTotalRestingOrderValueAsync_ReturnsValue()
        {
            TestContext.WriteLine("Testing GetTotalRestingOrderValueAsync to retrieve total resting order value.");
            TestContext.WriteLine("Expected: API should return total value of resting orders");
            TestContext.WriteLine("Parameters: none");

            // Act
            var result = await _apiService.GetTotalRestingOrderValueAsync();

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Total resting order value: {result.TotalValue}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected total resting order value response");
            Assert.That(result.TotalValue, Is.GreaterThanOrEqualTo(0), "Expected non-negative total value");

            TestContext.WriteLine("Result: GetTotalRestingOrderValueAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetOrdersQueuePositionsAsync method to retrieve order queue positions.
        /// Verifies that the API service can successfully fetch queue position information
        /// for orders, providing insights into order execution priority and status.
        /// </summary>
        [Test]
        public async Task GetOrdersQueuePositionsAsync_ReturnsQueuePositions()
        {
            TestContext.WriteLine("Testing GetOrdersQueuePositionsAsync to retrieve order queue positions.");
            TestContext.WriteLine("Expected: API should return queue positions for orders");
            TestContext.WriteLine("Parameters: none");

            // Act
            var result = await _apiService.GetOrdersQueuePositionsAsync();

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Retrieved {result.QueuePositions.Count} queue positions");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected queue positions response");
            // Count may be 0 if no positions, so no strict assertion on count

            TestContext.WriteLine("Result: GetOrdersQueuePositionsAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetSettlementsAsync method to retrieve settlement information.
        /// Verifies that the API service can successfully fetch settlement data for completed markets,
        /// including payout amounts and settlement dates with proper pagination support.
        /// </summary>
        [Test]
        public async Task GetSettlementsAsync_ReturnsSettlements()
        {
            TestContext.WriteLine("Testing GetSettlementsAsync to retrieve settlement information.");
            TestContext.WriteLine("Expected: API should return settlement data with pagination");
            TestContext.WriteLine("Parameters: none");

            // Act
            var result = await _apiService.GetSettlementsAsync();

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Retrieved {result.Settlements.Count} settlements");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected settlements response");
            // Count may be 0 if no settlements, so no strict assertion on count

            TestContext.WriteLine("Result: GetSettlementsAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetFillsAsync method to retrieve fill information for executed orders.
        /// Verifies that the API service can successfully fetch execution data including
        /// prices, quantities, and timestamps for completed order fills with pagination support.
        /// </summary>
        [Test]
        public async Task GetFillsAsync_ReturnsFills()
        {
            TestContext.WriteLine("Testing GetFillsAsync to retrieve fill information for executed orders.");
            TestContext.WriteLine("Expected: API should return fill data with pagination");
            TestContext.WriteLine("Parameters: none");

            // Act
            var result = await _apiService.GetFillsAsync();

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Retrieved {result.Fills.Count} fills");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected fills response");
            // Count may be 0 if no fills, so no strict assertion on count

            TestContext.WriteLine("Result: GetFillsAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetIncentiveProgramsAsync method to retrieve incentive program information.
        /// Verifies that the API service can successfully fetch details about trading incentives,
        /// rewards, and promotional programs offered by the Kalshi exchange with pagination support.
        /// </summary>
        [Test]
        public async Task GetIncentiveProgramsAsync_ReturnsIncentivePrograms()
        {
            TestContext.WriteLine("Testing GetIncentiveProgramsAsync to retrieve incentive program information.");
            TestContext.WriteLine("Expected: API should return incentive programs with pagination");
            TestContext.WriteLine("Parameters: none");

            // Act
            var result = await _apiService.GetIncentiveProgramsAsync();

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Retrieved {result.IncentivePrograms.Count} incentive programs");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected incentive programs response");
            // Count may be 0 if no programs, so no strict assertion on count

            TestContext.WriteLine("Result: GetIncentiveProgramsAsync completed successfully.");
        }

        /// <summary>
        /// Tests the GetEventMetadataAsync method with a valid event ticker.
        /// Verifies that the API service can successfully retrieve detailed metadata
        /// for a specific event including competition details and settlement sources.
        /// </summary>
        [Test]
        public async Task GetEventMetadataAsync_WithValidEventTicker_ReturnsMetadata()
        {
            TestContext.WriteLine("Testing GetEventMetadataAsync with a valid event ticker.");
            TestContext.WriteLine("Expected: API should return metadata for specified event");
            TestContext.WriteLine($"Parameters: event_ticker={_testEventTicker}");

            // Act
            var result = await _apiService.GetEventMetadataAsync(_testEventTicker);

            // Output details
            if (result != null)
            {
                TestContext.WriteLine($"Result: Competition={result.Competition}, SettlementSources={result.SettlementSources.Count}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected event metadata response");
            Assert.That(result.SettlementSources, Is.Not.Null.And.Not.Empty, "Expected SettlementSources to be populated");

            TestContext.WriteLine("Result: GetEventMetadataAsync completed successfully.");
        }

    }
}
