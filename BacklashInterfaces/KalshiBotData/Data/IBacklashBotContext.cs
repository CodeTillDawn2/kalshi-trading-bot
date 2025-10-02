using BacklashDTOs;
using BacklashDTOs.Data;

namespace BacklashBotData.Data.Interfaces
{
    /// <summary>
    /// Interface for the BacklashBot database context providing access to various data entities and operations.
    /// </summary>
    public interface IBacklashBotContext : IDisposable
    {
        #region Series
        /// <summary>
        /// Gets a series by its ticker symbol.
        /// </summary>
        /// <param name="seriesTicker">The ticker symbol of the series.</param>
        /// <returns>The series DTO if found, null otherwise.</returns>
        Task<SeriesDTO?> GetSeriesByTicker(string seriesTicker);

        /// <summary>
        /// Adds or updates a series in the database.
        /// </summary>
        /// <param name="dto">The series DTO to add or update.</param>
        Task AddOrUpdateSeries(SeriesDTO dto);
        #endregion

        #region Events
        /// <summary>
        /// Gets an event by its ticker symbol.
        /// </summary>
        /// <param name="eventTicker">The ticker symbol of the event.</param>
        /// <returns>The event DTO if found, null otherwise.</returns>
        Task<EventDTO?> GetEventByTicker(string eventTicker);

        /// <summary>
        /// Adds or updates an event in the database.
        /// </summary>
        /// <param name="dto">The event DTO to add or update.</param>
        Task AddOrUpdateEvent(EventDTO dto);

        /// <summary>
        /// Adds or updates multiple events in the database.
        /// </summary>
        /// <param name="dtos">The list of event DTOs to add or update.</param>
        Task AddOrUpdateEvents(List<EventDTO> dtos);

        /// <summary>
        /// Retrieves a list of events with optional wildcard search on ticker.
        /// </summary>
        /// <param name="tickerWildcard">Optional wildcard pattern to search in event ticker (e.g., "mention*").</param>
        /// <returns>List of event data transfer objects matching the specified criteria.</returns>
        Task<List<EventDTO>> GetEvents(string? tickerWildcard = null);
        #endregion

        #region Markets
        /// <summary>
        /// Gets a market by its ticker symbol.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>The market DTO if found, null otherwise.</returns>
        Task<MarketDTO?> GetMarketByTicker(string marketTicker);

        /// <summary>
        /// Gets the latest candlestick data for a market.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <param name="intervalType">Optional interval type for the candlestick (e.g., 1-minute).</param>
        /// <returns>The latest candlestick DTO if found, null otherwise.</returns>
        Task<CandlestickDTO?> GetLatestCandlestick(string marketTicker, int? intervalType);

        /// <summary>
        /// Gets a market watch by its ticker symbol.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>The market watch DTO if found, null otherwise.</returns>
        Task<MarketWatchDTO?> GetMarketWatchByTicker(string marketTicker);

        /// <summary>
        /// Gets markets filtered by various criteria such as status, markets, and interest scores.
        /// </summary>
        /// <param name="includedStatuses">Optional set of included market statuses.</param>
        /// <param name="excludedStatuses">Optional set of excluded market statuses.</param>
        /// <param name="includedMarkets">Optional set of included market tickers.</param>
        /// <param name="excludedMarkets">Optional set of excluded market tickers.</param>
        /// <param name="hasMarketWatch">Optional flag to filter markets with a market watch.</param>
        /// <param name="minimumInterestScore">Optional minimum interest score threshold.</param>
        /// <param name="maxInterestScoreDate">Optional maximum date for interest score filtering.</param>
        /// <param name="maxAPILastFetchTime">Optional maximum API last fetch time for filtering.</param>
        /// <returns>A list of filtered market DTOs.</returns>
        Task<List<MarketDTO>> GetMarketsFiltered(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null,
            bool? hasMarketWatch = null, double? minimumInterestScore = null,
            DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);

