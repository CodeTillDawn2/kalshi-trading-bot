using System;
using System.Collections.Generic;
using BacklashDTOs.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace BacklashBot
{
    /// <summary>
    /// Simple console app to test MarketDataConfig instantiation and validation,
    /// ValidateTickerData method with valid and invalid data, and a mock for the retry policy.
    /// </summary>
    public class MarketDataTests
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting MarketData Tests...\n");

            // Test 1: MarketDataConfig Instantiation and Validation
            TestMarketDataConfig();

            // Test 2: ValidateTickerData with Valid Data
            TestValidateTickerDataValid();

            // Test 3: ValidateTickerData with Invalid Data
            TestValidateTickerDataInvalid();

            // Test 4: Mock Retry Policy
            TestRetryPolicyMock();

            Console.WriteLine("All tests completed.");
        }

        /// <summary>
        /// Tests MarketDataConfig instantiation and validation.
        /// Expected Output: Success for valid config, failure for invalid config.
        /// </summary>
        private static void TestMarketDataConfig()
        {
            Console.WriteLine("Test 1: MarketDataConfig Instantiation and Validation");

            // Valid config
            var validConfig = new MarketDataConfig
            {
                SemaphoreTimeoutMs = 5000,
                TickerBatchSize = 100,
                ApiRetryTimeoutMs = 30000
            };

            var validator = validConfig as IValidateOptions<MarketDataConfig>;
            var result = validator.Validate("MarketData", validConfig);

            Console.WriteLine($"Valid Config Validation Result: {result.Succeeded}");
            if (!result.Succeeded)
            {
                foreach (var failure in result.Failures)
                {
                    Console.WriteLine($"  Failure: {failure}");
                }
            }
            // Expected: Succeeded = True

            // Invalid config
            var invalidConfig = new MarketDataConfig
            {
                SemaphoreTimeoutMs = -1, // Invalid
                TickerBatchSize = 0,     // Invalid
                ApiRetryTimeoutMs = 30000
            };

            result = validator.Validate("MarketData", invalidConfig);

            Console.WriteLine($"Invalid Config Validation Result: {result.Succeeded}");
            if (!result.Succeeded)
            {
                foreach (var failure in result.Failures)
                {
                    Console.WriteLine($"  Failure: {failure}");
                }
            }
            // Expected: Succeeded = False, Failures: SemaphoreTimeoutMs must be greater than 0, TickerBatchSize must be greater than 0

            Console.WriteLine();
        }

        /// <summary>
        /// Tests ValidateTickerData with valid data.
        /// Expected Output: True
        /// </summary>
        private static void TestValidateTickerDataValid()
        {
            Console.WriteLine("Test 2: ValidateTickerData with Valid Data");

            // Valid data
            string marketTicker = "TEST-123";
            int price = 50;
            int yesBid = 45;
            int yesAsk = 55;
            int volume = 1000;
            int openInterest = 500;
            int dollarVolume = 50000;
            int dollarOpenInterest = 25000;
            long ts = 1638360000000; // Valid timestamp
            DateTime loggedDate = DateTime.UtcNow;
            DateTime? processedDate = DateTime.UtcNow;

            bool isValid = ValidateTickerData(marketTicker, price, yesBid, yesAsk, volume, openInterest, dollarVolume, dollarOpenInterest, ts, loggedDate, processedDate);

            Console.WriteLine($"Valid Ticker Data Result: {isValid}");
            // Expected: True

            Console.WriteLine();
        }

        /// <summary>
        /// Tests ValidateTickerData with invalid data.
        /// Expected Output: False, with logged warnings
        /// </summary>
        private static void TestValidateTickerDataInvalid()
        {
            Console.WriteLine("Test 3: ValidateTickerData with Invalid Data");

            // Invalid data: price <= 0, yesAsk <= yesBid, volume < 0, etc.
            string marketTicker = "TEST-123";
            int price = 0; // Invalid
            int yesBid = 55;
            int yesAsk = 50; // Invalid: <= yesBid
            int volume = -100; // Invalid
            int openInterest = 500;
            int dollarVolume = 50000;
            int dollarOpenInterest = 25000;
            long ts = 0; // Invalid
            DateTime loggedDate = default(DateTime); // Invalid
            DateTime? processedDate = default(DateTime?); // Invalid

            bool isValid = ValidateTickerData(marketTicker, price, yesBid, yesAsk, volume, openInterest, dollarVolume, dollarOpenInterest, ts, loggedDate, processedDate);

            Console.WriteLine($"Invalid Ticker Data Result: {isValid}");
            // Expected: False (warnings logged to console)

            Console.WriteLine();
        }

        /// <summary>
        /// Tests a mock for the retry policy.
        /// Expected Output: Retry attempts logged, final success or failure.
        /// </summary>
        private static void TestRetryPolicyMock()
        {
            Console.WriteLine("Test 4: Mock Retry Policy");

            // Mock retry policy similar to the one in MarketDataService
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (exception, timeSpan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} for mock operation after {timeSpan.TotalSeconds} seconds. Exception: {exception.Message}");
            });

            // Mock operation that fails twice then succeeds
            int attemptCount = 0;
            Func<Task<string>> mockOperation = async () =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new Exception($"Mock failure on attempt {attemptCount}");
                }
                return "Success";
            };

            // Execute the policy
            var result = retryPolicy.ExecuteAsync(mockOperation).Result;

            Console.WriteLine($"Mock Retry Policy Result: {result}");
            // Expected: Retry 1 after 2 seconds, Retry 2 after 4 seconds, then Success

            Console.WriteLine();
        }

        /// <summary>
        /// Validates ticker data parameters (copied from MarketDataService for testing).
        /// </summary>
        private static bool ValidateTickerData(string marketTicker, int price, int yesBid, int yesAsk, int volume, int openInterest, int dollarVolume, int dollarOpenInterest, long ts, DateTime loggedDate, DateTime? processedDate)
        {
            var errors = new List<string>();

            if (price <= 0) errors.Add($"price <= 0 ({price})");
            if (yesBid < 0) errors.Add($"yesBid < 0 ({yesBid})");
            if (yesAsk <= yesBid) errors.Add($"yesAsk <= yesBid ({yesAsk} <= {yesBid})");
            if (volume < 0) errors.Add($"volume < 0 ({volume})");
            if (openInterest < 0) errors.Add($"openInterest < 0 ({openInterest})");
            if (dollarVolume < 0) errors.Add($"dollarVolume < 0 ({dollarVolume})");
            if (dollarOpenInterest < 0) errors.Add($"dollarOpenInterest < 0 ({dollarOpenInterest})");
            if (ts <= 0) errors.Add($"ts <= 0 ({ts})");
            if (loggedDate == default(DateTime)) errors.Add($"loggedDate is default ({loggedDate})");
            if (processedDate == default(DateTime?)) errors.Add($"processedDate is default ({processedDate})");

            if (errors.Any())
            {
                Console.WriteLine($"Invalid ticker data for {marketTicker}: {string.Join(", ", errors)}");
                return false;
            }

            return true;
        }
    }
}