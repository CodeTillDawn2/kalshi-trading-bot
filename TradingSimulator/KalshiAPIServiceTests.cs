using KalshiBotAPI.Configuration;
using KalshiBotAPI.KalshiAPI;
using KalshiBotData.Data;
using KalshiBotData.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State.Interfaces;
using SmokehouseDTOs.Data;
using SmokehouseDTOs.Helpers;
using SmokehouseDTOs.KalshiAPI;
using SmokehouseInterfaces.Constants;

namespace TradingSimulator.Tests
{
    [TestFixture]
    public class KalshiAPIServiceTests
    {
        private Mock<ILogger<IKalshiAPIService>> _loggerMock;
        private IConfiguration _configuration;
        private Mock<IServiceScopeFactory> _scopeFactoryMock;
        private Mock<IStatusTrackerService> _statusTrackerMock;
        private IOptions<KalshiConfig> _kalshiConfigOptions;
        private KalshiAPIService _apiService;
        private Mock<IKalshiBotContext> _contextMock;
        private KalshiBotContext _realContext; // Real context for querying test data

        private string _testMarketTicker;
        private string _testSeriesTicker;
        private string _testEventTicker;
        private const string TestInterval = "minute";
        private long _testStartTs;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Load configuration from appsettings.local.json (real credentials)
            var basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SmokehouseBot"));
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.local.json", optional: false, reloadOnChange: false)
                .Build();

            // Initialize real context to query for dynamic test data
            _realContext = new KalshiBotContext(_configuration);

            // Query for an active market
            var activeMarket = await _realContext.GetMarkets_cached(
                includedStatuses: new HashSet<string> { KalshiConstants.Status_Active }
            );

            Assert.That(activeMarket, Is.Not.Empty, "No active markets found in the database");

            // Pick the first active market
            var marketDto = activeMarket.Where(x => x.yes_bid > 20 && x.no_bid > 20).OrderByDescending(x => x.APILastFetchedDate).FirstOrDefault();
            _testMarketTicker = marketDto.market_ticker;
            _testEventTicker = marketDto.event_ticker;

            // Get series ticker from event
            var eventDto = await _realContext.GetEventByTicker_cached(_testEventTicker);
            Assert.That(eventDto, Is.Not.Null, "Event not found");
            _testSeriesTicker = eventDto.series_ticker;

            _testStartTs = UnixHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddDays(-1)); // 24 hours ago
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _realContext.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            var kalshiConfig = new KalshiConfig();
            _configuration.GetSection("Kalshi").Bind(kalshiConfig);

