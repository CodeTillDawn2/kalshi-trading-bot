using KalshiBotAPI.Configuration;
using BacklashBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BacklashBot.KalshiAPI.Interfaces;
using BacklashBot.Services.Interfaces;
using BacklashBot.State.Interfaces;
using BacklashInterfaces.PerformanceMetrics;
using BacklashDTOs.Data;
using BacklashDTOs.Exceptions;
using BacklashDTOs.Helpers;
using BacklashDTOs.KalshiAPI;
using BacklashInterfaces.Constants;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace KalshiBotAPI.KalshiAPI
{
    /// <summary>
    /// Provides a comprehensive service for interacting with the Kalshi trading API.
    /// This service handles authentication, market data retrieval, order management, position tracking,
    /// and various other API operations required for automated trading on the Kalshi platform.
    /// It implements the IKalshiAPIService interface and uses RSA-based authentication with API keys.
    /// The service maintains execution time tracking for performance monitoring, supports parallel processing
    /// for high-volume data operations, and includes configurable parameters for lookback periods and calculations.
    /// Robust error handling with cancellation token support throughout all operations.
    /// </summary>
    public class KalshiAPIService : IKalshiAPIService
    {
        private readonly ILogger<IKalshiAPIService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly KalshiConfig _kalshiConfig;
        private readonly HttpClient _httpClient;
        private readonly RSA _privateKey;
        private readonly string _keyId;
        private readonly string _connectionString;
        private Dictionary<string, string> AuthHeaders = new Dictionary<string, string>();
        private IStatusTrackerService _statusTrackerService;
        private readonly IPerformanceMonitor _performanceMonitor;

        private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _methodExecutionDurations = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _calculationExecutionDurations = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _apiResponseDurations = new();
        private readonly ConcurrentDictionary<string, ConcurrentBag<int>> _errorCounts = new();
        private readonly bool _enablePerformanceMetrics;

        private readonly Dictionary<string, (int Minutes, int DbType, int MaxDays, int CushionSeconds)> _intervals;

        /// <summary>
        /// Initializes a new instance of the KalshiAPIService class.
        /// </summary>
        /// <param name="logger">Logger instance for recording API operations and errors.</param>
        /// <param name="config">Configuration instance for accessing connection strings and settings.</param>
        /// <param name="scopeFactory">Factory for creating service scopes for database operations.</param>
        /// <param name="statusTrackerService">Service for tracking system status and cancellation tokens.</param>
        /// <param name="kalshiConfig">Configuration options specific to Kalshi API integration.</param>
        /// <param name="performanceMonitor">Central performance monitor for unified metrics collection.</param>
        public KalshiAPIService(
            ILogger<IKalshiAPIService> logger,
            IConfiguration config,
            IServiceScopeFactory scopeFactory,
            IStatusTrackerService statusTrackerService,
            IOptions<KalshiConfig> kalshiConfig,
            IPerformanceMonitor performanceMonitor)
        {
            _logger = logger;
            _statusTrackerService = statusTrackerService;
            _kalshiConfig = kalshiConfig.Value;
            _performanceMonitor = performanceMonitor;
            _enablePerformanceMetrics = _kalshiConfig.KalshiAPIServiceEnablePerformanceMetrics;

            // Initialize connection string from configuration
            _connectionString = config.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("DefaultConnection connection string not configured");

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.elections.kalshi.com/trade-api/v2/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            string keyFile = _kalshiConfig.KeyFile ?? throw new ArgumentNullException("Kalshi:KeyFile not configured");
            _keyId = _kalshiConfig.KeyId ?? throw new ArgumentNullException("Kalshi:KeyId not configured");
            _privateKey = RSA.Create();
            _privateKey.ImportFromPem(File.ReadAllText(keyFile));
            _scopeFactory = scopeFactory;

            // Initialize intervals from configuration
            _intervals = new Dictionary<string, (int Minutes, int DbType, int MaxDays, int CushionSeconds)>
            {
                ["minute"] = (1, 1, _kalshiConfig.CandlestickLookback.Minute, _kalshiConfig.CandlestickCushion.Minute),
                ["hour"] = (60, 2, _kalshiConfig.CandlestickLookback.Hour, _kalshiConfig.CandlestickCushion.Hour),
                ["day"] = (1440, 3, _kalshiConfig.CandlestickLookback.Day, _kalshiConfig.CandlestickCushion.Day)
            };
        }

        /// <summary>
        /// Generates authentication headers required for Kalshi API requests.
        /// Creates a timestamp, constructs a message, signs it with the private key, and returns the necessary headers.
        /// </summary>
        /// <param name="method">The HTTP method (e.g., "GET", "POST") for the request.</param>
        /// <param name="path">The API endpoint path for the request.</param>
        /// <returns>A dictionary containing the authentication headers (KALSHI-ACCESS-KEY, KALSHI-ACCESS-SIGNATURE, KALSHI-ACCESS-TIMESTAMP).</returns>
        private Dictionary<string, string> GenerateAuthHeaders(string method, string path)
        {
            var stopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var message = $"{timestamp}{method}{path.Split('?')[0]}";
                var signature = Convert.ToBase64String(_privateKey.SignData(
                    Encoding.UTF8.GetBytes(message),
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss));

                return new Dictionary<string, string>
                {
                    { "KALSHI-ACCESS-KEY", _keyId },
                    { "KALSHI-ACCESS-SIGNATURE", signature },
                    { "KALSHI-ACCESS-TIMESTAMP", timestamp }
                };
            }
            finally
            {
                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    RecordMethodExecutionDuration(nameof(GenerateAuthHeaders), stopwatch.ElapsedMilliseconds);
                }
            }
        }

        /// <summary>
        /// Fetches market data from the Kalshi API based on the provided filters.
        /// This method retrieves market information in batches if tickers are specified, processes the API responses into MarketDTO objects,
        /// and persists them to the database. It handles pagination automatically and supports filtering by various market attributes.
        /// If specific tickers are requested and updateNotFoundToClosed is true, any tickers not returned by the API will be marked as closed in the database.
        /// </summary>
        /// <param name="eventTicker">Optional filter to retrieve markets for a specific event ticker.</param>
        /// <param name="seriesTicker">Optional filter to retrieve markets for a specific series ticker.</param>
        /// <param name="maxCloseTs">Optional filter for markets with close time before this timestamp.</param>
        /// <param name="minCloseTs">Optional filter for markets with close time after this timestamp.</param>
        /// <param name="status">Optional filter for market status (e.g., "active", "closed").</param>
        /// <param name="tickers">Optional array of specific market tickers to retrieve. If provided, markets are fetched in batches of up to 20.</param>
        /// <param name="updateNotFoundToClosed">If true and tickers are specified, marks any requested tickers not found in API responses as closed.</param>
        /// <returns>A tuple containing the count of successfully processed markets and the count of errors encountered.</returns>
        public async Task<(int ProcessedCount, int ErrorCount)> FetchMarketsAsync(
    string? eventTicker = null, string? seriesTicker = null, string? maxCloseTs = null,
    string? minCloseTs = null, string? status = null, string[]? tickers = null, bool updateNotFoundToClosed = true)
        {
            var stopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
            try
            {
                int processedCount = 0;
                int errorCount = 0;
                var foundTickers = new HashSet<string>();
                bool responseWasSuccessful = true;

                // Build base (non-list) query params once
                var baseQueryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(eventTicker)) baseQueryParams["event_ticker"] = eventTicker;
                if (!string.IsNullOrEmpty(seriesTicker)) baseQueryParams["series_ticker"] = seriesTicker;
                if (!string.IsNullOrEmpty(maxCloseTs)) baseQueryParams["max_close_ts"] = maxCloseTs;
                if (!string.IsNullOrEmpty(minCloseTs)) baseQueryParams["min_close_ts"] = minCloseTs;
                if (!string.IsNullOrEmpty(status)) baseQueryParams["status"] = status;

                // Determine batches (<=20) if tickers list provided, else single null batch
                var batches = (tickers != null && tickers.Length > 0)
                    ? Enumerable.Range(0, (tickers.Length + 19) / 20)
                                .Select(i => tickers.Skip(i * 20).Take(20).ToArray())
                    : new[] { Array.Empty<string>() };

                foreach (var batch in batches)
                {
                    _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                    // Clone base params and add current batch tickers if any
                    var queryParams = new Dictionary<string, string>(baseQueryParams);
                    if (batch.Length > 0)
                        queryParams["tickers"] = string.Join(",", batch);

                    string cursor = "";

                    while (true)
                    {
                        _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

                        if (!string.IsNullOrEmpty(cursor)) queryParams["cursor"] = cursor;

                        string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                        string url = $"markets{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";

                        var headers = GenerateAuthHeaders("GET", "/trade-api/v2/markets");
                        var request = new HttpRequestMessage(HttpMethod.Get, url);
                        foreach (var header in headers) request.Headers.Add(header.Key, header.Value);

                        MarketResponse? responseData = null;
                        string jsonString = "";

                        try
                        {
                            var apiStopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
                            var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                            if (apiStopwatch != null)
                            {
                                apiStopwatch.Stop();
                                RecordApiResponseDuration(nameof(FetchMarketsAsync), apiStopwatch.ElapsedMilliseconds);
                            }
                            response.EnsureSuccessStatusCode();
                            jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());
                            responseData = JsonSerializer.Deserialize<MarketResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        catch (JsonException ex)
                        {
                            responseWasSuccessful = false;
                            _logger.LogWarning(ex, "Failed to deserialize response for {Url}, json={0}", url, jsonString);
                            RecordError(nameof(FetchMarketsAsync));
                        }
                        catch (HttpRequestException ex)
                        {
                            responseWasSuccessful = false;
                            _logger.LogWarning("HTTP request failed for {Url}. Message: {message}", url, ex.Message);
                            errorCount++;
                            RecordError(nameof(FetchMarketsAsync));
                            break;
                        }

                        if (responseData == null)
                            _logger.LogWarning(new Exception($"No response data found. String: {jsonString}"), "No response data found. String: {jsonString}", jsonString);

                        try
                        {
                            var marketsToUpdate = new ConcurrentBag<MarketDTO>();

#pragma warning disable CS1998
                            await Parallel.ForEachAsync(responseData?.Markets ?? new List<KalshiMarket>(), new ParallelOptions { CancellationToken = _statusTrackerService.GetCancellationToken() }, async (apiMarket, cancellationToken) =>
#pragma warning restore CS1998
                            {
                                var calcStopwatch = Stopwatch.StartNew();
                                try
                                {
                                    foundTickers.Add(apiMarket.Ticker);

                                    var market = new MarketDTO
                                    {
                                        market_ticker = apiMarket.Ticker,
                                        event_ticker = apiMarket.EventTicker,
                                        market_type = apiMarket.MarketType,
                                        title = apiMarket.Title,
                                        subtitle = apiMarket.Subtitle,
                                        yes_sub_title = apiMarket.YesSubTitle,
                                        no_sub_title = apiMarket.NoSubTitle,
                                        open_time = apiMarket.OpenTime,
                                        close_time = apiMarket.CloseTime,
                                        expected_expiration_time = apiMarket.ExpectedExpirationTime,
                                        expiration_time = apiMarket.ExpirationTime,
                                        latest_expiration_time = apiMarket.LatestExpirationTime,
                                        settlement_timer_seconds = apiMarket.SettlementTimerSeconds,
                                        status = apiMarket.Status,
                                        response_price_units = apiMarket.ResponsePriceUnits,
                                        notional_value = apiMarket.NotionalValue,
                                        tick_size = apiMarket.TickSize,
                                        yes_bid = apiMarket.YesBid,
                                        yes_ask = apiMarket.YesAsk,
                                        no_bid = apiMarket.NoBid,
                                        no_ask = apiMarket.NoAsk,
                                        last_price = apiMarket.LastPrice,
                                        previous_yes_bid = apiMarket.PreviousYesBid,
                                        previous_yes_ask = apiMarket.PreviousYesAsk,
                                        previous_price = apiMarket.PreviousPrice,
                                        volume = apiMarket.Volume,
                                        volume_24h = apiMarket.Volume24h,
                                        liquidity = apiMarket.Liquidity,
                                        open_interest = apiMarket.OpenInterest,
                                        result = apiMarket.Result,
                                        can_close_early = apiMarket.CanCloseEarly,
                                        expiration_value = apiMarket.ExpirationValue,
                                        risk_limit_cents = apiMarket.RiskLimitCents,
                                        strike_type = apiMarket.StrikeType,
                                        floor_strike = (apiMarket.StrikeType == "" || apiMarket.FloorStrike == null) ? 0 : apiMarket.FloorStrike,
                                        rules_primary = apiMarket.RulesPrimary,
                                        rules_secondary = apiMarket.RulesSecondary,
                                        APILastFetchedDate = DateTime.Now,
                                        LastModifiedDate = DateTime.Now,
                                        category = ""
                                    };

                                    marketsToUpdate.Add(market);
                                    Interlocked.Increment(ref processedCount);
                                }
                                finally
                                {
                                    calcStopwatch.Stop();
                                    RecordCalculationExecutionDuration("ProcessMarketDTO", calcStopwatch.ElapsedMilliseconds);
                                }
                            });

                            using var scope = _scopeFactory.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();
                            await context.AddOrUpdateMarkets(marketsToUpdate.ToList());
                            await Task.CompletedTask;
                        }
                        catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key"))
                        {
                            if (stopwatch != null)
                            {
                                stopwatch.Stop();
                            }
                            _logger.LogWarning(ex, "Duplicate market encountered while saving market data");
                            return (0, 1);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("FetchMarketsAsync cancelled");
                        }
                        catch (Exception ex)
                        {
                            if (tickers?.Count() == 1)
                            {
                                _logger.LogWarning(new MarketTransientFailureException(tickers[0], $"Failed to save market {tickers[0]}"),
                                    "Failed to save market {Ticker}. Message: {message}, Inner message {inmsg}, Stack trace: {st}",
                                    tickers[0], ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Failed to save market {Ticker}. Message: {message}, Inner message {inmsg}, Stack trace: {st}",
                                    tickers, ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                            }
                            errorCount++;
                        }

                        cursor = responseData?.Cursor ?? "";
                        if (string.IsNullOrEmpty(cursor)) break;
                    }
                }

                // Update not-found tickers to closed only if all API requests succeeded
                if (tickers?.Length > 0 && updateNotFoundToClosed && responseWasSuccessful)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                    foreach (var ticker in tickers.Where(t => !foundTickers.Contains(t)))
                    {
                        var existingMarket = await context.GetMarketByTicker(ticker);

                        if (existingMarket != null)
                        {
                            if (existingMarket.status == KalshiConstants.Status_Active)
                                existingMarket.status = KalshiConstants.Status_Closed;
                            existingMarket.LastModifiedDate = DateTime.Now;

                            try
                            {
                                await context.AddOrUpdateMarket(existingMarket);
                                processedCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(
                                    "Failed to update market {Ticker} to closed. Message: {message}, Inner message {inmsg}, Stack trace: {st}",
                                    ticker, ex.Message, ex.InnerException != null ? ex.InnerException.Message : "", ex.StackTrace);
                                errorCount++;
                            }
                        }
                    }
                }

                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    RecordMethodExecutionDuration(nameof(FetchMarketsAsync), stopwatch.ElapsedMilliseconds);
                }
                return (processedCount, errorCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchMarketsAsync was cancelled");
                return (0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in FetchMarketsAsync: {ExceptionType} - {Message}, Inner exception {0}", ex.GetType().Name, ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                return (0, 0);
            }
        }


        /// <summary>
        /// Retrieves the current exchange schedule from the Kalshi API.
        /// This includes trading hours, maintenance windows, and other operational schedule information for the exchange.
        /// </summary>
        /// <returns>The exchange schedule response containing schedule details, or an empty response if the operation fails.</returns>
        public async Task<ExchangeScheduleResponse> GetExchangeScheduleAsync()
        {
            var stopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
            string url = "exchange/schedule";
            try
            {
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/exchange/schedule");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var scheduleData = JsonSerializer.Deserialize<ExchangeScheduleResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (stopwatch != null)
                {
                    stopwatch.Stop();
                    RecordMethodExecutionDuration(nameof(GetExchangeScheduleAsync), stopwatch.ElapsedMilliseconds);
                }
                return scheduleData ?? new ExchangeScheduleResponse();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetExchangeScheduleAsync was cancelled");
                return new ExchangeScheduleResponse();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetExchangeScheduleAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                RecordError(nameof(GetExchangeScheduleAsync));
                return new ExchangeScheduleResponse();
            }
        }

        /// <summary>
        /// Creates a new order on the Kalshi platform for the specified market.
        /// Validates the order request parameters and sends the order to the API.
        /// Supports both market and limit orders with appropriate validation for each type.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market for which to create the order.</param>
        /// <param name="orderRequest">The order details including action, type, side, count, and pricing information.</param>
        /// <returns>The API response containing order details if successful, null if the operation fails or is cancelled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when marketTicker is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when required order fields are missing or invalid.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to the API fails.</exception>
        public async Task<CreateOrderResponse?> CreateOrderAsync(string marketTicker, CreateOrderRequest orderRequest)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(marketTicker))
                {
                    throw new ArgumentNullException(nameof(marketTicker), "Market ticker is required");
                }
                if (string.IsNullOrEmpty(orderRequest.Action) || string.IsNullOrEmpty(orderRequest.Type) ||
                    string.IsNullOrEmpty(orderRequest.Side) || orderRequest.Count <= 0)
                {
                    throw new ArgumentException("Required fields: action, type, side, count");
                }
                if (orderRequest.Type == "limit" && (orderRequest.YesPrice == null && orderRequest.NoPrice == null) ||
                    (orderRequest.YesPrice != null && orderRequest.NoPrice != null))
                {
                    throw new ArgumentException("For limit orders, exactly one of yes_price or no_price must be provided");
                }
                if (orderRequest.Type == "market" && orderRequest.Action == "buy" && orderRequest.BuyMaxCost == null)
                {
                    throw new ArgumentException("For market buy, buy_max_cost is recommended");
                }

                string url = $"portfolio/orders";
                var headers = GenerateAuthHeaders("POST", $"/trade-api/v2/portfolio/orders");
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var jsonContent = JsonSerializer.Serialize(orderRequest, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var orderResponse = JsonSerializer.Deserialize<CreateOrderResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(CreateOrderAsync), stopwatch.ElapsedMilliseconds);
                return orderResponse;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("CreateOrderAsync was cancelled");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error creating order for {MarketTicker}", marketTicker);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in CreateOrderAsync for {MarketTicker}: {ExceptionType} - {Message}",
                    marketTicker, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Cancels an existing order on the Kalshi platform.
        /// Sends a DELETE request to the API to cancel the specified order by its ID.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order to cancel.</param>
        /// <returns>The API response containing cancellation details if successful, null if the operation fails or is cancelled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderId is null or empty.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to the API fails.</exception>
        public async Task<CancelOrderResponse?> CancelOrderAsync(string orderId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    throw new ArgumentNullException(nameof(orderId), "Order ID is required");
                }

                string url = $"portfolio/orders/{Uri.EscapeDataString(orderId)}";
                var headers = GenerateAuthHeaders("DELETE", $"/trade-api/v2/portfolio/orders/{orderId}");
                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var cancelResponse = JsonSerializer.Deserialize<CancelOrderResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(CancelOrderAsync), stopwatch.ElapsedMilliseconds);
                return cancelResponse;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("CancelOrderAsync was cancelled");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP error canceling order {OrderId}", orderId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in CancelOrderAsync for {OrderId}: {ExceptionType} - {Message}",
                    orderId, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Fetches detailed information about a specific series from the Kalshi API.
        /// Retrieves series metadata including tags and settlement sources, processes the data into DTOs,
        /// and persists the information to the database for future reference.
        /// </summary>
        /// <param name="seriesTicker">The ticker symbol of the series to retrieve.</param>
        /// <returns>The API response containing series details if successful, null if the operation fails or is cancelled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when seriesTicker is null or empty.</exception>
        public async Task<SeriesResponse?> FetchSeriesAsync(string seriesTicker)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(seriesTicker))
                {
                    throw new ArgumentNullException(nameof(seriesTicker), "Series ticker is required");
                }

                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();
                string url = $"series/{Uri.EscapeDataString(seriesTicker)}";
                var headers = GenerateAuthHeaders("GET", $"/trade-api/v2/series/{seriesTicker}");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());
                var seriesResponse = JsonSerializer.Deserialize<SeriesResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                if (seriesResponse == null) throw new Exception(url + $" returned null response. JSON: {jsonString}");

                if (seriesResponse.Series == null)
                {
                    throw new Exception("Series response is null");
                }

                var newTags = (seriesResponse.Series.Tags ?? new List<string>())
                    .Select(tag => new SeriesTagDTO
                    {
                        series_ticker = seriesResponse.Series.Ticker,
                        tag = tag
                    })
                    .ToList();

                var settlementSources = seriesResponse.Series.SettlementSources ?? new List<SeriesSettlementSourceDTO>();
                var newSources = settlementSources
                  .Select(ss => new SeriesSettlementSourceDTO
                  {
                      series_ticker = seriesResponse.Series.Ticker,
                      name = ss.name,
                      url = ss.url
                  })
                  .ToList();

                SeriesDTO series = new SeriesDTO
                {
                    series_ticker = seriesResponse.Series.Ticker,
                    frequency = seriesResponse.Series.Frequency,
                    title = seriesResponse.Series.Title,
                    category = seriesResponse.Series.Category,
                    contract_url = seriesResponse.Series.ContractUrl,
                    CreatedDate = DateTime.Now,
                    Tags = newTags,
                    SettlementSources = newSources
                };

                await context.AddOrUpdateSeries(series);

                _logger.LogInformation("Successfully fetched and saved series data for ticker: {SeriesTicker}", seriesTicker);
                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(FetchSeriesAsync), stopwatch.ElapsedMilliseconds);
                return seriesResponse;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchSeriesAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in FetchSeriesAsync for ticker: {SeriesTicker}: {ExceptionType} - {Message} - {Inner}"
                    , seriesTicker, ex.GetType().Name, ex.Message, ex.InnerException != null ? ex.InnerException.Message : "N/A");
                return null;
            }
        }

        /// <summary>
        /// Fetches detailed information about a specific event from the Kalshi API.
        /// Optionally includes nested market data if requested. Processes the event data into DTOs
        /// and persists both event and market information to the database.
        /// </summary>
        /// <param name="eventTicker">The ticker symbol of the event to retrieve.</param>
        /// <param name="withNestedMarkets">If true, includes detailed market data nested within the event response.</param>
        /// <returns>The API response containing event details if successful, null if the operation fails or is cancelled.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventTicker is null or empty.</exception>
        public async Task<EventResponse?> FetchEventAsync(string eventTicker, bool withNestedMarkets = false)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(eventTicker))
                {
                    throw new ArgumentNullException(nameof(eventTicker), "Event ticker is required");
                }

                string url = $"events/{Uri.EscapeDataString(eventTicker)}";
                if (withNestedMarkets)
                {
                    url += "?with_nested_markets=true";
                }

                var headers = GenerateAuthHeaders("GET", $"/trade-api/v2/events/{eventTicker}");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());
                var eventResponse = JsonSerializer.Deserialize<EventResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                if (eventResponse == null || eventResponse.Event == null)
                {
                    throw new Exception("Event response is null");
                }

                var eventDTO = new EventDTO
                {
                    event_ticker = eventResponse.Event.EventTicker,
                    series_ticker = eventResponse.Event.SeriesTicker,
                    title = eventResponse.Event.Title,
                    sub_title = eventResponse.Event.SubTitle,
                    collateral_return_type = eventResponse.Event.CollateralReturnType,
                    mutually_exclusive = eventResponse.Event.MutuallyExclusive,
                    category = eventResponse.Event.Category ?? "",
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };

                await context.AddOrUpdateEvent(eventDTO);

                string extractedCategory = eventResponse.Event.Category ?? "";

                if (withNestedMarkets && eventResponse.Event.Markets != null && eventResponse.Event.Markets.Any())
                {
                    foreach (var apiMarket in eventResponse.Event.Markets)
                    {
                        using var marketScope = _scopeFactory.CreateScope();
                        var marketContext = marketScope.ServiceProvider.GetRequiredService<IBacklashBotContext>();


                        var market = new MarketDTO
                        {
                            market_ticker = apiMarket.Ticker,
                            event_ticker = apiMarket.EventTicker,
                            market_type = apiMarket.MarketType,
                            title = apiMarket.Title,
                            subtitle = apiMarket.Subtitle,
                            yes_sub_title = apiMarket.YesSubTitle,
                            no_sub_title = apiMarket.NoSubTitle,
                            open_time = apiMarket.OpenTime,
                            close_time = apiMarket.CloseTime,
                            expected_expiration_time = apiMarket.ExpectedExpirationTime,
                            expiration_time = apiMarket.ExpirationTime,
                            latest_expiration_time = apiMarket.LatestExpirationTime,
                            settlement_timer_seconds = apiMarket.SettlementTimerSeconds,
                            status = apiMarket.Status,
                            response_price_units = apiMarket.ResponsePriceUnits,
                            notional_value = apiMarket.NotionalValue,
                            tick_size = apiMarket.TickSize,
                            yes_bid = apiMarket.YesBid,
                            yes_ask = apiMarket.YesAsk,
                            no_bid = apiMarket.NoBid,
                            no_ask = apiMarket.NoAsk,
                            last_price = apiMarket.LastPrice,
                            previous_yes_bid = apiMarket.PreviousYesBid,
                            previous_yes_ask = apiMarket.PreviousYesAsk,
                            previous_price = apiMarket.PreviousPrice,
                            volume = apiMarket.Volume,
                            volume_24h = apiMarket.Volume24h,
                            liquidity = apiMarket.Liquidity,
                            open_interest = apiMarket.OpenInterest,
                            result = apiMarket.Result,
                            can_close_early = apiMarket.CanCloseEarly,
                            expiration_value = apiMarket.ExpirationValue,
                            category = extractedCategory,
                            risk_limit_cents = apiMarket.RiskLimitCents,
                            strike_type = apiMarket.StrikeType,
                            floor_strike = apiMarket.StrikeType == "" ? 0 : apiMarket.FloorStrike,
                            rules_primary = apiMarket.RulesPrimary,
                            rules_secondary = apiMarket.RulesSecondary,
                            LastModifiedDate = DateTime.Now,
                            APILastFetchedDate = DateTime.Now
                        };

                        await marketContext.AddOrUpdateMarket(market);
                    }
                }

                _logger.LogInformation("Successfully fetched and saved event data for ticker: {EventTicker}", eventTicker);
                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(FetchEventAsync), stopwatch.ElapsedMilliseconds);
                return eventResponse;
            }
            catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key"))
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Duplicate event ticker {EventTicker} encountered while saving event data", eventTicker);
                return null;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogDebug("FetchEventAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning("Unexpected error in FetchEventAsync for ticker: {EventTicker}: {ExceptionType} - {Message}, Inner {0}"
                    , eventTicker, ex.GetType().Name, ex.Message, ex.InnerException != null ? ex.InnerException.Message : "");
                return null;
            }
        }

        /// <summary>
        /// Fetches the user's current positions from the Kalshi API.
        /// Retrieves position data with support for pagination, filtering, and settlement status.
        /// Processes the API responses into position DTOs and updates the database, removing positions
        /// that are no longer present in the API response.
        /// </summary>
        /// <param name="cursor">Optional pagination cursor for retrieving subsequent pages of positions.</param>
        /// <param name="limit">Optional limit on the number of positions to retrieve per request.</param>
        /// <param name="countFilter">Optional filter for positions based on count (e.g., "gt:0" for positions greater than 0).</param>
        /// <param name="settlementStatus">Optional filter for positions by settlement status.</param>
        /// <returns>A tuple containing the count of successfully processed positions and the count of errors encountered.</returns>
        public async Task<(int ProcessedCount, int ErrorCount)> FetchPositionsAsync(
            string? cursor = null,
            int? limit = null,
            string? countFilter = null,
            string? settlementStatus = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(cursor)) queryParams["cursor"] = cursor;
                if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();
                if (!string.IsNullOrEmpty(countFilter)) queryParams["count_filter"] = countFilter;
                if (!string.IsNullOrEmpty(settlementStatus)) queryParams["settlement_status"] = settlementStatus;

                int processedCount = 0;
                int errorCount = 0;
                string currentCursor = cursor ?? "";
                var allMarketPositions = new List<MarketPositionApi>();

                while (true)
                {
                    if (!string.IsNullOrEmpty(currentCursor)) queryParams["cursor"] = currentCursor;
                    string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    string url = $"portfolio/positions{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";

                    var headers = GenerateAuthHeaders("GET", "/trade-api/v2/portfolio/positions");
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    try
                    {
                        var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                        response.EnsureSuccessStatusCode();
                        var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                        var responseData = JsonSerializer.Deserialize<PositionsResponse>(
                            jsonString,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (responseData == null)
                        {
                            _logger.LogWarning("Failed to deserialize positions response");
                            continue;
                        }

                        var marketPositions = responseData.MarketPositions ?? new List<MarketPositionApi>();
                        if (marketPositions.Count > 0)
                        {
                            allMarketPositions.AddRange(marketPositions);
                        }

                        currentCursor = responseData.Cursor ?? "";
                        if (string.IsNullOrEmpty(currentCursor)) break;
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "API request failed for {Url}", url);
                        errorCount += allMarketPositions.Count;
                        return (processedCount, errorCount);
                    }
                }

                if (allMarketPositions.Count == 0)
                {
                    _logger.LogWarning("No positions found in API response.");
                    return (0, 0);
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                var existingPositions = await context.GetMarketPositions();
                var apiTickerSet = allMarketPositions.Select(p => p.Ticker).ToHashSet();

                foreach (var apiPosition in allMarketPositions)
                {
                    using var positionScope = _scopeFactory.CreateScope();
                    var positionContext = positionScope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                    var position = new MarketPositionDTO
                    {
                        Ticker = apiPosition.Ticker,
                        TotalTraded = apiPosition.TotalTraded,
                        Position = apiPosition.Position,
                        MarketExposure = apiPosition.MarketExposure,
                        RealizedPnl = apiPosition.RealizedPnl,
                        RestingOrdersCount = apiPosition.RestingOrdersCount,
                        FeesPaid = apiPosition.FeesPaid,
                        LastUpdatedUTC = apiPosition.LastUpdatedTs,
                        LastModified = DateTime.UtcNow,
                    };

                    try
                    {
                        await context.AddOrUpdateMarketPosition(position);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save position {Ticker}", position.Ticker);
                        errorCount++;
                    }
                }

                foreach (var positionToRemove in existingPositions.Where(ep => !apiTickerSet.Contains(ep.Ticker)))
                {

                    try
                    {
                        await context.RemoveMarketPosition(positionToRemove.Ticker);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove position {Ticker}", positionToRemove.Ticker);
                        errorCount++;
                    }
                }
                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(FetchPositionsAsync), stopwatch.ElapsedMilliseconds);
                return (processedCount, errorCount);
            }
            catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key"))
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "Duplicate position encountered while saving event data");
                return (0, 1);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchPositionsAsync was cancelled");
                return (0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in FetchPositionsAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return (0, 1);
            }

        }

        /// <summary>
        /// Fetches candlestick data for a specific market and time interval from the Kalshi API.
        /// Supports minute, hour, and day intervals with automatic pagination and rate limiting handling.
        /// Processes the data into candlestick DTOs and persists them to the database.
        /// Optionally updates the market's last candlestick timestamp for tracking purposes.
        /// </summary>
        /// <param name="seriesTicker">The ticker symbol of the series containing the market.</param>
        /// <param name="marketTicker">The ticker symbol of the market for which to retrieve candlesticks.</param>
        /// <param name="interval">The time interval for candlesticks ("minute", "hour", or "day").</param>
        /// <param name="startTs">The starting timestamp for the data range (Unix timestamp).</param>
        /// <param name="endTs">Optional ending timestamp for the data range. If not provided, calculated based on interval.</param>
        /// <param name="updateLastCandlestick">If true and interval is "minute", updates the market's last candlestick timestamp.</param>
        /// <returns>A tuple containing the count of successfully processed candlesticks and the count of errors encountered.</returns>
        /// <exception cref="ArgumentException">Thrown when interval is invalid.</exception>
        /// <exception cref="HttpRequestException">Thrown when the HTTP request to the API fails.</exception>
        public async Task<(int ProcessedCount, int ErrorCount)> FetchCandlesticksAsync(
            string seriesTicker,
            string marketTicker,
            string interval,
            long startTs,
            long? endTs = null,
            bool updateLastCandlestick = true)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (!_intervals.ContainsKey(interval))
                {
                    _logger.LogError("Invalid interval: {Interval}", interval);
                    return (0, 1);
                }

                var (minutes, dbType, maxDays, cushionSeconds) = _intervals[interval];
                long maxSeconds = maxDays * 24 * 60 * 60;
                long currentStart = startTs;
                DateTime now = DateTime.UtcNow;
                long finalEndTs = endTs ?? UnixHelper.ConvertToUnixTimestamp(now);
                switch (interval)
                {
                    case "minute":
                        finalEndTs = UnixHelper.ConvertToUnixTimestamp(now.AddMinutes(-1));
                        break;
                    case "hour":
                        finalEndTs = UnixHelper.ConvertToUnixTimestamp(now.AddHours(-1));
                        break;
                    case "day":
                        finalEndTs = UnixHelper.ConvertToUnixTimestamp(now.AddDays(-1));
                        break;
                }
                var candlesticks = new List<APICandlestick>();
                int processedCount = 0;
                int errorCount = 0;

                while (currentStart < finalEndTs)
                {
                    long currentEnd = Math.Min(currentStart + maxSeconds, finalEndTs);
                    var queryParams = new Dictionary<string, string>
                    {
                        ["period_interval"] = minutes.ToString(),
                        ["start_ts"] = currentStart.ToString(),
                        ["end_ts"] = currentEnd.ToString()
                    };

                    string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    string url = $"series/{seriesTicker}/markets/{marketTicker}/candlesticks?{queryString}";
                    var headers = GenerateAuthHeaders("GET", $"/trade-api/v2/series/{seriesTicker}/markets/{marketTicker}/candlesticks");
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    try
                    {
                        var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());

                        // Handle rate limiting (429 Too Many Requests)
                        if (response.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("Rate limit exceeded for candlesticks API. Delaying 30 seconds before retrying. Market: {MarketTicker}, URL: {Url}",
                                marketTicker, url);
                            await Task.Delay(TimeSpan.FromSeconds(30), _statusTrackerService.GetCancellationToken());
                            continue; // Retry the same request after delay
                        }

                        response.EnsureSuccessStatusCode();
                        var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                        var responseData = JsonSerializer.Deserialize<CandlestickResponse>(
                            jsonString,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (responseData?.Candlesticks != null && responseData.Candlesticks.Count > 0)
                        {
                            candlesticks.AddRange(responseData.Candlesticks);
                        }

                        currentStart = currentEnd - cushionSeconds;
                        if (currentEnd >= finalEndTs) break;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("FetchCandlesticksAsync was cancelled");
                        return (0, 0);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(new CandlestickFetchException(marketTicker, $"API request failed for {url}", ex),
                            "API request failed for {Url}", url);
                        errorCount++;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while requesting API {Url}", url);
                        errorCount++;
                        break;
                    }
                }

                _logger.LogInformation("Fetched {Count} candlesticks for {MarketTicker} from API", candlesticks.Count, marketTicker);

                await Parallel.ForEachAsync(candlesticks, new ParallelOptions { CancellationToken = _statusTrackerService.GetCancellationToken() }, async (apiCandlestick, cancellationToken) =>
                {
                    var calcStopwatch = Stopwatch.StartNew();
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                        var endDateTime = DateTimeOffset.FromUnixTimeSeconds(apiCandlestick.EndPeriodTs).UtcDateTime;
                        if (endDateTime > now)
                        {
                            _logger.LogWarning("Future candlestick timestamp detected for {MarketTicker}, {Interval}: Original={Original}, Adjusting to={Adjusted}",
                                marketTicker, interval, endDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                            apiCandlestick.EndPeriodTs = UnixHelper.ConvertToUnixTimestamp(now);
                        }

                        if (apiCandlestick == null) return;

                        var candlestick = new CandlestickDTO
                        {
                            market_ticker = marketTicker,
                            interval_type = dbType,
                            end_period_ts = apiCandlestick.EndPeriodTs,
                            open_interest = apiCandlestick.OpenInterest,
                            volume = apiCandlestick.Volume,
                            price_close = apiCandlestick.Price?.Close ?? 0,
                            price_high = apiCandlestick.Price?.High ?? 0,
                            price_low = apiCandlestick.Price?.Low ?? 0,
                            price_mean = apiCandlestick.Price?.Mean ?? 0,
                            price_open = apiCandlestick.Price?.Open ?? 0,
                            price_previous = apiCandlestick.Price?.Previous ?? 0,
                            yes_ask_close = apiCandlestick.YesAsk?.Close ?? 0,
                            yes_ask_high = apiCandlestick.YesAsk?.High ?? 0,
                            yes_ask_low = apiCandlestick.YesAsk?.Low ?? 0,
                            yes_ask_open = apiCandlestick.YesAsk?.Open ?? 0,
                            yes_bid_close = apiCandlestick.YesBid?.Close ?? 0,
                            yes_bid_high = apiCandlestick.YesBid?.High ?? 0,
                            yes_bid_low = apiCandlestick.YesBid?.Low ?? 0,
                            yes_bid_open = apiCandlestick.YesBid?.Open ?? 0
                        };

                        await context.AddOrUpdateCandlestick(candlestick);
                        Interlocked.Increment(ref processedCount);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("FetchCandlesticksAsync was cancelled");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save candlestick for {MarketTicker}", marketTicker);
                        Interlocked.Increment(ref errorCount);
                    }
                    finally
                    {
                        calcStopwatch.Stop();
                        RecordCalculationExecutionDuration("ProcessCandlestick", calcStopwatch.ElapsedMilliseconds);
                    }
                });

                if (candlesticks.Count > 0 && interval == "minute" && updateLastCandlestick)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                    try
                    {
                        _logger.LogInformation("API: Updating last candlestick for market {0}", marketTicker);
                        await context.UpdateMarketLastCandlestick(marketTicker);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to update market {MarketTicker} LastCandlestick. Exception: {0} Inner Exception: {1}",
                            marketTicker, ex.Message, ex.InnerException != null ? ex.InnerException.Message : "N/A");
                        errorCount++;
                    }
                }

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(FetchCandlesticksAsync), stopwatch.ElapsedMilliseconds);
                return (processedCount, errorCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchCandlesticksAsync was cancelled");
                return (0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(new CandlestickTransientFailureException(marketTicker, $"Unexpected error in FetchCandlesticksAsync for {marketTicker}, interval: {interval}: {ex.GetType().Name} - {ex.Message}"),
                    "Unexpected error in FetchCandlesticksAsync for {MarketTicker}, interval: {Interval}: {ExceptionType} - {Message}", marketTicker, interval, ex.GetType().Name, ex.Message);
                return (0, 0);
            }
        }

        /// <summary>
        /// Retrieves the current account balance from the Kalshi API.
        /// Returns the balance in cents as a long integer.
        /// </summary>
        /// <returns>The current account balance in cents, or 0 if the operation fails.</returns>
        public async Task<long> GetBalanceAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            string url = "portfolio/balance";
            try
            {

                _logger.LogDebug("Fetching account balance");

                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/portfolio/balance");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var apiStopwatch = _enablePerformanceMetrics ? Stopwatch.StartNew() : null;
                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                if (apiStopwatch != null)
                {
                    apiStopwatch.Stop();
                    RecordApiResponseDuration(nameof(GetExchangeScheduleAsync), apiStopwatch.ElapsedMilliseconds);
                }
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var balanceData = JsonSerializer.Deserialize<BalanceResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (balanceData == null)
                {
                    _logger.LogWarning("Failed to deserialize balance response");
                    return 0;
                }

                _logger.LogInformation("Balance: {Balance}", balanceData.Balance);
                return balanceData.Balance;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetBalanceAsync was cancelled");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetBalanceAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return 0;
            }
            finally
            {
                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetBalanceAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Retrieves the current operational status of the Kalshi exchange.
        /// This includes information about whether the exchange is open for trading, in maintenance, or closed.
        /// </summary>
        /// <returns>The exchange status information, or an empty status object if the operation fails.</returns>
        public async Task<ExchangeStatus> GetExchangeStatusAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            string url = "exchange/status";
            try
            {
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/exchange/status");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                string? jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                JsonSerializerOptions? options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                ExchangeStatus? statusData = JsonSerializer.Deserialize<ExchangeStatus>(
                    jsonString,
                    options
                );
                return statusData ?? new ExchangeStatus();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetExchangeStatusAsync was cancelled");
                return new ExchangeStatus();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetExchangeStatusAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return new ExchangeStatus();
            }
            finally
            {
                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetExchangeStatusAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Fetches order data from the Kalshi API with support for various filters and pagination.
        /// Retrieves orders based on ticker, event ticker, timestamp ranges, status, and other criteria.
        /// Processes the API responses into order DTOs and persists them to the database.
        /// </summary>
        /// <param name="ticker">Optional filter for orders by market ticker.</param>
        /// <param name="eventTicker">Optional filter for orders by event ticker.</param>
        /// <param name="minTs">Optional minimum timestamp filter for order creation time.</param>
        /// <param name="maxTs">Optional maximum timestamp filter for order creation time.</param>
        /// <param name="status">Optional filter for order status (e.g., "pending", "filled").</param>
        /// <param name="cursor">Optional pagination cursor for retrieving subsequent pages of orders.</param>
        /// <param name="limit">Optional limit on the number of orders to retrieve per request.</param>
        /// <returns>A tuple containing the count of successfully processed orders and the count of errors encountered.</returns>
        public async Task<(int ProcessedCount, int ErrorCount)> FetchOrdersAsync(
            string? ticker = null,
            string? eventTicker = null,
            long? minTs = null,
            long? maxTs = null,
            string? status = null,
            string? cursor = null,
            int? limit = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(ticker)) queryParams["ticker"] = ticker;
                if (!string.IsNullOrEmpty(eventTicker)) queryParams["event_ticker"] = eventTicker;
                if (minTs.HasValue) queryParams["min_ts"] = minTs.Value.ToString();
                if (maxTs.HasValue) queryParams["max_ts"] = maxTs.Value.ToString();
                if (!string.IsNullOrEmpty(status)) queryParams["status"] = status;
                if (!string.IsNullOrEmpty(cursor)) queryParams["cursor"] = cursor;
                if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

                int processedCount = 0;
                int errorCount = 0;
                string currentCursor = cursor ?? "";
                var allOrders = new List<OrderApi>();

                while (true)
                {
                    if (!string.IsNullOrEmpty(currentCursor)) queryParams["cursor"] = currentCursor;
                    string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                    string url = $"portfolio/orders{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";

                    var headers = GenerateAuthHeaders("GET", "/trade-api/v2/portfolio/orders");
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }

                    try
                    {
                        var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                        response.EnsureSuccessStatusCode();
                        var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                        var responseData = JsonSerializer.Deserialize<OrdersResponse>(
                            jsonString,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        if (responseData == null)
                        {
                            _logger.LogWarning("Failed to deserialize orders response");
                            break;
                        }

                        var orders = responseData.Orders ?? new List<OrderApi>();
                        if (orders.Count > 0)
                        {
                            allOrders.AddRange(orders);
                        }

                        currentCursor = responseData.Cursor ?? "";
                        if (string.IsNullOrEmpty(currentCursor)) break;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("FetchOrdersAsync was cancelled");
                        break;
                    }
                }

                if (allOrders.Count == 0)
                {
                    _logger.LogWarning("No orders found in API response.");
                    return (0, 0);
                }

                foreach (var apiOrder in allOrders)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                    var order = new OrderDTO
                    {
                        OrderId = apiOrder.OrderId,
                        Ticker = apiOrder.Ticker,
                        UserId = new Guid(apiOrder.UserId),
                        Action = apiOrder.Action,
                        Side = apiOrder.Side,
                        Type = apiOrder.Type,
                        Status = apiOrder.Status,
                        YesPrice = apiOrder.YesPrice,
                        NoPrice = apiOrder.NoPrice,
                        CreatedTimeUTC = apiOrder.CreatedTime,
                        LastUpdateTimeUTC = apiOrder.LastUpdateTime,
                        ExpirationTimeUTC = apiOrder.ExpirationTime,
                        ClientOrderId = apiOrder.ClientOrderId,
                        PlaceCount = apiOrder.PlaceCount,
                        DecreaseCount = apiOrder.DecreaseCount,
                        AmendCount = apiOrder.AmendCount,
                        AmendTakerFillCount = apiOrder.AmendTakerFillCount,
                        MakerFillCount = apiOrder.MakerFillCount,
                        TakerFillCount = apiOrder.TakerFillCount,
                        RemainingCount = apiOrder.RemainingCount,
                        QueuePosition = apiOrder.QueuePosition,
                        MakerFillCost = apiOrder.MakerFillCost,
                        TakerFillCost = apiOrder.TakerFillCost,
                        MakerFees = apiOrder.MakerFees,
                        TakerFees = apiOrder.TakerFees,
                        FccCancelCount = apiOrder.FccCancelCount,
                        CloseCancelCount = apiOrder.CloseCancelCount,
                        TakerSelfTradeCancelCount = apiOrder.TakerSelfTradeCancelCount,
                        LastModified = DateTime.Now
                    };

                    try
                    {
                        await context.AddOrUpdateOrder(order);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save order {OrderId}", order.OrderId);
                        errorCount++;
                    }
                }
                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(FetchOrdersAsync), stopwatch.ElapsedMilliseconds);
                return (processedCount, errorCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchOrdersAsync was cancelled");
                return (0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in FetchOrdersAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return (0, 0);
            }
        }

        /// <summary>
        /// Fetches the latest announcements from the Kalshi exchange.
        /// Retrieves important notifications and updates from the platform and stores them in the database.
        /// </summary>
        /// <returns>A tuple containing the count of successfully processed announcements and the count of errors encountered.</returns>
        public async Task<(int ProcessedCount, int ErrorCount)> FetchAnnouncementsAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                int processedCount = 0;
                int errorCount = 0;

                string url = "exchange/announcements";
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/exchange/announcements");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var responseData = JsonSerializer.Deserialize<AnnouncementResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (responseData == null || responseData.Announcements == null || responseData.Announcements.Count == 0)
                {
                    _logger.LogWarning("No announcements found in API response.");
                    return (0, 0);
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                var announcementsToAdd = new List<AnnouncementDTO>();
                foreach (var apiAnnouncement in responseData.Announcements)
                {
                    var announcement = new AnnouncementDTO
                    {
                        DeliveryTime = apiAnnouncement.DeliveryTime,
                        Message = apiAnnouncement.Message,
                        Status = apiAnnouncement.Status,
                        Type = apiAnnouncement.Type,
                        CreatedDate = DateTime.Now,
                        LastModifiedDate = DateTime.Now
                    };
                    announcementsToAdd.Add(announcement);
                }

                await context.AddAnnouncements(announcementsToAdd);
                processedCount = announcementsToAdd.Count;

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(FetchAnnouncementsAsync), stopwatch.ElapsedMilliseconds);
                return (processedCount, errorCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchAnnouncementsAsync was cancelled");
                return (0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in FetchAnnouncementsAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return (0, 1);
            }
        }

        /// <summary>
        /// Fetches the complete exchange schedule including trading hours and maintenance windows.
        /// Processes the schedule data into DTOs and persists the information to the database for operational planning.
        /// </summary>
        /// <returns>A tuple containing the count of successfully processed schedule items and the count of errors encountered.</returns>
        public async Task<(int ProcessedCount, int ErrorCount)> FetchExchangeScheduleAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                int processedCount = 0;
                int errorCount = 0;

                string url = "exchange/schedule";
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/exchange/schedule");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var responseData = JsonSerializer.Deserialize<ExchangeScheduleResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (responseData == null || responseData.Schedule == null)
                {
                    _logger.LogWarning("No exchange schedule data found in API response.");
                    return (0, 0);
                }

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IBacklashBotContext>();

                // Create ExchangeSchedule DTO
                var exchangeScheduleDTO = new ExchangeScheduleDTO
                {
                    LastUpdated = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    MaintenanceWindows = responseData.Schedule.MaintenanceWindows.Select(mw => new MaintenanceWindowDTO
                    {
                        StartDateTime = mw.StartDateTime,
                        EndDateTime = mw.EndDateTime,
                        CreatedDate = DateTime.Now,
                        LastModifiedDate = DateTime.Now
                    }).ToList(),
                    StandardHours = responseData.Schedule.StandardHours.Select(sh => new StandardHoursDTO
                    {
                        StartTime = sh.StartTime,
                        EndTime = sh.EndTime,
                        CreatedDate = DateTime.Now,
                        LastModifiedDate = DateTime.Now,
                        Sessions = new List<StandardHoursSessionDTO>()
                    }).ToList()
                };

                // Process sessions for each standard hours entry
                foreach (var (apiStandardHours, dtoStandardHours) in responseData.Schedule.StandardHours.Zip(exchangeScheduleDTO.StandardHours, (api, dto) => (api, dto)))
                {
                    // Monday
                    foreach (var session in apiStandardHours.Monday)
                    {
                        dtoStandardHours.Sessions.Add(new StandardHoursSessionDTO
                        {
                            DayOfWeek = "Monday",
                            StartTime = TimeSpan.Parse(session.OpenTime ?? "00:00:00"),
                            EndTime = TimeSpan.Parse(session.CloseTime ?? "00:00:00"),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }

                    // Tuesday
                    foreach (var session in apiStandardHours.Tuesday)
                    {
                        dtoStandardHours.Sessions.Add(new StandardHoursSessionDTO
                        {
                            DayOfWeek = "Tuesday",
                            StartTime = TimeSpan.Parse(session.OpenTime ?? "00:00:00"),
                            EndTime = TimeSpan.Parse(session.CloseTime ?? "00:00:00"),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }

                    // Wednesday
                    foreach (var session in apiStandardHours.Wednesday)
                    {
                        dtoStandardHours.Sessions.Add(new StandardHoursSessionDTO
                        {
                            DayOfWeek = "Wednesday",
                            StartTime = TimeSpan.Parse(session.OpenTime ?? "00:00:00"),
                            EndTime = TimeSpan.Parse(session.CloseTime ?? "00:00:00"),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }

                    // Thursday
                    foreach (var session in apiStandardHours.Thursday)
                    {
                        dtoStandardHours.Sessions.Add(new StandardHoursSessionDTO
                        {
                            DayOfWeek = "Thursday",
                            StartTime = TimeSpan.Parse(session.OpenTime ?? "00:00:00"),
                            EndTime = TimeSpan.Parse(session.CloseTime ?? "00:00:00"),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }

                    // Friday
                    foreach (var session in apiStandardHours.Friday)
                    {
                        dtoStandardHours.Sessions.Add(new StandardHoursSessionDTO
                        {
                            DayOfWeek = "Friday",
                            StartTime = TimeSpan.Parse(session.OpenTime ?? "00:00:00"),
                            EndTime = TimeSpan.Parse(session.CloseTime ?? "00:00:00"),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }

                    // Saturday
                    foreach (var session in apiStandardHours.Saturday)
                    {
                        dtoStandardHours.Sessions.Add(new StandardHoursSessionDTO
                        {
                            DayOfWeek = "Saturday",
                            StartTime = TimeSpan.Parse(session.OpenTime ?? "00:00:00"),
                            EndTime = TimeSpan.Parse(session.CloseTime ?? "00:00:00"),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }

                    // Sunday
                    foreach (var session in apiStandardHours.Sunday)
                    {
                        dtoStandardHours.Sessions.Add(new StandardHoursSessionDTO
                        {
                            DayOfWeek = "Sunday",
                            StartTime = TimeSpan.Parse(session.OpenTime ?? "00:00:00"),
                            EndTime = TimeSpan.Parse(session.CloseTime ?? "00:00:00"),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }
                }

                await context.AddExchangeSchedule(exchangeScheduleDTO);
                processedCount = 1; // One schedule processed

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(FetchExchangeScheduleAsync), stopwatch.ElapsedMilliseconds);
                return (processedCount, errorCount);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("FetchExchangeScheduleAsync was cancelled");
                return (0, 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in FetchExchangeScheduleAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return (0, 1);
            }
        }

        /// <summary>
        /// Retrieves the total value of all resting orders in the user's account.
        /// This provides insight into the current exposure from open orders.
        /// </summary>
        /// <returns>The total resting order value response, or null if the operation fails.</returns>
        public async Task<TotalRestingOrderValueResponse?> GetTotalRestingOrderValueAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                string url = "portfolio/summary/total_resting_order_value";
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/portfolio/summary/total_resting_order_value");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<TotalRestingOrderValueResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Total resting order value: {TotalValue}", result?.TotalValue);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetTotalRestingOrderValueAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetTotalRestingOrderValueAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetTotalRestingOrderValueAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the queue position information for a specific order.
        /// This provides insight into where the order stands in the matching queue.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order to check.</param>
        /// <returns>The order queue position response, or null if the operation fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderId is null or empty.</exception>
        public async Task<OrderQueuePositionResponse?> GetOrderQueuePositionAsync(string orderId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    throw new ArgumentNullException(nameof(orderId), "Order ID is required");
                }

                string url = $"portfolio/orders/{Uri.EscapeDataString(orderId)}/queue_position";
                var headers = GenerateAuthHeaders("GET", $"/trade-api/v2/portfolio/orders/{orderId}/queue_position");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<OrderQueuePositionResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Order {OrderId} queue position: {QueuePosition}", orderId, result?.QueuePosition);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetOrderQueuePositionAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetOrderQueuePositionAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetOrderQueuePositionAsync for {OrderId}: {ExceptionType} - {Message}",
                    orderId, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves detailed information about a specific order.
        /// Provides comprehensive order data including status, fills, and execution details.
        /// </summary>
        /// <param name="orderId">The unique identifier of the order to retrieve details for.</param>
        /// <returns>The order details response, or null if the operation fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderId is null or empty.</exception>
        public async Task<OrderResponse?> GetOrderDetailsAsync(string orderId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    throw new ArgumentNullException(nameof(orderId), "Order ID is required");
                }

                string url = $"portfolio/orders/{Uri.EscapeDataString(orderId)}";
                var headers = GenerateAuthHeaders("GET", $"/trade-api/v2/portfolio/orders/{orderId}");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<OrderResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Order {OrderId} details: Action={Action}, Status={Status}, Ticker={Ticker}",
                    orderId, result?.Order.Action, result?.Order.Status, result?.Order.Ticker);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetOrderDetailsAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetOrderDetailsAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetOrderDetailsAsync for {OrderId}: {ExceptionType} - {Message}",
                    orderId, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves queue position information for orders in specified markets.
        /// Provides insights into order queue status across multiple markets.
        /// </summary>
        /// <param name="marketTickers">Optional comma-separated list of market tickers to filter the results.</param>
        /// <returns>The queue positions response containing position data for the specified markets, or null if the operation fails.</returns>
        public async Task<QueuePositionsResponse?> GetOrdersQueuePositionsAsync(string? marketTickers = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(marketTickers))
                {
                    queryParams["market_tickers"] = marketTickers;
                }

                string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                string url = $"portfolio/orders/queue_positions{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/portfolio/orders/queue_positions");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<QueuePositionsResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Retrieved {Count} queue positions", result?.QueuePositions.Count ?? 0);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetOrdersQueuePositionsAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetOrdersQueuePositionsAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetOrdersQueuePositionsAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Creates multiple orders in a single batch operation.
        /// Allows efficient submission of multiple orders to reduce API round trips.
        /// </summary>
        /// <param name="request">The batch order request containing the list of orders to create.</param>
        /// <returns>The batch order response containing results for each order, or null if the operation fails.</returns>
        /// <exception cref="ArgumentException">Thrown when the orders list is null, empty, or contains invalid orders.</exception>
        public async Task<BatchOrdersResponse?> CreateOrdersBatchAsync(BatchOrdersRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (request == null || request.Orders == null || request.Orders.Count == 0)
                {
                    throw new ArgumentException("Orders list cannot be empty");
                }

                string url = "portfolio/orders/batched";
                var headers = GenerateAuthHeaders("POST", "/trade-api/v2/portfolio/orders/batched");
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
                foreach (var header in headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<BatchOrdersResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Batch created {Count} orders", result?.Orders.Count ?? 0);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(CreateOrdersBatchAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("CreateOrdersBatchAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in CreateOrdersBatchAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Cancels multiple orders in a single batch operation.
        /// Allows efficient cancellation of multiple orders to reduce API round trips.
        /// </summary>
        /// <param name="request">The batch delete request containing the list of order IDs to cancel.</param>
        /// <returns>The batch delete response containing results for each order cancellation, or null if the operation fails.</returns>
        /// <exception cref="ArgumentException">Thrown when the order IDs list is null or empty.</exception>
        public async Task<DeleteOrdersBatchResponse?> DeleteOrdersBatchAsync(DeleteOrdersBatchRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (request == null || request.Ids == null || request.Ids.Count == 0)
                {
                    throw new ArgumentException("Order IDs list cannot be empty");
                }

                string url = "portfolio/orders/batched";
                var headers = GenerateAuthHeaders("DELETE", "/trade-api/v2/portfolio/orders/batched");
                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, url);
                foreach (var header in headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }

                var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
                httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<DeleteOrdersBatchResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Batch deleted {Count} orders", result?.Orders.Count ?? 0);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(DeleteOrdersBatchAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("DeleteOrdersBatchAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in DeleteOrdersBatchAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Resets an order group, clearing its state and allowing it to be reused.
        /// This operation resets the order group's execution state without deleting it.
        /// </summary>
        /// <param name="orderGroupId">The unique identifier of the order group to reset.</param>
        /// <returns>True if the reset operation was successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderGroupId is null or empty.</exception>
        public async Task<bool> ResetOrderGroupAsync(string orderGroupId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(orderGroupId))
                {
                    throw new ArgumentNullException(nameof(orderGroupId), "Order group ID is required");
                }

                string url = $"portfolio/order_groups/{Uri.EscapeDataString(orderGroupId)}/reset";
                var headers = GenerateAuthHeaders("PUT", $"/trade-api/v2/portfolio/order_groups/{orderGroupId}/reset");
                var request = new HttpRequestMessage(HttpMethod.Put, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Order group {OrderGroupId} reset successfully", orderGroupId);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(ResetOrderGroupAsync), stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("ResetOrderGroupAsync was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in ResetOrderGroupAsync for {OrderGroupId}: {ExceptionType} - {Message}",
                    orderGroupId, ex.GetType().Name, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Deletes an order group and all associated orders.
        /// This permanently removes the order group and cancels any remaining orders in it.
        /// </summary>
        /// <param name="orderGroupId">The unique identifier of the order group to delete.</param>
        /// <returns>True if the delete operation was successful, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderGroupId is null or empty.</exception>
        public async Task<bool> DeleteOrderGroupAsync(string orderGroupId)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(orderGroupId))
                {
                    throw new ArgumentNullException(nameof(orderGroupId), "Order group ID is required");
                }

                string url = $"portfolio/order_groups/{Uri.EscapeDataString(orderGroupId)}";
                var headers = GenerateAuthHeaders("DELETE", $"/trade-api/v2/portfolio/order_groups/{orderGroupId}");
                var request = new HttpRequestMessage(HttpMethod.Delete, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Order group {OrderGroupId} deleted successfully", orderGroupId);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(DeleteOrderGroupAsync), stopwatch.ElapsedMilliseconds);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("DeleteOrderGroupAsync was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in DeleteOrderGroupAsync for {OrderGroupId}: {ExceptionType} - {Message}",
                    orderGroupId, ex.GetType().Name, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Retrieves settlement information for completed markets.
        /// Provides details about market settlements including payout amounts and settlement dates.
        /// </summary>
        /// <param name="cursor">Optional pagination cursor for retrieving subsequent pages of settlements.</param>
        /// <param name="limit">Optional limit on the number of settlements to retrieve per request.</param>
        /// <returns>The settlements response containing settlement data, or null if the operation fails.</returns>
        public async Task<SettlementsResponse?> GetSettlementsAsync(string? cursor = null, int? limit = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(cursor)) queryParams["cursor"] = cursor;
                if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

                string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                string url = $"portfolio/settlements{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/portfolio/settlements");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<SettlementsResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Retrieved {Count} settlements", result?.Settlements.Count ?? 0);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetSettlementsAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetSettlementsAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetSettlementsAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves fill information for executed orders.
        /// Provides details about order executions including prices, quantities, and timestamps.
        /// </summary>
        /// <param name="cursor">Optional pagination cursor for retrieving subsequent pages of fills.</param>
        /// <param name="limit">Optional limit on the number of fills to retrieve per request.</param>
        /// <returns>The fills response containing execution data, or null if the operation fails.</returns>
        public async Task<FillsResponse?> GetFillsAsync(string? cursor = null, int? limit = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(cursor)) queryParams["cursor"] = cursor;
                if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

                string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                string url = $"portfolio/fills{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/portfolio/fills");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<FillsResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Retrieved {Count} fills", result?.Fills.Count ?? 0);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetFillsAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetFillsAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetFillsAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves information about available incentive programs.
        /// Provides details about trading incentives, rewards, and promotional programs offered by the exchange.
        /// </summary>
        /// <param name="cursor">Optional pagination cursor for retrieving subsequent pages of incentive programs.</param>
        /// <param name="limit">Optional limit on the number of incentive programs to retrieve per request.</param>
        /// <returns>The incentive programs response containing program data, or null if the operation fails.</returns>
        public async Task<IncentiveProgramsResponse?> GetIncentiveProgramsAsync(string? cursor = null, int? limit = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var queryParams = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(cursor)) queryParams["cursor"] = cursor;
                if (limit.HasValue) queryParams["limit"] = limit.Value.ToString();

                string queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                string url = $"incentive_programs{(string.IsNullOrEmpty(queryString) ? "" : "?" + queryString)}";
                var headers = GenerateAuthHeaders("GET", "/trade-api/v2/incentive_programs");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<IncentiveProgramsResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Retrieved {Count} incentive programs", result?.IncentivePrograms.Count ?? 0);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetIncentiveProgramsAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetIncentiveProgramsAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetIncentiveProgramsAsync: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Retrieves detailed metadata for a specific event.
        /// Provides additional information about the event including competition details and settlement sources.
        /// </summary>
        /// <param name="eventTicker">The ticker symbol of the event to retrieve metadata for.</param>
        /// <returns>The event metadata response containing additional event information, or null if the operation fails.</returns>
        /// <exception cref="ArgumentNullException">Thrown when eventTicker is null or empty.</exception>
        public async Task<EventMetadataResponse?> GetEventMetadataAsync(string eventTicker)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (string.IsNullOrEmpty(eventTicker))
                {
                    throw new ArgumentNullException(nameof(eventTicker), "Event ticker is required");
                }

                string url = $"events/{Uri.EscapeDataString(eventTicker)}/metadata";
                var headers = GenerateAuthHeaders("GET", $"/trade-api/v2/events/{eventTicker}/metadata");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var result = JsonSerializer.Deserialize<EventMetadataResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Retrieved metadata for event {EventTicker}: Competition={Competition}, Sources={SourcesCount}",
                    eventTicker, result?.Competition, result?.SettlementSources.Count ?? 0);

                stopwatch.Stop();
                RecordMethodExecutionDuration(nameof(GetEventMetadataAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetEventMetadataAsync was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unexpected error in GetEventMetadataAsync for {EventTicker}: {ExceptionType} - {Message}",
                    eventTicker, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Records the execution duration of a method for performance monitoring.
        /// Stores the elapsed time in a thread-safe concurrent bag for later analysis.
        /// </summary>
        /// <param name="methodName">The name of the method being tracked.</param>
        /// <param name="elapsedMs">The execution time in milliseconds.</param>
        private void RecordMethodExecutionDuration(string methodName, long elapsedMs)
        {
            if (!_enablePerformanceMetrics) return;
            var bag = _methodExecutionDurations.GetOrAdd(methodName, _ => new ConcurrentBag<long>());
            bag.Add(elapsedMs);

            // Post to performance monitor for unified tracking
            _performanceMonitor.RecordExecutionTime(methodName, elapsedMs);
        }

        /// <summary>
        /// Records the execution duration of a specific calculation for performance monitoring.
        /// Stores the elapsed time in a thread-safe concurrent bag for later analysis and optimization.
        /// Used to track timing of data processing operations like DTO creation and database saves.
        /// </summary>
        /// <param name="calculationName">The name of the calculation being tracked (e.g., "ProcessCandlestick", "ProcessMarketDTO").</param>
        /// <param name="elapsedMs">The execution time in milliseconds.</param>
        private void RecordCalculationExecutionDuration(string calculationName, long elapsedMs)
        {
            if (!_enablePerformanceMetrics) return;
            var bag = _calculationExecutionDurations.GetOrAdd(calculationName, _ => new ConcurrentBag<long>());
            bag.Add(elapsedMs);
        }

        /// <summary>
        /// Records the execution duration of API response times for performance monitoring.
        /// Stores the elapsed time in a thread-safe concurrent bag for later analysis.
        /// Used to track timing of HTTP requests to the Kalshi API.
        /// </summary>
        /// <param name="methodName">The name of the method making the API call.</param>
        /// <param name="elapsedMs">The execution time in milliseconds.</param>
        private void RecordApiResponseDuration(string methodName, long elapsedMs)
        {
            if (!_enablePerformanceMetrics) return;
            var bag = _apiResponseDurations.GetOrAdd(methodName, _ => new ConcurrentBag<long>());
            bag.Add(elapsedMs);
        }

        /// <summary>
        /// Records an error occurrence for a specific method for reliability monitoring.
        /// Adds an error count in a thread-safe manner.
        /// </summary>
        /// <param name="methodName">The name of the method where the error occurred.</param>
        private void RecordError(string methodName)
        {
            if (!_enablePerformanceMetrics) return;
            var bag = _errorCounts.GetOrAdd(methodName, _ => new ConcurrentBag<int>());
            bag.Add(1);
        }

        /// <summary>
        /// Gets the method execution durations for performance monitoring.
        /// Returns a dictionary mapping method names to their execution time measurements.
        /// </summary>
        /// <returns>A concurrent dictionary containing method execution durations.</returns>
        public ConcurrentDictionary<string, ConcurrentBag<long>> GetMethodExecutionDurations()
        {
            return _methodExecutionDurations;
        }

        /// <summary>
        /// Gets the calculation execution durations for performance monitoring.
        /// Returns a dictionary mapping calculation names to their execution time measurements.
        /// </summary>
        /// <returns>A concurrent dictionary containing calculation execution durations.</returns>
        public ConcurrentDictionary<string, ConcurrentBag<long>> GetCalculationExecutionDurations()
        {
            return _calculationExecutionDurations;
        }

        /// <summary>
        /// Gets aggregated performance metrics for method executions.
        /// Returns statistics like average, min, max execution times for each method.
        /// </summary>
        /// <returns>A dictionary containing aggregated method performance metrics.</returns>
        public Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> GetMethodPerformanceMetrics()
        {
            var metrics = new Dictionary<string, (int, double, long, long)>();
            foreach (var kvp in _methodExecutionDurations)
            {
                var durations = kvp.Value.ToArray();
                if (durations.Length > 0)
                {
                    var count = durations.Length;
                    var average = durations.Average();
                    var min = durations.Min();
                    var max = durations.Max();
                    metrics[kvp.Key] = (count, average, min, max);
                }
            }
            return metrics;
        }

        /// <summary>
        /// Gets aggregated performance metrics for calculation executions.
        /// Returns statistics like average, min, max execution times for each calculation.
        /// </summary>
        /// <returns>A dictionary containing aggregated calculation performance metrics.</returns>
        public Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> GetCalculationPerformanceMetrics()
        {
            var metrics = new Dictionary<string, (int, double, long, long)>();
            foreach (var kvp in _calculationExecutionDurations)
            {
                var durations = kvp.Value.ToArray();
                if (durations.Length > 0)
                {
                    var count = durations.Length;
                    var average = durations.Average();
                    var min = durations.Min();
                    var max = durations.Max();
                    metrics[kvp.Key] = (count, average, min, max);
                }
            }
            return metrics;
        }

        /// <summary>
        /// Gets the API response durations for performance monitoring.
        /// Returns a dictionary mapping method names to their API response time measurements.
        /// </summary>
        /// <returns>A concurrent dictionary containing API response durations.</returns>
        public ConcurrentDictionary<string, ConcurrentBag<long>> GetApiResponseDurations()
        {
            return _apiResponseDurations;
        }

        /// <summary>
        /// Gets the error counts for reliability monitoring.
        /// Returns a dictionary mapping method names to their error count measurements.
        /// </summary>
        /// <returns>A concurrent dictionary containing error counts.</returns>
        public ConcurrentDictionary<string, ConcurrentBag<int>> GetErrorCounts()
        {
            return _errorCounts;
        }

        /// <summary>
        /// Gets aggregated performance metrics for API response times.
        /// Returns statistics like average, min, max response times for each method.
        /// </summary>
        /// <returns>A dictionary containing aggregated API response performance metrics.</returns>
        public Dictionary<string, (int Count, double AverageMs, long MinMs, long MaxMs)> GetApiResponsePerformanceMetrics()
        {
            var metrics = new Dictionary<string, (int, double, long, long)>();
            foreach (var kvp in _apiResponseDurations)
            {
                var durations = kvp.Value.ToArray();
                if (durations.Length > 0)
                {
                    var count = durations.Length;
                    var average = durations.Average();
                    var min = durations.Min();
                    var max = durations.Max();
                    metrics[kvp.Key] = (count, average, min, max);
                }
            }
            return metrics;
        }

        /// <summary>
        /// Gets aggregated error metrics for reliability monitoring.
        /// Returns the total error count for each method.
        /// </summary>
        /// <returns>A dictionary containing total error counts per method.</returns>
        public Dictionary<string, int> GetErrorMetrics()
        {
            var metrics = new Dictionary<string, int>();
            foreach (var kvp in _errorCounts)
            {
                metrics[kvp.Key] = kvp.Value.Sum();
            }
            return metrics;
        }
    }
}
