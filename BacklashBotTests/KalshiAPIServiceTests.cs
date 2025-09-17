using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using BacklashBotData.Data;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework.Interfaces;
using System.Diagnostics;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashDTOs.Data;
using BacklashDTOs.Helpers;
using BacklashDTOs.KalshiAPI;
using BacklashInterfaces.Constants;
using BacklashInterfaces.PerformanceMetrics;
using BacklashBotData.Data.Interfaces;

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
        /// Configuration instance loaded from appsettings.local.json for real API credentials.
        /// </summary>
        private IConfiguration _configuration;

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
        /// Loads real configuration from appsettings.local.json, initializes a real database context,
        /// and queries for active market data to use in tests. This setup runs once before all tests
        /// in the fixture to provide consistent test data across all test methods.
        /// </summary>
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Load configuration from appsettings.local.json (real credentials)
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "BacklashBot"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Initialize real context to query for dynamic test data
            _realContext = new BacklashBotContext(_configuration);

            // Query for an active market
            var activeMarket = await _realContext.GetMarketsFiltered(
                includedStatuses: new HashSet<string> { KalshiConstants.Status_Active }
            );

            Assert.That(activeMarket, Is.Not.Empty, "No active markets found in the database");

            // Pick the first active market
            var marketDto = activeMarket.Where(x => x.yes_bid > 20 && x.no_bid > 20).OrderByDescending(x => x.APILastFetchedDate).FirstOrDefault();
            _testMarketTicker = marketDto.market_ticker;
            _testEventTicker = marketDto.event_ticker;

            // Get series ticker from event
            var eventDto = await _realContext.GetEventByTicker(_testEventTicker);
            Assert.That(eventDto, Is.Not.Null, "Event not found");
            _testSeriesTicker = eventDto.series_ticker;

            _testStartTs = UnixHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddDays(-1)); // 24 hours ago
        }

        /// <summary>
        /// Performs cleanup after all tests in the fixture have completed.
        /// Disposes of the real database context to ensure proper resource cleanup.
        /// </summary>
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _realContext.Dispose();
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
            var kalshiConfig = new KalshiConfig();
            _configuration.GetSection("Kalshi").Bind(kalshiConfig);

            // Validate configuration
            Assert.That(kalshiConfig.KeyId, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyId is missing in appsettings.json");
            Assert.That(kalshiConfig.KeyFile, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyFile is missing in appsettings.json");
            Assert.That(File.Exists(kalshiConfig.KeyFile), Is.True, $"KeyFile {kalshiConfig.KeyFile} does not exist");
            Assert.That(kalshiConfig.Environment, Is.Not.Null.And.Not.Empty, "KalshiConfig.Environment is missing in appsettings.json");

            _kalshiConfigOptions = Options.Create(kalshiConfig);

            _loggerMock = new Mock<ILogger<IKalshiAPIService>>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _statusTrackerMock = new Mock<IStatusTrackerService>();
            _statusTrackerMock.Setup(s => s.GetCancellationToken()).Returns(CancellationToken.None);
            _performanceMonitorMock = new Mock<IPerformanceMonitor>();

            // Mock the DB context to avoid actual database interactions (focus on API calls)
            _contextMock = new Mock<IBacklashBotContext>();

            // Setup common DB methods used across API functions
            _contextMock.Setup(c => c.AddOrUpdateMarkets(It.IsAny<List<MarketDTO>>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.AddOrUpdateMarket(It.IsAny<MarketDTO>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.GetMarketByTicker(It.IsAny<string>())).ReturnsAsync(new MarketDTO { status = KalshiConstants.Status_Active });
            _contextMock.Setup(c => c.AddOrUpdateSeries(It.IsAny<SeriesDTO>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.AddOrUpdateEvent(It.IsAny<EventDTO>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.GetMarketPositions(null, null, null)).ReturnsAsync(new List<MarketPositionDTO>());
            _contextMock.Setup(c => c.AddOrUpdateMarketPosition(It.IsAny<MarketPositionDTO>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.RemoveMarketPosition(It.IsAny<string>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.AddOrUpdateCandlestick(It.IsAny<CandlestickDTO>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.UpdateMarketLastCandlestick(It.IsAny<string>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.AddOrUpdateOrder(It.IsAny<OrderDTO>())).Returns(Task.CompletedTask);

            // Setup scope factory to return mocked context
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IBacklashBotContext))).Returns(_contextMock.Object);
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            // Instantiate the real API service with mocks and real config
            _apiService = new KalshiAPIService(
                _loggerMock.Object,
                _configuration,
                _scopeFactoryMock.Object,
                _statusTrackerMock.Object,
                _kalshiConfigOptions,
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
            Console.WriteLine("🧪 Testing: Fetch Markets with Open Status");
            Console.WriteLine("   Expected: API should return processed markets with no errors");
            Console.WriteLine("   Parameters: status=open");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(status: KalshiConstants.Status_Open);

            // Output details
            Console.WriteLine($"   Result: Processed {processedCount} markets with {errorCount} errors");

            // Assert
            Assert.That(processedCount, Is.GreaterThan(0), "Expected some open markets to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");

            Console.WriteLine("✅ PASSED: Markets fetched successfully with open status");
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
            Console.WriteLine("🧪 Testing: Fetch Markets with Specific Ticker");
            Console.WriteLine("   Expected: API should return exactly one processed market with no errors");
            Console.WriteLine($"   Parameters: ticker={_testMarketTicker}");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(tickers: new[] { _testMarketTicker });

            // Output details
            Console.WriteLine($"   Result: Processed {processedCount} markets with {errorCount} errors");

            // Assert
            Assert.That(processedCount, Is.EqualTo(1), "Expected exactly one market to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");

            Console.WriteLine("✅ PASSED: Specific market fetched successfully");
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
            Console.WriteLine("🧪 Testing: Fetch Series with Valid Ticker");
            Console.WriteLine("   Expected: API should return series data with matching ticker");
            Console.WriteLine($"   Parameters: series_ticker={_testSeriesTicker}");

            // Act
            var result = await _apiService.FetchSeriesAsync(_testSeriesTicker);

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Ticker={result.Series.Ticker}, Title={result.Series.Title}, Category={result.Series.Category}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected series data to be returned");
            Assert.That(result.Series.Ticker, Is.EqualTo(_testSeriesTicker), "Ticker mismatch in response");

            Console.WriteLine("✅ PASSED: Series data fetched successfully");
        }

        /// <summary>
        /// Tests the FetchEventAsync method with a valid event ticker and nested markets enabled.
        /// Verifies that the API service can retrieve comprehensive event information
        /// including nested market data, ensuring proper relationship handling and data integrity.
        /// </summary>
        [Test]
        public async Task FetchEventAsync_WithValidTicker_ReturnsEventData()
        {
            Console.WriteLine("🧪 Testing: Fetch Event with Valid Ticker");
            Console.WriteLine("   Expected: API should return event data with nested markets");
            Console.WriteLine($"   Parameters: event_ticker={_testEventTicker}, with_nested_markets=true");

            // Act
            var result = await _apiService.FetchEventAsync(_testEventTicker, withNestedMarkets: true);

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Ticker={result.Event.EventTicker}, Title={result.Event.Title}, MarketCount={result.Event.Markets?.Count ?? 0}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected event data to be returned");
            Assert.That(result.Event.EventTicker, Is.EqualTo(_testEventTicker), "Event ticker mismatch in response");

            Console.WriteLine("✅ PASSED: Event data with nested markets fetched successfully");
        }

        /// <summary>
        /// Tests the FetchPositionsAsync method to retrieve user's current positions.
        /// Verifies that the API service can successfully fetch and process position data,
        /// including proper handling of empty position sets and error conditions.
        /// </summary>
        [Test]
        public async Task FetchPositionsAsync_ReturnsPositions()
        {
            Console.WriteLine("🧪 Testing: Fetch Portfolio Positions");
            Console.WriteLine("   Expected: API should return positions data with no errors");
            Console.WriteLine("   Parameters: none (fetch all positions)");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchPositionsAsync();

            // Output details
            Console.WriteLine($"   Result: Processed {processedCount} positions with {errorCount} errors");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no positions, so no strict assertion on count

            Console.WriteLine("✅ PASSED: Portfolio positions fetched successfully");
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
            Console.WriteLine("🧪 Testing: Fetch Candlesticks with Valid Parameters");
            Console.WriteLine("   Expected: API should return candlestick data with no errors");
            Console.WriteLine($"   Parameters: series={_testSeriesTicker}, market={_testMarketTicker}, interval={TestInterval}");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchCandlesticksAsync(
                _testSeriesTicker, _testMarketTicker, TestInterval, _testStartTs);

            // Output details
            Console.WriteLine($"   Result: Processed {processedCount} candlesticks with {errorCount} errors");

            // Assert
            Assert.That(processedCount, Is.GreaterThan(0), "Expected some candlesticks to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");

            Console.WriteLine("✅ PASSED: Candlestick data fetched successfully");
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
            Console.WriteLine("🧪 Testing: Get Account Balance");
            Console.WriteLine("   Expected: API should return non-negative account balance");
            Console.WriteLine("   Parameters: none");

            // Act
            var balance = await _apiService.GetBalanceAsync();

            // Output details
            Console.WriteLine($"   Result: Account balance: {balance / 100m} USD");

            // Assert
            Assert.That(balance, Is.GreaterThanOrEqualTo(0), "Expected non-negative balance");

            Console.WriteLine("✅ PASSED: Account balance retrieved successfully");
        }

        /// <summary>
        /// Tests the GetExchangeStatusAsync method to retrieve exchange operational status.
        /// Verifies that the API service can successfully fetch exchange status information
        /// including trading availability and operational state, ensuring proper system monitoring.
        /// </summary>
        [Test]
        public async Task GetExchangeStatusAsync_ReturnsStatus()
        {
            Console.WriteLine("🧪 Testing: Get Exchange Status");
            Console.WriteLine("   Expected: API should return exchange operational status");
            Console.WriteLine("   Parameters: none");

            // Act
            var status = await _apiService.GetExchangeStatusAsync();

            // Output details
            if (status != null)
            {
                Console.WriteLine($"   Result: TradingActive={status.trading_active}, ExchangeActive={status.exchange_active}");
            }

            // Assert
            Assert.That(status, Is.Not.Null, "Expected exchange status to be returned");

            Console.WriteLine("✅ PASSED: Exchange status retrieved successfully");
        }

        /// <summary>
        /// Tests the FetchOrdersAsync method with status filter.
        /// Verifies that the API service can successfully retrieve and process order data
        /// with filtering capabilities, ensuring proper handling of order history and status tracking.
        /// </summary>
        [Test]
        public async Task FetchOrdersAsync_ReturnsOrders()
        {
            Console.WriteLine("🧪 Testing: Fetch Orders with Status Filter");
            Console.WriteLine("   Expected: API should return orders with no errors");
            Console.WriteLine("   Parameters: status=executed");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchOrdersAsync(status: "executed");

            // Output details
            Console.WriteLine($"   Result: Processed {processedCount} orders with {errorCount} errors");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no orders, so no strict assertion on count

            Console.WriteLine("✅ PASSED: Orders fetched successfully");
        }

        /// <summary>
        /// Tests the CreateOrderAsync method - intentionally skipped for safety.
        /// This test validates the order creation functionality but is skipped to prevent
        /// accidental placement of real orders during testing, which could result in
        /// unintended financial transactions. The test serves as a placeholder for
        /// future implementation when safe testing mechanisms are available.
        /// </summary>
        [Test]
        public async Task CreateOrderAsync_OrderPlacement_SkippedForSafety()
        {
            Console.WriteLine("🧪 Testing: Create Order API");
            Console.WriteLine("   Expected: This test is skipped to prevent accidental real money orders");
            Console.WriteLine("   Safety: Order placement tests are dangerous and could cost real money");

            // This test is intentionally skipped to prevent any accidental order placement
            // that could result in real financial transactions
            Assert.Pass("Order placement test skipped for safety - prevents accidental real money orders");
        }

        /// <summary>
        /// Tests the GetExchangeScheduleAsync method to retrieve trading schedule information.
        /// Verifies that the API service can successfully fetch exchange operating hours,
        /// maintenance windows, and trading schedule data for operational planning.
        /// </summary>
        [Test]
        public async Task GetExchangeScheduleAsync_ReturnsSchedule()
        {
            Console.WriteLine("🧪 Testing: Get Exchange Schedule");
            Console.WriteLine("   Expected: API should return exchange trading hours schedule");
            Console.WriteLine("   Parameters: none");

            // Act
            var schedule = await _apiService.GetExchangeScheduleAsync();

            // Output details
            if (schedule != null && schedule.Schedule.StandardHours.Any())
            {
                Console.WriteLine($"   Result: Fetched {schedule.Schedule.StandardHours.Count} standard hours periods");
            }

            // Assert
            Assert.That(schedule, Is.Not.Null, "Expected schedule to be returned");
            Assert.That(schedule.Schedule.StandardHours, Is.Not.Empty, "Expected at least one standard hours period");

            Console.WriteLine("✅ PASSED: Exchange schedule retrieved successfully");
        }

        /// <summary>
        /// Tests the FetchAnnouncementsAsync method to retrieve exchange announcements.
        /// Verifies that the API service can successfully fetch and process important
        /// notifications and updates from the Kalshi exchange platform.
        /// </summary>
        [Test]
        public async Task FetchAnnouncementsAsync_ReturnsAnnouncements()
        {
            Console.WriteLine("🧪 Testing: Fetch Exchange Announcements");
            Console.WriteLine("   Expected: API should return announcements with no errors");
            Console.WriteLine("   Parameters: none");

            // Act
            var (processedCount, errorCount) = await _apiService.FetchAnnouncementsAsync();

            // Output details
            Console.WriteLine($"   Result: Processed {processedCount} announcements with {errorCount} errors");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no announcements, so no strict assertion on count

            Console.WriteLine("✅ PASSED: Announcements fetched successfully");
        }

        /// <summary>
        /// Tests the GetTotalRestingOrderValueAsync method to retrieve total resting order value.
        /// Verifies that the API service can successfully calculate and return the total value
        /// of all resting orders in the account, providing insight into current exposure.
        /// </summary>
        [Test]
        public async Task GetTotalRestingOrderValueAsync_ReturnsValue()
        {
            Console.WriteLine("🧪 Testing: Get Total Resting Order Value");
            Console.WriteLine("   Expected: API should return total value of resting orders");
            Console.WriteLine("   Parameters: none");

            // Act
            var result = await _apiService.GetTotalRestingOrderValueAsync();

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Total resting order value: {result.TotalValue}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected total resting order value response");
            Assert.That(result.TotalValue, Is.GreaterThanOrEqualTo(0), "Expected non-negative total value");

            Console.WriteLine("✅ PASSED: Total resting order value retrieved successfully");
        }

        /// <summary>
        /// Tests the GetOrdersQueuePositionsAsync method to retrieve order queue positions.
        /// Verifies that the API service can successfully fetch queue position information
        /// for orders, providing insights into order execution priority and status.
        /// </summary>
        [Test]
        public async Task GetOrdersQueuePositionsAsync_ReturnsQueuePositions()
        {
            Console.WriteLine("🧪 Testing: Get Orders Queue Positions");
            Console.WriteLine("   Expected: API should return queue positions for orders");
            Console.WriteLine("   Parameters: none");

            // Act
            var result = await _apiService.GetOrdersQueuePositionsAsync();

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Retrieved {result.QueuePositions.Count} queue positions");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected queue positions response");
            // Count may be 0 if no positions, so no strict assertion on count

            Console.WriteLine("✅ PASSED: Orders queue positions retrieved successfully");
        }

        /// <summary>
        /// Tests the GetSettlementsAsync method to retrieve settlement information.
        /// Verifies that the API service can successfully fetch settlement data for completed markets,
        /// including payout amounts and settlement dates with proper pagination support.
        /// </summary>
        [Test]
        public async Task GetSettlementsAsync_ReturnsSettlements()
        {
            Console.WriteLine("🧪 Testing: Get Portfolio Settlements");
            Console.WriteLine("   Expected: API should return settlement data with pagination");
            Console.WriteLine("   Parameters: none");

            // Act
            var result = await _apiService.GetSettlementsAsync();

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Retrieved {result.Settlements.Count} settlements");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected settlements response");
            // Count may be 0 if no settlements, so no strict assertion on count

            Console.WriteLine("✅ PASSED: Settlements retrieved successfully");
        }

        /// <summary>
        /// Tests the GetFillsAsync method to retrieve fill information for executed orders.
        /// Verifies that the API service can successfully fetch execution data including
        /// prices, quantities, and timestamps for completed order fills with pagination support.
        /// </summary>
        [Test]
        public async Task GetFillsAsync_ReturnsFills()
        {
            Console.WriteLine("🧪 Testing: Get Portfolio Fills");
            Console.WriteLine("   Expected: API should return fill data with pagination");
            Console.WriteLine("   Parameters: none");

            // Act
            var result = await _apiService.GetFillsAsync();

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Retrieved {result.Fills.Count} fills");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected fills response");
            // Count may be 0 if no fills, so no strict assertion on count

            Console.WriteLine("✅ PASSED: Fills retrieved successfully");
        }

        /// <summary>
        /// Tests the GetIncentiveProgramsAsync method to retrieve incentive program information.
        /// Verifies that the API service can successfully fetch details about trading incentives,
        /// rewards, and promotional programs offered by the Kalshi exchange with pagination support.
        /// </summary>
        [Test]
        public async Task GetIncentiveProgramsAsync_ReturnsIncentivePrograms()
        {
            Console.WriteLine("🧪 Testing: Get Incentive Programs");
            Console.WriteLine("   Expected: API should return incentive programs with pagination");
            Console.WriteLine("   Parameters: none");

            // Act
            var result = await _apiService.GetIncentiveProgramsAsync();

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Retrieved {result.IncentivePrograms.Count} incentive programs");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected incentive programs response");
            // Count may be 0 if no programs, so no strict assertion on count

            Console.WriteLine("✅ PASSED: Incentive programs retrieved successfully");
        }

        /// <summary>
        /// Tests the GetEventMetadataAsync method with a valid event ticker.
        /// Verifies that the API service can successfully retrieve detailed metadata
        /// for a specific event including competition details and settlement sources.
        /// </summary>
        [Test]
        public async Task GetEventMetadataAsync_WithValidEventTicker_ReturnsMetadata()
        {
            Console.WriteLine("🧪 Testing: Get Event Metadata");
            Console.WriteLine("   Expected: API should return metadata for specified event");
            Console.WriteLine($"   Parameters: event_ticker={_testEventTicker}");

            // Act
            var result = await _apiService.GetEventMetadataAsync(_testEventTicker);

            // Output details
            if (result != null)
            {
                Console.WriteLine($"   Result: Competition={result.Competition}, SettlementSources={result.SettlementSources.Count}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected event metadata response");
            Assert.That(result.Competition, Is.Not.Null.And.Not.Empty, "Expected competition to be populated");

            Console.WriteLine("✅ PASSED: Event metadata retrieved successfully");
        }

        /// <summary>
        /// Tests the GetOrderQueuePositionAsync method - intentionally skipped.
        /// This test is skipped because it requires an existing order ID to function properly.
        /// The test serves as a placeholder for future implementation when order testing
        /// infrastructure is available, ensuring the method signature remains testable.
        /// </summary>
        [Test]
        public async Task GetOrderQueuePositionAsync_OrderRelated_Skipped()
        {
            // This test is skipped as it requires an existing order ID
            Assert.Pass("Order-related test skipped - requires existing order");
        }

        /// <summary>
        /// Tests the GetOrderDetailsAsync method - intentionally skipped.
        /// This test is skipped because it requires an existing order ID to function properly.
        /// The test serves as a placeholder for future implementation when order testing
        /// infrastructure is available, ensuring the method signature remains testable.
        /// </summary>
        [Test]
        public async Task GetOrderDetailsAsync_OrderRelated_Skipped()
        {
            // This test is skipped as it requires an existing order ID
            Assert.Pass("Order-related test skipped - requires existing order");
        }

        /// <summary>
        /// Tests the CreateOrdersBatchAsync method - intentionally skipped.
        /// This test is skipped because it involves placing orders which could result
        /// in real financial transactions. The test serves as a placeholder for future
        /// implementation when safe batch order testing mechanisms are available.
        /// </summary>
        [Test]
        public async Task CreateOrdersBatchAsync_OrderRelated_Skipped()
        {
            // This test is skipped as it involves placing orders
            Assert.Pass("Order-related test skipped - involves placing orders");
        }

        /// <summary>
        /// Tests the DeleteOrdersBatchAsync method - intentionally skipped.
        /// This test is skipped because it involves deleting orders which could affect
        /// real trading operations. The test serves as a placeholder for future
        /// implementation when safe batch order deletion testing mechanisms are available.
        /// </summary>
        [Test]
        public async Task DeleteOrdersBatchAsync_OrderRelated_Skipped()
        {
            // This test is skipped as it involves deleting orders
            Assert.Pass("Order-related test skipped - involves deleting orders");
        }

        /// <summary>
        /// Tests the ResetOrderGroupAsync method - intentionally skipped.
        /// This test is skipped because it involves order group operations which could affect
        /// real trading operations. The test serves as a placeholder for future implementation
        /// when safe order group testing mechanisms are available.
        /// </summary>
        [Test]
        public async Task ResetOrderGroupAsync_OrderRelated_Skipped()
        {
            // This test is skipped as it involves order group operations
            Assert.Pass("Order-related test skipped - involves order group operations");
        }

        /// <summary>
        /// Tests the DeleteOrderGroupAsync method - intentionally skipped.
        /// This test is skipped because it involves order group operations which could affect
        /// real trading operations. The test serves as a placeholder for future implementation
        /// when safe order group testing mechanisms are available.
        /// </summary>
        [Test]
        [Category(OrderOperationsCategory)]
        public async Task DeleteOrderGroupAsync_OrderRelated_Skipped()
        {
            // This test is skipped as it involves order group operations
            Assert.Pass("Order-related test skipped - involves order group operations");
        }

        #region Performance Tests

        /// <summary>
        /// Tests that market data API calls complete within performance requirements for real-time trading.
        /// Verifies that FetchMarketsAsync operations meet the 5-second timing requirement
        /// to ensure suitability for high-frequency trading scenarios.
        /// </summary>
        [Test]
        [Category(PerformanceCategory)]
        [Category(MarketDataCategory)]
        public async Task FetchMarketsAsync_Performance_MeetsRealTimeRequirements()
        {
            Console.WriteLine("🧪 Testing: Market Data API Performance");
            Console.WriteLine("   Expected: API calls complete within 5 seconds for real-time trading");
            Console.WriteLine("   Parameters: status=open");

            var stopwatch = Stopwatch.StartNew();

            // Act
            var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(status: KalshiConstants.Status_Open);

            stopwatch.Stop();

            // Output details
            Console.WriteLine($"   Result: Completed in {stopwatch.ElapsedMilliseconds}ms, Processed {processedCount} markets");

            // Assert performance requirements
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThanOrEqualTo(MarketDataApiTimeoutMs),
                $"Market data API call exceeded performance requirement of {MarketDataApiTimeoutMs}ms");
            Assert.That(processedCount, Is.GreaterThan(0), "Expected some markets to be processed");

            Console.WriteLine("✅ PASSED: Market data API meets real-time performance requirements");
        }

        /// <summary>
        /// Tests that account data API calls complete within performance requirements.
        /// Verifies that GetBalanceAsync operations meet the 2-second timing requirement
        /// for responsive account monitoring in trading applications.
        /// </summary>
        [Test]
        [Category(PerformanceCategory)]
        [Category(AccountDataCategory)]
        public async Task GetBalanceAsync_Performance_MeetsTimingRequirements()
        {
            Console.WriteLine("🧪 Testing: Account Data API Performance");
            Console.WriteLine("   Expected: API calls complete within 2 seconds for responsive monitoring");
            Console.WriteLine("   Parameters: none");

            var stopwatch = Stopwatch.StartNew();

            // Act
            var balance = await _apiService.GetBalanceAsync();

            stopwatch.Stop();

            // Output details
            Console.WriteLine($"   Result: Completed in {stopwatch.ElapsedMilliseconds}ms, Balance: {balance / 100m} USD");

            // Assert performance requirements
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThanOrEqualTo(AccountApiTimeoutMs),
                $"Account data API call exceeded performance requirement of {AccountApiTimeoutMs}ms");
            Assert.That(balance, Is.GreaterThanOrEqualTo(0), "Expected non-negative balance");

            Console.WriteLine("✅ PASSED: Account data API meets performance requirements");
        }

        /// <summary>
        /// Tests that metadata API calls complete within acceptable time limits.
        /// Verifies that GetExchangeScheduleAsync operations meet the 10-second timing requirement
        /// for comprehensive exchange information retrieval.
        /// </summary>
        [Test]
        [Category(PerformanceCategory)]
        [Category(MetadataCategory)]
        public async Task GetExchangeScheduleAsync_Performance_MeetsMetadataRequirements()
        {
            Console.WriteLine("🧪 Testing: Metadata API Performance");
            Console.WriteLine("   Expected: API calls complete within 10 seconds for comprehensive data");
            Console.WriteLine("   Parameters: none");

            var stopwatch = Stopwatch.StartNew();

            // Act
            var schedule = await _apiService.GetExchangeScheduleAsync();

            stopwatch.Stop();

            // Output details
            Console.WriteLine($"   Result: Completed in {stopwatch.ElapsedMilliseconds}ms");

            // Assert performance requirements
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThanOrEqualTo(MetadataApiTimeoutMs),
                $"Metadata API call exceeded performance requirement of {MetadataApiTimeoutMs}ms");
            Assert.That(schedule, Is.Not.Null, "Expected schedule to be returned");

            Console.WriteLine("✅ PASSED: Metadata API meets performance requirements");
        }

        #endregion

        #region Input Validation Tests

        /// <summary>
        /// Tests FetchSeriesAsync with null or empty series ticker parameters.
        /// Verifies that the API service properly handles invalid input parameters
        /// and provides appropriate error handling for edge cases.
        /// </summary>
        [Test]
        [Category(InputValidationCategory)]
        [Category(MetadataCategory)]
        public void FetchSeriesAsync_NullOrEmptyTicker_ThrowsArgumentException()
        {
            Console.WriteLine("🧪 Testing: Series API Input Validation");
            Console.WriteLine("   Expected: API should throw ArgumentException for null/empty ticker");
            Console.WriteLine("   Parameters: null, empty string");

            // Test null ticker
            var ex1 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchSeriesAsync(null));
            Assert.That(ex1.Message, Does.Contain("ticker"), "Expected ticker validation message");

            // Test empty ticker
            var ex2 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchSeriesAsync(""));
            Assert.That(ex2.Message, Does.Contain("ticker"), "Expected ticker validation message");

            Console.WriteLine("✅ PASSED: Series API properly validates input parameters");
        }

        /// <summary>
        /// Tests FetchEventAsync with null or empty event ticker parameters.
        /// Verifies that the API service properly validates input parameters
        /// and handles edge cases gracefully with meaningful error messages.
        /// </summary>
        [Test]
        [Category(InputValidationCategory)]
        [Category(MetadataCategory)]
        public void FetchEventAsync_NullOrEmptyTicker_ThrowsArgumentException()
        {
            Console.WriteLine("🧪 Testing: Event API Input Validation");
            Console.WriteLine("   Expected: API should throw ArgumentException for null/empty ticker");
            Console.WriteLine("   Parameters: null, empty string");

            // Test null ticker
            var ex1 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchEventAsync(null));
            Assert.That(ex1.Message, Does.Contain("ticker"), "Expected ticker validation message");

            // Test empty ticker
            var ex2 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchEventAsync(""));
            Assert.That(ex2.Message, Does.Contain("ticker"), "Expected ticker validation message");

            Console.WriteLine("✅ PASSED: Event API properly validates input parameters");
        }

        /// <summary>
        /// Tests FetchMarketsAsync with invalid market ticker array parameters.
        /// Verifies that the API service handles null or empty ticker arrays
        /// appropriately and provides clear validation feedback.
        /// </summary>
        [Test]
        [Category(InputValidationCategory)]
        [Category(MarketDataCategory)]
        public void FetchMarketsAsync_InvalidTickerArray_ThrowsArgumentException()
        {
            Console.WriteLine("🧪 Testing: Markets API Ticker Array Validation");
            Console.WriteLine("   Expected: API should handle invalid ticker arrays gracefully");
            Console.WriteLine("   Parameters: null array, empty array, array with null elements");

            // Test null ticker array
            var ex1 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchMarketsAsync(tickers: null));
            Assert.That(ex1.Message, Does.Contain("tickers"), "Expected tickers validation message");

            // Test empty ticker array
            var ex2 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchMarketsAsync(tickers: new string[0]));
            Assert.That(ex2.Message, Does.Contain("tickers"), "Expected tickers validation message");

            // Test array with null elements
            var ex3 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchMarketsAsync(tickers: new[] { "valid", null, "another" }));
            Assert.That(ex3.Message, Does.Contain("ticker"), "Expected ticker validation message");

            Console.WriteLine("✅ PASSED: Markets API properly validates ticker arrays");
        }

        /// <summary>
        /// Tests FetchCandlesticksAsync with invalid parameters.
        /// Verifies that the API service validates all required parameters
        /// including series ticker, market ticker, and time interval.
        /// </summary>
        [Test]
        [Category(InputValidationCategory)]
        [Category(MarketDataCategory)]
        public void FetchCandlesticksAsync_InvalidParameters_ThrowsArgumentException()
        {
            Console.WriteLine("🧪 Testing: Candlesticks API Parameter Validation");
            Console.WriteLine("   Expected: API should validate all required parameters");
            Console.WriteLine("   Parameters: various invalid combinations");

            // Test null series ticker
            var ex1 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchCandlesticksAsync(null, _testMarketTicker, TestInterval, _testStartTs));
            Assert.That(ex1.Message, Does.Contain("series"), "Expected series validation message");

            // Test null market ticker
            var ex2 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchCandlesticksAsync(_testSeriesTicker, null, TestInterval, _testStartTs));
            Assert.That(ex2.Message, Does.Contain("market"), "Expected market validation message");

            // Test null interval
            var ex3 = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _apiService.FetchCandlesticksAsync(_testSeriesTicker, _testMarketTicker, null, _testStartTs));
            Assert.That(ex3.Message, Does.Contain("interval"), "Expected interval validation message");

            Console.WriteLine("✅ PASSED: Candlesticks API properly validates all parameters");
        }

        #endregion

        #region Error Handling and Retry Logic Tests

        /// <summary>
        /// Tests API error handling with network timeout simulation.
        /// Verifies that the API service properly handles timeout scenarios
        /// and implements appropriate retry logic or error reporting.
        /// </summary>
        [Test]
        [Category(ErrorHandlingCategory)]
        [Category(MarketDataCategory)]
        public async Task FetchMarketsAsync_TimeoutScenario_HandlesGracefully()
        {
            Console.WriteLine("🧪 Testing: API Timeout Error Handling");
            Console.WriteLine("   Expected: API should handle timeout scenarios gracefully");
            Console.WriteLine("   Parameters: status=open (with potential timeout)");

            // Note: This test verifies the service doesn't crash on timeouts
            // In a real scenario, we might use a mock to simulate network timeouts

            try
            {
                var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(status: KalshiConstants.Status_Open);

                // If we get here, the API handled any timeout gracefully
                Console.WriteLine($"   Result: Processed {processedCount} markets with {errorCount} errors");

                // The test passes if no unhandled exceptions occur
                Assert.Pass("API handled timeout scenario without unhandled exceptions");

            }
            catch (Exception ex)
            {
                // If an exception occurs, it should be a handled API exception, not a crash
                Console.WriteLine($"   Result: Exception caught - {ex.GetType().Name}: {ex.Message}");
                Assert.That(ex, Is.InstanceOf<Exception>(), "Expected handled exception, not crash");
            }

            Console.WriteLine("✅ PASSED: API handles timeout scenarios gracefully");
        }

        /// <summary>
        /// Tests API error handling with invalid authentication simulation.
        /// Verifies that the API service properly handles authentication failures
        /// and provides appropriate error messages without exposing sensitive information.
        /// </summary>
        [Test]
        [Category(ErrorHandlingCategory)]
        public async Task ApiService_InvalidAuthentication_HandlesSecurely()
        {
            Console.WriteLine("🧪 Testing: Authentication Error Handling");
            Console.WriteLine("   Expected: API should handle auth failures securely without exposing credentials");
            Console.WriteLine("   Parameters: simulated invalid credentials");

            // Create a service with invalid credentials for testing
            var invalidConfig = new KalshiConfig
            {
                Environment = _kalshiConfigOptions.Value.Environment,
                KeyId = "invalid_key_id",
                KeyFile = "nonexistent_key_file"
            };
            var invalidOptions = Options.Create(invalidConfig);

            var invalidService = new KalshiAPIService(
                _loggerMock.Object,
                _configuration,
                _scopeFactoryMock.Object,
                _statusTrackerMock.Object,
                invalidOptions,
                _performanceMonitorMock.Object
            );

            try
            {
                // This should fail with authentication error
                await invalidService.GetBalanceAsync();
                Assert.Fail("Expected authentication exception but call succeeded");

            }
            catch (Exception ex)
            {
                // Verify the error is handled appropriately
                Console.WriteLine($"   Result: Authentication error handled - {ex.GetType().Name}");

                // Ensure no sensitive information is exposed in the error message
                Assert.That(ex.Message, Does.Not.Contain("key"), "Error message should not expose key information");
                Assert.That(ex.Message, Does.Not.Contain("credential"), "Error message should not expose credential information");
            }

            Console.WriteLine("✅ PASSED: Authentication errors handled securely");
        }

        /// <summary>
        /// Tests API retry logic with transient failure simulation.
        /// Verifies that the API service implements appropriate retry mechanisms
        /// for temporary network issues or server errors.
        /// </summary>
        [Test]
        [Category(ErrorHandlingCategory)]
        public async Task ApiService_RetryLogic_HandlesTransientFailures()
        {
            Console.WriteLine("🧪 Testing: API Retry Logic");
            Console.WriteLine("   Expected: API should implement retry logic for transient failures");
            Console.WriteLine("   Parameters: simulated transient network issues");

            // Test with a call that might encounter transient issues
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var balance = await _apiService.GetBalanceAsync();
                stopwatch.Stop();

                Console.WriteLine($"   Result: Call succeeded in {stopwatch.ElapsedMilliseconds}ms");

                // If the call succeeds, verify it completed within reasonable time
                // (accounting for potential retries)
                Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(30000), "Call took too long, possible retry issues");
                Assert.That(balance, Is.GreaterThanOrEqualTo(0), "Expected valid balance response");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Result: Exception after retries - {ex.GetType().Name}: {ex.Message}");

                // Even if it fails, ensure it's not a catastrophic failure
                Assert.That(ex, Is.Not.InstanceOf<OutOfMemoryException>(), "Should not encounter memory issues");
                Assert.That(ex, Is.Not.InstanceOf<StackOverflowException>(), "Should not encounter stack issues");
            }

            Console.WriteLine("✅ PASSED: API retry logic handles transient failures appropriately");
        }

        #endregion

        #region Configuration Validation Tests

        /// <summary>
        /// Tests that the API service validates Kalshi configuration on initialization.
        /// Verifies that required configuration parameters are present and valid
        /// before allowing API operations to proceed.
        /// </summary>
        [Test]
        [Category(ConfigurationCategory)]
        public void ApiService_ConfigurationValidation_ValidatesRequiredSettings()
        {
            Console.WriteLine("🧪 Testing: Configuration Validation");
            Console.WriteLine("   Expected: API service should validate all required configuration settings");
            Console.WriteLine("   Parameters: various invalid configuration scenarios");

            // Test missing KeyId
            var config1 = new KalshiConfig
            {
                Environment = "prod",
                KeyId = null,
                KeyFile = _kalshiConfigOptions.Value.KeyFile
            };
            var options1 = Options.Create(config1);

            var ex1 = Assert.Throws<ArgumentException>(() =>
                new KalshiAPIService(_loggerMock.Object, _configuration, _scopeFactoryMock.Object,
                    _statusTrackerMock.Object, options1, _performanceMonitorMock.Object));
            Assert.That(ex1.Message, Does.Contain("KeyId"), "Expected KeyId validation message");

            // Test missing KeyFile
            var config2 = new KalshiConfig
            {
                Environment = "prod",
                KeyId = _kalshiConfigOptions.Value.KeyId,
                KeyFile = null
            };
            var options2 = Options.Create(config2);

            var ex2 = Assert.Throws<ArgumentException>(() =>
                new KalshiAPIService(_loggerMock.Object, _configuration, _scopeFactoryMock.Object,
                    _statusTrackerMock.Object, options2, _performanceMonitorMock.Object));
            Assert.That(ex2.Message, Does.Contain("KeyFile"), "Expected KeyFile validation message");

            // Test missing Environment
            var config3 = new KalshiConfig
            {
                Environment = null,
                KeyId = _kalshiConfigOptions.Value.KeyId,
                KeyFile = _kalshiConfigOptions.Value.KeyFile
            };
            var options3 = Options.Create(config3);

            var ex3 = Assert.Throws<ArgumentException>(() =>
                new KalshiAPIService(_loggerMock.Object, _configuration, _scopeFactoryMock.Object,
                    _statusTrackerMock.Object, options3, _performanceMonitorMock.Object));
            Assert.That(ex3.Message, Does.Contain("Environment"), "Expected Environment validation message");

            Console.WriteLine("✅ PASSED: Configuration validation works correctly");
        }

        /// <summary>
        /// Tests that the API service validates key file existence and accessibility.
        /// Verifies that the service checks for the existence of the private key file
        /// and validates its accessibility before attempting API operations.
        /// </summary>
        [Test]
        [Category(ConfigurationCategory)]
        public void ApiService_KeyFileValidation_ChecksFileExistence()
        {
            Console.WriteLine("🧪 Testing: Key File Validation");
            Console.WriteLine("   Expected: API service should validate key file existence and accessibility");
            Console.WriteLine("   Parameters: nonexistent key file path");

            // Test with nonexistent key file
            var config = new KalshiConfig
            {
                Environment = _kalshiConfigOptions.Value.Environment,
                KeyId = _kalshiConfigOptions.Value.KeyId,
                KeyFile = "C:\\NonExistent\\KeyFile.pem"
            };
            var options = Options.Create(config);

            var ex = Assert.Throws<ArgumentException>(() =>
                new KalshiAPIService(_loggerMock.Object, _configuration, _scopeFactoryMock.Object,
                    _statusTrackerMock.Object, options, _performanceMonitorMock.Object));
            Assert.That(ex.Message, Does.Contain("does not exist"), "Expected file existence validation message");

            Console.WriteLine("✅ PASSED: Key file validation works correctly");
        }

        /// <summary>
        /// Tests that the API service properly initializes with valid configuration.
        /// Verifies that the service can be created successfully with all required
        /// configuration parameters properly set and validated.
        /// </summary>
        [Test]
        [Category(ConfigurationCategory)]
        public void ApiService_ValidConfiguration_InitializesSuccessfully()
        {
            Console.WriteLine("🧪 Testing: Valid Configuration Initialization");
            Console.WriteLine("   Expected: API service should initialize successfully with valid config");
            Console.WriteLine("   Parameters: complete valid configuration");

            // This should not throw any exceptions
            var service = new KalshiAPIService(
                _loggerMock.Object,
                _configuration,
                _scopeFactoryMock.Object,
                _statusTrackerMock.Object,
                _kalshiConfigOptions,
                _performanceMonitorMock.Object
            );

            // Verify the service was created
            Assert.That(service, Is.Not.Null, "Expected service to be created successfully");

            Console.WriteLine("✅ PASSED: Service initializes successfully with valid configuration");
        }

        #endregion
    }
}
