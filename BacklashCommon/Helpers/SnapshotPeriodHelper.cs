using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BacklashCommon.Configuration;
using BacklashDTOs;
using BacklashDTOs.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TradingStrategies.Classification.Interfaces;


namespace BacklashCommon.Helpers
{
    /// <summary>
    /// Provides functionality for processing and grouping market snapshots into valid analysis periods.
    /// This class implements the core logic for identifying continuous market periods based on time gaps
    /// and price stability, ensuring that only stable market conditions are used for trading strategy
    /// evaluation and backtesting. It handles the serialization of snapshot groups to disk and their
    /// subsequent loading for analysis. Supports async operations, parallel processing, and progress reporting.
    /// </summary>
    /// <remarks>
    /// The class uses configurable thresholds for time gaps (SmallGapMinutes and MaxActiveGapHours)
    /// and price changes (PriceChangeThreshold) to determine period boundaries. Snapshots are grouped
    /// when they are temporally close and price changes are minimal, allowing for meaningful analysis
    /// of market behavior during stable periods. Configuration is injected via constructor for flexibility.
    /// Async file I/O improves performance with large snapshot groups, parallel processing accelerates
    /// data flattening, and progress reporting enables monitoring of long-running operations.
    /// </remarks>
    public class SnapshotPeriodHelper : ISnapshotPeriodHelper
    {
        private readonly SnapshotPeriodHelperConfig _config;

