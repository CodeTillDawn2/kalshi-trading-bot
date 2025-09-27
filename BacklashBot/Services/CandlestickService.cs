using BacklashBot.Configuration;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashBotData.Data.Interfaces;
using BacklashCommon.Configuration;
using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashDTOs.Exceptions;
using BacklashDTOs.Helpers;
using BacklashInterfaces.PerformanceMetrics;
using Microsoft.Extensions.Options;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BacklashBot.Services
{
    /// <summary>
    /// Service responsible for managing candlestick data for trading markets.
    /// Handles loading, processing, and persistence of historical price data from both
    /// Parquet files and SQL database, including forward filling and deduplication.
    /// Provides market data population and candlestick synchronization functionality.
    /// </summary>
    public class CandlestickService : ICandlestickService
    {
        private readonly ILogger<ICandlestickService> _logger;
        private readonly CandlestickServiceConfig _candlestickConfig;
        private readonly DataStorageConfig _storageConfig;
        private readonly LoggingConfig _loggingConfig;
        private readonly IStatusTrackerService _statusTracker;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceFactory _serviceFactory;
        private readonly IPerformanceMonitor _performanceMonitor;

        /// <summary>
        /// Initializes a new instance of the CandlestickService with required dependencies.
        /// </summary>
        /// <param name="logger">Logger for recording service operations and errors</param>
        /// <param name="scopeFactory">Factory for creating service scopes for database operations</param>
        /// <param name="statusTracker">Service for tracking system status and cancellation tokens</param>
        /// <param name="candlestickConfig">Configuration options for candlestick service parameters</param>
        /// <param name="loggingConfig">Configuration options for logging behavior</param>
        /// <param name="storageConfig">Configuration options for storage behavior</param>
        /// <param name="serviceFactory">Factory for accessing other system services</param>
        /// <param name="performanceMonitor">Performance monitor for recording metrics</param>
        public CandlestickService(
            ILogger<ICandlestickService> logger,
            IServiceScopeFactory scopeFactory,
            IStatusTrackerService statusTracker,
            IOptions<CandlestickServiceConfig> candlestickConfig,
            IOptions<LoggingConfig> loggingConfig,
            IOptions<DataStorageConfig> storageConfig,
            IServiceFactory serviceFactory,
            IPerformanceMonitor performanceMonitor)
        {
            _logger = logger;
            _statusTracker = statusTracker;
            _scopeFactory = scopeFactory;
            _storageConfig = storageConfig.Value;
            _candlestickConfig = candlestickConfig.Value;
            _loggingConfig = loggingConfig.Value;
            _serviceFactory = serviceFactory;
            _performanceMonitor = performanceMonitor;
        }


        /// <summary>
        /// Logs performance metrics for operations if enabled in configuration.
        /// </summary>
        /// <param name="operationName">Name of the operation being measured</param>
        /// <param name="elapsedMilliseconds">Time elapsed in milliseconds</param>
        /// <param name="additionalData">Optional additional data to log</param>
        private void LogPerformanceMetric(string operationName, long elapsedMilliseconds, string? additionalData = null)
        {
            RecordExecutionTimePrivate(operationName, elapsedMilliseconds, _candlestickConfig.EnablePerformanceMetrics);
            
        }

        /// <summary>
        /// Updates candlestick data for a specific market by fetching the latest data from the API.
        /// Synchronizes minute, hour, and day interval candlesticks and updates market metadata.
        /// </summary>
        /// <param name="marketTicker">The market ticker identifier to update candlesticks for</param>
        /// <returns>A task representing the asynchronous update operation</returns>
        public async Task UpdateCandlesticksAsync(string marketTicker)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData)) return;

                _logger.LogInformation("Updating candlesticks for {MarketTicker}", marketTicker);

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                var marketService = scope.ServiceProvider.GetRequiredService<IKalshiAPIService>();
                var market = await context.GetMarketByTicker(marketTicker);
                if (market == null) return;

                DateTime startTime = market.open_time.ToUniversalTime();
                long startTimestamp = UnixHelper.ConvertToUnixTimestamp(startTime);

                long lastMinuteTimestamp = startTimestamp;
                long lastHourTimestamp = startTimestamp;
                long lastDayTimestamp = startTimestamp;

                CandlestickDTO? lastCandle = await context.GetLatestCandlestick(marketTicker, 1);
                if (lastCandle != null)
                {
                    // Apply cushion: subtract configured days to get more historical data
                    DateTime lastMinuteTime = UnixHelper.ConvertFromUnixTimestamp(lastCandle.end_period_ts);
                    DateTime cushionStart = lastMinuteTime.AddDays(-_candlestickConfig.CandlestickMandatoryOverlapDaysMinute);
                    lastMinuteTimestamp = UnixHelper.ConvertToUnixTimestamp(cushionStart);
                    _logger.LogDebug("Last minute candlestick for {MarketTicker}: cushion_applied_ts={Timestamp} (cushion: {CushionDays} days)",
                        marketTicker, UnixHelper.ConvertFromUnixTimestamp(lastMinuteTimestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), _candlestickConfig.CandlestickMandatoryOverlapDaysMinute);
                }
                else
                {
                    _logger.LogDebug("No existing minute candlesticks for {MarketTicker}, using market open time: {Timestamp}",
                        marketTicker, UnixHelper.ConvertFromUnixTimestamp(lastMinuteTimestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                }

                lastCandle = await context.GetLatestCandlestick(marketTicker, 2);
                if (lastCandle != null)
                {
                    // Apply cushion: subtract configured days to get more historical data
                    DateTime lastHourTime = UnixHelper.ConvertFromUnixTimestamp(lastCandle.end_period_ts);
                    DateTime cushionStart = lastHourTime.AddDays(-_candlestickConfig.CandlestickMandatoryOverlapDaysHour);
                    lastHourTimestamp = UnixHelper.ConvertToUnixTimestamp(cushionStart);
                    _logger.LogDebug("Last hour candlestick for {MarketTicker}: cushion_applied_ts={Timestamp} (cushion: {CushionDays} days)",
                       marketTicker, UnixHelper.ConvertFromUnixTimestamp(lastHourTimestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), _candlestickConfig.CandlestickMandatoryOverlapDaysHour);
                }
                else
                {
                    _logger.LogDebug("No existing hour candlesticks for {MarketTicker}, using market open time: {Timestamp}",
                        marketTicker, UnixHelper.ConvertFromUnixTimestamp(lastHourTimestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                }

                lastCandle = await context.GetLastCandlestick(marketTicker, 3);
                if (lastCandle != null)
                {
                    // Apply cushion: subtract configured days to get more historical data
                    DateTime lastDayTime = UnixHelper.ConvertFromUnixTimestamp(lastCandle.end_period_ts);
                    DateTime cushionStart = lastDayTime.AddDays(-_candlestickConfig.CandlestickMandatoryOverlapDaysDay);
                    lastDayTimestamp = UnixHelper.ConvertToUnixTimestamp(cushionStart);
                    _logger.LogDebug("Last day candlestick for {MarketTicker}: cushion_applied_ts={Timestamp} (cushion: {CushionDays} days)",
                        marketTicker, UnixHelper.ConvertFromUnixTimestamp(lastDayTimestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), _candlestickConfig.CandlestickMandatoryOverlapDaysDay);
                }
                else
                {
                    _logger.LogDebug("No existing day candlesticks for {MarketTicker}, using market open time: {Timestamp}",
                        marketTicker, UnixHelper.ConvertFromUnixTimestamp(lastDayTimestamp).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                }

                // Last applicable candlestick, plus some cushion time
                foreach (var intervalData in new[] { ("minute", lastMinuteTimestamp - _candlestickConfig.CandlestickMandatoryOverlapDaysMinute * 60),
                    ("hour", lastHourTimestamp - _candlestickConfig.CandlestickMandatoryOverlapDaysHour * 3600),
                    ("day", lastDayTimestamp - _candlestickConfig.CandlestickMandatoryOverlapDaysDay * 86400) })
                {

                    _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                    var (processed, errors) = await marketService.FetchCandlesticksAsync(
                        market.Event.Series.series_ticker, marketTicker, intervalData.Item1, intervalData.Item2, null, false);
                    _logger.LogDebug("Fetched {Processed} candlesticks for {MarketTicker} at {Interval}, errors: {Errors}",
                        processed, marketTicker, intervalData.Item1, errors);
                }
                _logger.LogInformation("Completed candlestick update for {MarketTicker}", marketTicker);

                try
                {
                    await context.UpdateMarketLastCandlestick(marketTicker);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error updating last candlestick metadata for {MarketTicker}: {Message}, Inner: {Inner}",
                        marketTicker, ex.Message, ex.InnerException?.Message ?? "None");
                }

                marketData.RefreshCandlestickMetadata();

                stopwatch.Stop();
                LogPerformanceMetric("UpdateCandlesticksAsync", stopwatch.ElapsedMilliseconds, $"Market: {marketTicker}");
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                LogPerformanceMetric("UpdateCandlesticksAsync", stopwatch.ElapsedMilliseconds, $"Market: {marketTicker} (cancelled)");
                _logger.LogInformation("Candlestick update was cancelled for {MarketTicker}", marketTicker);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    new CandlestickTransientFailureException(marketTicker
                    , $"Transient failure while updating candlesticks for market {marketTicker}"
                    , ex),
                    "Transient failure while updating candlesticks for market {MarketTicker}. Exception: {Message}, Inner: {Inner}", marketTicker, ex.Message, ex.InnerException?.Message ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(new MarketTransientFailureException(marketTicker, $"Failed to update candlesticks for {marketTicker}.", ex),
                    "Failed to update candlesticks for {MarketTicker}. Exception: {Message}, Inner: {Inner}", marketTicker, ex.Message, ex.InnerException?.Message ?? "None");
            }
        }

        /// <summary>
        /// Retrieves historical candlestick data for a specific market and timeframe.
        /// </summary>
        /// <param name="marketTicker">The market ticker identifier</param>
        /// <param name="timeframe">The timeframe interval (e.g., "minute", "hour", "day")</param>
        /// <returns>A list of candlestick data for the specified market and timeframe</returns>
        public List<CandlestickData> RetrieveHistoricalCandlesticksAsync(string marketTicker, string timeframe)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                return new List<CandlestickData>();

            return marketData.Candlesticks[timeframe].ToList();
        }

        /// <summary>
        /// Populates market data for a specific market by loading candlesticks from multiple sources
        /// and calculating market statistics. Handles parallel processing of different time intervals
        /// and performs data validation and deduplication.
        /// </summary>
        /// <param name="marketTicker">The market ticker identifier to populate data for</param>
        /// <returns>A task representing the asynchronous population operation</returns>
        public async Task PopulateMarketDataAsync(string marketTicker)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                _logger.LogDebug("Populating market data for {MarketTicker}", marketTicker);
                if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                {
                    if (!_serviceFactory.GetDataCache().RecentlyRemovedMarkets.Contains(marketTicker))
                    {
                        _logger.LogWarning("Market data not found for {MarketTicker}", marketTicker);
                    }
                    else
                    {
                        _logger.LogInformation("Market data not found for {MarketTicker}, but it was recently removed.", marketTicker);
                    }
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                var market = await context.GetMarketByTicker(marketTicker);
                if (market == null)
                {
                    _logger.LogWarning("Market not found in database for {MarketTicker}", marketTicker);
                    return;
                }
                marketData.MarketInfo = market;

                // Process all intervals in parallel with concurrency limit
                using var semaphore = new SemaphoreSlim(_candlestickConfig.MaxParallelCandlestickTasks);
                var intervalTasks = new[] { "minute", "hour", "day" }
                    .Select(interval => Task.Run(async () =>
                    {
                        await semaphore.WaitAsync(_statusTracker.GetCancellationToken());
                        try
                        {
                            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
                            _logger.LogDebug("Processing {Interval} candlesticks for market {MarketTicker}", interval, marketTicker);

                            string hardDataStorageLocation = _storageConfig.HardDataStorageLocation;
                            var existingCandlesticks = marketData.Candlesticks[interval];
                            var latestExistingDate = existingCandlesticks.Any() ? existingCandlesticks.Max(c => c.Date) : (DateTime?)null;

                            // Load new candlesticks
                            List<CandlestickData> newCandlesticks;

                            try
                            {
                                newCandlesticks = await LoadAndProcessCandlesticksAsync(existingCandlesticks, marketTicker, interval, hardDataStorageLocation, latestExistingDate, market.open_time);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("Error loading candlesticks for {Interval}, market {MarketTicker}: {Message}, Inner: {Inner}", interval, marketTicker, ex.Message, ex.InnerException?.Message ?? "None");
                                newCandlesticks = new List<CandlestickData>();
                            }

                            // Validate newCandlesticks
                            if (newCandlesticks == null)
                            {
                                _logger.LogWarning(new MarketTransientFailureException(marketTicker, $"LoadAndProcessCandlesticksAsync returned null for {interval}, market {marketTicker}"),
                                    "LoadAndProcessCandlesticksAsync returned null for {Interval}, market {MarketTicker}", interval, marketTicker);
                                return (Interval: interval, Candlesticks: new List<CandlestickData>());
                            }

                            _logger.LogDebug("Loaded {Count} new candlesticks for {Interval}, market {MarketTicker}",
                                newCandlesticks.Count, interval, marketTicker);

                            // Validate Date properties and sorted order
                            if (existingCandlesticks.Any(c => c.Date == default) || newCandlesticks.Any(c => c.Date == default))
                            {
                                _logger.LogWarning(new MarketTransientFailureException(marketTicker, $"Invalid Date detected in candlesticks for {interval}, market {market}"),
                                    "Invalid Date detected in candlesticks for {interval}, market {market}", interval, marketTicker);
                                return (Interval: interval, Candlesticks: new List<CandlestickData>());
                            }
                            // Verify existingCandlesticks is sorted
                            if (existingCandlesticks.Count > 1 && existingCandlesticks.Zip(existingCandlesticks.Skip(1), (a, b) => a.Date <= b.Date).Any(x => !x))
                            {
                                _logger.LogWarning("existingCandlesticks not sorted for {interval}, market {market}. Sorting required.", interval, marketTicker);
                                existingCandlesticks = existingCandlesticks.OrderBy(c => c.Date).ToList();
                            }
                            // Verify newCandlesticks is sorted and starts after existing
                            if (newCandlesticks.Count > 1 && newCandlesticks.Zip(newCandlesticks.Skip(1), (a, b) => a.Date <= b.Date).Any(x => !x))
                            {
                                _logger.LogWarning("newCandlesticks not sorted for {interval}, market {market}. Sorting required.", interval, marketTicker);
                                newCandlesticks = newCandlesticks.OrderBy(c => c.Date).ToList();
                            }
                            if (latestExistingDate.HasValue && newCandlesticks.Any() && newCandlesticks.First().Date < latestExistingDate.Value)
                            {
                                newCandlesticks = newCandlesticks.Where(c => c.Date >= latestExistingDate.Value).ToList();
                            }

                            // Filter existing candlesticks
                            var matched = existingCandlesticks
                                .Where(c => !newCandlesticks.Any(n => n.Date == c.Date))
                                .ToList();
                            _logger.LogDebug("Merging {interval} for market {market}. matched: {count2}",
                                interval, marketTicker, matched.Count);

                            // Merge without sorting, assuming inputs are sorted
                            var mergedCandlesticks = new List<CandlestickData>(matched.Count + newCandlesticks.Count);
                            mergedCandlesticks.AddRange(matched);
                            mergedCandlesticks.AddRange(newCandlesticks);

                            _logger.LogDebug("Merged {interval} for market {market}. TotalCount: {count}",
                                interval, marketTicker, mergedCandlesticks.Count);
                            return (Interval: interval, Candlesticks: mergedCandlesticks);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("Task canceled for {interval}, market {market}.", interval, marketTicker);
                            return (Interval: interval, Candlesticks: new List<CandlestickData>());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing {interval} for market {market}.", interval, marketTicker);
                            return (Interval: interval, Candlesticks: new List<CandlestickData>());
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, _statusTracker.GetCancellationToken())).ToList();

                try
                {
                    var results = await Task.WhenAll(intervalTasks);
                    _logger.LogDebug("When all done for market {market}.", marketTicker);

                    foreach (var result in results)
                    {
                        string interval = result.Interval;
                        List<CandlestickData> candlesticks = result.Candlesticks;
                        marketData.Candlesticks[interval] = candlesticks;
                        _logger.LogDebug("Populated {Count} candlesticks for {MarketTicker} at {Interval}, Latest: {LatestDate}",
                            candlesticks.Count, marketTicker, interval,
                            candlesticks.Any() ? candlesticks.Max(c => c.Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : "None");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(
                        new CandlestickTransientFailureException(marketTicker
                        , $"Transient failure while updating candlesticks in PopulateMarketDataAsync for market {marketTicker}"
                        , ex),
                        "Transient failure while updating candlesticks in PopulateMarketDataAsync for market {market}. Exception: {Message}, Inner: {Inner}", marketTicker, ex.Message, ex.InnerException?.Message ?? "None");
                }
                catch (AggregateException ex)
                {
                    foreach (var innerEx in ex.Flatten().InnerExceptions)
                    {
                        _logger.LogError(innerEx, "Task failed for market {marketTicker}", marketTicker);
                    }
                    return;
                }

                // Calculate market statistics from minute candlesticks
                var validMinuteCandles = marketData.Candlesticks["minute"]
                    .SkipWhile(x => x.Volume == 0)
                    .ToList();

                _logger.LogDebug("Calculating market statistics for {MarketTicker} using {Count} valid minute candles",
                    marketTicker, validMinuteCandles.Count);

                // Calculate all-time highs and lows
                var highestBidCandle = validMinuteCandles.MaxBy(x => x.BidHigh);
                marketData.AllTimeHighYes_Bid = highestBidCandle != null ? (highestBidCandle.BidHigh, highestBidCandle.Date) : default;

                var lowestBidCandle = validMinuteCandles.MinBy(x => x.BidLow);
                marketData.AllTimeLowYes_Bid = lowestBidCandle != null ? (lowestBidCandle.BidLow, lowestBidCandle.Date) : default;

                // Calculate recent (3-month) highs and lows
                DateTime recentCutoff = DateTime.UtcNow.AddMonths(-3);
                var recentCandles = validMinuteCandles.Where(x => x.Date >= recentCutoff).ToList();

                if (recentCandles.Any())
                {
                    _logger.LogDebug("Calculating recent statistics for {MarketTicker} using {Count} recent candles",
                        marketTicker, recentCandles.Count);

                    highestBidCandle = recentCandles.MaxBy(x => x.BidHigh);
                    marketData.RecentHighYes_Bid = (highestBidCandle.BidHigh, highestBidCandle.Date);

                    lowestBidCandle = recentCandles.MinBy(x => x.BidLow);
                    marketData.RecentLowYes_Bid = (lowestBidCandle.BidLow, lowestBidCandle.Date);
                }

                _logger.LogDebug("Calculating volume statistics for {MarketTicker}", marketTicker);

                // Calculate volume statistics for different time periods
                recentCutoff = DateTime.UtcNow.AddMonths(-1);
                marketData.RecentVolume_LastMonth = validMinuteCandles
                    .Where(x => x.Date >= recentCutoff)
                    .Sum(x => x.Volume);

                recentCutoff = DateTime.UtcNow.AddHours(-3);
                marketData.RecentVolume_LastThreeHours = validMinuteCandles
                    .Where(x => x.Date >= recentCutoff)
                    .Sum(x => x.Volume);

                recentCutoff = DateTime.UtcNow.AddHours(-1);
                marketData.RecentVolume_LastHour = validMinuteCandles
                    .Where(x => x.Date >= recentCutoff)
                    .Sum(x => x.Volume);

                // Calculate highest volumes for different intervals
                var validHourCandles = marketData.Candlesticks["hour"]
                    .SkipWhile(x => x.Volume == 0)
                    .ToList();
                var validDayCandles = marketData.Candlesticks["day"]
                    .SkipWhile(x => x.Volume == 0)
                    .ToList();

                marketData.HighestVolume_Day = validDayCandles.Any() ? validDayCandles.Max(x => x.Volume) : 0;
                marketData.HighestVolume_Hour = validHourCandles.Any() ? validHourCandles.Max(x => x.Volume) : 0;
                marketData.HighestVolume_Minute = validMinuteCandles.Any() ? validMinuteCandles.Max(x => x.Volume) : 0;

                // Refresh tickers if WebSocket events are being stored
                if (_loggingConfig.StoreWebSocketEvents)
                {
                    _logger.LogDebug("Refreshing tickers for {MarketTicker}", marketTicker);
                    await RefreshTickersFromData(marketTicker);
                }

                // Final metadata refresh
                _logger.LogDebug("Refreshing metadata for {MarketTicker}", marketTicker);
                await marketData.RefreshAllMetadata();
                _logger.LogInformation("Completed market data population for {MarketTicker}", marketTicker);

                stopwatch.Stop();
                LogPerformanceMetric("PopulateMarketDataAsync", stopwatch.ElapsedMilliseconds, $"Market: {marketTicker}");
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                LogPerformanceMetric("PopulateMarketDataAsync", stopwatch.ElapsedMilliseconds, $"Market: {marketTicker} (cancelled)");
                _logger.LogInformation("Market data population was cancelled for {MarketTicker}", marketTicker);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    new CandlestickTransientFailureException(marketTicker
                    , $"Transient failure while populating market data for market {marketTicker}"
                    , ex),
                    "Transient failure while populating market data for market {MarketTicker}. Exception: {Message}, Inner: {Inner}", marketTicker, ex.Message, ex.InnerException?.Message ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to populate market data for {MarketTicker}", marketTicker);
            }
        }

        /// <summary>
        /// Loads and processes candlestick data from both Parquet files and SQL database.
        /// Performs deduplication, forward filling, and data validation before returning
        /// the final processed candlestick list.
        /// </summary>
        /// <param name="existingCandlesticks">Currently loaded candlesticks for this market/interval</param>
        /// <param name="marketTicker">The market ticker identifier</param>
        /// <param name="interval">The time interval (minute/hour/day)</param>
        /// <param name="hardDataStorageLocation">Path to the hard data storage location</param>
        /// <param name="latestExistingDate">Latest date of existing candlesticks</param>
        /// <param name="marketOpenTime">Market open time</param>
        /// <returns>A list of processed and validated candlestick data</returns>
        private async Task<List<CandlestickData>> LoadAndProcessCandlesticksAsync(List<CandlestickData> existingCandlesticks, string marketTicker, string interval, string hardDataStorageLocation, DateTime? latestExistingDate, DateTime marketOpenTime)
        {
            var stopwatch = Stopwatch.StartNew();
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            int intervalType = interval == "minute" ? 1 : interval == "hour" ? 2 : 3;
            string intervalSuffix = interval == "minute" ? "_Minute" : interval == "hour" ? "_Hour" : "_Day";
            var candlesticks = new List<CandlestickData>();
            int parquetFilesLoaded = 0;
            int parquetFilesSkipped = 0;
            int parquetCandlesticksLoaded = 0;
            var skippedFiles = new List<string>();

            // Determine start date: latest existing or marketOpenTime
            DateTime startDate = marketOpenTime;
            if (existingCandlesticks.Any())
            {
                DateTime lastTimestamp = existingCandlesticks.Where(x => x.IntervalType == intervalType).Max(c => c.Date);

                startDate = intervalType switch
                {
                    1 => lastTimestamp.AddDays(-1), // Next minute
                    2 => lastTimestamp.AddDays(-4),   // Next hour
                    3 => lastTimestamp.AddDays(-8),    // Next day
                    _ => marketOpenTime
                };
            }
            else if (latestExistingDate.HasValue && latestExistingDate.Value > startDate)
            {
                startDate = latestExistingDate.Value;
            }

            _logger.LogInformation("Processing candlesticks for {MarketTicker} at {Interval}: StartDate={StartDate}Z, ExistingCount={ExistingCount}",
                marketTicker, interval, startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), existingCandlesticks.Count);

            // Load from Parquet files
            var parquetStopwatch = Stopwatch.StartNew();
            string basePath = Path.Combine(hardDataStorageLocation, _candlestickConfig.CandlestickFolderName, marketTicker);
            var filesToLoad = new List<string>();
            if (Directory.Exists(basePath))
            {
                foreach (var yearDir in Directory.GetDirectories(basePath))
                {
                    var year = int.Parse(Path.GetFileName(yearDir));
                    if (year < startDate.Year)
                    {
                        var yearFiles = Directory.GetFiles(yearDir, $"*{intervalSuffix}.parquet", SearchOption.AllDirectories);
                        parquetFilesSkipped += yearFiles.Length;
                        skippedFiles.AddRange(yearFiles);
                        continue;
                    }

                    if (interval == "day")
                    {
                        var dayFiles = Directory.GetFiles(yearDir, $"*{intervalSuffix}.parquet");
                        foreach (var file in dayFiles)
                        {
                            var fileMonth = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[0]);
                            if (year == startDate.Year && fileMonth < startDate.Month)
                            {
                                parquetFilesSkipped++;
                                skippedFiles.Add(file);
                                continue;
                            }
                            filesToLoad.Add(file);
                        }
                    }
                    else // minute or hour (both use weekly grouping)
                    {
                        foreach (var monthDir in Directory.GetDirectories(yearDir))
                        {
                            var month = int.Parse(Path.GetFileName(monthDir));
                            if (year == startDate.Year && month < startDate.Month)
                            {
                                var monthFiles = Directory.GetFiles(monthDir, $"*Week*{intervalSuffix}.parquet");
                                parquetFilesSkipped += monthFiles.Length;
                                skippedFiles.AddRange(monthFiles);
                                continue;
                            }
                            var weekFiles = Directory.GetFiles(monthDir, $"*Week*{intervalSuffix}.parquet");
                            foreach (var file in weekFiles)
                            {
                                // Check if the week is before startDate
                                var fileName = Path.GetFileNameWithoutExtension(file);
                                var weekPart = fileName.Split('_')[0]; // WeekN
                                var weekNumber = int.Parse(weekPart.Replace("Week", ""));
                                var startOfWeek = new DateTime(year, month, (weekNumber - 1) * 7 + 1);
                                if (year == startDate.Year && month == startDate.Month && startOfWeek < startDate.Date)
                                {
                                    parquetFilesSkipped++;
                                    skippedFiles.Add(file);
                                    continue;
                                }
                                filesToLoad.Add(file);
                            }
                        }
                    }
                }
            }

            // Load files concurrently in batches
            var loadTasks = new List<Task<List<CandlestickData>>>();
            using var loadSemaphore = new SemaphoreSlim(_candlestickConfig.MaxParallelParquetTasks);
            foreach (var file in filesToLoad)
            {
                loadTasks.Add(Task.Run(async () =>
                {
                    await loadSemaphore.WaitAsync(_statusTracker.GetCancellationToken());
                    try
                    {
                        return await LoadFromParquetAsync(file);
                    }
                    finally
                    {
                        loadSemaphore.Release();
                    }
                }, _statusTracker.GetCancellationToken()));
            }

            var loadResults = await Task.WhenAll(loadTasks);
            foreach (var result in loadResults)
            {
                candlesticks.AddRange(result);
                parquetCandlesticksLoaded += result.Count;
                parquetFilesLoaded++;
            }

            // Filter and deduplicate Parquet candlesticks
            int preFilterCount = candlesticks.Count;
            var dedupedParquet = new Dictionary<DateTime, CandlestickData>();
            foreach (var c in candlesticks)
            {
                if (c.IntervalType == intervalType && c.Date >= startDate)
                {
                    if (!dedupedParquet.ContainsKey(c.Date))
                    {
                        dedupedParquet[c.Date] = c;
                    }
                }
            }
            candlesticks = dedupedParquet.Values.OrderBy(c => c.Date).ToList();
            _logger.LogDebug("Parquet processing for {MarketTicker} at {Interval}: FilesLoaded={FilesLoaded}, FilesSkipped={FilesSkipped}, CandlesticksLoaded={CandlesticksLoaded}, CandlesticksFiltered={CandlesticksFiltered}, CandlesticksAfterDeduplication={FinalCount}, SkippedFiles=[{SkippedFiles}]",
                marketTicker, interval, parquetFilesLoaded, parquetFilesSkipped, parquetCandlesticksLoaded, preFilterCount - candlesticks.Count, candlesticks.Count, string.Join(";", skippedFiles));
            parquetStopwatch.Stop();
            LogPerformanceMetric("LoadParquetFiles", parquetStopwatch.ElapsedMilliseconds, $"Market: {marketTicker}, Interval: {interval}, Files: {parquetFilesLoaded}, Candlesticks: {candlesticks.Count}");

            // Load SQL candlesticks
            DateTime sqlStartTime = candlesticks.Any() ? candlesticks.Max(c => c.Date) : startDate;
            // Adjust sqlStartTime to the next interval boundary to avoid overlap
            sqlStartTime = intervalType switch
            {
                1 => sqlStartTime.AddMinutes(1).Date.AddHours(sqlStartTime.Hour).AddMinutes(sqlStartTime.Minute + 1), // Next minute
                2 => sqlStartTime.AddHours(1).Date.AddHours(sqlStartTime.Hour + 1), // Next hour
                3 => sqlStartTime.AddDays(1).Date, // Next day
                _ => sqlStartTime
            };

            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

            var rawCandlesticks = await context.RetrieveCandlesticksAsync(
                _statusTracker.GetCancellationToken(), intervalType, marketTicker, sqlStartTime);

            _logger.LogInformation("Loaded {SqlCount} candlesticks from SQL for {MarketTicker} at {Interval} starting from {StartTime}",
                rawCandlesticks.Count(), marketTicker, interval, sqlStartTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            // Deduplicate SQL candlesticks
            var dedupedSql = new Dictionary<DateTime, CandlestickData>();
            foreach (var c in rawCandlesticks)
            {
                if (!dedupedSql.ContainsKey(c.Date))
                {
                    dedupedSql[c.Date] = c;
                }
            }
            rawCandlesticks = dedupedSql.Values.OrderBy(c => c.Date).ToList();

            // Insert last candlestick of Parquet data as the starting point for forward fill
            int preForwardFillCount = rawCandlesticks.Count;
            bool candlestickAdded = false;
            if (candlesticks.Any())
            {
                var lastParquetCandlestick = candlesticks.Last();
                // Only add if it doesn't overlap with rawCandlesticks
                if (!rawCandlesticks.Any(r => r.Date == lastParquetCandlestick.Date))
                {
                    rawCandlesticks.Insert(0, lastParquetCandlestick);
                    candlestickAdded = true;
                }
            }

            // Forward fill new candlesticks
            _logger.LogInformation("Forward filling {SqlCount} candlesticks for {MarketTicker} at {Interval}",
                rawCandlesticks.Count(), marketTicker, interval);
            rawCandlesticks = _serviceFactory.GetMarketDataService().ForwardFillCandlesticks(rawCandlesticks, marketTicker);
            // Remove the extra candlestick if added
            if (candlestickAdded)
            {
                rawCandlesticks.RemoveAt(0);
            }

            // Deduplicate after forward fill
            var dedupedForwardFill = new Dictionary<DateTime, CandlestickData>();
            foreach (var c in rawCandlesticks)
            {
                if (!dedupedForwardFill.ContainsKey(c.Date))
                {
                    dedupedForwardFill[c.Date] = c;
                }
            }
            rawCandlesticks = dedupedForwardFill.Values.OrderBy(c => c.Date).ToList();

            int forwardFilledCount = rawCandlesticks.Count - preForwardFillCount;
            // Combine Parquet and forward-filled SQL candlesticks, deduplicating
            var combinedDeduped = new Dictionary<DateTime, CandlestickData>();
            foreach (var c in candlesticks.Concat(rawCandlesticks))
            {
                if (!combinedDeduped.ContainsKey(c.Date))
                {
                    combinedDeduped[c.Date] = c;
                }
            }
            var combinedCandlesticks = combinedDeduped.Values.OrderBy(c => c.Date).ToList();

            // Combine with existing candlesticks, ensuring no duplicates
            var finalDeduped = new Dictionary<DateTime, CandlestickData>();
            foreach (var c in existingCandlesticks.Where(c => c.Date < startDate).Concat(combinedCandlesticks))
            {
                if (!finalDeduped.ContainsKey(c.Date))
                {
                    finalDeduped[c.Date] = c;
                }
            }
            var finalCandlesticks = finalDeduped.Values.OrderBy(c => c.Date).ToList();

            _logger.LogDebug("Processed candlesticks for {MarketTicker} at {Interval}: ForwardFilledAdded={ForwardFilledCount}, TotalAfterFill={TotalCount}",
                marketTicker, interval, forwardFilledCount, finalCandlesticks.Count);

            _logger.LogDebug("Saving Previous Periods To Parquets for {interval}", intervalType);

            // Save new forward-filled candlesticks to Parquet
            try
            {
                await SavePreviousPeriodsToParquetAsync(marketTicker, interval, finalCandlesticks.Where(c => c.Date >= startDate).ToList(), hardDataStorageLocation);

                // Log final summary
                _logger.LogDebug("Completed candlestick processing for {MarketTicker} at {Interval}: ParquetFilesLoaded={ParquetFilesLoaded}, ParquetFilesSkipped={ParquetFilesSkipped}, ParquetCandlesticksLoaded={ParquetCandlesticksLoaded}, SqlCandlesticksLoaded={SqlCount}, ForwardFilledAdded={ForwardFilledCount}, TotalCandlesticks={TotalCount}",
                    marketTicker, interval, parquetFilesLoaded, parquetFilesSkipped, parquetCandlesticksLoaded, rawCandlesticks.Count(), forwardFilledCount, finalCandlesticks.Count);

            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed candlestick processing for {MarketTicker} at {Interval}: ParquetFilesLoaded={ParquetFilesLoaded}, ParquetFilesSkipped={ParquetFilesSkipped}, ParquetCandlesticksLoaded={ParquetCandlesticksLoaded}, SqlCandlesticksLoaded={SqlCount}, ForwardFilledAdded={ForwardFilledCount}, TotalCandlesticks={TotalCount}, Exception: {0}, Inner: {1}",
                              marketTicker, interval, parquetFilesLoaded, parquetFilesSkipped,
                              parquetCandlesticksLoaded, rawCandlesticks.Count(), forwardFilledCount,
                              finalCandlesticks.Count, ex.Message, ex.InnerException?.Message);

            }

            stopwatch.Stop();
            LogPerformanceMetric("LoadAndProcessCandlesticksAsync", stopwatch.ElapsedMilliseconds,
                $"Market: {marketTicker}, Interval: {interval}, Loaded: {finalCandlesticks.Count}");

            return finalCandlesticks;
        }

        /// <summary>
        /// Saves historical candlestick data to organized Parquet files based on time intervals.
        /// Groups data by appropriate time periods (daily for minutes, weekly for hours, monthly for days)
        /// and saves to structured directory hierarchy for efficient storage and retrieval.
        /// </summary>
        /// <param name="marketTicker">The market ticker identifier</param>
        /// <param name="interval">The time interval (minute/hour/day)</param>
        /// <param name="candlesticks">The candlestick data to save</param>
        /// <param name="hardDataStorageLocation">Base path for data storage</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        private async Task SavePreviousPeriodsToParquetAsync(string marketTicker, string interval, List<CandlestickData> candlesticks, string hardDataStorageLocation)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            int intervalType = interval == "minute" ? 1 : interval == "hour" ? 2 : 3;
            string intervalSuffix = interval == "minute" ? "_Minute" : interval == "hour" ? "_Hour" : "_Day";

            // Filter out future data and data that is not of the current interval type
            var relevantCandlesticks = candlesticks
                .Where(c => c.Date.Date < DateTime.UtcNow.Date && c.IntervalType == intervalType)
                .ToList();

            IEnumerable<IGrouping<string, CandlestickData>> groups = interval switch
            {
                "day" => relevantCandlesticks.GroupBy(c => $"{c.Date.Year}-{c.Date.Month:D2}"),
                "hour" or "minute" => relevantCandlesticks.GroupBy(c =>
                {
                    var dayOfMonth = c.Date.Day;
                    var weekNumber = (dayOfMonth - 1) / 7 + 1; // 1-based week number (1-5)
                    return $"{c.Date.Year}-{c.Date.Month:D2}-Week{weekNumber}";
                }),
                _ => throw new ArgumentException($"Unsupported interval: {interval}")
            };

            var saveTasks = groups.Select(async group =>
            {
                var key = group.Key;
                var groupCandles = group.ToList();

                if (!groupCandles.Any()) return;

                string filePath;
                if (interval == "day")
                {
                    // Save in year folder: HardDataStorageLocation\CandlestickFolderName\MarketTicker\Year\MM_Day.parquet
                    var parts = key.Split('-');
                    filePath = Path.Combine(
                        hardDataStorageLocation,
                        _candlestickConfig.CandlestickFolderName,
                        marketTicker,
                        parts[0], // Year
                        $"{parts[1]}{intervalSuffix}.parquet" // Month_Day.parquet
                    );
                }
                else // hour or minute (both use weekly grouping)
                {
                    // Save in month folder: HardDataStorageLocation\CandlestickFolderName\MarketTicker\Year\Month\WeekN_Hour/Minute.parquet
                    var parts = key.Split('-');
                    filePath = Path.Combine(
                        hardDataStorageLocation,
                        _candlestickConfig.CandlestickFolderName,
                        marketTicker,
                        parts[0], // Year
                        parts[1], // Month
                        $"{parts[2]}{intervalSuffix}.parquet" // WeekN_Hour/Minute.parquet
                    );
                }

                // Check if file already exists
                if (File.Exists(filePath))
                {
                    return;
                }

                // Create directory if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // Save to parquet
                await SaveToParquetAsync(groupCandles, filePath);
                _logger.LogDebug("Saved {Count} candlesticks to {FilePath} for {Interval}", groupCandles.Count, filePath, interval);
            });

            await Task.WhenAll(saveTasks);
        }

        /// <summary>
        /// Refreshes ticker data for a specific market from the database.
        /// Updates the market data cache with the latest ticker information ordered by date.
        /// </summary>
        /// <param name="marketTicker">The market ticker identifier to refresh tickers for</param>
        /// <returns>A task representing the asynchronous refresh operation</returns>
        private async Task RefreshTickersFromData(string marketTicker)
        {
            _statusTracker.GetCancellationToken().ThrowIfCancellationRequested();
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
            if (!_serviceFactory.GetDataCache().Markets.TryGetValue(marketTicker, out var marketData))
                return;

            var tickers = await context.GetTickers(marketTicker: marketTicker);
            marketData.Tickers = new ConcurrentBag<TickerDTO>(tickers.OrderByDescending(t => t.LoggedDate));
        }

        /// <summary>
        /// Saves candlestick data to a Parquet file with retry logic for robustness.
        /// Creates the Parquet schema and writes data in columnar format for efficient storage.
        /// </summary>
        /// <param name="data">The candlestick data to save</param>
        /// <param name="filePath">The file path where the Parquet file should be saved</param>
        /// <returns>A task representing the asynchronous save operation</returns>
        private async Task SaveToParquetAsync(List<CandlestickData> data, string filePath)
        {
            if (!data.Any()) return;

            const int maxAttempts = 3;
            int attempt = 0;
            bool success = false;

            // Define the schema for the Parquet file
            var schema = new ParquetSchema(
                new DataField<DateTime>("Date"),
                new DataField<string>("MarketTicker"),
                new DataField<int>("IntervalType"),
                new DataField<int>("OpenInterest"),
                new DataField<int>("Volume"),
                new DataField<int>("AskOpen"),
                new DataField<int>("AskHigh"),
                new DataField<int>("AskLow"),
                new DataField<int>("AskClose"),
                new DataField<int>("BidOpen"),
                new DataField<int>("BidHigh"),
                new DataField<int>("BidLow"),
                new DataField<int>("BidClose")
            );

            while (attempt < maxAttempts && !success)
            {
                attempt++;
                try
                {
                    using (Stream fileStream = File.Create(filePath))
                    {
                        using (var writer = await ParquetWriter.CreateAsync(schema, fileStream))
                        {
                            using (var groupWriter = writer.CreateRowGroup())
                            {
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "Date"), data.Select(d => d.Date).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "MarketTicker"), data.Select(d => d.MarketTicker).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "IntervalType"), data.Select(d => d.IntervalType).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "OpenInterest"), data.Select(d => d.OpenInterest).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "Volume"), data.Select(d => d.Volume).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "AskOpen"), data.Select(d => d.AskOpen).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "AskHigh"), data.Select(d => d.AskHigh).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "AskLow"), data.Select(d => d.AskLow).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "AskClose"), data.Select(d => d.AskClose).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "BidOpen"), data.Select(d => d.BidOpen).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "BidHigh"), data.Select(d => d.BidHigh).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "BidLow"), data.Select(d => d.BidLow).ToArray()));
                                await groupWriter.WriteColumnAsync(new DataColumn((DataField)schema.Fields.First(f => f.Name == "BidClose"), data.Select(d => d.BidClose).ToArray()));
                            }
                        }
                    }

                    success = true;
                    _logger.LogDebug("Saved {Count} candlesticks to {FilePath}", data.Count, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Attempt {Attempt} failed to save Parquet file: {Message}, Inner: {Inner}", attempt, ex.Message, ex.InnerException?.Message ?? "None");
                    if (attempt < maxAttempts && File.Exists(filePath))
                    {
                        await Task.Run(() => File.Delete(filePath));
                        _logger.LogDebug("Deleted potentially corrupted file: {FilePath}", filePath);
                    }
                }
            }

            if (!success)
            {
                _logger.LogError("Failed to save valid Parquet file after {MaxAttempts} attempts to {FilePath}", maxAttempts, filePath);
            }
        }

        /// <summary>
        /// Loads candlestick data from a Parquet file and converts it to CandlestickData objects.
        /// Handles file reading errors gracefully by deleting corrupted files and returning empty data.
        /// </summary>
        /// <param name="filePath">The path to the Parquet file to load</param>
        /// <returns>A list of candlestick data loaded from the file</returns>
        private async Task<List<CandlestickData>> LoadFromParquetAsync(string filePath)
        {
            var candlestickData = new List<CandlestickData>();
            if (!File.Exists(filePath)) return candlestickData;

            try
            {
                using (Stream fileStream = File.OpenRead(filePath))
                {
                    using (var reader = await ParquetReader.CreateAsync(fileStream))
                    {
                        var schema = reader.Schema;

                        // Create a dictionary to quickly map column names to their DataFields
                        var fieldMap = schema.Fields.ToDictionary(f => f.Name, f => f as DataField);

                        for (int i = 0; i < reader.RowGroupCount; i++)
                        {
                            using (var groupReader = reader.OpenRowGroupReader(i))
                            {
                                // Read columns using the mapped DataFields
                                var dateColumn = await groupReader.ReadColumnAsync(fieldMap["Date"]);
                                var marketTickerColumn = await groupReader.ReadColumnAsync(fieldMap["MarketTicker"]);
                                var intervalTypeColumn = await groupReader.ReadColumnAsync(fieldMap["IntervalType"]);
                                var openInterestColumn = await groupReader.ReadColumnAsync(fieldMap["OpenInterest"]);
                                var volumeColumn = await groupReader.ReadColumnAsync(fieldMap["Volume"]);
                                var askOpenColumn = await groupReader.ReadColumnAsync(fieldMap["AskOpen"]);
                                var askHighColumn = await groupReader.ReadColumnAsync(fieldMap["AskHigh"]);
                                var askLowColumn = await groupReader.ReadColumnAsync(fieldMap["AskLow"]);
                                var askCloseColumn = await groupReader.ReadColumnAsync(fieldMap["AskClose"]);
                                var bidOpenColumn = await groupReader.ReadColumnAsync(fieldMap["BidOpen"]);
                                var bidHighColumn = await groupReader.ReadColumnAsync(fieldMap["BidHigh"]);
                                var bidLowColumn = await groupReader.ReadColumnAsync(fieldMap["BidLow"]);
                                var bidCloseColumn = await groupReader.ReadColumnAsync(fieldMap["BidClose"]);

                                long numRows = dateColumn.Data.Length;

                                for (int j = 0; j < numRows; j++)
                                {
                                    candlestickData.Add(new CandlestickData
                                    {
                                        Date = (DateTime)dateColumn.Data.GetValue(j),
                                        MarketTicker = (string)marketTickerColumn.Data.GetValue(j),
                                        IntervalType = (int)intervalTypeColumn.Data.GetValue(j),
                                        OpenInterest = (int)openInterestColumn.Data.GetValue(j),
                                        Volume = (int)volumeColumn.Data.GetValue(j),
                                        AskOpen = (int)askOpenColumn.Data.GetValue(j),
                                        AskHigh = (int)askHighColumn.Data.GetValue(j),
                                        AskLow = (int)askLowColumn.Data.GetValue(j),
                                        AskClose = (int)askCloseColumn.Data.GetValue(j),
                                        BidOpen = (int)bidOpenColumn.Data.GetValue(j),
                                        BidHigh = (int)bidHighColumn.Data.GetValue(j),
                                        BidLow = (int)bidLowColumn.Data.GetValue(j),
                                        BidClose = (int)bidCloseColumn.Data.GetValue(j)
                                    });
                                }
                            }
                        }
                    }
                }
                _logger.LogDebug("Loaded {Count} candlesticks from {FilePath}", candlestickData.Count, filePath);
                return candlestickData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error reading Parquet file {FilePath}: {Message}, Inner: {Inner}", filePath, ex.Message, ex.InnerException?.Message ?? "None");
                try
                {
                    if (File.Exists(filePath))
                    {
                        await Task.Run(() => File.Delete(filePath));
                        _logger.LogDebug("Deleted corrupted file: {FilePath}", filePath);
                    }
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Error deleting corrupted file {FilePath}: {Message}", filePath, deleteEx.Message);
                }
                return new List<CandlestickData>();
            }
        }

        /// <summary>
        /// Records execution time metrics using the IPerformanceMonitor interface.
        /// </summary>
        /// <param name="operationName">Name of the operation being timed</param>
        /// <param name="executionTimeMs">Execution time in milliseconds</param>
        /// <param name="enableMetrics">Whether performance metrics are enabled</param>
        private void RecordExecutionTimePrivate(string operationName, long executionTimeMs, bool enableMetrics)
        {
            string className = "CandlestickService";
            string category = "CandlestickOperations";

            if (!enableMetrics)
            {
                // Send disabled metric
                _performanceMonitor?.RecordDisabledMetric(className, operationName, $"{operationName} Execution Time", $"Execution time for {operationName}", executionTimeMs, "ms", category, false);
            }
            else
            {
                // Record actual metric
                _performanceMonitor?.RecordSpeedDialMetric(className, operationName, $"{operationName} Execution Time", $"Execution time for {operationName}", executionTimeMs, "ms", category, null, null, null, true);
            }
        }
    }
}