        /// <summary>
        /// Gets market watches filtered by brain locks, interest scores, and dates.
        /// </summary>
        /// <param name="marketTickers">Optional set of market tickers to filter by.</param>
        /// <param name="brainLocksIncluded">Optional set of included brain lock GUIDs.</param>
        /// <param name="brainLocksExcluded">Optional set of excluded brain lock GUIDs.</param>
        /// <param name="brainLockIsNull">Optional flag to filter by null brain locks.</param>
        /// <param name="minInterestScore">Optional minimum interest score threshold.</param>
        /// <param name="maxInterestScore">Optional maximum interest score threshold.</param>
        /// <param name="maxInterestScoreDate">Optional maximum date for interest score filtering.</param>
        /// <returns>A hash set of filtered market watch DTOs.</returns>
        Task<HashSet<MarketWatchDTO>> GetMarketWatchesFiltered(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null);

        /// <summary>
        /// Gets snapshot groups filtered by market tickers and maximum group count.
        /// </summary>
        /// <param name="marketTickersToInclude">Optional list of market tickers to include.</param>
        /// <param name="maxGroups">Optional maximum number of groups to return.</param>
        /// <returns>A list of filtered snapshot group DTOs.</returns>
        Task<List<SnapshotGroupDTO>> GetSnapshotGroupsFiltered(List<string>? marketTickersToInclude = null, int? maxGroups = null);

        /// <summary>
        /// Gets snapshots filtered by market ticker, validation status, dates, and limits.
        /// </summary>
        /// <param name="marketTicker">Optional market ticker to filter by.</param>
        /// <param name="isValidated">Optional flag to filter validated snapshots.</param>
        /// <param name="startDate">Optional start date for filtering.</param>
        /// <param name="endDate">Optional end date for filtering.</param>
        /// <param name="MaxRecords">Optional maximum number of records to return.</param>
        /// <param name="MaxSnapshotVersion">Optional maximum snapshot version to filter by.</param>
        /// <returns>A list of filtered snapshot DTOs.</returns>
        Task<List<SnapshotDTO>> GetSnapshotsFiltered(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);

        /// <summary>
        /// Gets finalized market watches by brain lock GUID.
        /// </summary>
        /// <param name="brainLock">The brain lock GUID to filter by.</param>
        /// <returns>A list of market watch DTOs associated with the brain lock.</returns>
        Task<List<MarketWatchDTO>> GetFinalizedMarketWatchesByBrainLock(Guid brainLock);

        /// <summary>
        /// Gets a hash set of market tickers that have snapshots.
        /// </summary>
        /// <returns>A hash set of market tickers with snapshots.</returns>
        Task<HashSet<string>> GetMarketTickersWithSnapshots();

        /// <summary>
        /// Gets a hash set of markets that have snapshots.
        /// </summary>
        /// <returns>A hash set of market tickers with snapshots.</returns>
        Task<HashSet<string>> GetMarketsWithSnapshots();

        /// <summary>
        /// Gets a hash set of inactive market tickers without snapshots.
        /// </summary>
        /// <returns>A hash set of inactive market tickers without snapshots.</returns>
        Task<HashSet<string>> GetInactiveMarketsWithNoSnapshots();

        /// <summary>
        /// Gets a hash set of inactive market tickers without snapshots.
        /// </summary>
        /// <returns>A hash set of inactive market tickers without snapshots.</returns>
        Task<HashSet<string>> GetInactiveMarketTickersWithoutSnapshots();

        /// <summary>
        /// Gets a hash set of processed market tickers.
        /// </summary>
        /// <returns>A hash set of processed market tickers.</returns>
        Task<HashSet<string>> GetProcessedMarkets();

        /// <summary>
        /// Adds or updates a market in the database.
        /// </summary>
        /// <param name="dto">The market DTO to add or update.</param>
        Task AddOrUpdateMarket(MarketDTO dto);

        /// <summary>
        /// Deletes a market by its ticker symbol.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to delete.</param>
        Task DeleteMarket(string marketTicker);

