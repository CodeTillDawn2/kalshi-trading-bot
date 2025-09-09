using KalshiBotData.Data.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmokehouseDTOs.Data;
using SmokehouseInterfaces.Constants;
using System.Linq;
using System.Threading.Tasks;

namespace KalshiBotOverseer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MarketWatchController : ControllerBase
    {
        private readonly IKalshiBotContext _context;
        private readonly IMemoryCache _cache;
        private const string MarketsCacheKey = "ActiveMarkets";
        private const string BrainInstancesCacheKey = "BrainInstances";
        private const string LogDataCacheKey = "LogData";
        private readonly TimeSpan MarketsCacheDuration = TimeSpan.FromMinutes(15);
        private readonly TimeSpan LogDataCacheDuration = TimeSpan.FromMinutes(5); // Shorter cache for log data

        public MarketWatchController(IKalshiBotContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return PhysicalFile(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "marketwatch.html"), "text/html");
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetMarketWatchData()
        {
            try
            {
                // Get cached active markets or fetch fresh data
                var markets = await _cache.GetOrCreateAsync(MarketsCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetMarkets(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active })).ToList();
                });

                // Get cached brain instances for lookup
                var brainInstances = await _cache.GetOrCreateAsync(BrainInstancesCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetBrainInstances_cached())
                        .Where(bi => bi.BrainLock.HasValue)
                        .ToDictionary(bi => bi.BrainLock.Value, bi => bi.BrainInstanceName);
                });

                // Get market watches for active markets only
                var activeMarketTickers = markets.Select(m => m.market_ticker).ToHashSet();
                var marketWatches = (await _context.GetMarketWatches(marketTickers: activeMarketTickers)).ToList();

                // Create a lookup for faster market access
                var marketLookup = markets.ToDictionary(m => m.market_ticker, m => m);

                var marketWatchData = marketWatches.Select(mw =>
                {
                    marketLookup.TryGetValue(mw.market_ticker, out var market);
                    brainInstances.TryGetValue(mw.BrainLock ?? Guid.Empty, out var brainInstanceName);

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
                Console.WriteLine($"Error in GetMarketWatchData: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to retrieve market watch data", details = ex.Message });
            }
        }

        [HttpGet("brainlocks")]
        public async Task<IActionResult> GetBrainLocksData()
        {
            try
            {
                // Get cached active markets
                var markets = await _cache.GetOrCreateAsync(MarketsCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetMarkets(includedStatuses: new HashSet<string> { KalshiConstants.Status_Active })).ToList();
                });

                // Get cached brain instances for lookup
                var brainInstances = await _cache.GetOrCreateAsync(BrainInstancesCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = MarketsCacheDuration;
                    return (await _context.GetBrainInstances_cached())
                        .Where(bi => bi.BrainLock.HasValue)
                        .ToDictionary(bi => bi.BrainLock.Value, bi => bi.BrainInstanceName);
                });

                // Get market watches for active markets only
                var activeMarketTickers = markets.Select(m => m.market_ticker).ToHashSet();
                var marketWatches = (await _context.GetMarketWatches(marketTickers: activeMarketTickers)).ToList();

                // Get snapshot and error data from LogEntry table for brain locks (optimized with caching)
                var brainLockIds = marketWatches.Where(mw => mw.BrainLock.HasValue).Select(mw => mw.BrainLock.Value).Distinct().ToList();

                var logData = await _cache.GetOrCreateAsync(LogDataCacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = LogDataCacheDuration;

                    var snapshotData = new Dictionary<Guid, DateTime?>();
                    var errorData = new Dictionary<Guid, DateTime?>();

                    // Get all brain instance names for the brain locks
                    var brainInstanceNames = brainLockIds
                        .Where(id => brainInstances.ContainsKey(id))
                        .Select(id => brainInstances[id])
                        .Distinct()
                        .ToList();

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
                                    var snapshotLogs = await _context.GetLogEntries(
                                        brainInstance: brainInstanceName,
                                        maxRecords: 1);

                                    if (snapshotLogs.Any() && snapshotLogs.First().Message.Contains("snapshot", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // Find the brain lock ID for this brain instance
                                        var brainLockId = brainInstances.FirstOrDefault(kvp => kvp.Value == brainInstanceName).Key;
                                        if (brainLockId != Guid.Empty)
                                        {
                                            snapshotData[brainLockId] = snapshotLogs.First().Timestamp;
                                        }
                                    }

                                    // Query for error messages
                                    var errorLogs = await _context.GetLogEntries(
                                        brainInstance: brainInstanceName,
                                        level: "ERROR",
                                        maxRecords: 1);

                                    if (errorLogs.Any())
                                    {
                                        // Find the brain lock ID for this brain instance
                                        var brainLockId = brainInstances.FirstOrDefault(kvp => kvp.Value == brainInstanceName).Key;
                                        if (brainLockId != Guid.Empty)
                                        {
                                            errorData[brainLockId] = errorLogs.First().Timestamp;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error getting log data for brain instance {brainInstanceName}: {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting log data: {ex.Message}");
                        }
                    }

                    return new { SnapshotData = snapshotData, ErrorData = errorData };
                });

                var snapshotData = logData.SnapshotData;
                var errorData = logData.ErrorData;

                // Group by brain lock and calculate rollup stats
                var brainLockGroups = marketWatches
                    .Where(mw => mw.BrainLock.HasValue)
                    .GroupBy(mw => mw.BrainLock.Value)
                    .Select(group => {
                        brainInstances.TryGetValue(group.Key, out var brainInstanceName);
                        snapshotData.TryGetValue(group.Key, out var lastSnapshotDate);
                        errorData.TryGetValue(group.Key, out var lastErrorDate);
                        return new
                        {
                            BrainLockId = group.Key,
                            BrainInstanceName = brainInstanceName ?? "Unknown",
                            MarketCount = group.Count(),
                            AverageInterestScore = group.Average(mw => mw.InterestScore ?? 0),
                            MaxInterestScore = group.Max(mw => mw.InterestScore ?? 0),
                            MinInterestScore = group.Min(mw => mw.InterestScore ?? 0),
                            AverageWebsocketEvents = group.Average(mw => mw.AverageWebsocketEventsPerMinute ?? 0),
                            LastWatched = group.Max(mw => mw.LastWatched),
                            LastSnapshotSaved = lastSnapshotDate,
                            LastErrorEncountered = lastErrorDate,
                            Markets = group.Select(mw => new
                            {
                                mw.market_ticker,
                                mw.InterestScore,
                                mw.InterestScoreDate,
                                mw.LastWatched,
                                mw.AverageWebsocketEventsPerMinute
                            }).ToList()
                        };
                    })
                    .OrderByDescending(bl => bl.AverageInterestScore)
                    .ToList();

                return Ok(brainLockGroups);
            }
            catch (Exception ex)
            {
                // Log the error and return a proper error response
                Console.WriteLine($"Error in GetBrainLocksData: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to retrieve brain locks data", details = ex.Message });
            }
        }
    }
}