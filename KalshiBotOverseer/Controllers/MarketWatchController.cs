using KalshiBotData.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using BacklashDTOs.Data;
using BacklashInterfaces.Constants;
using BacklashBot.KalshiAPI.Interfaces;
using System.Linq;
using System.Threading.Tasks;
using KalshiBotOverseer.Services;

namespace KalshiBotOverseer.Controllers
{
    /// <summary>
    /// ASP.NET Core Web API controller that provides endpoints for retrieving market watch data,
    /// brain instance locks, trading positions, orders, account information, and snapshot data.
    /// This controller serves as the primary interface for the Kalshi trading bot's monitoring and
    /// data retrieval operations, utilizing caching for performance optimization and integrating
    /// with database and external API services.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MarketWatchController : ControllerBase
    {
        private readonly IKalshiBotContext _context;
        private readonly IMemoryCache _cache;
        private readonly IKalshiAPIService _apiService;
        private readonly SnapshotService _snapshotService;
        private readonly ILogger<MarketWatchController> _logger;
        private const string MarketsCacheKey = "ActiveMarkets";
        private const string BrainInstancesCacheKey = "BrainInstances";
        private const string AllBrainInstancesCacheKey = "AllBrainInstances";
        private const string LogDataCacheKey = "LogData";
        private readonly TimeSpan MarketsCacheDuration = TimeSpan.FromMinutes(15);
        private readonly TimeSpan LogDataCacheDuration = TimeSpan.FromMinutes(5); // Shorter cache for log data

        /// <summary>
        /// Initializes a new instance of the MarketWatchController with required dependencies.
        /// </summary>
        /// <param name="context">Database context for accessing trading data.</param>
        /// <param name="cache">Memory cache for performance optimization.</param>
        /// <param name="apiService">Service for interacting with Kalshi API.</param>
        /// <param name="snapshotService">Service for managing market snapshots.</param>
        /// <param name="logger">Logger for recording operational information and errors.</param>
        public MarketWatchController(IKalshiBotContext context, IMemoryCache cache, IKalshiAPIService apiService, SnapshotService snapshotService, ILogger<MarketWatchController> logger)
        {
            _context = context;
            _cache = cache;
            _apiService = apiService;
            _snapshotService = snapshotService;
            _logger = logger;
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
            try
            {
                // Get cached active markets or fetch fresh data
                var markets = await _cache.GetOrCreateAsync(MarketsCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetMarketsFiltered(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active })).ToList();
                });