        /// <summary>
        /// Adds or updates multiple markets in the database.
        /// </summary>
        /// <param name="dtos">The list of market DTOs to add or update.</param>
        Task AddOrUpdateMarkets(List<MarketDTO> dtos);

        /// <summary>
        /// Gets markets filtered by statuses, markets, and other criteria.
        /// </summary>
        /// <param name="includedStatuses">Optional set of included market statuses.</param>
        /// <param name="excludedStatuses">Optional set of excluded market statuses.</param>
        /// <param name="includedMarkets">Optional set of included market tickers.</param>
        /// <param name="excludedMarkets">Optional set of excluded market tickers.</param>
        /// <param name="eventTicker">Optional event ticker to filter markets by.</param>
        /// <param name="hasMarketWatch">Optional flag to filter markets with a market watch.</param>
        /// <param name="minimumInterestScore">Optional minimum interest score threshold.</param>
        /// <param name="maxInterestScoreDate">Optional maximum date for interest score filtering.</param>
        /// <param name="maxAPILastFetchTime">Optional maximum API last fetch time for filtering.</param>
        /// <returns>A list of filtered market DTOs.</returns>
        Task<List<MarketDTO>> GetMarkets(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null, string? eventTicker = null,
            bool? hasMarketWatch = null, double? minimumInterestScore = null, DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);

        /// <summary>
        /// Updates the last candlestick timestamp for a market.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market to update.</param>
        Task UpdateMarketLastCandlestick(string marketTicker);

        /// <summary>
        /// Gets the status of a market, including whether the market, event, and series are found.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market.</param>
        /// <returns>A tuple indicating if the market, event, and series are found.</returns>
        Task<(bool MarketFound, bool EventFound, bool SeriesFound)> GetMarketStatus(string marketTicker);
        #endregion

        #region Tickers
        /// <summary>
        /// Gets tickers filtered by market ticker and logged date.
        /// </summary>
        /// <param name="marketTicker">Optional market ticker to filter by.</param>
        /// <param name="loggedDate">Optional logged date for filtering.</param>
        /// <returns>A list of filtered ticker DTOs.</returns>
        Task<List<TickerDTO>> GetTickers(string? marketTicker = null, DateTime? loggedDate = null);

        /// <summary>
        /// Adds or updates multiple tickers in the database.
        /// </summary>
        /// <param name="dtos">The list of ticker DTOs to add or update.</param>
        Task AddOrUpdateTickers(List<TickerDTO> dtos);

        /// <summary>
        /// Adds or updates a ticker in the database.
        /// </summary>
        /// <param name="dto">The ticker DTO to add or update.</param>
        Task AddOrUpdateTicker(TickerDTO dto);
        #endregion

        #region Candlesticks
        /// <summary>
        /// Gets candlesticks for a specific market and interval type.
        /// </summary>
        /// <param name="marketTicker">The market ticker to filter by.</param>
        /// <param name="intervalType">Optional interval type for the candlesticks.</param>
        /// <returns>A list of candlestick DTOs.</returns>
        Task<List<CandlestickDTO>> GetCandlesticks(string marketTicker, int? intervalType = null);

        /// <summary>
        /// Gets the last candlestick for a market and interval type.
        /// </summary>
        /// <param name="marketTicker">The market ticker to filter by.</param>
        /// <param name="intervalType">Optional interval type for the candlestick.</param>
        /// <returns>The last candlestick DTO if found, null otherwise.</returns>
        Task<CandlestickDTO?> GetLastCandlestick(string marketTicker, int? intervalType);

        /// <summary>
        /// Adds or updates a candlestick in the database.
        /// </summary>
        /// <param name="dto">The candlestick DTO to add or update.</param>
        Task AddOrUpdateCandlestick(CandlestickDTO dto);

        /// <summary>
        /// Retrieves candlestick data asynchronously with cancellation support.
        /// </summary>
        /// <param name="token">Cancellation token for the operation.</param>
        /// <param name="intervalType">The interval type for candlesticks.</param>
        /// <param name="marketTicker">The market ticker to filter by.</param>
        /// <param name="sqlStartTime">The start time for SQL query filtering.</param>
        /// <returns>A list of candlestick data objects.</returns>
        Task<List<CandlestickData>> RetrieveCandlesticksAsync(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime);

