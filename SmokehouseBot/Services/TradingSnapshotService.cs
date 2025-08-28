using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using SmokehouseBot.Exceptions;
using SmokehouseBot.Helpers;
using SmokehouseBot.Services.Interfaces;
using SmokehouseDTOs;
using SmokehouseDTOs.Converters;
using SmokehouseDTOs.Data;
using SmokehouseInterfaces.Constants;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TradingStrategies.Configuration;

namespace SmokehouseBot.Services
{
    public class TradingSnapshotService : ITradingSnapshotService
    {
        private readonly ILogger<ITradingSnapshotService> _logger;
        private readonly IOptions<SnapshotConfig> _snapshotConfig;
        private readonly IOptions<TradingConfig> _tradingConfig;
        private DateTime? _lastSnapshotTimestamp; // Actual timestamp of the last saved snapshot
        private DateTime? _nextExpectedSnapshotTimestamp; // The timestamp when the next snapshot is ideally expected
        private bool _isFirstSnapshot = true;
        private readonly TimeSpan _expectedInterval;
        private readonly TimeSpan _tolerance;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _snapshotDirectory = @"\\DESKTOP-ITC50UT\SmokehouseDataStorage\NewSnapshots"; // Hardcoded directory path

        public TradingSnapshotService(
            ILogger<ITradingSnapshotService> logger,
            IOptions<SnapshotConfig> snapshotConfig,
            IOptions<TradingConfig> tradingConfig,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _snapshotConfig = snapshotConfig;
            _tradingConfig = tradingConfig;
            _scopeFactory = scopeFactory;
            _expectedInterval = TimeSpan.FromSeconds(tradingConfig.Value.DecisionFrequencySeconds);
            _tolerance = TimeSpan.FromSeconds(snapshotConfig.Value.SnapshotToleranceSeconds);

            // Ensure the directory exists
            if (!Directory.Exists(_snapshotDirectory))
            {
                Directory.CreateDirectory(_snapshotDirectory);
            }
        }