                // Get cached brain instances for lookup
                var brainInstancesByLock = await _cache.GetOrCreateAsync(BrainInstancesCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetBrainInstancesFiltered(hasBrainLock: true))
                        .Where(bi => bi.BrainLock.HasValue)
                        .ToDictionary(bi => bi.BrainLock.Value, bi => bi.BrainInstanceName);
                });

                // Get all brain instances for comprehensive lookup
                var allBrainInstances = await _cache.GetOrCreateAsync(AllBrainInstancesCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetBrainInstancesFiltered()).ToList();
                });

                // Get market watches for active markets only
                var activeMarketTickers = markets.Select(m => m.market_ticker).ToHashSet();
                var marketWatches = (await _context.GetMarketWatches(marketTickers: activeMarketTickers)).ToList();

                // Create a lookup for faster market access
                var marketLookup = markets.ToDictionary(m => m.market_ticker, m => m);

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

                return Ok(marketWatchData);
            }
            catch (Exception ex)
            {
                // Log the error and return a proper error response
                _logger.LogError(ex, "Failed to retrieve market watch data");
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
            try
            {
                // Get cached active markets
                var markets = await _cache.GetOrCreateAsync(MarketsCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetMarketsFiltered(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active })).ToList();
                });

                // Get all brain instances from database (not just those with BrainLock)
                var allBrainInstances = await _cache.GetOrCreateAsync(AllBrainInstancesCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetBrainInstancesFiltered()).ToList();
                });

                // Get cached brain instances with BrainLock for lookup
                var brainInstancesByLock = await _cache.GetOrCreateAsync(BrainInstancesCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetBrainInstancesFiltered(hasBrainLock: true))
                        .Where(bi => bi.BrainLock.HasValue)
                        .ToDictionary(bi => bi.BrainLock.Value, bi => bi.BrainInstanceName);
                });

                // Get market watches for active markets only
                var activeMarketTickers = markets.Select(m => m.market_ticker).ToHashSet();
                var marketWatches = (await _context.GetMarketWatches(marketTickers: activeMarketTickers)).ToList();

                // Get snapshot and error data from LogEntry table for brain instances (optimized with caching)
                var brainInstanceNames = allBrainInstances.Select(bi => bi.BrainInstanceName).Distinct().ToList();

                var brainInstanceLogData = await _cache.GetOrCreateAsync(LogDataCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = LogDataCacheDuration;

                    var snapshotData = new Dictionary<string, DateTime?>();
                    var errorData = new Dictionary<string, DateTime?>();

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
                                        snapshotData[brainInstanceName] = snapshotLogs.First().Timestamp;
                                    }

                                    // Query for error messages
                                    var errorLogs = await _context.GetLogEntriesFiltered(
                                        brainInstance: brainInstanceName,
                                        level: "ERROR",
                                        maxRecords: 1);

                                    if (errorLogs != null && errorLogs.Any())
                                    {
                                        errorData[brainInstanceName] = errorLogs.First().Timestamp;
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

                    return new { SnapshotData = snapshotData, ErrorData = errorData };
                });

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

                return Ok(brainLockGroups);
            }
            catch (Exception ex)
            {
                // Log the error and return a proper error response
                _logger.LogError(ex, "Failed to retrieve brain locks data");
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
            try
            {
                // Get market positions with optional filtering
                var positions = await _cache.GetOrCreateAsync($"Positions_{currentOnly}", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;

                    // If currentOnly is true, only get positions where Position != 0
                    // If false, get all positions (historical + current)
                    bool? hasPosition = currentOnly ? true : null;

                    return (await _context.GetMarketPositions(hasPosition: hasPosition)).ToList();
                });

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

                return Ok(positionsData);
            }
            catch (Exception ex)
            {
                // Log the error and return a proper error response
                _logger.LogError(ex, "Failed to retrieve positions data");
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
            try
            {
                // Get orders data
                var orders = await _cache.GetOrCreateAsync("Orders", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetOrders()).ToList();
                });

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

                return Ok(ordersData);
            }
            catch (Exception ex)
            {
                // Log the error and return a proper error response
                _logger.LogError(ex, "Failed to retrieve orders data");
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
            try
            {
                // Get balance from Kalshi API
                var balance = await _apiService.GetBalanceAsync();
                var balanceInDollars = (double)balance / 100.0;

                // Portfolio value calculation is not yet implemented
                return Ok(new
                {
                    balance = balanceInDollars,
                    portfolioValue = 0.0
                });
            }
            catch (Exception ex)
            {
                // Log the error and return a proper error response
                _logger.LogError(ex, "Failed to retrieve account data");
                return StatusCode(500, new { error = "Failed to retrieve account data", details = ex.Message });
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
            try
            {
                // Log the event using structured logging
                _logger.LogInformation("Client log event received: {Request}", request);

                // Optionally, you could also insert into the LogEntry table if needed
                // await _context.InsertLogEntry(new LogEntry { ... });

                return Ok(new { status = "logged" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log client event");
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
            try
            {
                var snapshotsData = await _snapshotService.GetSnapshotGroupsDataAsync();
                return Ok(snapshotsData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve snapshots data");
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
            try
            {
                // Get all brain instances from database
                var brainInstances = await _cache.GetOrCreateAsync(AllBrainInstancesCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetBrainInstancesFiltered()).ToList();
                });

                // Get all market watches
                var marketWatches = await _cache.GetOrCreateAsync("AllMarketWatches", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetMarketWatches()).ToList();
                });

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
                        brainInstanceName = brainName,
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

                return Ok(brainData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve brains data");
                return StatusCode(500, new { error = "Failed to retrieve brains data", details = ex.Message });
            }
        }


    }
}