        /// <summary>
        /// Imports candlesticks from JSON data into the database.
        /// </summary>
        Task ImportJsonCandlesticks();
        #endregion

        #region Market Watches
        /// <summary>
        /// Gets a market watch by its ticker symbol.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market watch.</param>
        /// <returns>The market watch DTO if found, null otherwise.</returns>
        Task<MarketWatchDTO?> GetMarketWatch(string marketTicker);

        /// <summary>
        /// Gets market watches filtered by various criteria.
        /// </summary>
        /// <param name="marketTickers">Optional set of market tickers to filter by.</param>
        /// <param name="brainLocksIncluded">Optional set of included brain lock GUIDs.</param>
        /// <param name="brainLocksExcluded">Optional set of excluded brain lock GUIDs.</param>
        /// <param name="brainLockIsNull">Optional flag to filter by null brain locks.</param>
        /// <param name="minInterestScore">Optional minimum interest score threshold.</param>
        /// <param name="maxInterestScore">Optional maximum interest score threshold.</param>
        /// <param name="maxInterestScoreDate">Optional maximum date for interest score filtering.</param>
        /// <returns>A hash set of filtered market watch DTOs.</returns>
        Task<HashSet<MarketWatchDTO>> GetMarketWatches(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null);

        /// <summary>
        /// Gets finalized market watches by brain lock GUID.
        /// </summary>
        /// <param name="brainLock">The brain lock GUID to filter by.</param>
        /// <returns>A list of market watch DTOs associated with the brain lock.</returns>
        Task<List<MarketWatchDTO>> GetFinalizedMarketWatches(Guid brainLock);

        /// <summary>
        /// Adds or updates a market watch in the database.
        /// </summary>
        /// <param name="dto">The market watch DTO to add or update.</param>
        Task AddOrUpdateMarketWatch(MarketWatchDTO dto);

        /// <summary>
        /// Adds or updates multiple market watches in the database.
        /// </summary>
        /// <param name="watches">The list of market watch DTOs to add or update.</param>
        Task AddOrUpdateMarketWatches(List<MarketWatchDTO> watches);

        /// <summary>
        /// Removes multiple market watches from the database.
        /// </summary>
        /// <param name="dtoRange">The list of market watch DTOs to remove.</param>
        Task RemoveMarketWatches(List<MarketWatchDTO> dtoRange);

        /// <summary>
        /// Removes orphaned brain locks from the database.
        /// </summary>
        Task RemoveOrphanedBrainLocks();

        /// <summary>
        /// Removes closed market watches from the database.
        /// </summary>
        Task RemoveClosedWatches();
        #endregion

        #region Market Positions
        /// <summary>
        /// Gets a market position by its ticker symbol.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market position.</param>
        /// <returns>The market position DTO if found, null otherwise.</returns>
        Task<MarketPositionDTO?> GetMarketPosition(string marketTicker);

        /// <summary>
        /// Gets market positions filtered by market tickers, position existence, and resting orders.
        /// </summary>
        /// <param name="marketTickers">Optional set of market tickers to filter by.</param>
        /// <param name="hasPosition">Optional flag to filter by positions.</param>
        /// <param name="hasRestingOrder">Optional flag to filter by resting orders.</param>
        /// <returns>A list of filtered market position DTOs.</returns>
        Task<List<MarketPositionDTO>> GetMarketPositions(HashSet<string>? marketTickers = null,
            bool? hasPosition = null, bool? hasRestingOrder = null);

        /// <summary>
        /// Adds or updates a market position in the database.
        /// </summary>
        /// <param name="dto">The market position DTO to add or update.</param>
        Task AddOrUpdateMarketPosition(MarketPositionDTO dto);

        /// <summary>
        /// Removes a market position by its ticker symbol.
        /// </summary>
        /// <param name="marketTicker">The ticker symbol of the market position to remove.</param>
        Task RemoveMarketPosition(string marketTicker);
        #endregion