            // Validate configuration
            Assert.That(kalshiConfig.KeyId, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyId is missing in appsettings.local.json");
            Assert.That(kalshiConfig.KeyFile, Is.Not.Null.And.Not.Empty, "KalshiConfig.KeyFile is missing in appsettings.local.json");
            Assert.That(File.Exists(kalshiConfig.KeyFile), Is.True, $"KeyFile {kalshiConfig.KeyFile} does not exist");
            Assert.That(kalshiConfig.Environment, Is.Not.Null.And.Not.Empty, "KalshiConfig.Environment is missing in appsettings.local.json");

            _kalshiConfigOptions = Options.Create(kalshiConfig);

            _loggerMock = new Mock<ILogger<IKalshiAPIService>>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _statusTrackerMock = new Mock<IStatusTrackerService>();
            _statusTrackerMock.Setup(s => s.GetCancellationToken()).Returns(CancellationToken.None);

            // Mock the DB context to avoid actual database interactions (focus on API calls)
            _contextMock = new Mock<IKalshiBotContext>();

            // Setup common DB methods used across API functions
            _contextMock.Setup(c => c.AddOrUpdateMarkets(It.IsAny<List<MarketDTO>>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.AddOrUpdateMarket(It.IsAny<MarketDTO>())).Returns(Task.CompletedTask);
            _contextMock.Setup(c => c.GetMarketByTicker_cached(It.IsAny<string>())).ReturnsAsync(new MarketDTO { status = KalshiConstants.Status_Active });
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
            serviceProviderMock.Setup(sp => sp.GetService(typeof(IKalshiBotContext))).Returns(_contextMock.Object);
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            // Instantiate the real API service with mocks and real config
            _apiService = new KalshiAPIService(
                _loggerMock.Object,
                _configuration,
                _scopeFactoryMock.Object,
                _statusTrackerMock.Object,
                _kalshiConfigOptions
            );
        }

        [Test]
        public async Task FetchMarketsAsync_WithStatusOpen_ReturnsProcessedMarkets()
        {
            // Act
            var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(status: KalshiConstants.Status_Open);

            // Output details
            TestContext.Out.WriteLine($"Processed {processedCount} markets with {errorCount} errors.");

            // Assert
            Assert.That(processedCount, Is.GreaterThan(0), "Expected some open markets to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
        }

        [Test]
        public async Task FetchMarketsAsync_WithSpecificTicker_ReturnsProcessedMarket()
        {
            // Act
            var (processedCount, errorCount) = await _apiService.FetchMarketsAsync(tickers: new[] { _testMarketTicker });

            // Output details
            TestContext.Out.WriteLine($"Processed {processedCount} markets for ticker {_testMarketTicker} with {errorCount} errors.");

            // Assert
            Assert.That(processedCount, Is.EqualTo(1), "Expected exactly one market to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
        }

        [Test]
        public async Task FetchSeriesAsync_WithValidTicker_ReturnsSeriesData()
        {
            // Act
            var result = await _apiService.FetchSeriesAsync(_testSeriesTicker);

            // Output details
            if (result != null)
            {
                TestContext.Out.WriteLine($"Fetched series: Ticker={result.Series.Ticker}, Title={result.Series.Title}, Category={result.Series.Category}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected series data to be returned");
            Assert.That(result.Series.Ticker, Is.EqualTo(_testSeriesTicker), "Ticker mismatch in response");
        }

        [Test]
        public async Task FetchEventAsync_WithValidTicker_ReturnsEventData()
        {
            // Act
            var result = await _apiService.FetchEventAsync(_testEventTicker, withNestedMarkets: true);

            // Output details
            if (result != null)
            {
                TestContext.Out.WriteLine($"Fetched event: Ticker={result.Event.EventTicker}, Title={result.Event.Title}, MarketCount={result.Event.Markets?.Count ?? 0}");
            }

            // Assert
            Assert.That(result, Is.Not.Null, "Expected event data to be returned");
            Assert.That(result.Event.EventTicker, Is.EqualTo(_testEventTicker), "Event ticker mismatch in response");
        }

        [Test]
        public async Task FetchPositionsAsync_ReturnsPositions()
        {
            // Act
            var (processedCount, errorCount) = await _apiService.FetchPositionsAsync();

            // Output details
            TestContext.Out.WriteLine($"Processed {processedCount} positions with {errorCount} errors.");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no positions, so no strict assertion on count
        }

        [Test]
        public async Task FetchCandlesticksAsync_WithValidParams_ReturnsCandlesticks()
        {
            // Act
            var (processedCount, errorCount) = await _apiService.FetchCandlesticksAsync(
                _testSeriesTicker, _testMarketTicker, TestInterval, _testStartTs);

            // Output details
            TestContext.Out.WriteLine($"Processed {processedCount} candlesticks for {_testMarketTicker} with {errorCount} errors.");

            // Assert
            Assert.That(processedCount, Is.GreaterThan(0), "Expected some candlesticks to be processed");
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
        }

        [Test]
        public async Task GetBalanceAsync_ReturnsPositiveBalance()
        {
            // Act
            var balance = await _apiService.GetBalanceAsync();

            // Output details
            TestContext.Out.WriteLine($"Account balance: {balance / 100m} USD");

            // Assert
            Assert.That(balance, Is.GreaterThanOrEqualTo(0), "Expected non-negative balance");
        }

        [Test]
        public async Task GetExchangeStatusAsync_ReturnsStatus()
        {
            // Act
            var status = await _apiService.GetExchangeStatusAsync();

            // Output details
            if (status != null)
            {
                TestContext.Out.WriteLine($"Exchange status: TradingActive={status.trading_active}, ExchangeActive={status.exchange_active}");
            }

            // Assert
            Assert.That(status, Is.Not.Null, "Expected exchange status to be returned");
        }

        [Test]
        public async Task FetchOrdersAsync_ReturnsOrders()
        {
            // Act
            var (processedCount, errorCount) = await _apiService.FetchOrdersAsync(status: "executed");

            // Output details
            TestContext.Out.WriteLine($"Processed {processedCount} orders with {errorCount} errors.");

            // Assert
            Assert.That(errorCount, Is.EqualTo(0), "Expected no errors during fetch");
            // processedCount may be 0 if no orders, so no strict assertion on count
        }

        [Test]
        public async Task CreateOrderAsync_PlacesLimitBuyOrder_ThenCancels_ReturnsOrderIdAndCancelConfirmation()
        {
            // Arrange
            var orderRequest = new CreateOrderRequest
            {
                Ticker = _testMarketTicker,
                Action = "buy",
                Type = "limit",
                Side = "yes",
                Count = 1,
                YesPrice = 1,
                ClientOrderId = Guid.NewGuid().ToString()
            };

            // Act - Create
            var createResult = await _apiService.CreateOrderAsync(_testMarketTicker, orderRequest);

            // Output create details
            string orderId = "";
            if (createResult != null)
            {
                orderId = createResult.Order.OrderId;
                TestContext.Out.WriteLine($"Created order: ID={orderId}");
            }

            // Assert create
            Assert.That(createResult, Is.Not.Null, "Expected order creation response");
            Assert.That(orderId, Is.Not.Empty, "Expected valid order ID");

            // Act - Cancel
            var cancelResult = await _apiService.CancelOrderAsync(orderId);

            // Output cancel details
            if (cancelResult != null)
            {
                TestContext.Out.WriteLine($"Canceled order: ID={cancelResult.Order.OrderId}, Status={cancelResult.Order.Status}, ReducedBy={cancelResult.ReducedBy}");
            }

            // Assert cancel
            Assert.That(cancelResult, Is.Not.Null, "Expected cancel response");
            Assert.That(cancelResult.Order.Status, Is.EqualTo("canceled"), "Expected order to be canceled");
        }

        [Test]
        public async Task GetExchangeScheduleAsync_ReturnsSchedule()
        {
            // Act
            var schedule = await _apiService.GetExchangeScheduleAsync();

            // Output details
            if (schedule != null && schedule.Schedule.StandardHours.Any())
            {
                TestContext.Out.WriteLine($"Fetched {schedule.Schedule.StandardHours.Count} standard hours periods. First start_time={schedule.Schedule.StandardHours[0].StartTime}");
            }

            // Assert
            Assert.That(schedule, Is.Not.Null, "Expected schedule to be returned");
            Assert.That(schedule.Schedule.StandardHours, Is.Not.Empty, "Expected at least one standard hours period");
        }
    }
}