        public async Task<int> SaveSnapshotAsync(string BrainInstance, CacheSnapshot cacheSnapshot)
        {
            try
            {
                var timestamp = cacheSnapshot.Timestamp;
                var timestampString = timestamp.ToString("yyyyMMddTHHmmssZ");

                var discardThreshold = _expectedInterval + TimeSpan.FromSeconds(_snapshotConfig.Value.SnapshotToleranceSeconds * 2);

                if (_isFirstSnapshot)
                {
                    _nextExpectedSnapshotTimestamp = timestamp + _expectedInterval;
                }
                else if (_lastSnapshotTimestamp.HasValue)
                {
                    if (_nextExpectedSnapshotTimestamp.HasValue && timestamp > _nextExpectedSnapshotTimestamp.Value + discardThreshold)
                    {
                        _logger.LogWarning(
                            "BRAIN: Skipping extremely late snapshot: {CurrentTimestamp} is {TimeSinceExpected} seconds " +
                            "after its expected time of {ExpectedTimestamp} (tolerance {DiscardThresholdSeconds}s). Discarding.",
                            timestampString, (timestamp - _nextExpectedSnapshotTimestamp.Value).TotalSeconds,
                            _nextExpectedSnapshotTimestamp.Value.ToString("yyyyMMddTHHmmssZ"), discardThreshold.TotalSeconds);
                        return 0;
                    }

                    var actualTimeSinceLastSnapshot = timestamp - _lastSnapshotTimestamp.Value;
                    var minInterval = _expectedInterval - _tolerance;
                    var maxInterval = _expectedInterval + _tolerance;

                    if (actualTimeSinceLastSnapshot < minInterval || actualTimeSinceLastSnapshot > maxInterval)
                    {
                        _logger.LogWarning(
                            "Snapshot timing irregularity detected: {CurrentTimestamp} is {TimeSinceLastSnapshot} seconds " +
                            "after {LastTimestamp}, expected approximately {ExpectedInterval} seconds",
                            timestampString, actualTimeSinceLastSnapshot.TotalSeconds,
                            _lastSnapshotTimestamp.Value.ToString("yyyyMMddTHHmmssZ"), _expectedInterval.TotalSeconds);
                    }
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    Converters = { new SimplifiedTupleConverter(), new ShortIsoDateTimeConverter(), new OrderbookSlimConverter(), new MarketSnapshotSlimConverter() }
                };

                var schema = JsonSchema.FromType<CacheSnapshot>();
                var schemaData = schema.ToJson();

                int SavedCount = 0;

                if (cacheSnapshot.LastWebSocketTimestamp <= cacheSnapshot.Timestamp.AddMinutes(-10) && cacheSnapshot.Markets.Count > 5)
                {
                    _logger.LogWarning(new Exception($"Last web socket time is too old. {cacheSnapshot.LastWebSocketTimestamp} vs {cacheSnapshot.Timestamp}. Market skipped."), "Last web socket time is too old. {LastWebSocketTimestamp} vs {CacheTimestamp}. Market skipped.",
                        cacheSnapshot.LastWebSocketTimestamp, cacheSnapshot.Timestamp);
                }
                else
                {
                    var snapshotsToSave = new List<object>();
                    foreach (var marketSnapshot in cacheSnapshot.Markets)
                    {
                        if (KalshiConstants.MarketIsEnded(marketSnapshot.Value.MarketStatus))
                        {
                            _logger.LogInformation("Market {market} has ended so snapshot was skipped.", marketSnapshot.Key);
                            continue;
                        }
                        if ((marketSnapshot.Value.BestNoBid == 0 && marketSnapshot.Value.BestYesBid == 0)
                            || marketSnapshot.Value.OrderbookData == null || marketSnapshot.Value.OrderbookData.Count == 0)
                        {
                            _logger.LogInformation("Market {market} prices have not been loaded yet.", marketSnapshot.Key);
                            continue;
                        }

                        if (!SnapshotIsValid(marketSnapshot.Value))
                        {
                            continue;
                        }

                        var rawJson = JsonSerializer.Serialize(marketSnapshot.Value, options);

                        var snapshotData = new
                        {
                            MarketTicker = marketSnapshot.Value.MarketTicker,
                            SnapshotDate = timestamp,
                            JSONSchemaVersion = _snapshotConfig.Value.SnapshotSchemaVersion,
                            PositionSize = marketSnapshot.Value.PositionSize,
                            ChangeMetricsMature = marketSnapshot.Value.ChangeMetricsMature,
                            VelocityPerMinute_Top_Yes_Bid = marketSnapshot.Value.VelocityPerMinute_Top_Yes_Bid,
                            VelocityPerMinute_Top_No_Bid = marketSnapshot.Value.VelocityPerMinute_Top_No_Bid,
                            VelocityPerMinute_Bottom_Yes_Bid = marketSnapshot.Value.VelocityPerMinute_Bottom_Yes_Bid,
                            VelocityPerMinute_Bottom_No_Bid = marketSnapshot.Value.VelocityPerMinute_Bottom_No_Bid,
                            OrderVolume_Yes_Bid = marketSnapshot.Value.OrderVolumePerMinute_YesBid,
                            OrderVolume_No_Bid = marketSnapshot.Value.OrderVolumePerMinute_NoBid,
                            TradeVolume_Yes = marketSnapshot.Value.TradeVolumePerMinute_Yes,
                            TradeVolume_No = marketSnapshot.Value.TradeVolumePerMinute_No,
                            AverageTradeSize_Yes = marketSnapshot.Value.AverageTradeSize_Yes,
                            AverageTradeSize_No = marketSnapshot.Value.AverageTradeSize_No,
                            RawJSON = rawJson,
                            MarketTypeID = (int?)null,
                            IsValidated = false,
                            BrainInstance = BrainInstance
                        };

                        snapshotsToSave.Add(snapshotData);
                        SavedCount += 1;
                    }

                    if (snapshotsToSave.Any())
                    {
                        var fileJson = JsonSerializer.Serialize(snapshotsToSave, options);
                        var fileName = $"SnapshotGroup_{timestampString}.json";
                        var fullPath = Path.Combine(_snapshotDirectory, fileName);
                        await File.WriteAllTextAsync(fullPath, fileJson, Encoding.Unicode);
                        _logger.LogInformation("Saved {Count} snapshots to file: {FilePath}", SavedCount, fullPath);
                    }

                    _lastSnapshotTimestamp = timestamp;
                    _nextExpectedSnapshotTimestamp = timestamp + _expectedInterval;
                    _isFirstSnapshot = false;
                }

                return SavedCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error saving snapshot at {Timestamp}", cacheSnapshot.Timestamp);
                return 0;
            }
        }