        #region Orders
        /// <summary>
        /// Gets orders filtered by market ticker and status.
        /// </summary>
        /// <param name="marketTicker">Optional market ticker to filter by.</param>
        /// <param name="status">Optional status to filter by.</param>
        /// <returns>A list of filtered order DTOs.</returns>
        Task<List<OrderDTO>> GetOrders(string? marketTicker = null, string? status = null);

        /// <summary>
        /// Adds or updates an order in the database.
        /// </summary>
        /// <param name="dto">The order DTO to add or update.</param>
        Task AddOrUpdateOrder(OrderDTO dto);
        #endregion

        #region Snapshot
        /// <summary>
        /// Gets snapshots filtered by market ticker, validation status, dates, and limits.
        /// </summary>
        /// <param name="marketTicker">Optional market ticker to filter by.</param>
        /// <param name="isValidated">Optional flag to filter validated snapshots.</param>
        /// <param name="startDate">Optional start date for filtering.</param>
        /// <param name="endDate">Optional end date for filtering.</param>
        /// <param name="MaxRecords">Optional maximum number of records to return.</param>
        /// <param name="MaxSnapshotVersion">Optional maximum snapshot version to filter by.</param>
        /// <returns>A list of filtered snapshot DTOs.</returns>
        Task<List<SnapshotDTO>> GetSnapshots(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);

        /// <summary>
        /// Adds or updates a snapshot in the database.
        /// </summary>
        /// <param name="dto">The snapshot DTO to add or update.</param>
        Task AddOrUpdateSnapshot(SnapshotDTO dto);

        /// <summary>
        /// Adds or updates multiple snapshots in the database.
        /// </summary>
        /// <param name="dtos">The list of snapshot DTOs to add or update.</param>
        Task AddOrUpdateSnapshots(List<SnapshotDTO> dtos);

        /// <summary>
        /// Adds multiple snapshots to the database.
        /// </summary>
        /// <param name="snapshots">The list of snapshot DTOs to add.</param>
        Task AddSnapshots(List<SnapshotDTO> snapshots);

        /// <summary>
        /// Gets the count of snapshots for a specific market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to count snapshots for.</param>
        /// <returns>The number of snapshots for the market.</returns>
        Task<long> GetSnapshotCount(string marketTicker);
        #endregion

        #region Snapshot Schemas
        /// <summary>
        /// Gets a snapshot schema by its version.
        /// </summary>
        /// <param name="version">The version of the snapshot schema.</param>
        /// <returns>The snapshot schema DTO if found, null otherwise.</returns>
        Task<SnapshotSchemaDTO?> GetSnapshotSchema(int version);

        /// <summary>
        /// Gets all snapshot schemas from the database.
        /// </summary>
        /// <returns>A list of all snapshot schema DTOs.</returns>
        Task<List<SnapshotSchemaDTO>> GetSnapshotSchemas();

        /// <summary>
        /// Adds a snapshot schema to the database.
        /// </summary>
        /// <param name="dto">The snapshot schema DTO to add.</param>
        /// <returns>The added snapshot schema DTO.</returns>
        Task<SnapshotSchemaDTO> AddSnapshotSchema(SnapshotSchemaDTO dto);
        #endregion

        #region Brain Instances
        /// <summary>
        /// Gets a brain instance by its name.
        /// </summary>
        /// <param name="instanceName">Optional instance name to filter by.</param>
        /// <returns>The brain instance DTO if found, null otherwise.</returns>
        Task<BrainInstanceDTO?> GetBrainInstanceByName(string? instanceName);

        /// <summary>
        /// Gets a brain instance by its name.
        /// </summary>
        /// <param name="instanceName">Optional instance name to filter by.</param>
        /// <returns>The brain instance DTO if found, null otherwise.</returns>
        Task<BrainInstanceDTO?> GetBrainInstance(string? instanceName);

