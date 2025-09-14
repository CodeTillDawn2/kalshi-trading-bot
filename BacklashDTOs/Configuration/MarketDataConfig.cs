using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace BacklashDTOs.Configuration;

/// <summary>
/// Configuration class for market data settings.
/// </summary>
public class MarketDataConfig : IValidateOptions<MarketDataConfig>
{
    /// <summary>
    /// Gets or sets the semaphore timeout in milliseconds. Default is 5000.
    /// </summary>
    public int SemaphoreTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the ticker batch size. Default is 100.
    /// </summary>
    public int TickerBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the API retry timeout in milliseconds. Default is 30000.
    /// </summary>
    public int ApiRetryTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
    public ValidateOptionsResult Validate(string? name, MarketDataConfig options)
    {
        var failures = new List<string>();

        if (options.SemaphoreTimeoutMs <= 0)
        {
            failures.Add($"{nameof(SemaphoreTimeoutMs)} must be greater than 0.");
        }

        if (options.TickerBatchSize <= 0)
        {
            failures.Add($"{nameof(TickerBatchSize)} must be greater than 0.");
        }

        if (options.ApiRetryTimeoutMs <= 0)
        {
            failures.Add($"{nameof(ApiRetryTimeoutMs)} must be greater than 0.");
        }

        return failures.Any() ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}