        // Updated LoadManySnapshots method in TradingSnapshotService.cs with parallelization
        public async Task<Dictionary<string, List<MarketSnapshot>>> LoadManySnapshots(List<SnapshotDTO> snapshots, bool forceLoad = false)
        {
            bool SchemaMatches = await CheckSchemaMatches();

            if (!forceLoad && !SchemaMatches)
            {
                _logger.LogWarning("Schema mismatch detected while deserializing snapshots");
                return new Dictionary<string, List<MarketSnapshot>>();
            }
            try
            {
                var result = new Dictionary<string, List<MarketSnapshot>>();
                var options = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    Converters = { new SimplifiedTupleConverter(), new ShortIsoDateTimeConverter(), new OrderbookSlimConverter(), new MarketSnapshotSlimConverter() }
                };

                // Group snapshots by MarketTicker
                var groupedSnapshots = snapshots
                    .GroupBy(s => s.MarketTicker)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Process each market group in parallel
                Parallel.ForEach(groupedSnapshots, group =>
                {
                    var marketTicker = group.Key;
                    var newCacheSnapshots = new List<MarketSnapshot>(group.Value.Count);  // Pre-allocate

                    // Process snapshots for this group sequentially (order matters), but groups in parallel
                    foreach (var snapshot in group.Value)
                    {
                        try
                        {
                            string safeJSON = snapshot.RawJSON;
                            var cacheSnapshot = JsonSerializer.Deserialize<MarketSnapshot>(safeJSON, options);

                            if (cacheSnapshot != null)
                            {
                                newCacheSnapshots.Add(cacheSnapshot);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to deserialize snapshot for {MarketTicker} at {Timestamp}",
                                    marketTicker, snapshot.SnapshotDate);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error deserializing snapshot for {MarketTicker} at {Timestamp}",
                                marketTicker, snapshot.SnapshotDate);
                        }
                    }

                    // Upgrade in batch
                    foreach (var cacheSnapshot in newCacheSnapshots)
                    {
                        cacheSnapshot.UpgradeSnapshot(_snapshotConfig.Value.SnapshotSchemaVersion);
                    }

                    lock (result)  // Sync access to result dict
                    {
                        if (newCacheSnapshots.Any())
                        {
                            result[marketTicker] = newCacheSnapshots;
                            _logger.LogDebug("Loaded {Count} snapshots for {MarketTicker}", newCacheSnapshots.Count, marketTicker);
                        }
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading multiple snapshots");
                return new Dictionary<string, List<MarketSnapshot>>();
            }
        }

        public bool SnapshotIsValid(MarketSnapshot marketSnapshot)
        {
            bool hasValidMarket = false;

            var result = SnapshotDiscrepancyValidator.ValidateDiscrepancies(marketSnapshot);
            if (result.IsValid)
            {
                hasValidMarket = true;
            }
            else
            {
                var logMessage = $"Snapshot for {marketSnapshot.MarketTicker} is invalid.";
                if (result.IsOrderbookMissing)
                {
                    logMessage += $" IsOrderbookMissing: {result.IsOrderbookMissing} (OrderbookData: {(marketSnapshot.OrderbookData == null ? "null" : marketSnapshot.OrderbookData.Any() ? "present" : "empty")})";
                }
                if (result.DoPricesOverlap)
                {
                    logMessage += $" DoPricesOverlap: {result.DoPricesOverlap} (BestYesBid: {marketSnapshot.BestYesBid}, BestYesAsk: {marketSnapshot.BestYesAsk})";
                }
                if (result.IsRateDiscrepancy)
                {
                    logMessage += $" IsRateDiscrepancy: {result.IsRateDiscrepancy} (" +
                                  $"VelocityYesTop: {marketSnapshot.VelocityPerMinute_Top_Yes_Bid}, " +
                                  $"VelocityYesBottom: {marketSnapshot.VelocityPerMinute_Bottom_Yes_Bid}, " +
                                  $"OrderYesVolume: {marketSnapshot.OrderVolumePerMinute_YesBid}, " +
                                  $"TradeYesVolume: {marketSnapshot.TradeVolumePerMinute_Yes}, " +
                                  $"VelocityNoTop: {marketSnapshot.VelocityPerMinute_Top_No_Bid}, " +
                                  $"VelocityNoBottom: {marketSnapshot.VelocityPerMinute_Bottom_No_Bid}, " +
                                  $"OrderNoVolume: {marketSnapshot.OrderVolumePerMinute_NoBid}, " +
                                  $"TradeNoVolume: {marketSnapshot.TradeVolumePerMinute_No})";
                }

                var exception = new SnapshotInvalidException(marketSnapshot.MarketTicker, logMessage);

                if (result.IsOrderbookMissing)
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} is missing its orderbook. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
                else if (result.DoPricesOverlap)
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} has overlapping prices. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
                else if (result.IsRateDiscrepancy)
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} has rate discrepancies. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
                else
                {
                    _logger.LogWarning(exception, "Snapshot for {MarketTicker} is invalid. Details: {Details}",
                        marketSnapshot.MarketTicker, logMessage);
                }
            }