        /// <summary>
        /// Gets brain instances filtered by name and brain lock status.
        /// </summary>
        /// <param name="instanceName">Optional instance name to filter by.</param>
        /// <param name="hasBrainLock">Optional flag to filter by brain lock existence.</param>
        /// <returns>A list of filtered brain instance DTOs.</returns>
        Task<List<BrainInstanceDTO>> GetBrainInstancesFiltered(string? instanceName = null, bool? hasBrainLock = null);

        /// <summary>
        /// Gets stale brain instances by brain lock GUID.
        /// </summary>
        /// <param name="brainLock">The brain lock GUID to filter stale instances.</param>
        /// <returns>A list of stale brain instance DTOs.</returns>
        Task<List<BrainInstanceDTO>> GetStaleBrains(Guid brainLock);

        /// <summary>
        /// Adds or updates a brain instance in the database.
        /// </summary>
        /// <param name="dto">The brain instance DTO to add or update.</param>
        Task AddOrUpdateBrainInstance(BrainInstanceDTO dto);

        /// <summary>
        /// Saves persistence data for a brain instance.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <param name="persistenceData">The persistence data to save.</param>
        Task SaveBrainPersistence(string brainInstanceName, string persistenceData);

        /// <summary>
        /// Loads persistence data for a brain instance.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance.</param>
        /// <returns>The persistence data if found, null otherwise.</returns>
        Task<string?> LoadBrainPersistence(string brainInstanceName);

        /// <summary>
        /// Deletes persistence data for a brain instance.
        /// </summary>
        /// <param name="brainInstanceName">The name of the brain instance to delete persistence for.</param>
        Task DeleteBrainPersistence(string brainInstanceName);

        /// <summary>
        /// Gets all brain persistence names.
        /// </summary>
        /// <returns>A list of all brain persistence names.</returns>
        Task<List<string>> GetAllBrainPersistenceNames();
        #endregion

        #region Snapshot Groups
        /// <summary>
        /// Gets snapshot groups filtered by market tickers and maximum group count.
        /// </summary>
        /// <param name="marketTickersToInclude">Optional list of market tickers to include.</param>
        /// <param name="maxGroups">Optional maximum number of groups to return.</param>
        /// <returns>A list of filtered snapshot group DTOs.</returns>
        Task<List<SnapshotGroupDTO>> GetSnapshotGroups(List<string>? marketTickersToInclude = null, int? maxGroups = null);

        /// <summary>
        /// Gets all snapshot group names.
        /// </summary>
        /// <returns>A hash set of snapshot group names.</returns>
        Task<HashSet<string>> GetSnapshotGroupNames();

        /// <summary>
        /// Gets ungrouped snapshots up to a maximum number of markets.
        /// </summary>
        /// <param name="maxMarkets">The maximum number of markets to consider.</param>
        /// <returns>A list of ungrouped snapshot DTOs.</returns>
        Task<List<SnapshotDTO>> GetUngroupedSnapshots(int maxMarkets);

        /// <summary>
        /// Adds or updates multiple snapshot groups in the database.
        /// </summary>
        /// <param name="dtoRange">The list of snapshot group DTOs to add or update.</param>
        Task AddOrUpdateSnapshotGroups(List<SnapshotGroupDTO> dtoRange);

        /// <summary>
        /// Adds or updates a snapshot group in the database.
        /// </summary>
        /// <param name="dto">The snapshot group DTO to add or update.</param>
        Task AddOrUpdateSnapshotGroup(SnapshotGroupDTO dto);
        #endregion

        #region Log Entries
        /// <summary>
        /// Adds a log entry to the database.
        /// </summary>
        /// <param name="dto">The log entry DTO to add.</param>
        Task AddLogEntry(LogEntryDTO dto);

        /// <summary>
        /// Adds an overseer log entry to the database.
        /// </summary>
        /// <param name="dto">The overseer log entry DTO to add.</param>
        Task AddOverseerLogEntry(LogEntryDTO dto);

        /// <summary>
        /// Adds a backtesting log entry to the database.
        /// </summary>
        /// <param name="dto">The backtesting log entry DTO to add.</param>
        Task AddBacktestingLogEntry(LogEntryDTO dto);

