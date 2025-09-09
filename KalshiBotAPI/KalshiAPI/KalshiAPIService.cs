using KalshiBotAPI.Configuration;
using KalshiBotData.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmokehouseBot.KalshiAPI.Interfaces;
using SmokehouseBot.Services.Interfaces;
using SmokehouseBot.State.Interfaces;
using SmokehouseDTOs.Data;
using SmokehouseDTOs.Exceptions;
using SmokehouseDTOs.Helpers;
using SmokehouseDTOs.KalshiAPI;
using SmokehouseInterfaces.Constants;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KalshiBotAPI.KalshiAPI
{
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

        private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _executionTimes = new();

        private readonly Dictionary<string, (int Minutes, int DbType, int MaxDays, int CushionSeconds)> _intervals = new()
        {
            ["minute"] = (1, 1, 3, 60),
            ["hour"] = (60, 2, 7, 3600),
            ["day"] = (1440, 3, 15, 86400)
        };

        public KalshiAPIService(
            ILogger<IKalshiAPIService> logger,
            IConfiguration config,
            IServiceScopeFactory scopeFactory,
            IStatusTrackerService statusTrackerService,
            IOptions<KalshiConfig> kalshiConfig)
        {
            _logger = logger;
            _statusTrackerService = statusTrackerService;
            _kalshiConfig = kalshiConfig.Value;

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
        }

        private Dictionary<string, string> GenerateAuthHeaders(string method, string path)
        {
            var stopwatch = Stopwatch.StartNew();
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
                stopwatch.Stop();
                RecordExecutionTime(nameof(GenerateAuthHeaders), stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Use updateNotFoundToClosed=false if using filters that might exclude some markets even though explicitly requested
        /// </summary>
        /// <param name="eventTicker"></param>
        /// <param name="seriesTicker"></param>
        /// <param name="maxCloseTs"></param>
        /// <param name="minCloseTs"></param>
        /// <param name="status"></param>
        /// <param name="tickers"></param>
        /// <param name="updateNotFoundToClosed"></param>
        /// <returns></returns>
        public async Task<(int ProcessedCount, int ErrorCount)> FetchMarketsAsync(
    string? eventTicker = null, string? seriesTicker = null, string? maxCloseTs = null,
    string? minCloseTs = null, string? status = null, string[]? tickers = null, bool updateNotFoundToClosed = true)
        {
            var stopwatch = Stopwatch.StartNew();
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
                            var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                            response.EnsureSuccessStatusCode();
                            jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());
                            responseData = JsonSerializer.Deserialize<MarketResponse>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        }
                        catch (JsonException ex)
                        {
                            responseWasSuccessful = false;
                            _logger.LogWarning(ex, "Failed to deserialize response for {Url}, json={0}", url, jsonString);
                        }
                        catch (HttpRequestException ex)
                        {
                            responseWasSuccessful = false;
                            _logger.LogWarning("HTTP request failed for {Url}. Message: {message}", url, ex.Message);
                            errorCount++;
                            break;
                        }

                        if (responseData == null)
                            _logger.LogWarning(new Exception($"No response data found. String: {jsonString}"), "No response data found. String: {jsonString}", jsonString);

                        try
                        {
                            List<MarketDTO> marketsToUpdate = new List<MarketDTO>();

                            foreach (var apiMarket in responseData?.Markets)
                            {
                                _statusTrackerService.GetCancellationToken().ThrowIfCancellationRequested();

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
                                processedCount++;
                            }

                            using var scope = _scopeFactory.CreateScope();
                            var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();
                            await context.AddOrUpdateMarkets(marketsToUpdate);
                        }
                        catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Cannot insert duplicate key"))
                        {
                            stopwatch.Stop();
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
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                    foreach (var ticker in tickers.Where(t => !foundTickers.Contains(t)))
                    {
                        var existingMarket = await context.GetMarketByTicker_cached(ticker);

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

                stopwatch.Stop();
                RecordExecutionTime(nameof(FetchMarketsAsync), stopwatch.ElapsedMilliseconds);
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


        public async Task<ExchangeScheduleResponse> GetExchangeScheduleAsync()
        {
            var stopwatch = Stopwatch.StartNew();
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

                stopwatch.Stop();
                RecordExecutionTime(nameof(GetExchangeScheduleAsync), stopwatch.ElapsedMilliseconds);
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
                return new ExchangeScheduleResponse();
            }
        }

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
                    IgnoreNullValues = true,
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
                RecordExecutionTime(nameof(CreateOrderAsync), stopwatch.ElapsedMilliseconds);
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
                RecordExecutionTime(nameof(CancelOrderAsync), stopwatch.ElapsedMilliseconds);
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
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                if (seriesResponse == null) throw new Exception(url + $" returned null response. JSON: {jsonString}");

                var newTags = (seriesResponse.Series.Tags ?? new List<string>())
                    .Select(tag => new SeriesTagDTO
                    {
                        series_ticker = seriesResponse.Series.Ticker,
                        tag = tag
                    })
                    .ToList();

                var newSources = (seriesResponse.Series.SettlementSources)
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

                _logger.LogDebug("Successfully fetched and saved series data for ticker: {SeriesTicker}", seriesTicker);
                stopwatch.Stop();
                RecordExecutionTime(nameof(FetchSeriesAsync), stopwatch.ElapsedMilliseconds);
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
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                var eventDTO = new EventDTO
                {
                    event_ticker = eventResponse.Event.EventTicker,
                    series_ticker = eventResponse.Event.SeriesTicker,
                    title = eventResponse.Event.Title,
                    sub_title = eventResponse.Event.SubTitle,
                    collateral_return_type = eventResponse.Event.CollateralReturnType,
                    mutually_exclusive = eventResponse.Event.MutuallyExclusive,
                    category = eventResponse.Event.Category,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };

                await context.AddOrUpdateEvent(eventDTO);

                string extractedCategory = eventResponse.Event.Category;

                if (withNestedMarkets && eventResponse.Event.Markets != null && eventResponse.Event.Markets.Any())
                {
                    foreach (var apiMarket in eventResponse.Event.Markets)
                    {
                        using var marketScope = _scopeFactory.CreateScope();
                        var marketContext = marketScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();


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

                _logger.LogDebug("Successfully fetched and saved event data for ticker: {EventTicker}", eventTicker);
                stopwatch.Stop();
                RecordExecutionTime(nameof(FetchEventAsync), stopwatch.ElapsedMilliseconds);
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
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                var existingPositions = await context.GetMarketPositions();
                var apiTickerSet = allMarketPositions.Select(p => p.Ticker).ToHashSet();

                foreach (var apiPosition in allMarketPositions)
                {
                    using var positionScope = _scopeFactory.CreateScope();
                    var positionContext = positionScope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

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
                RecordExecutionTime(nameof(FetchPositionsAsync), stopwatch.ElapsedMilliseconds);
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

                _logger.LogDebug("Fetched {Count} candlesticks for {MarketTicker} from API", candlesticks.Count, marketTicker);

                foreach (var apiCandlestick in candlesticks)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                    var endDateTime = DateTimeOffset.FromUnixTimeSeconds(apiCandlestick.EndPeriodTs).UtcDateTime;
                    if (endDateTime > now)
                    {
                        _logger.LogWarning("Future candlestick timestamp detected for {MarketTicker}, {Interval}: Original={Original}, Adjusting to={Adjusted}",
                            marketTicker, interval, endDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                        apiCandlestick.EndPeriodTs = UnixHelper.ConvertToUnixTimestamp(now);
                    }

                    var candlestick = new CandlestickDTO
                    {
                        market_ticker = marketTicker,
                        interval_type = dbType,
                        end_period_ts = apiCandlestick.EndPeriodTs,
                        open_interest = apiCandlestick.OpenInterest,
                        volume = apiCandlestick.Volume,
                        price_close = apiCandlestick.Price.Close,
                        price_high = apiCandlestick.Price.High,
                        price_low = apiCandlestick.Price.Low,
                        price_mean = apiCandlestick.Price.Mean,
                        price_open = apiCandlestick.Price.Open,
                        price_previous = apiCandlestick.Price.Previous,
                        yes_ask_close = apiCandlestick.YesAsk.Close ?? 0,
                        yes_ask_high = apiCandlestick.YesAsk.High ?? 0,
                        yes_ask_low = apiCandlestick.YesAsk.Low ?? 0,
                        yes_ask_open = apiCandlestick.YesAsk.Open ?? 0,
                        yes_bid_close = apiCandlestick.YesBid.Close ?? 0,
                        yes_bid_high = apiCandlestick.YesBid.High ?? 0,
                        yes_bid_low = apiCandlestick.YesBid.Low ?? 0,
                        yes_bid_open = apiCandlestick.YesBid.Open ?? 0
                    };

                    try
                    {
                        await context.AddOrUpdateCandlestick(candlestick);
                        processedCount++;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogDebug("FetchCandlesticksAsync was cancelled");
                        return (0, 0);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save candlestick for {MarketTicker}", marketTicker);
                        errorCount++;
                    }

                }

                if (candlesticks.Count > 0 && interval == "minute" && updateLastCandlestick)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

                    try
                    {
                        _logger.LogDebug("API: Updating last candlestick for market {0}", marketTicker);
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
                RecordExecutionTime(nameof(FetchCandlesticksAsync), stopwatch.ElapsedMilliseconds);
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

                var response = await _httpClient.SendAsync(request, _statusTrackerService.GetCancellationToken());
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync(_statusTrackerService.GetCancellationToken());

                var balanceData = JsonSerializer.Deserialize<BalanceResponse>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogDebug("Balance: {Balance}", balanceData.Balance);
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
                RecordExecutionTime(nameof(GetBalanceAsync), stopwatch.ElapsedMilliseconds);
            }
        }

        public async Task<ExchangeStatus?> GetExchangeStatusAsync()
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
                return statusData;
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
                RecordExecutionTime(nameof(GetExchangeStatusAsync), stopwatch.ElapsedMilliseconds);
            }
        }

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
                    var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

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
                RecordExecutionTime(nameof(FetchOrdersAsync), stopwatch.ElapsedMilliseconds);
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
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

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
                RecordExecutionTime(nameof(FetchAnnouncementsAsync), stopwatch.ElapsedMilliseconds);
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
                var context = scope.ServiceProvider.GetRequiredService<IKalshiBotContext>();

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
                            StartTime = TimeSpan.Parse(session.OpenTime),
                            EndTime = TimeSpan.Parse(session.CloseTime),
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
                            StartTime = TimeSpan.Parse(session.OpenTime),
                            EndTime = TimeSpan.Parse(session.CloseTime),
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
                            StartTime = TimeSpan.Parse(session.OpenTime),
                            EndTime = TimeSpan.Parse(session.CloseTime),
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
                            StartTime = TimeSpan.Parse(session.OpenTime),
                            EndTime = TimeSpan.Parse(session.CloseTime),
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
                            StartTime = TimeSpan.Parse(session.OpenTime),
                            EndTime = TimeSpan.Parse(session.CloseTime),
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
                            StartTime = TimeSpan.Parse(session.OpenTime),
                            EndTime = TimeSpan.Parse(session.CloseTime),
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
                            StartTime = TimeSpan.Parse(session.OpenTime),
                            EndTime = TimeSpan.Parse(session.CloseTime),
                            CreatedDate = DateTime.Now,
                            LastModifiedDate = DateTime.Now
                        });
                    }
                }

                await context.AddExchangeSchedule(exchangeScheduleDTO);
                processedCount = 1; // One schedule processed

                stopwatch.Stop();
                RecordExecutionTime(nameof(FetchExchangeScheduleAsync), stopwatch.ElapsedMilliseconds);
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

        private void RecordExecutionTime(string broadcastType, long elapsedMs)
        {
            var bag = _executionTimes.GetOrAdd(broadcastType, _ => new ConcurrentBag<long>());
            bag.Add(elapsedMs);
        }
    }
}