        /// <summary>
        /// Initializes a new instance of the SnapshotPeriodHelper class with the specified configuration.
        /// </summary>
        /// <param name="config">The snapshot period helper configuration containing thresholds and settings.</param>
        public SnapshotPeriodHelper(SnapshotPeriodHelperConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Splits a sequence of market snapshots into valid analysis periods based on time continuity
        /// and price stability criteria. Snapshots are grouped together when they represent a
        /// continuous market period without significant price changes or excessive time gaps.
        /// </summary>
        /// <param name="snapshots">The ordered list of market snapshots to process. Must be sorted by timestamp.</param>
        /// <param name="snapshotDirectory">The directory path where snapshot group files will be saved.</param>
        /// <param name="progress">Optional progress reporter for long-running operations.</param>
        /// <returns>A task representing the asynchronous operation, containing a list of snapshot groups.</returns>
        /// <remarks>
        /// The method processes snapshots sequentially, starting a new group when:
        /// - Time gaps exceed the maximum active gap threshold, or
        /// - Price changes exceed the threshold (unless within the small gap tolerance)
        ///
        /// Each group is saved as a JSON file and returned as a SnapshotGroupDTO containing
        /// start/end times, price ranges, liquidity metrics, and file path information.
        /// </remarks>
        public async Task<List<SnapshotGroupDTO>> SplitIntoValidGroups(List<SnapshotDTO> snapshots, string snapshotDirectory, IProgress<double>? progress = null)
        {
            // Input validation
            if (snapshots == null)
                throw new ArgumentNullException(nameof(snapshots));
            if (string.IsNullOrWhiteSpace(snapshotDirectory))
                throw new ArgumentException("Snapshot directory cannot be null or empty.", nameof(snapshotDirectory));

            // Data integrity validation
            foreach (var snapshot in snapshots)
            {
                if (snapshot == null)
                    throw new ArgumentException("Snapshot list contains null elements.", nameof(snapshots));
                if (string.IsNullOrWhiteSpace(snapshot.RawJSON))
                    throw new ArgumentException("Snapshot contains invalid or empty RawJSON.", nameof(snapshots));
                try
                {
                    JObject.Parse(snapshot.RawJSON); // Validate JSON
                }
                catch (JsonException)
                {
                    throw new ArgumentException("Snapshot contains invalid JSON in RawJSON.", nameof(snapshots));
                }
            }

            if (snapshots.Count == 0)
                return new List<SnapshotGroupDTO>();

            var validPeriods = new List<SnapshotGroupDTO>();
            var currentPeriod = new List<SnapshotDTO> { snapshots[0] };
            int groupNumber = 1;

            for (int i = 1; i < snapshots.Count; i++)
            {
                var t1 = snapshots[i - 1].SnapshotDate;
                var t2 = snapshots[i].SnapshotDate;

                var json1 = JObject.Parse(snapshots[i - 1].RawJSON);
                var json2 = JObject.Parse(snapshots[i].RawJSON);
                var yb1 = json1["BestYesBid"]?.Value<int>() ?? 0;
                var nb1 = json1["BestNoBid"]?.Value<int>() ?? 0;
                var yb2 = json2["BestYesBid"]?.Value<int>() ?? 0;
                var nb2 = json2["BestNoBid"]?.Value<int>() ?? 0;

                if (IsAcceptableGap(t1, t2) && (!PriceChanged(yb1, nb1, yb2, nb2)
                      || (t2 - t1).TotalMinutes <= _config.SmallGapMinutes))
                {
                    currentPeriod.Add(snapshots[i]);
                }
                else
                {
                    var snapshotGroup = await CreateSnapshotGroup(currentPeriod, groupNumber, snapshotDirectory);
                    groupNumber = groupNumber + 1;
                    if (snapshotGroup != null)
                        validPeriods.Add(snapshotGroup);
                    currentPeriod = new List<SnapshotDTO> { snapshots[i] };
                }

                // Report progress
                progress?.Report((double)i / snapshots.Count * 100);
            }

            if (currentPeriod.Count > 0)
            {
                var snapshotGroup = await CreateSnapshotGroup(currentPeriod, groupNumber, snapshotDirectory);
                groupNumber = groupNumber + 1;
                if (snapshotGroup != null)
                    validPeriods.Add(snapshotGroup);
            }

            return validPeriods;
        }

        /// <summary>
        /// Creates a snapshot group DTO from a collection of related snapshots, aggregating
        /// their data and saving the flattened representation to a JSON file.
        /// </summary>
        /// <param name="snapshots">The list of snapshots to include in this group. Must contain at least one snapshot.</param>
        /// <param name="groupNumber">The sequential number identifying this group within the market.</param>
        /// <param name="snapshotDirectory">The directory where the group's JSON file will be saved.</param>
        /// <returns>A task containing a SnapshotGroupDTO with aggregated group information, or null if no snapshots provided.</returns>
        /// <remarks>
        /// This method performs the following operations:
        /// 1. Extracts start/end times and prices from the first and last snapshots
        /// 2. Calculates average liquidity across all snapshots
        /// 3. Flattens the nested JSON structure of each snapshot for analysis
        /// 4. Saves the flattened data to a uniquely named JSON file
        /// 5. Returns a DTO with all aggregated information
        ///
        /// The flattening process preserves the Orderbook structure while converting all other
        /// nested properties to a flat key-value format suitable for data analysis.
        /// </remarks>
        private async Task<SnapshotGroupDTO?> CreateSnapshotGroup(List<SnapshotDTO> snapshots, int groupNumber, string snapshotDirectory)
        {
            if (snapshots == null || !snapshots.Any())
                return null;

            var firstSnapshot = snapshots.First();
            var lastSnapshot = snapshots.Last();

            var firstJson = JObject.Parse(firstSnapshot.RawJSON);
            var lastJson = JObject.Parse(lastSnapshot.RawJSON);

            // Calculate average liquidity
            double averageLiquidity = snapshots.Average(s =>
            {
                var json = JObject.Parse(s.RawJSON);
                return (json["CumulativeYesBidDepth"]?.Value<double>() ?? 0) +
                       (json["CumulativeNoBidDepth"]?.Value<double>() ?? 0);
            });

            var flattenedRows = new List<Dictionary<string, string>>();
            // Use parallel processing for large snapshot groups
            var lockObject = new object();
            Parallel.ForEach(snapshots, snapshot =>
            {
                var json = JObject.Parse(snapshot.RawJSON);
                // Flatten all properties, preserving Orderbook
                var dict = json.ToObject<Dictionary<string, object>>();
                if (dict == null) dict = new Dictionary<string, object>();
                var flattenedMarket = FlattenDictionary(dict, "");
                // Add MarketTicker as a single field
                if (flattenedMarket != null)
                {
                    flattenedMarket["MarketTicker"] = snapshot.MarketTicker;
                    lock (lockObject)
                    {
                        flattenedRows.Add(flattenedMarket);
                    }
                }
            });

            // Convert to JSON string using Newtonsoft.Json
            var concatenatedJson = JsonConvert.SerializeObject(flattenedRows);

            // Save to file
            string filePath = await SaveSnapshotGroupJsonToFileAsync(firstSnapshot.MarketTicker, groupNumber, concatenatedJson, snapshotDirectory);

            return new SnapshotGroupDTO
            {
                MarketTicker = firstSnapshot.MarketTicker,
                StartTime = firstSnapshot.SnapshotDate,
                EndTime = lastSnapshot.SnapshotDate,
                YesStart = firstJson["BestYesBid"]?.Value<int>() ?? 0,
                NoStart = firstJson["BestNoBid"]?.Value<int>() ?? 0,
                YesEnd = lastJson["BestYesBid"]?.Value<int>() ?? 0,
                NoEnd = lastJson["BestNoBid"]?.Value<int>() ?? 0,
                AverageLiquidity = Math.Round(averageLiquidity, 2),
                SnapshotSchema = firstJson["SnapshotSchemaVersion"]?.Value<int>() ?? 21,
                ProcessedDttm = DateTime.UtcNow,
                JsonPath = filePath
            };
        }

        /// <summary>
        /// Recursively flattens a nested object structure into a flat dictionary of string key-value pairs.
        /// This method handles complex JSON structures by converting nested objects, arrays, and JObject/JArray
        /// instances into a flattened format suitable for data analysis and serialization.
        /// </summary>
        /// <param name="data">The object to flatten. Can be a dictionary, list, JObject, JArray, or primitive value.</param>
        /// <param name="prefix">The prefix to prepend to keys for nested structures, building the full key path.</param>
        /// <returns>A dictionary containing all flattened key-value pairs from the input data structure.</returns>
        /// <remarks>
        /// The flattening process:
        /// - Preserves Orderbook structures as JSON strings to maintain their complex nested format
        /// - Converts nested dictionaries and objects to flattened key paths (e.g., "Parent_Child_Property")
        /// - Handles arrays by indexing elements (e.g., "Array_0", "Array_1")
        /// - Converts all values to strings for consistent storage
        /// - Recursively processes JObject and JArray instances from JSON parsing
        ///
        /// This allows complex market snapshot data to be stored in a format suitable for analysis
        /// while preserving the essential structure of order book information.
        /// </remarks>
        private Dictionary<string, string> FlattenDictionary(object data, string prefix)
        {
            var result = new Dictionary<string, string>();
            if (data is Dictionary<string, object> dict)
            {
                foreach (var kvp in dict)
                {
                    string key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}_{kvp.Key}";
                    if (kvp.Key == "Orderbook")
                    {
                        // Preserve Orderbook as JSON string
                        result[key] = JsonConvert.SerializeObject(kvp.Value);
                    }
                    else if (kvp.Value is Dictionary<string, object> childDict)
                    {
                        var nestedFlattened = FlattenDictionary(childDict, key);
                        foreach (var nestedKvp in nestedFlattened)
                            result[nestedKvp.Key] = nestedKvp.Value;
                    }
                    else if (kvp.Value is List<object> list)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (list[i] != null)
                            {
                                var itemFlattened = FlattenDictionary(list[i], $"{key}_{i}");
                                if (itemFlattened != null)
                                {
                                    foreach (var itemKvp in itemFlattened)
                                        result[itemKvp.Key] = itemKvp.Value;
                                }
                            }
                        }
                    }
                    else if (kvp.Value is JObject jObject)
                    {
                        var jObjectDict = jObject.ToObject<Dictionary<string, object>>();
                        if (jObjectDict != null)
                        {
                            var nestedFlattened = FlattenDictionary(jObjectDict, key);
                            if (nestedFlattened != null)
                            {
                                foreach (var nestedKvp in nestedFlattened)
                                    if (nestedKvp.Value != null)
                                        result[nestedKvp.Key] = nestedKvp.Value;
                            }
                        }
                    }
                    else if (kvp.Value is JArray jArray)
                    {
                        for (int i = 0; i < jArray.Count; i++)
                        {
                            var item = jArray[i].ToObject<object>();
                            if (item != null)
                            {
                                var itemFlattened = FlattenDictionary(item, $"{key}_{i}");
                                if (itemFlattened != null)
                                {
                                    foreach (var itemKvp in itemFlattened)
                                        if (itemKvp.Value != null)
                                            result[itemKvp.Key] = itemKvp.Value;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (kvp.Value != null)
                        {
                            result[key] = kvp.Value.ToString();
                        }
                    }
                }
            }
            else if (data is List<object> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null)
                    {
                        var itemFlattened = FlattenDictionary(list[i], $"{prefix}_{i}");
                        if (itemFlattened != null)
                        {
                            foreach (var itemKvp in itemFlattened)
                                if (itemKvp.Value != null)
                                    result[itemKvp.Key] = itemKvp.Value;
                        }
                    }
                }
            }
            else if (data is JObject jObject)
            {
                var jObjectDict = jObject.ToObject<Dictionary<string, object>>();
                if (jObjectDict != null)
                {
                    var nestedFlattened = FlattenDictionary(jObjectDict, prefix);
                    if (nestedFlattened != null)
                    {
                        foreach (var nestedKvp in nestedFlattened)
                            if (nestedKvp.Value != null)
                                result[nestedKvp.Key] = nestedKvp.Value;
                    }
                }
            }
            else if (data is JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    var item = jArray[i].ToObject<object>();
                    if (item != null)
                    {
                        var itemFlattened = FlattenDictionary(item, $"{prefix}_{i}");
                        if (itemFlattened != null)
                        {
                            foreach (var itemKvp in itemFlattened)
                                if (itemKvp.Value != null)
                                    result[itemKvp.Key] = itemKvp.Value;
                        }
                    }
                }
            }
            else
            {
                if (data != null)
                {
                    result[prefix] = data.ToString();
                }
            }
            return result;
        }

        /// <summary>
        /// Loads a snapshot group from a JSON file and reconstructs the original snapshot DTOs.
        /// This method reverses the flattening process performed during group creation, converting
        /// the stored flat data back into structured SnapshotDTO objects with proper JSON formatting.
        /// </summary>
        /// <param name="filePath">The full path to the JSON file containing the flattened snapshot group data.</param>
        /// <returns>A task containing a list of reconstructed SnapshotDTO objects, ordered by snapshot date.</returns>
        /// <remarks>
        /// The loading process:
        /// 1. Reads the JSON file containing flattened snapshot data
        /// 2. Deserializes into a list of flattened dictionaries
        /// 3. Reconstructs each snapshot's RawJSON by mapping flattened fields back to MarketSnapshot structure
        /// 4. Orders snapshots by timestamp for consistency
        /// 5. Enhances snapshots with market type information for downstream processing
        ///
        /// Error handling ensures that file reading or parsing failures don't crash the application,
        /// with errors logged for debugging while returning any successfully processed snapshots.
        /// </remarks>
        public async Task<List<SnapshotDTO>> LoadSnapshotGroup(string filePath)
        {
            var snapshotDTOs = new List<SnapshotDTO>();

            try
            {
                // Read JSON content asynchronously
                string jsonContent = await File.ReadAllTextAsync(filePath);

                // Deserialize into list of dictionaries
                var flattenedRows = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonContent);
                if (flattenedRows == null) flattenedRows = new List<Dictionary<string, string>>();

                foreach (var row in flattenedRows)
                {
                    // Reconstruct RawJSON as MarketSnapshot
                    var rawJson = new Dictionary<string, object>
            {
                { "Timestamp", DateTime.Parse(row["StartTimestamp"]) },
                { "MarketTicker", row["MarketTicker"] },
                { "BestYesBid", int.Parse(row["BestYesBid"]) },
                { "BestNoBid", int.Parse(row["BestNoBid"]) },
                { "CumulativeYesBidDepth", double.Parse(row["CumulativeYesBidDepth"]) },
                { "CumulativeNoBidDepth", double.Parse(row["CumulativeNoBidDepth"]) },
                { "LastWebSocketMessageReceived", row.ContainsKey("LastWebSocketMessageReceived") && row["LastWebSocketMessageReceived"] != null ? DateTime.Parse(row["LastWebSocketMessageReceived"]) : DateTime.MinValue },
                { "SnapshotSchemaVersion", row.ContainsKey("SnapshotSchemaVersion") && row["SnapshotSchemaVersion"] != null ? int.Parse(row["SnapshotSchemaVersion"]) : 21 },
                { "Orderbook", row.ContainsKey("Orderbook") && row["Orderbook"] != null ? JsonConvert.DeserializeObject(row["Orderbook"]) : null }
            };

                    var snapshot = new SnapshotDTO
                    {
                        MarketTicker = row["MarketTicker"],
                        SnapshotDate = DateTime.Parse(row["StartTimestamp"]),
                        RawJSON = JsonConvert.SerializeObject(rawJson)
                    };

                    snapshotDTOs.Add(snapshot);
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }

            var orderedSnapshots = snapshotDTOs.OrderBy(s => s.SnapshotDate).ToList();

            // Convert to MarketSnapshot and compute MarketType for downstream processing
            var helper = new MarketTypeHelper();
            foreach (var dto in orderedSnapshots)
            {
                // Parse RawJSON to MarketSnapshot and determine market type
                if (dto.RawJSON != null)
                {
                    var marketSnapshot = JsonConvert.DeserializeObject<MarketSnapshot>(dto.RawJSON);
                    if (marketSnapshot != null)
                    {
                        marketSnapshot.MarketType = helper.GetMarketType(marketSnapshot).ToString();
                        // Market type information is now available in the snapshot for analysis
                    }
                }
            }

            return orderedSnapshots;
        }

        /// <summary>
        /// Saves the JSON content of a snapshot group to a uniquely named file in the specified directory.
        /// Creates the directory if it doesn't exist and handles file I/O errors gracefully.
        /// </summary>
        /// <param name="marketTicker">The market ticker symbol used in the filename.</param>
        /// <param name="groupNumber">The group number used in the filename for uniqueness.</param>
        /// <param name="jsonContent">The JSON string content to write to the file.</param>
        /// <param name="snapshotDirectory">The directory where the file should be saved.</param>
        /// <returns>A task containing the full path to the saved file, or an empty string if saving failed.</returns>
        /// <remarks>
        /// File naming convention: {MarketTicker}_{GroupNumber}.json
        /// This ensures unique filenames for each snapshot group within a market.
        /// Directory creation is handled automatically, and exceptions during file operations
        /// are caught to prevent application crashes, with empty string returned as failure indicator.
        /// </remarks>
        private async Task<string> SaveSnapshotGroupJsonToFileAsync(string marketTicker, int groupNumber, string jsonContent, string snapshotDirectory)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(snapshotDirectory))
                {
                    Directory.CreateDirectory(snapshotDirectory);
                }

                // Generate unique file name using MarketTicker and group number
                string fileName = $"{marketTicker}_{groupNumber}.json";
                string filePath = Path.Combine(snapshotDirectory, fileName);

                // Write JSON to file asynchronously
                await File.WriteAllTextAsync(filePath, jsonContent);

                return filePath;
            }
            catch (Exception)
            {
                // Return empty string if file saving fails
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines if the price has changed significantly between two snapshots.
        /// Compares the best bid prices for both Yes and No positions against a configurable threshold.
        /// </summary>
        /// <param name="yb1">The BestYesBid price from the first snapshot.</param>
        /// <param name="nb1">The BestNoBid price from the first snapshot.</param>
        /// <param name="yb2">The BestYesBid price from the second snapshot.</param>
        /// <param name="nb2">The BestNoBid price from the second snapshot.</param>
        /// <returns>True if any price difference exceeds the threshold, indicating significant change.</returns>
        /// <remarks>
        /// Uses a configurable price change threshold to allow for minor price fluctuations while detecting
        /// meaningful market movements that should break snapshot group continuity.
        /// This helps ensure that analysis periods contain relatively stable market conditions.
        /// </remarks>
        private bool PriceChanged(int yb1, int nb1, int yb2, int nb2)
        {
            return Math.Abs(yb1 - yb2) > _config.PriceChangeThreshold || Math.Abs(nb1 - nb2) > _config.PriceChangeThreshold;
        }

        /// <summary>
        /// Determines if the time gap between two snapshots is acceptable for maintaining
        /// group continuity. Considers both small gaps (always acceptable) and larger gaps
        /// within the active market threshold.
        /// </summary>
        /// <param name="t1">The timestamp of the first snapshot.</param>
        /// <param name="t2">The timestamp of the second snapshot.</param>
        /// <returns>True if the gap is acceptable and snapshots can remain in the same group.</returns>
        /// <remarks>
        /// Gap acceptability rules:
        /// - Gaps of SmallGapMinutes or less are always acceptable
        /// - Larger gaps are acceptable if within MaxActiveGapHours of active trading
        /// - Invalid time sequences (t2 <= t1) are not acceptable
        ///
        /// This allows for brief interruptions while preventing analysis of disconnected market periods.
        /// </remarks>
        private bool IsAcceptableGap(DateTime t1, DateTime t2)
        {
            if (t2 <= t1) return false;

            TimeSpan timediff = t2 - t1;
            var diffMinutes = timediff.TotalMinutes;
            if (diffMinutes <= _config.SmallGapMinutes) return true;

            double totalTimeHours = timediff.TotalHours;
            bool gapLessThanMaxActiveGap = totalTimeHours <= _config.MaxActiveGapHours;

            return gapLessThanMaxActiveGap;
        }


    }
}