        /// <summary>
        /// Gets log entries filtered by brain instance, level, dates, and maximum records.
        /// </summary>
        /// <param name="brainInstance">Optional brain instance to filter by.</param>
        /// <param name="level">Optional log level to filter by.</param>
        /// <param name="startDate">Optional start date for filtering.</param>
        /// <param name="endDate">Optional end date for filtering.</param>
        /// <param name="maxRecords">Optional maximum number of records to return.</param>
        /// <returns>A list of filtered log entry DTOs.</returns>
        Task<List<LogEntryDTO>> GetLogEntries(string? brainInstance = null, string? level = null,
            DateTime? startDate = null, DateTime? endDate = null, int? maxRecords = null);

        /// <summary>
        /// Gets filtered log entries by brain instance, level, dates, and maximum records.
        /// </summary>
        /// <param name="brainInstance">Optional brain instance to filter by.</param>
        /// <param name="level">Optional log level to filter by.</param>
        /// <param name="startDate">Optional start date for filtering.</param>
        /// <param name="endDate">Optional end date for filtering.</param>
        /// <param name="maxRecords">Optional maximum number of records to return.</param>
        /// <returns>A list of filtered log entry DTOs.</returns>
        Task<List<LogEntryDTO>> GetLogEntriesFiltered(string? brainInstance = null, string? level = null,
            DateTime? startDate = null, DateTime? endDate = null, int? maxRecords = null);
        #endregion

        #region WeightSets
        /// <summary>
        /// Gets a weight set by strategy name.
        /// </summary>
        /// <param name="strategyName">The name of the strategy.</param>
        /// <returns>The weight set DTO if found, null otherwise.</returns>
        Task<WeightSetDTO?> GetWeightSetByStrategyName(string strategyName);

        /// <summary>
        /// Gets weight sets filtered by strategy names.
        /// </summary>
        /// <param name="strategyNames">Optional set of strategy names to filter by.</param>
        /// <returns>A list of filtered weight set DTOs.</returns>
        Task<List<WeightSetDTO>> GetWeightSets(HashSet<string>? strategyNames = null);

        /// <summary>
        /// Gets weight sets by market ticker.
        /// </summary>
        /// <param name="marketTicker">The market ticker to filter by.</param>
        /// <returns>A list of weight set DTOs for the market ticker.</returns>
        Task<List<WeightSetDTO>> GetWeightSetsByMarketTicker(string marketTicker);

        /// <summary>
        /// Adds or updates a weight set in the database.
        /// </summary>
        /// <param name="dto">The weight set DTO to add or update.</param>
        Task AddOrUpdateWeightSet(WeightSetDTO dto);

        /// <summary>
        /// Adds or updates multiple weight sets in the database.
        /// </summary>
        /// <param name="dtos">The list of weight set DTOs to add or update.</param>
        Task AddOrUpdateWeightSets(List<WeightSetDTO> dtos);

        /// <summary>
        /// Deletes a weight set by strategy name.
        /// </summary>
        /// <param name="strategyName">The strategy name of the weight set to delete.</param>
        Task DeleteWeightSet(string strategyName);
        #endregion

        #region Announcements
        /// <summary>
        /// Adds multiple announcements to the database.
        /// </summary>
        /// <param name="announcements">The list of announcement DTOs to add.</param>
        Task AddAnnouncements(List<AnnouncementDTO> announcements);
        #endregion

        #region Exchange Schedule
        /// <summary>
        /// Adds an exchange schedule to the database.
        /// </summary>
        /// <param name="exchangeSchedule">The exchange schedule DTO to add.</param>
        Task AddExchangeSchedule(ExchangeScheduleDTO exchangeSchedule);
        #endregion

        #region Other
        /// <summary>
        /// Gets market liquidity states from the database.
        /// </summary>
        /// <returns>A list of market liquidity stats DTOs.</returns>
        Task<List<MarketLiquidityStatsDTO>> GetMarketLiquidityStates();
        #endregion

