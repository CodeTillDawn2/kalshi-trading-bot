using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BacklashDTOs;
using BacklashDTOs.Data;
using TradingStrategies.Classification.Interfaces;
using TradingStrategies.Trading.Helpers;

namespace TradingStrategies.Classification
{
    public class SnapshotPeriodHelper : ISnapshotPeriodHelper
    {
        private double smallGapMinutes = 10.0;
        private double maxActiveGapHours = 1.0;

        public List<SnapshotGroupDTO> SplitIntoValidGroups(List<SnapshotDTO> snapshots, string snapshotDirectory)
        {
            if (snapshots == null || snapshots.Count == 0)
                return new List<SnapshotGroupDTO>();

            var validPeriods = new List<SnapshotGroupDTO>();
            var currentPeriod = new List<SnapshotDTO> { snapshots[0] };
            int GroupNumber = 1;

            for (int i = 1; i < snapshots.Count; i++)
            {
                var t1 = snapshots[i - 1].SnapshotDate;
                var t2 = snapshots[i].SnapshotDate;

                // Extract price data from RawJSON (now a MarketSnapshot)
                var json1 = JObject.Parse(snapshots[i - 1].RawJSON);
                var json2 = JObject.Parse(snapshots[i].RawJSON);
                var yb1 = json1["BestYesBid"]?.Value<int>() ?? 0;
                var nb1 = json1["BestNoBid"]?.Value<int>() ?? 0;
                var yb2 = json2["BestYesBid"]?.Value<int>() ?? 0;
                var nb2 = json2["BestNoBid"]?.Value<int>() ?? 0;

                if (IsAcceptableGap(t1, t2) && (!PriceChanged(yb1, nb1, yb2, nb2)
                     || (t2 - t1).TotalMinutes <= smallGapMinutes))
                {
                    currentPeriod.Add(snapshots[i]);
                }
                else
                {
                    var snapshotGroup = CreateSnapshotGroup(currentPeriod, GroupNumber, snapshotDirectory);
                    GroupNumber = GroupNumber + 1;
                    if (snapshotGroup != null)
                        validPeriods.Add(snapshotGroup);
                    currentPeriod = new List<SnapshotDTO> { snapshots[i] };
                }
            }

            if (currentPeriod.Count > 0)
            {
                var snapshotGroup = CreateSnapshotGroup(currentPeriod, GroupNumber, snapshotDirectory);
                GroupNumber = GroupNumber + 1;
                if (snapshotGroup != null)
                    validPeriods.Add(snapshotGroup);
            }

            return validPeriods;
        }

