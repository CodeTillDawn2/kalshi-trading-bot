
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using BacklashOverseer.Config;
using BacklashOverseer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace BacklashOverseer.Controllers
{
    /// <summary>
    /// ASP.NET Core Web API controller that provides endpoints for retrieving market watch data,
    /// brain instance locks, trading positions, orders, account information, and snapshot data.
    /// This controller serves as the primary interface for the Kalshi trading bot's monitoring and
    /// data retrieval operations, utilizing caching for performance optimization and integrating
    /// with database and external API services. Includes configurable performance metrics tracking
    /// for cache operations and system monitoring.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MarketWatchController : ControllerBase
    {
        private readonly IBacklashBotContext _context;
        private readonly IMemoryCache _cache;
        private readonly IKalshiAPIService _apiService;
        private readonly SnapshotAggregationService _snapshotService;
        private readonly ILogger<MarketWatchController> _logger;
        private readonly IConfiguration _configuration;
        private readonly PerformanceMetricsService _performanceMetricsService;
        private readonly MarketWatchControllerConfig _config;
        private const string MarketsCacheKey = "ActiveMarkets";
        private const string BrainInstancesCacheKey = "BrainInstances";
        private const string AllBrainInstancesCacheKey = "AllBrainInstances";
        private const string LogDataCacheKey = "LogData";
        private readonly TimeSpan MarketsCacheDuration;
        private readonly TimeSpan LogDataCacheDuration; // Shorter cache for log data

        // Performance metrics configuration
        private readonly bool _enableMarketWatchControllerPerformanceMetrics;

        // Local cache metrics for batch posting
        private long _localCacheHits;
        private long _localCacheMisses;

        private class LogData
        {
            public Dictionary<string, DateTime?> SnapshotData { get; set; }
            public Dictionary<string, DateTime?> ErrorData { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the MarketWatchController with required dependencies.
        /// </summary>
        /// <param name="context">Database context for accessing trading data.</param>
        /// <param name="cache">Memory cache for performance optimization.</param>
        /// <param name="apiService">Service for interacting with Kalshi API.</param>
        /// <param name="snapshotService">Service for managing market snapshots.</param>
        /// <param name="logger">Logger for recording operational information and errors.</param>
        /// <param name="configuration">Configuration for cache durations and performance metrics settings.</param>
        /// <param name="performanceMetricsService">Service for tracking performance metrics and cache statistics.</param>
        /// <param name="config">Configuration options for MarketWatchController behavior, including cache durations and performance metrics settings.</param>
        public MarketWatchController(IBacklashBotContext context, IMemoryCache cache, IKalshiAPIService apiService, SnapshotAggregationService snapshotService, ILogger<MarketWatchController> logger, IConfiguration configuration, PerformanceMetricsService performanceMetricsService, IOptions<MarketWatchControllerConfig> config)
        {
            _context = context;
            _cache = cache;
            _apiService = apiService;
            _snapshotService = snapshotService;
            _logger = logger;
            _configuration = configuration;
            _performanceMetricsService = performanceMetricsService;
            _config = config.Value;

            MarketsCacheDuration = TimeSpan.FromMinutes(_config.MarketsCacheDurationMinutes);
            LogDataCacheDuration = TimeSpan.FromMinutes(_config.LogDataCacheDurationMinutes);

            _enableMarketWatchControllerPerformanceMetrics = _config.EnablePerformanceMetrics;
        }

        /// <summary>
        /// Serves the main market watch HTML page for the web interface.
        /// </summary>
        /// <returns>The marketwatch.html file as a physical file response.</returns>
        [HttpGet]
        public IActionResult Index()
        {
            return PhysicalFile(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "marketwatch.html"), "text/html");
        }

        /// <summary>
        /// Retrieves comprehensive market watch data including active markets, brain instance assignments,
        /// interest scores, and market details for monitoring and analysis.
        /// </summary>
        /// <returns>A list of market watch data objects containing market information, brain assignments, and metrics.</returns>
        /// <response code="200">Returns the market watch data successfully.</response>
        /// <response code="500">Internal server error occurred while retrieving data.</response>
        [HttpGet("data")]
        public async Task<IActionResult> GetMarketWatchData()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Get cached active markets or fetch fresh data
                List<MarketDTO> markets;
                if (_cache.TryGetValue(MarketsCacheKey, out var cachedMarkets))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    markets = (List<MarketDTO>)cachedMarkets;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    markets = (await _context.GetMarketsFiltered(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active })).ToList();
                    _cache.Set(MarketsCacheKey, markets, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Get cached brain instances for lookup
                Dictionary<Guid, string> brainInstancesByLock;
                if (_cache.TryGetValue(BrainInstancesCacheKey, out var cachedBrainInstances))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    brainInstancesByLock = (Dictionary<Guid, string>)cachedBrainInstances;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    brainInstancesByLock = (await _context.GetBrainInstancesFiltered(hasBrainLock: true))
                        .Where(bi => bi.BrainLock.HasValue)
                        .ToDictionary(bi => bi.BrainLock!.Value, bi => bi.BrainInstanceName!);
                    _cache.Set(BrainInstancesCacheKey, brainInstancesByLock, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Get all brain instances for comprehensive lookup
                List<BrainInstanceDTO> allBrainInstances;
                if (_cache.TryGetValue(AllBrainInstancesCacheKey, out var cachedAllBrainInstances))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    allBrainInstances = (List<BrainInstanceDTO>)cachedAllBrainInstances;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    allBrainInstances = (await _context.GetBrainInstancesFiltered()).ToList();
                    _cache.Set(AllBrainInstancesCacheKey, allBrainInstances, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Get market watches for active markets only
                var activeMarketTickers = markets.Where(m => m.market_ticker != null).Select(m => m.market_ticker!).ToHashSet();
                var marketWatches = (await _context.GetMarketWatches(marketTickers: activeMarketTickers)).ToList();

                // Create a lookup for faster market access
                var marketLookup = markets.Where(m => m.market_ticker != null).ToDictionary(m => m.market_ticker!, m => m);

                var marketWatchData = marketWatches.Select(mw =>
                {
                    marketLookup.TryGetValue(mw.market_ticker, out var market);
                    brainInstancesByLock.TryGetValue(mw.BrainLock ?? Guid.Empty, out var brainInstanceName);

                    return new
                    {
                        mw.market_ticker,
                        BrainLock = mw.BrainLock,
                        BrainInstanceName = brainInstanceName ?? (mw.BrainLock.HasValue ? "Unknown" : null),
                        mw.InterestScore,
                        mw.InterestScoreDate,
                        mw.LastWatched,
                        mw.AverageWebsocketEventsPerMinute,
                        Market = market == null ? null : new
                        {
                            market.title,
                            market.subtitle,
                            market.yes_sub_title,
                            market.market_type,
                            market.status,
                            market.category,
                            market.open_time,
                            market.close_time,
                            market.yes_bid,
                            market.yes_ask,
                            market.no_bid,
                            market.no_ask,
                            market.last_price,
                            market.volume,
                            market.liquidity,
                            market.open_interest
                        }
                    };
                }).ToList();

                PostCacheMetrics();
                stopwatch.Stop();
                _logger.LogInformation("GetMarketWatchData completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(marketWatchData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve market watch data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "Failed to retrieve market watch data", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves comprehensive brain lock data including brain instance assignments, market counts,
        /// interest scores, and activity metrics for monitoring brain performance and distribution.
        /// </summary>
        /// <returns>A list of brain lock data objects containing brain instance information, market assignments, and metrics.</returns>
        /// <response code="200">Returns the brain locks data successfully.</response>
        /// <response code="500">Internal server error occurred while retrieving data.</response>
        [HttpGet("brainlocks")]
        public async Task<IActionResult> GetBrainLocksData()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Get cached active markets
                List<MarketDTO> markets;
                if (_cache.TryGetValue(MarketsCacheKey, out var cachedMarkets))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    markets = (List<MarketDTO>)cachedMarkets;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    markets = (await _context.GetMarketsFiltered(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active })).ToList();
                    _cache.Set(MarketsCacheKey, markets, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Get all brain instances from database (not just those with BrainLock)
                List<BrainInstanceDTO> allBrainInstances;
                if (_cache.TryGetValue(AllBrainInstancesCacheKey, out var cachedAllBrainInstances))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    allBrainInstances = (List<BrainInstanceDTO>)cachedAllBrainInstances;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    allBrainInstances = (await _context.GetBrainInstancesFiltered()).ToList();
                    _cache.Set(AllBrainInstancesCacheKey, allBrainInstances, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Get cached brain instances with BrainLock for lookup
                Dictionary<Guid, string> brainInstancesByLock;
                if (_cache.TryGetValue(BrainInstancesCacheKey, out var cachedBrainInstances))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    brainInstancesByLock = (Dictionary<Guid, string>)cachedBrainInstances;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    brainInstancesByLock = (await _context.GetBrainInstancesFiltered(hasBrainLock: true))
                        .Where(bi => bi.BrainLock.HasValue)
                        .ToDictionary(bi => bi.BrainLock.Value, bi => bi.BrainInstanceName);
                    _cache.Set(BrainInstancesCacheKey, brainInstancesByLock, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Get market watches for active markets only
                var activeMarketTickers = markets.Where(m => m.market_ticker != null).Select(m => m.market_ticker!).ToHashSet();
                var marketWatches = (await _context.GetMarketWatches(marketTickers: activeMarketTickers)).ToList();

                // Get snapshot and error data from LogEntry table for brain instances (optimized with caching)
                var brainInstanceNames = allBrainInstances.Select(bi => bi.BrainInstanceName).Distinct().ToList();

                LogData brainInstanceLogData;
                if (_cache.TryGetValue(LogDataCacheKey, out var cachedLogData))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    brainInstanceLogData = (LogData)cachedLogData;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }

                    var localSnapshotData = new Dictionary<string, DateTime?>();
                    var localErrorData = new Dictionary<string, DateTime?>();

                    if (brainInstanceNames.Any())
                    {
                        try
                        {
                            // Query logs for each brain instance individually (since GetLogEntries doesn't support multiple)
                            foreach (var brainInstanceName in brainInstanceNames)
                            {
                                try
                                {
                                    // Query for snapshot messages
                                    var snapshotLogs = await _context.GetLogEntriesFiltered(
                                        brainInstance: brainInstanceName,
                                        maxRecords: 1);

                                    if (snapshotLogs != null && snapshotLogs.Any() && snapshotLogs.First().Message != null &&
                                        snapshotLogs.First().Message.Contains("snapshot", StringComparison.OrdinalIgnoreCase))
                                    {
                                        localSnapshotData[brainInstanceName] = snapshotLogs.First().Timestamp;
                                    }

                                    // Query for error messages
                                    var errorLogs = await _context.GetLogEntriesFiltered(
                                        brainInstance: brainInstanceName,
                                        level: "ERROR",
                                        maxRecords: 1);

                                    if (errorLogs != null && errorLogs.Any())
                                    {
                                        localErrorData[brainInstanceName] = errorLogs.First().Timestamp;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to retrieve log data for brain instance {BrainInstanceName}", brainInstanceName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to retrieve brain instance log data");
                        }
                    }

                    brainInstanceLogData = new LogData { SnapshotData = localSnapshotData, ErrorData = localErrorData };
                    _cache.Set(LogDataCacheKey, brainInstanceLogData, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = LogDataCacheDuration });
                }

                var snapshotData = brainInstanceLogData.SnapshotData;
                var errorData = brainInstanceLogData.ErrorData;

                // Create brain lock data for all brain instances
                var brainLockGroups = allBrainInstances.Select(brainInstance =>
                {
                    // Find market watches for this brain instance
                    var brainMarketWatches = marketWatches.Where(mw =>
                        mw.BrainLock.HasValue &&
                        brainInstancesByLock.TryGetValue(mw.BrainLock.Value, out var name) &&
                        name == brainInstance.BrainInstanceName).ToList();

                    snapshotData.TryGetValue(brainInstance.BrainInstanceName, out var lastSnapshotDate);
                    errorData.TryGetValue(brainInstance.BrainInstanceName, out var lastErrorDate);

                    return new
                    {
                        BrainLockId = brainInstance.BrainLock ?? Guid.NewGuid(), // Use existing BrainLock or generate temp ID
                        BrainInstanceName = brainInstance.BrainInstanceName,
                        MarketCount = brainMarketWatches.Count,
                        AverageInterestScore = brainMarketWatches.Any() ? brainMarketWatches.Average(mw => mw.InterestScore ?? 0) : 0,
                        MaxInterestScore = brainMarketWatches.Any() ? brainMarketWatches.Max(mw => mw.InterestScore ?? 0) : 0,
                        MinInterestScore = brainMarketWatches.Any() ? brainMarketWatches.Min(mw => mw.InterestScore ?? 0) : 0,
                        AverageWebsocketEvents = brainMarketWatches.Any() ? brainMarketWatches.Average(mw => mw.AverageWebsocketEventsPerMinute ?? 0) : 0,
                        LastWatched = brainMarketWatches.Any() ? brainMarketWatches.Max(mw => mw.LastWatched) : null,
                        LastSnapshotSaved = lastSnapshotDate,
                        LastErrorEncountered = lastErrorDate,
                        LastSeen = brainInstance.LastSeen,
                        Markets = brainMarketWatches.Select(mw => new
                        {
                            mw.market_ticker,
                            mw.InterestScore,
                            mw.InterestScoreDate,
                            mw.LastWatched,
                            mw.AverageWebsocketEventsPerMinute
                        }).ToList()
                    };
                })
                .OrderByDescending(bl => bl.MarketCount) // Order by market count, then by name
                .ThenBy(bl => bl.BrainInstanceName)
                .ToList();

                PostCacheMetrics();
                stopwatch.Stop();
                _logger.LogInformation("GetBrainLocksData completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(brainLockGroups);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve brain locks data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "Failed to retrieve brain locks data", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves trading position data with optional filtering for current positions only.
        /// </summary>
        /// <param name="currentOnly">If true, returns only positions with non-zero values; if false, returns all positions.</param>
        /// <returns>A list of position data objects containing trading position information.</returns>
        /// <response code="200">Returns the positions data successfully.</response>
        /// <response code="500">Internal server error occurred while retrieving data.</response>
        [HttpGet("positions")]
        public async Task<IActionResult> GetPositionsData(bool currentOnly = true)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Get market positions with optional filtering
                List<MarketPositionDTO> positions;
                var positionsKey = $"Positions_{currentOnly}";
                if (_cache.TryGetValue(positionsKey, out var cachedPositions))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    positions = (List<MarketPositionDTO>)cachedPositions;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }

                    // If currentOnly is true, only get positions where Position != 0
                    // If false, get all positions (historical + current)
                    bool? hasPosition = currentOnly ? true : null;

                    positions = (await _context.GetMarketPositions(hasPosition: hasPosition)).ToList();
                    _cache.Set(positionsKey, positions, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                var positionsData = positions.Select(p => new
                {
                    p.Ticker,
                    p.TotalTraded,
                    p.Position,
                    p.MarketExposure,
                    p.RealizedPnl,
                    p.RestingOrdersCount,
                    p.FeesPaid,
                    LastUpdatedUTC = p.LastUpdatedUTC,
                    LastModified = p.LastModified
                }).ToList();

                PostCacheMetrics();
                stopwatch.Stop();
                _logger.LogInformation("GetPositionsData completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(positionsData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve positions data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "Failed to retrieve positions data", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves trading order data including active and historical orders.
        /// </summary>
        /// <returns>A list of order data objects containing order information and status.</returns>
        /// <response code="200">Returns the orders data successfully.</response>
        /// <response code="500">Internal server error occurred while retrieving data.</response>
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrdersData()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Get orders data
                List<OrderDTO> orders;
                if (_cache.TryGetValue("Orders", out var cachedOrders))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    orders = (List<OrderDTO>)cachedOrders;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    orders = (await _context.GetOrders()).ToList();
                    _cache.Set("Orders", orders, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                var ordersData = orders.Select(o => new
                {
                    MarketTicker = o.Ticker,
                    o.OrderId,
                    o.Side,
                    Quantity = o.RemainingCount,
                    QuantityFilled = o.MakerFillCount + o.TakerFillCount,
                    Price = o.Side?.ToLower() == "yes" ? o.YesPrice : o.NoPrice,
                    o.Status,
                    OrderType = o.Type,
                    CreatedAt = o.CreatedTimeUTC,
                    UpdatedAt = o.LastUpdateTimeUTC
                }).ToList();

                PostCacheMetrics();
                stopwatch.Stop();
                _logger.LogInformation("GetOrdersData completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(ordersData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve orders data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "Failed to retrieve orders data", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves account balance and portfolio information from the Kalshi API.
        /// </summary>
        /// <returns>An object containing account balance and portfolio value.</returns>
        /// <response code="200">Returns the account data successfully.</response>
        /// <response code="500">Internal server error occurred while retrieving data.</response>
        [HttpGet("account")]
        public async Task<IActionResult> GetAccountData()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Get balance from Kalshi API
                var balance = await _apiService.GetBalanceAsync();
                var balanceInDollars = (double)balance / 100.0;

                // Portfolio value calculation is not yet implemented
                stopwatch.Stop();
                _logger.LogInformation("GetAccountData completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(new
                {
                    balance = balanceInDollars,
                    portfolioValue = 0.0
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve account data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "Failed to retrieve account data", details = ex.Message });
            }
        }

        /// <summary>
        /// Posts accumulated cache performance metrics to the performance metrics service.
        /// </summary>
        private void PostCacheMetrics()
        {
            if (_enableMarketWatchControllerPerformanceMetrics && (_localCacheHits > 0 || _localCacheMisses > 0))
            {
                _performanceMetricsService.PostCacheMetrics(_localCacheHits, _localCacheMisses);
                _localCacheHits = 0;
                _localCacheMisses = 0;
            }
        }

        /// <summary>
        /// Logs client-side events for monitoring and debugging purposes.
        /// </summary>
        /// <param name="request">The event data to log.</param>
        /// <returns>A confirmation response indicating the event was logged.</returns>
        /// <response code="200">Event logged successfully.</response>
        /// <response code="500">Internal server error occurred while logging the event.</response>
        [HttpPost("log")]
        public async Task<IActionResult> LogEvent([FromBody] object request)
        {
            var stopwatch = Stopwatch.StartNew();
            if (request == null)
            {
                stopwatch.Stop();
                _logger.LogWarning("LogEvent called with null request in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return BadRequest("Request body is required");
            }

            try
            {
                // Log the event using structured logging
                _logger.LogInformation("Client log event received: {Request}", request);

                // Optionally, you could also insert into the LogEntry table if needed
                // await _context.InsertLogEntry(new LogEntry { ... });

                stopwatch.Stop();
                _logger.LogInformation("LogEvent completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(new { status = "logged" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to log client event in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "Failed to log event", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves snapshot group data aggregated by market for analysis and monitoring.
        /// </summary>
        /// <returns>A list of snapshot group data objects containing market snapshot information.</returns>
        /// <response code="200">Returns the snapshots data successfully.</response>
        /// <response code="500">Internal server error occurred while retrieving data.</response>
        [HttpGet("snapshots")]
        public async Task<IActionResult> GetSnapshotsData()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var snapshotsData = await _snapshotService.GetSnapshotGroupsDataAsync();
                stopwatch.Stop();
                _logger.LogInformation("GetSnapshotsData completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(snapshotsData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to retrieve snapshots data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "Failed to retrieve snapshots data", details = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves comprehensive brain instance data including watched markets and configuration.
        /// </summary>
        /// <returns>A list of brain data objects containing brain instance information and market assignments.</returns>
        /// <response code="200">Returns the brains data successfully.</response>
        /// <response code="500">Internal server error occurred while retrieving data.</response>
        [HttpGet("brains")]
        public async Task<IActionResult> GetBrainsData()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Get all brain instances from database
                List<BrainInstanceDTO> brainInstances;
                if (_cache.TryGetValue(AllBrainInstancesCacheKey, out var cachedBrainInstances))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    brainInstances = (List<BrainInstanceDTO>)cachedBrainInstances;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    brainInstances = (await _context.GetBrainInstancesFiltered()).ToList();
                    _cache.Set(AllBrainInstancesCacheKey, brainInstances, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Filter to only show brains with activity in the last 48 hours
                brainInstances = brainInstances.Where(bi => bi.LastSeen.HasValue && bi.LastSeen.Value > DateTime.UtcNow.AddHours(-48)).ToList();

                // Get all market watches
                List<MarketWatchDTO> marketWatches;
                if (_cache.TryGetValue("AllMarketWatches", out var cachedMarketWatches))
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheHits++;
                    }
                    marketWatches = (List<MarketWatchDTO>)cachedMarketWatches;
                }
                else
                {
                    if (_enableMarketWatchControllerPerformanceMetrics)
                    {
                        _localCacheMisses++;
                    }
                    marketWatches = (await _context.GetMarketWatches()).ToList();
                    _cache.Set("AllMarketWatches", marketWatches, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = MarketsCacheDuration });
                }

                // Create a lookup for brain instances by BrainLock
                var brainLookup = brainInstances
                    .Where(bi => bi.BrainLock.HasValue)
                    .ToDictionary(bi => bi.BrainLock.Value, bi => bi);

                // Group market watches by BrainLock
                var marketWatchesByBrainLock = marketWatches
                    .Where(mw => mw.BrainLock.HasValue)
                    .GroupBy(mw => mw.BrainLock.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Create brain data with their watched markets
                var brainData = new List<object>();

                foreach (var brainInstance in brainInstances)
                {
                    var brainName = brainInstance.BrainInstanceName;

                    // Get markets watched by this brain instance
                    List<MarketWatchDTO> watchedMarkets = new List<MarketWatchDTO>();
                    if (brainInstance.BrainLock.HasValue &&
                        marketWatchesByBrainLock.TryGetValue(brainInstance.BrainLock.Value, out var markets))
                    {
                        watchedMarkets = markets;
                    }

                    // Use a single consistent property name for brain instance
                    // to avoid JSON property collisions due to camelCase naming policy.
                    brainData.Add(new
                    {
                        BrainInstanceName = brainName,
                        // Expose BrainLock and other fields under their original names
                        brainInstance.BrainLock,
                        brainInstance.LastSeen,
                        brainInstance.TargetWatches,
                        Mode = "Autonomous",
                        WatchedMarkets = watchedMarkets.Select(mw => new
                        {
                            mw.market_ticker,
                            mw.LastWatched,
                            mw.InterestScore,
                            mw.InterestScoreDate,
                            mw.AverageWebsocketEventsPerMinute
                        }).ToList()
                    });
                }

                PostCacheMetrics();
                stopwatch.Stop();
                _logger.LogInformation("GetBrainsData completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return Ok(brainData);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError("Failed to retrieve brains data in {ElapsedMilliseconds}ms Exception: {ex.Message}. Inner: {ex.InnerException?.Message ?? \"None\"}", stopwatch.ElapsedMilliseconds, ex.Message, ex.InnerException?.Message ?? "None");
                return StatusCode(500, new { error = "Failed to retrieve brains data", details = ex.Message });
            }
        }


    }
}