        #region SignalR Clients
        /// <summary>
        /// Gets SignalR clients filtered by client ID, IP address, and active status.
        /// </summary>
        /// <param name="clientId">Optional client ID to filter by.</param>
        /// <param name="ipAddress">Optional IP address to filter by.</param>
        /// <param name="isActive">Optional flag to filter active clients.</param>
        /// <returns>A list of filtered SignalR client objects.</returns>
        Task<List<SignalRClient>> GetSignalRClients(string? clientId = null, string? ipAddress = null, bool? isActive = null);

        /// <summary>
        /// Gets a SignalR client by its ID.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        /// <returns>The SignalR client if found, null otherwise.</returns>
        Task<SignalRClient?> GetSignalRClientById(string clientId);

        /// <summary>
        /// Gets a SignalR client by its ID.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        /// <returns>The SignalR client if found, null otherwise.</returns>
        Task<SignalRClient?> GetSignalRClient(string clientId);

        /// <summary>
        /// Adds or updates a SignalR client in the database.
        /// </summary>
        /// <param name="client">The SignalR client to add or update.</param>
        Task AddOrUpdateSignalRClient(SignalRClient client);

        /// <summary>
        /// Updates the connection ID for a SignalR client.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        /// <param name="connectionId">The new connection ID.</param>
        Task UpdateSignalRClientConnection(string clientId, string connectionId);

        /// <summary>
        /// Updates the last seen timestamp for a SignalR client.
        /// </summary>
        /// <param name="clientId">The client ID.</param>
        Task UpdateSignalRClientLastSeen(string clientId);

        /// <summary>
        /// Deactivates a SignalR client by its ID.
        /// </summary>
        /// <param name="clientId">The client ID to deactivate.</param>
        Task DeactivateSignalRClient(string clientId);
        #endregion

        #region Overseer Info
        /// <summary>
        /// Adds or updates overseer information in the database.
        /// </summary>
        /// <param name="overseerInfo">The overseer info DTO to add or update.</param>
        Task AddOrUpdateOverseerInfo(BacklashDTOs.Data.OverseerInfo overseerInfo);

        /// <summary>
        /// Gets all active overseer infos from the database.
        /// </summary>
        /// <returns>A list of active overseer info DTOs.</returns>
        Task<List<BacklashDTOs.Data.OverseerInfo>> GetActiveOverseerInfos();

        /// <summary>
        /// Gets overseer info by host name.
        /// </summary>
        /// <param name="hostName">The host name to filter by.</param>
        /// <returns>The overseer info DTO if found, null otherwise.</returns>
        Task<BacklashDTOs.Data.OverseerInfo?> GetOverseerInfoByHostName(string hostName);
        #endregion

        #region Database Health Check
        /// <summary>
        /// Tests basic database connectivity by executing a simple SELECT query.
        /// This method is used for health checks and doesn't depend on any existing data.
        /// </summary>
        Task TestDbAsync();
        #endregion

        #region Performance Metrics
        /// <summary>
        /// Gets the performance metrics for database operations.
        /// </summary>
        /// <returns>A read-only dictionary of performance metrics.</returns>
        IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> GetPerformanceMetrics();

        /// <summary>
        /// Resets all accumulated performance metrics for database operations to their initial state.
        /// This clears success counts, failure counts, total times, and average times for all tracked operations.
        /// </summary>
        void ResetPerformanceMetrics();
        #endregion
    }

    /// <summary>
    /// Interface for accessing database performance metrics from different monitoring services.
    /// This allows the KalshiBotContext to post metrics to the appropriate service based on the project.
    /// </summary>
    public interface IDatabasePerformanceMetrics
    {
        /// <summary>
        /// Gets the current database performance metrics.
        /// </summary>
        IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> DatabaseMetrics { get; }

        /// <summary>
        /// Records database performance metrics.
        /// </summary>
        /// <param name="metrics">Dictionary containing database operation metrics.</param>
        void RecordDatabaseMetrics(Dictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> metrics);
    }
}