        private SnapshotGroupDTO CreateSnapshotGroup(List<SnapshotDTO> snapshots, int groupNumber, string snapshotDirectory)
        {
            if (snapshots == null || !snapshots.Any())
                return null;

            var firstSnapshot = snapshots.First();
            var lastSnapshot = snapshots.Last();

            // Parse JSON to get price data (now a MarketSnapshot)
            var firstJson = JObject.Parse(firstSnapshot.RawJSON);
            var lastJson = JObject.Parse(lastSnapshot.RawJSON);

            // Calculate average liquidity
            double averageLiquidity = snapshots.Average(s =>
            {
                var json = JObject.Parse(s.RawJSON);
                return (json["CumulativeYesBidDepth"]?.Value<double>() ?? 0) +
                       (json["CumulativeNoBidDepth"]?.Value<double>() ?? 0);
            });

            // Flatten MarketSnapshot data
            var flattenedRows = new List<Dictionary<string, string>>();
            foreach (var snapshot in snapshots)
            {
                var json = JObject.Parse(snapshot.RawJSON);
                // Flatten all properties, preserving Orderbook
                var flattenedMarket = FlattenDictionary(json.ToObject<Dictionary<string, object>>(), "");
                // Add MarketTicker as a single field
                flattenedMarket["MarketTicker"] = snapshot.MarketTicker;
                flattenedRows.Add(flattenedMarket);
            }

            // Convert to JSON string using Newtonsoft.Json
            var concatenatedJson = JsonConvert.SerializeObject(flattenedRows);

            // Save to file
            string filePath = SaveSnapshotGroupJsonToFile(firstSnapshot.MarketTicker, groupNumber, concatenatedJson, snapshotDirectory);

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
                            var itemFlattened = FlattenDictionary(list[i], $"{key}_{i}");
                            foreach (var itemKvp in itemFlattened)
                                result[itemKvp.Key] = itemKvp.Value;
                        }
                    }
                    else if (kvp.Value is JObject jObject)
                    {
                        var jObjectDict = jObject.ToObject<Dictionary<string, object>>();
                        var nestedFlattened = FlattenDictionary(jObjectDict, key);
                        foreach (var nestedKvp in nestedFlattened)
                            result[nestedKvp.Key] = nestedKvp.Value;
                    }
                    else if (kvp.Value is JArray jArray)
                    {
                        for (int i = 0; i < jArray.Count; i++)
                        {
                            var itemFlattened = FlattenDictionary(jArray[i].ToObject<object>(), $"{key}_{i}");
                            foreach (var itemKvp in itemFlattened)
                                result[itemKvp.Key] = itemKvp.Value;
                        }
                    }
                    else
                    {
                        result[key] = kvp.Value?.ToString();
                    }
                }
            }
            else if (data is List<object> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var itemFlattened = FlattenDictionary(list[i], $"{prefix}_{i}");
                    foreach (var itemKvp in itemFlattened)
                        result[itemKvp.Key] = itemKvp.Value;
                }
            }
            else if (data is JObject jObject)
            {
                var jObjectDict = jObject.ToObject<Dictionary<string, object>>();
                var nestedFlattened = FlattenDictionary(jObjectDict, prefix);
                foreach (var nestedKvp in nestedFlattened)
                    result[nestedKvp.Key] = nestedKvp.Value;
            }
            else if (data is JArray jArray)
            {
                for (int i = 0; i < jArray.Count; i++)
                {
                    var itemFlattened = FlattenDictionary(jArray[i].ToObject<object>(), $"{prefix}_{i}");
                    foreach (var itemKvp in itemFlattened)
                        result[itemKvp.Key] = itemKvp.Value;
                }
            }
            else
            {
                result[prefix] = data?.ToString();
            }
            return result;
        }

        public List<SnapshotDTO> LoadSnapshotGroup(string filePath)
        {
            var snapshotDTOs = new List<SnapshotDTO>();

            try
            {
                // Read JSON content
                string jsonContent = File.ReadAllText(filePath);

                // Deserialize into list of dictionaries
                var flattenedRows = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonContent);

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
                { "LastWebSocketMessageReceived", row.ContainsKey("LastWebSocketMessageReceived") ? DateTime.Parse(row["LastWebSocketMessageReceived"]) : DateTime.MinValue },
                { "SnapshotSchemaVersion", row.ContainsKey("SnapshotSchemaVersion") ? int.Parse(row["SnapshotSchemaVersion"]) : 21 },
                { "Orderbook", row.ContainsKey("Orderbook") ? JsonConvert.DeserializeObject(row["Orderbook"]) : null }
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

            // NEW: Convert to MarketSnapshot and compute MarketType (if needed here; otherwise skip if handled downstream)
            var helper = new MarketTypeHelper();
            foreach (var dto in orderedSnapshots)
            {
                // Assuming you parse RawJSON to MarketSnapshot here; replace with your parsing logic
                var marketSnapshot = JsonConvert.DeserializeObject<MarketSnapshot>(dto.RawJSON);
                marketSnapshot.MarketType = helper.GetMarketType(marketSnapshot).ToString();
                // If needed, update dto.RawJSON or handle accordingly
            }

            return orderedSnapshots;
        }

        private string SaveSnapshotGroupJsonToFile(string marketTicker, int groupNumber, string jsonContent, string snapshotDirectory)
        {
            try
            {

                // Ensure directory exists
                if (!Directory.Exists(snapshotDirectory))
                {
                    Directory.CreateDirectory(snapshotDirectory);
                }

                // Generate unique file name using MarketTicker and timestamp
                string fileName = $"{marketTicker}_{groupNumber}.json";
                string filePath = Path.Combine(snapshotDirectory, fileName);

                // Write JSON to file
                File.WriteAllText(filePath, jsonContent);

                return filePath;
            }
            catch (Exception)
            {
                // Return empty string if file saving fails
                return string.Empty;
            }
        }

        private bool PriceChanged(int yb1, int nb1, int yb2, int nb2)
        {
            const int threshold = 3; // 3-point flat change margin
            return Math.Abs(yb1 - yb2) > threshold || Math.Abs(nb1 - nb2) > threshold;
        }

        private bool IsAcceptableGap(DateTime t1, DateTime t2)
        {
            if (t2 <= t1) return false;

            TimeSpan timediff = t2 - t1;
            var diffMinutes = timediff.TotalMinutes;
            if (diffMinutes <= smallGapMinutes) return true;

            double totalTimeHours = timediff.TotalHours;
            bool GapLessThanMaxActiveGap = totalTimeHours <= maxActiveGapHours;

            return GapLessThanMaxActiveGap;
        }


    }
}
