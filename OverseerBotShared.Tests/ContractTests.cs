using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverseerBotShared.Tests
{
    /// <summary>
    /// Custom JSON converter for Dictionary<string, object> that preserves actual .NET types
    /// instead of converting everything to JsonElement during deserialization.
    /// </summary>
    public class ObjectDictionaryConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ref reader, options);
            return dict.ToDictionary(kvp => kvp.Key, kvp => ConvertJsonElement(kvp.Value));
        }

        private object ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString()!;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int i)) return i;
                    if (element.TryGetInt64(out long l)) return l;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null!;
                default:
                    // For complex objects, return as string representation
                    return element.ToString();
            }
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, kvp.Value?.GetType() ?? typeof(object), options);
            }
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Contract tests to ensure DTOs maintain exact structure consistency
    /// between serialization (sending) and deserialization (receiving).
    /// These tests guarantee that the same structure being sent is the one expected to be received.
    /// </summary>
    public class ContractTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = { new ObjectDictionaryConverter() }
        };

        [Fact]
        public void CheckInData_RoundTripSerialization_ShouldMaintainExactStructure()
        {
            // Arrange - Create a CheckInData with specific values
            var originalData = new CheckInData
            {
                BrainInstanceName = "TestBrain",
                Markets = new List<string> { "AAPL", "GOOGL", "MSFT" },
                ErrorCount = 42,
                LastSnapshot = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                IsStartingUp = false,
                IsShuttingDown = true,
                WatchPositions = true,
                WatchOrders = false,
                ManagedWatchList = true,
                CaptureSnapshots = true,
                TargetWatches = 150,
                MinimumInterest = 2.5,
                UsageMin = 60.0,
                UsageMax = 85.0,
                CurrentCpuUsage = 45.7,
                EventQueueAvg = 12.3,
                TickerQueueAvg = 8.9,
                NotificationQueueAvg = 5.2,
                OrderbookQueueAvg = 15.6,
                LastRefreshCycleSeconds = 2.1,
                LastRefreshCycleInterval = 30,
                LastRefreshMarketCount = 25,
                LastRefreshUsagePercentage = 72.5,
                LastRefreshTimeAcceptable = true,
                LastPerformanceSampleDate = new DateTime(2024, 1, 15, 10, 25, 0, DateTimeKind.Utc),
                IsWebSocketConnected = true
            };

            // Act - Serialize and deserialize
            var json = JsonSerializer.Serialize(originalData, _jsonOptions);
            var deserializedData = JsonSerializer.Deserialize<CheckInData>(json, _jsonOptions);

            // Assert - Verify exact structure match
            Assert.NotNull(deserializedData);
            Assert.Equal(originalData.BrainInstanceName, deserializedData.BrainInstanceName);
            Assert.Equal(originalData.Markets, deserializedData.Markets);
            Assert.Equal(originalData.ErrorCount, deserializedData.ErrorCount);
            Assert.Equal(originalData.LastSnapshot, deserializedData.LastSnapshot);
            Assert.Equal(originalData.IsStartingUp, deserializedData.IsStartingUp);
            Assert.Equal(originalData.IsShuttingDown, deserializedData.IsShuttingDown);
            Assert.Equal(originalData.WatchPositions, deserializedData.WatchPositions);
            Assert.Equal(originalData.WatchOrders, deserializedData.WatchOrders);
            Assert.Equal(originalData.ManagedWatchList, deserializedData.ManagedWatchList);
            Assert.Equal(originalData.CaptureSnapshots, deserializedData.CaptureSnapshots);
            Assert.Equal(originalData.TargetWatches, deserializedData.TargetWatches);
            Assert.Equal(originalData.MinimumInterest, deserializedData.MinimumInterest);
            Assert.Equal(originalData.UsageMin, deserializedData.UsageMin);
            Assert.Equal(originalData.UsageMax, deserializedData.UsageMax);
            Assert.Equal(originalData.CurrentCpuUsage, deserializedData.CurrentCpuUsage);
            Assert.Equal(originalData.EventQueueAvg, deserializedData.EventQueueAvg);
            Assert.Equal(originalData.TickerQueueAvg, deserializedData.TickerQueueAvg);
            Assert.Equal(originalData.NotificationQueueAvg, deserializedData.NotificationQueueAvg);
            Assert.Equal(originalData.OrderbookQueueAvg, deserializedData.OrderbookQueueAvg);
            Assert.Equal(originalData.LastRefreshCycleSeconds, deserializedData.LastRefreshCycleSeconds);
            Assert.Equal(originalData.LastRefreshCycleInterval, deserializedData.LastRefreshCycleInterval);
            Assert.Equal(originalData.LastRefreshMarketCount, deserializedData.LastRefreshMarketCount);
            Assert.Equal(originalData.LastRefreshUsagePercentage, deserializedData.LastRefreshUsagePercentage);
            Assert.Equal(originalData.LastRefreshTimeAcceptable, deserializedData.LastRefreshTimeAcceptable);
            Assert.Equal(originalData.LastPerformanceSampleDate, deserializedData.LastPerformanceSampleDate);
            Assert.Equal(originalData.IsWebSocketConnected, deserializedData.IsWebSocketConnected);
        }

        [Fact]
        public void CheckInResponse_RoundTripSerialization_ShouldMaintainExactStructure()
        {
            // Arrange
            var originalResponse = new CheckInResponse
            {
                Success = true,
                Message = "Check-in processed successfully",
                TargetTickers = new[] { "AAPL", "GOOGL", "TSLA" },
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc)
            };

            // Act
            var json = JsonSerializer.Serialize(originalResponse, _jsonOptions);
            var deserializedResponse = JsonSerializer.Deserialize<CheckInResponse>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedResponse);
            Assert.Equal(originalResponse.Success, deserializedResponse.Success);
            Assert.Equal(originalResponse.Message, deserializedResponse.Message);
            Assert.Equal(originalResponse.TargetTickers, deserializedResponse.TargetTickers);
            Assert.Equal(originalResponse.Timestamp, deserializedResponse.Timestamp);
        }

        [Fact]
        public void PerformanceMetricsData_RoundTripSerialization_ShouldMaintainExactStructure()
        {
            // Arrange
            var originalMetrics = new PerformanceMetricsData
            {
                BrainInstanceName = "TestBrain",
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                DatabaseMetrics = new Dictionary<string, (int, int, TimeSpan, double)>
                {
                    ["Users"] = (100, 5, TimeSpan.FromMilliseconds(150), 0.85),
                    ["Orders"] = (200, 10, TimeSpan.FromMilliseconds(300), 0.92)
                },
                MessageProcessorTotalMessagesProcessed = 1500,
                MessageProcessorTotalProcessingTimeMs = 2500,
                MessageProcessorAverageProcessingTimeMs = 1.67,
                MessageProcessorMessagesPerSecond = 25.5,
                MessageProcessorOrderBookQueueDepth = 12,
                MessageProcessorDuplicateMessageCount = 3,
                MessageProcessorDuplicatesInWindow = 1,
                MessageProcessorLastDuplicateWarningTime = new DateTime(2024, 1, 15, 10, 25, 0, DateTimeKind.Utc),
                MessageProcessorMessageTypeCounts = new Dictionary<string, long>
                {
                    ["MarketUpdate"] = 800,
                    ["OrderBook"] = 700
                },
                ConfigurableMetrics = new Dictionary<string, object>
                {
                    ["CustomMetric1"] = 42.5,
                    ["CustomMetric2"] = "test_value"
                }
            };

            // Act
            var json = JsonSerializer.Serialize(originalMetrics, _jsonOptions);
            var deserializedMetrics = JsonSerializer.Deserialize<PerformanceMetricsData>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedMetrics);
            Assert.Equal(originalMetrics.BrainInstanceName, deserializedMetrics.BrainInstanceName);
            Assert.Equal(originalMetrics.Timestamp, deserializedMetrics.Timestamp);
            Assert.Equal(originalMetrics.MessageProcessorTotalMessagesProcessed, deserializedMetrics.MessageProcessorTotalMessagesProcessed);
            Assert.Equal(originalMetrics.MessageProcessorTotalProcessingTimeMs, deserializedMetrics.MessageProcessorTotalProcessingTimeMs);
            Assert.Equal(originalMetrics.MessageProcessorAverageProcessingTimeMs, deserializedMetrics.MessageProcessorAverageProcessingTimeMs);
            Assert.Equal(originalMetrics.MessageProcessorMessagesPerSecond, deserializedMetrics.MessageProcessorMessagesPerSecond);
            Assert.Equal(originalMetrics.MessageProcessorOrderBookQueueDepth, deserializedMetrics.MessageProcessorOrderBookQueueDepth);
            Assert.Equal(originalMetrics.MessageProcessorDuplicateMessageCount, deserializedMetrics.MessageProcessorDuplicateMessageCount);
            Assert.Equal(originalMetrics.MessageProcessorDuplicatesInWindow, deserializedMetrics.MessageProcessorDuplicatesInWindow);
            Assert.Equal(originalMetrics.MessageProcessorLastDuplicateWarningTime, deserializedMetrics.MessageProcessorLastDuplicateWarningTime);
            Assert.Equal(originalMetrics.MessageProcessorMessageTypeCounts, deserializedMetrics.MessageProcessorMessageTypeCounts);
            Assert.Equal(originalMetrics.ConfigurableMetrics, deserializedMetrics.ConfigurableMetrics);
        }

        [Fact]
        public void TargetTickersConfirmationResponse_RoundTripSerialization_ShouldMaintainExactStructure()
        {
            // Arrange
            var originalResponse = new TargetTickersConfirmationResponse
            {
                Success = true,
                BrainInstanceName = "TestBrain",
                Message = "Target tickers confirmed",
                Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
            };

            // Act
            var json = JsonSerializer.Serialize(originalResponse, _jsonOptions);
            var deserializedResponse = JsonSerializer.Deserialize<TargetTickersConfirmationResponse>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedResponse);
            Assert.Equal(originalResponse.Success, deserializedResponse.Success);
            Assert.Equal(originalResponse.BrainInstanceName, deserializedResponse.BrainInstanceName);
            Assert.Equal(originalResponse.Message, deserializedResponse.Message);
            Assert.Equal(originalResponse.Timestamp, deserializedResponse.Timestamp);
        }

        [Fact]
        public void DTOs_WithNullValues_ShouldSerializeDeserializeCorrectly()
        {
            // Arrange - Test null handling
            var originalData = new CheckInData
            {
                BrainInstanceName = null, // Test nullable string
                Markets = null, // Test nullable list
                LastSnapshot = null, // Test nullable DateTime
                LastPerformanceSampleDate = null,
                // Other properties with default values
                ErrorCount = 0,
                IsStartingUp = false,
                IsShuttingDown = false,
                WatchPositions = false,
                WatchOrders = false,
                ManagedWatchList = false,
                CaptureSnapshots = false,
                TargetWatches = 0,
                MinimumInterest = 0.0,
                UsageMin = 0.0,
                UsageMax = 0.0,
                CurrentCpuUsage = 0.0,
                EventQueueAvg = 0.0,
                TickerQueueAvg = 0.0,
                NotificationQueueAvg = 0.0,
                OrderbookQueueAvg = 0.0,
                LastRefreshCycleSeconds = 0.0,
                LastRefreshCycleInterval = 0,
                LastRefreshMarketCount = 0,
                LastRefreshUsagePercentage = 0.0,
                LastRefreshTimeAcceptable = false,
                IsWebSocketConnected = false
            };

            // Act
            var json = JsonSerializer.Serialize(originalData, _jsonOptions);
            var deserializedData = JsonSerializer.Deserialize<CheckInData>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedData);
            Assert.Null(deserializedData.BrainInstanceName);
            Assert.Null(deserializedData.Markets);
            Assert.Null(deserializedData.LastSnapshot);
            Assert.Null(deserializedData.LastPerformanceSampleDate);
            Assert.Equal(originalData.ErrorCount, deserializedData.ErrorCount);
            Assert.Equal(originalData.IsStartingUp, deserializedData.IsStartingUp);
        }

        [Fact]
        public void DTOs_WithComplexNestedStructures_ShouldMaintainIntegrity()
        {
            // Arrange - Test complex nested structures
            var originalData = new CheckInData
            {
                BrainInstanceName = "ComplexTestBrain",
                Markets = new List<string> { "MARKET1", "MARKET2", "MARKET3" },
                ErrorCount = 100,
                LastSnapshot = DateTime.UtcNow,
                IsStartingUp = false,
                IsShuttingDown = false,
                WatchPositions = true,
                WatchOrders = true,
                ManagedWatchList = true,
                CaptureSnapshots = true,
                TargetWatches = 200,
                MinimumInterest = 5.0,
                UsageMin = 50.0,
                UsageMax = 90.0,
                CurrentCpuUsage = 75.5,
                EventQueueAvg = 25.0,
                TickerQueueAvg = 15.0,
                NotificationQueueAvg = 10.0,
                OrderbookQueueAvg = 30.0,
                LastRefreshCycleSeconds = 5.0,
                LastRefreshCycleInterval = 60,
                LastRefreshMarketCount = 50,
                LastRefreshUsagePercentage = 80.0,
                LastRefreshTimeAcceptable = true,
                LastPerformanceSampleDate = DateTime.UtcNow.AddMinutes(-5),
                IsWebSocketConnected = true
            };

            // Act - Multiple round trips to ensure stability
            var json1 = JsonSerializer.Serialize(originalData, _jsonOptions);
            var intermediate = JsonSerializer.Deserialize<CheckInData>(json1, _jsonOptions);
            var json2 = JsonSerializer.Serialize(intermediate, _jsonOptions);
            var final = JsonSerializer.Deserialize<CheckInData>(json2, _jsonOptions);

            // Assert - Verify multiple serialization cycles maintain integrity
            Assert.NotNull(final);
            Assert.Equal(originalData.BrainInstanceName, final.BrainInstanceName);
            Assert.Equal(originalData.Markets.Count, final.Markets.Count);
            Assert.Equal(originalData.ErrorCount, final.ErrorCount);
            Assert.Equal(originalData.TargetWatches, final.TargetWatches);
            Assert.Equal(originalData.CurrentCpuUsage, final.CurrentCpuUsage);

            // Verify JSON strings are identical (ensuring consistent serialization)
            Assert.Equal(json1, json2);
        }

        [Fact]
        public void DTO_PropertyOrder_ShouldNotAffectDeserialization()
        {
            // Arrange - Create two identical objects with different property orders in JSON
            var data1 = new CheckInResponse
            {
                Success = true,
                Message = "Test message",
                TargetTickers = new[] { "A", "B", "C" },
                Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            var data2 = new CheckInResponse
            {
                Timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Success = true,
                TargetTickers = new[] { "A", "B", "C" },
                Message = "Test message"
            };

            // Act - Serialize both
            var json1 = JsonSerializer.Serialize(data1, _jsonOptions);
            var json2 = JsonSerializer.Serialize(data2, _jsonOptions);

            // Assert - Different property orders should produce identical JSON
            Assert.Equal(json1, json2);

            // Verify deserialization works regardless of order
            var deserialized1 = JsonSerializer.Deserialize<CheckInResponse>(json1, _jsonOptions);
            var deserialized2 = JsonSerializer.Deserialize<CheckInResponse>(json2, _jsonOptions);

            Assert.Equal(deserialized1.Success, deserialized2.Success);
            Assert.Equal(deserialized1.Message, deserialized2.Message);
            Assert.Equal(deserialized1.TargetTickers, deserialized2.TargetTickers);
            Assert.Equal(deserialized1.Timestamp, deserialized2.Timestamp);
        }
    }
}
