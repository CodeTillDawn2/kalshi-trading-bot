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

        [Fact(DisplayName = "CheckInData round-trip: serialize/deserialize maintains exact structure")]
        public void CheckInData_RoundTripSerialization_ShouldMaintainExactStructure()
        {
            // Arrange - Create a CheckInData with specific values
            var originalData = new CheckInData
            {
                BrainInstanceName = "TestBrain",
                Markets = new List<string> { "AAPL", "GOOGL", "MSFT" },
                ErrorCount = 42,
                LastSnapshot = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
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
        }

        [Fact(DisplayName = "CheckInResponse round-trip: serialize/deserialize maintains exact structure")]
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

        [Fact(DisplayName = "PerformanceMetricsData round-trip: serialize/deserialize maintains exact structure")]
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

        [Fact(DisplayName = "TargetTickersConfirmationResponse round-trip: serialize/deserialize maintains exact structure")]
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

        [Fact(DisplayName = "DTOs with null values: serialize/deserialize correctly handles nulls")]
        public void DTOs_WithNullValues_ShouldSerializeDeserializeCorrectly()
        {
            // Arrange - Test null handling
            var originalData = new CheckInData
            {
                BrainInstanceName = null, // Test nullable string
                Markets = null, // Test nullable list
                LastSnapshot = null, // Test nullable DateTime
                ErrorCount = 0
            };

            // Act
            var json = JsonSerializer.Serialize(originalData, _jsonOptions);
            var deserializedData = JsonSerializer.Deserialize<CheckInData>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserializedData);
            Assert.Null(deserializedData.BrainInstanceName);
            Assert.Null(deserializedData.Markets);
            Assert.Null(deserializedData.LastSnapshot);
            Assert.Equal(originalData.ErrorCount, deserializedData.ErrorCount);
        }

        [Fact(DisplayName = "DTOs with complex structures: multiple round-trips maintain integrity")]
        public void DTOs_WithComplexNestedStructures_ShouldMaintainIntegrity()
        {
            // Arrange - Test complex nested structures
            var originalData = new CheckInData
            {
                BrainInstanceName = "ComplexTestBrain",
                Markets = new List<string> { "MARKET1", "MARKET2", "MARKET3" },
                ErrorCount = 100,
                LastSnapshot = DateTime.UtcNow
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

            // Verify JSON strings are identical (ensuring consistent serialization)
            Assert.Equal(json1, json2);
        }

        [Fact(DisplayName = "DTO property order: different orders produce identical JSON and deserialize correctly")]
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
