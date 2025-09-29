using BacklashInterfaces.PerformanceMetrics;
using System.Text.Json;

namespace OverseerBotShared.Tests
{
    /// <summary>
    /// Integration tests that verify the communication contract between
    /// BacklashBot (client) and KalshiBotOverseer (server) to guarantee
    /// the same structure being sent is the one expected to be received.
    /// These tests simulate the actual SignalR communication flow.
    /// </summary>
    public class IntegrationTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Converters = { new ObjectDictionaryConverter() }
        };

        [Fact(DisplayName = "End-to-end CheckInData flow: simulates SignalR transmission, verifies exact structure maintained")]
        public void EndToEnd_CheckInDataFlow_ShouldMaintainExactStructure()
        {
            // Arrange - Simulate what BacklashBot would send
            var originalData = new CheckInData
            {
                BrainInstanceName = "ProductionBrain",
                Markets = new List<string> { "AAPL", "GOOGL", "MSFT", "TSLA", "NVDA" },
                ErrorCount = 3,
                LastSnapshot = new DateTime(2024, 1, 15, 16, 45, 0, DateTimeKind.Utc)
            };

            // Act - Simulate SignalR serialization (what actually happens during transmission)
            var jsonPayload = JsonSerializer.Serialize(originalData, _jsonOptions);

            // Simulate OverseerHub receiving and deserializing the data
            var receivedData = JsonSerializer.Deserialize<CheckInData>(jsonPayload, _jsonOptions);

            // Assert - Verify the Overseer received the exact same structure
            Assert.NotNull(receivedData);
            Assert.Equal(originalData.BrainInstanceName, receivedData.BrainInstanceName);
            Assert.Equal(originalData.Markets, receivedData.Markets);
            Assert.Equal(originalData.ErrorCount, receivedData.ErrorCount);
            Assert.Equal(originalData.LastSnapshot, receivedData.LastSnapshot);
        }

        [Fact(DisplayName = "End-to-end CheckInResponse flow: simulates SignalR transmission, verifies exact structure maintained")]
        public void EndToEnd_CheckInResponseFlow_ShouldMaintainExactStructure()
        {
            // Arrange - Simulate what Overseer would send back
            var originalResponse = new CheckInResponse
            {
                Success = true,
                Message = "Check-in processed successfully with target assignments",
                TargetTickers = new[] { "AMD", "CRM", "NOW", "SHOP" },
                Timestamp = new DateTime(2024, 1, 15, 16, 45, 30, DateTimeKind.Utc)
            };

            // Act - Simulate SignalR serialization (what actually happens during transmission)
            var jsonPayload = JsonSerializer.Serialize(originalResponse, _jsonOptions);

            // Simulate BacklashBot receiving and deserializing the response
            var receivedResponse = JsonSerializer.Deserialize<CheckInResponse>(jsonPayload, _jsonOptions);

            // Assert - Verify the BacklashBot received the exact same structure
            Assert.NotNull(receivedResponse);
            Assert.Equal(originalResponse.Success, receivedResponse.Success);
            Assert.Equal(originalResponse.Message, receivedResponse.Message);
            Assert.Equal(originalResponse.TargetTickers, receivedResponse.TargetTickers);
            Assert.Equal(originalResponse.Timestamp, receivedResponse.Timestamp);
        }

        [Fact(DisplayName = "End-to-end PerformanceMetrics flow: simulates SignalR transmission, verifies exact structure maintained")]
        public void EndToEnd_PerformanceMetricsFlow_ShouldMaintainExactStructure()
        {
            // Arrange - Simulate performance metrics transmission
            var originalMetrics = new PerformanceMetricsData
            {
                BrainInstanceName = "MetricsTestBrain",
                Timestamp = new DateTime(2024, 1, 15, 17, 0, 0, DateTimeKind.Utc),
                AllMetrics = new List<PerformanceMetricEntry>
                {
                    new PerformanceMetricEntry
                    {
                        ClassName = "MessageProcessor",
                        Metric = new GeneralPerformanceMetric
                        {
                            Id = "total-messages",
                            Name = "Total Messages Processed",
                            Description = "Total number of messages processed",
                            Value = 5000,
                            Unit = "count",
                            VisualType = VisualType.Counter,
                            Category = "Message Processing"
                        }
                    },
                    new PerformanceMetricEntry
                    {
                        ClassName = "MessageProcessor",
                        Metric = new GeneralPerformanceMetric
                        {
                            Id = "total-processing-time",
                            Name = "Total Processing Time",
                            Description = "Total processing time in milliseconds",
                            Value = 8500,
                            Unit = "ms",
                            VisualType = VisualType.NumericDisplay,
                            Category = "Message Processing"
                        }
                    },
                    new PerformanceMetricEntry
                    {
                        ClassName = "MessageProcessor",
                        Metric = new GeneralPerformanceMetric
                        {
                            Id = "avg-processing-time",
                            Name = "Average Processing Time",
                            Description = "Average processing time per message",
                            Value = 1.7,
                            Unit = "ms",
                            VisualType = VisualType.SpeedDial,
                            Category = "Message Processing"
                        }
                    },
                    new PerformanceMetricEntry
                    {
                        ClassName = "MessageProcessor",
                        Metric = new GeneralPerformanceMetric
                        {
                            Id = "messages-per-second",
                            Name = "Messages Per Second",
                            Description = "Processing rate",
                            Value = 42.8,
                            Unit = "msg/s",
                            VisualType = VisualType.NumericDisplay,
                            Category = "Message Processing"
                        }
                    },
                    new PerformanceMetricEntry
                    {
                        ClassName = "MessageProcessor",
                        Metric = new GeneralPerformanceMetric
                        {
                            Id = "queue-depth",
                            Name = "Order Book Queue Depth",
                            Description = "Current queue depth",
                            Value = 15,
                            Unit = "count",
                            VisualType = VisualType.Counter,
                            Category = "Message Processing"
                        }
                    },
                    new PerformanceMetricEntry
                    {
                        ClassName = "MessageProcessor",
                        Metric = new GeneralPerformanceMetric
                        {
                            Id = "duplicate-count",
                            Name = "Duplicate Message Count",
                            Description = "Number of duplicate messages detected",
                            Value = 5,
                            Unit = "count",
                            VisualType = VisualType.Counter,
                            Category = "Message Processing"
                        }
                    },
                    new PerformanceMetricEntry
                    {
                        ClassName = "MessageProcessor",
                        Metric = new GeneralPerformanceMetric
                        {
                            Id = "duplicates-in-window",
                            Name = "Duplicates In Window",
                            Description = "Duplicates detected in current window",
                            Value = 2,
                            Unit = "count",
                            VisualType = VisualType.Counter,
                            Category = "Message Processing"
                        }
                    }
                }
            };

            // Act - Simulate SignalR serialization
            var jsonPayload = JsonSerializer.Serialize(originalMetrics, _jsonOptions);

            // Simulate OverseerHub receiving and deserializing the metrics
            var receivedMetrics = JsonSerializer.Deserialize<PerformanceMetricsData>(jsonPayload, _jsonOptions);

            // Assert - Verify the Overseer received the exact same structure
            Assert.NotNull(receivedMetrics);
            Assert.Equal(originalMetrics.BrainInstanceName, receivedMetrics.BrainInstanceName);
            Assert.Equal(originalMetrics.Timestamp, receivedMetrics.Timestamp);
            Assert.NotNull(receivedMetrics.AllMetrics);
            Assert.Equal(originalMetrics.AllMetrics.Count, receivedMetrics.AllMetrics.Count);

            // Verify each metric matches
            for (int i = 0; i < originalMetrics.AllMetrics.Count; i++)
            {
                var original = originalMetrics.AllMetrics[i];
                var received = receivedMetrics.AllMetrics[i];

                Assert.Equal(original.ClassName, received.ClassName);
                Assert.Equal(original.Metric.Id, received.Metric.Id);
                Assert.Equal(original.Metric.Name, received.Metric.Name);
                Assert.Equal(original.Metric.Description, received.Metric.Description);
                Assert.Equal(original.Metric.Value, received.Metric.Value);
                Assert.Equal(original.Metric.Unit, received.Metric.Unit);
                Assert.Equal(original.Metric.VisualType, received.Metric.VisualType);
                Assert.Equal(original.Metric.Category, received.Metric.Category);
            }
        }

        [Fact(DisplayName = "Transmission with nulls: simulates SignalR with null values, verifies correct handling")]
        public void Transmission_WithNullValues_ShouldHandleCorrectly()
        {
            // Arrange - Test null value handling in transmission
            var originalData = new CheckInData
            {
                BrainInstanceName = null,
                Markets = null,
                LastSnapshot = null,
                ErrorCount = 0
            };

            // Act - Simulate transmission with nulls
            var jsonPayload = JsonSerializer.Serialize(originalData, _jsonOptions);
            var receivedData = JsonSerializer.Deserialize<CheckInData>(jsonPayload, _jsonOptions);

            // Assert - Verify null handling
            Assert.NotNull(receivedData);
            Assert.Null(receivedData.BrainInstanceName);
            Assert.Null(receivedData.Markets);
            Assert.Null(receivedData.LastSnapshot);
            Assert.Equal(0, receivedData.ErrorCount);
        }

        [Fact(DisplayName = "Multiple transmissions: simulates sequential SignalR sends, verifies consistency across all")]
        public void MultipleSequentialTransmissions_ShouldMaintainConsistency()
        {
            // Arrange - Test multiple transmissions
            var testDataSets = new List<CheckInData>
            {
                new CheckInData
                {
                    BrainInstanceName = "Brain1",
                    Markets = new List<string> { "MKT1", "MKT2" },
                    ErrorCount = 1
                },
                new CheckInData
                {
                    BrainInstanceName = "Brain2",
                    Markets = new List<string> { "MKT3", "MKT4", "MKT5" },
                    ErrorCount = 2
                },
                new CheckInData
                {
                    BrainInstanceName = "Brain3",
                    Markets = new List<string> { "MKT6" },
                    ErrorCount = 0
                }
            };

            // Act - Simulate multiple transmissions
            var transmittedData = new List<CheckInData>();
            foreach (var data in testDataSets)
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var received = JsonSerializer.Deserialize<CheckInData>(json, _jsonOptions);
                transmittedData.Add(received);
            }

            // Assert - Verify all transmissions maintained structure
            Assert.Equal(3, transmittedData.Count);

            for (int i = 0; i < testDataSets.Count; i++)
            {
                var original = testDataSets[i];
                var transmitted = transmittedData[i];

                Assert.Equal(original.BrainInstanceName, transmitted.BrainInstanceName);
                Assert.Equal(original.Markets, transmitted.Markets);
                Assert.Equal(original.ErrorCount, transmitted.ErrorCount);
            }
        }

        [Fact(DisplayName = "Error response transmission: simulates failed check-in SignalR, verifies error structure maintained")]
        public void ErrorResponseTransmission_ShouldMaintainExactStructure()
        {
            // Arrange - Test error response transmission
            var errorResponse = new CheckInResponse
            {
                Success = false,
                Message = "Check-in failed: Invalid brain instance name",
                TargetTickers = Array.Empty<string>(),
                Timestamp = new DateTime(2024, 1, 15, 16, 45, 0, DateTimeKind.Utc)
            };

            // Act - Simulate error transmission
            var jsonPayload = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            var receivedResponse = JsonSerializer.Deserialize<CheckInResponse>(jsonPayload, _jsonOptions);

            // Assert - Verify error structure is maintained
            Assert.NotNull(receivedResponse);
            Assert.False(receivedResponse.Success);
            Assert.Equal("Check-in failed: Invalid brain instance name", receivedResponse.Message);
            Assert.Empty(receivedResponse.TargetTickers);
            Assert.Equal(errorResponse.Timestamp, receivedResponse.Timestamp);
        }

        [Fact(DisplayName = "DTO property validation: creates and validates CheckInData/CheckInResponse properties, ensures integrity")]
        public void DTO_PropertyValidation_ShouldEnsureDataIntegrity()
        {
            // Test CheckInData validation
            var checkInData = new CheckInData
            {
                BrainInstanceName = "ValidBrain",
                Markets = new List<string> { "VALID1", "VALID2" },
                ErrorCount = 10
            };

            // Verify all properties are accessible and have expected values
            Assert.Equal("ValidBrain", checkInData.BrainInstanceName);
            Assert.Equal(2, checkInData.Markets.Count);
            Assert.Contains("VALID1", checkInData.Markets);
            Assert.Contains("VALID2", checkInData.Markets);
            Assert.Equal(10L, checkInData.ErrorCount);

            // Test CheckInResponse validation
            var checkInResponse = new CheckInResponse
            {
                Success = true,
                Message = "Success message",
                TargetTickers = new[] { "TICKER1", "TICKER2" },
                Timestamp = DateTime.UtcNow
            };

            Assert.True(checkInResponse.Success);
            Assert.Equal("Success message", checkInResponse.Message);
            Assert.Equal(2, checkInResponse.TargetTickers.Length);
            Assert.Contains("TICKER1", checkInResponse.TargetTickers);
            Assert.Contains("TICKER2", checkInResponse.TargetTickers);
            Assert.True(checkInResponse.Timestamp > DateTime.MinValue);
        }

        [Fact(DisplayName = "Complex nested structures: multiple round-trips, verifies integrity and consistent JSON")]
        public void ComplexNestedDataStructures_ShouldMaintainIntegrity()
        {
            // Arrange - Test complex nested structures
            var complexData = new CheckInData
            {
                BrainInstanceName = "ComplexBrain",
                Markets = new List<string> { "MARKET_A", "MARKET_B", "MARKET_C", "MARKET_D" },
                ErrorCount = 25,
                LastSnapshot = DateTime.UtcNow
            };

            // Act - Multiple round trips to ensure stability
            var json1 = JsonSerializer.Serialize(complexData, _jsonOptions);
            var intermediate = JsonSerializer.Deserialize<CheckInData>(json1, _jsonOptions);
            var json2 = JsonSerializer.Serialize(intermediate, _jsonOptions);
            var final = JsonSerializer.Deserialize<CheckInData>(json2, _jsonOptions);

            // Assert - Verify multiple serialization cycles maintain integrity
            Assert.NotNull(final);
            Assert.Equal(complexData.BrainInstanceName, final.BrainInstanceName);
            Assert.Equal(complexData.Markets.Count, final.Markets.Count);
            Assert.Equal(complexData.ErrorCount, final.ErrorCount);

            // Verify JSON strings are identical (ensuring consistent serialization)
            Assert.Equal(json1, json2);
        }
    }
}