            return hasValidMarket;
        }

        public void ResetLastSnapshot()
        {
            _lastSnapshotTimestamp = null;
            _nextExpectedSnapshotTimestamp = null; // Reset the expected timestamp as well
        }

        public async Task<bool> CheckSchemaMatches()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

            var schema = JsonSchema.FromType<CacheSnapshot>();
            var schemaData = schema.ToJson();

            int expectedVersion = _snapshotConfig.Value.SnapshotSchemaVersion;
            SnapshotSchemaDTO? schemaDTO = await context.GetSnapshotSchema_cached(expectedVersion);
            if (schemaDTO == null)
            {
                throw new Exception($"Schema version {expectedVersion} not found in database");
            }
            return schemaData == schemaDTO.SchemaDefinition;
        }

        /// <summary>
        /// Removes json fields which are no longer relevant
        /// </summary>
        /// <param name="currentVersion"></param>
        /// <param name="JSON"></param>
        /// <returns></returns>
        public string SterilizeJSON(int currentVersion, string JSON)
        {
            using JsonDocument doc = JsonDocument.Parse(JSON);
            var root = doc.RootElement.Clone();
            var markets = root.GetProperty("Markets");

            // Assuming "Markets" is an object with market keys
            var marketEnumerator = markets.EnumerateObject();
            if (marketEnumerator.MoveNext())
            {
                var marketValue = marketEnumerator.Current.Value;

                switch (currentVersion)
                {
                    case 19:
                    case 18:
                        break;
                    case 17:
                        {
                            // Remove properties using writer to rebuild object
                            using var ms = new MemoryStream();
                            using var writer = new Utf8JsonWriter(ms);
                            writer.WriteStartObject();
                            foreach (var prop in marketValue.EnumerateObject())
                            {
                                if (prop.Name != "CurrentPriceYes" && prop.Name != "CurrentPriceNo")
                                {
                                    prop.WriteTo(writer);
                                }
                            }
                            writer.WriteEndObject();
                            writer.Flush();
                            var updatedMarketJson = System.Text.Encoding.UTF8.GetString(ms.ToArray());

                            // Rebuild full JSON
                            using var fullMs = new MemoryStream();
                            using var fullWriter = new Utf8JsonWriter(fullMs);
                            fullWriter.WriteStartObject();
                            foreach (var fullProp in root.EnumerateObject())
                            {
                                if (fullProp.Name == "Markets")
                                {
                                    fullWriter.WritePropertyName("Markets");
                                    fullWriter.WriteRawValue("{" + $"\"{marketEnumerator.Current.Name}\":{updatedMarketJson}" + "}");
                                }
                                else
                                {
                                    fullProp.WriteTo(fullWriter);
                                }
                            }
                            fullWriter.WriteEndObject();
                            fullWriter.Flush();
                            JSON = System.Text.Encoding.UTF8.GetString(fullMs.ToArray());
                        }
                        break;
                    case 16:
                    case 15:
                    case 14:
                        break;
                    case 13:
                        {
                            // Similar rebuilding for removing "RestingOrders"
                            using var ms13 = new MemoryStream();
                            using var writer13 = new Utf8JsonWriter(ms13);
                            writer13.WriteStartObject();
                            foreach (var prop in marketValue.EnumerateObject())
                            {
                                if (prop.Name != "RestingOrders")
                                {
                                    prop.WriteTo(writer13);
                                }
                            }
                            writer13.WriteEndObject();
                            writer13.Flush();
                            var updatedMarketJson13 = System.Text.Encoding.UTF8.GetString(ms13.ToArray());

                            // Rebuild full JSON
                            using var fullMs13 = new MemoryStream();
                            using var fullWriter13 = new Utf8JsonWriter(fullMs13);
                            fullWriter13.WriteStartObject();
                            foreach (var fullProp in root.EnumerateObject())
                            {
                                if (fullProp.Name == "Markets")
                                {
                                    fullWriter13.WritePropertyName("Markets");
                                    fullWriter13.WriteRawValue("{" + $"\"{marketEnumerator.Current.Name}\":{updatedMarketJson13}" + "}");
                                }
                                else
                                {
                                    fullProp.WriteTo(fullWriter13);
                                }
                            }
                            fullWriter13.WriteEndObject();
                            fullWriter13.Flush();
                            JSON = System.Text.Encoding.UTF8.GetString(fullMs13.ToArray());
                        }
                        break;
                    default:
                        break;
                }
            }

            return JSON;
        }
    }
}