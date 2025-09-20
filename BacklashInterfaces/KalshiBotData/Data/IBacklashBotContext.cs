using BacklashDTOs;
using BacklashDTOs.Data;
using BacklashBotData.Data;

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

/// <summary>GetMarketByTicker</summary>
/// <summary>AddOrUpdateEvents</summary>
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
        #endregion
/// <summary>GetSnapshotGroupsFiltered</summary>

        #region Markets
/// <summary>GetFinalizedMarketWatchesByBrainLock</summary>
        Task<MarketDTO?> GetMarketByTicker(string marketTicker);
/// <summary>GetMarketsWithSnapshots</summary>
        Task<CandlestickDTO?> GetLatestCandlestick(string marketTicker, int? intervalType);
/// <summary>GetInactiveMarketTickersWithoutSnapshots</summary>
        Task<MarketWatchDTO?> GetMarketWatchByTicker(string marketTicker);
/// <summary>AddOrUpdateMarket</summary>
        Task<List<MarketDTO>> GetMarketsFiltered(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
/// <summary>AddOrUpdateMarkets</summary>
/// <summary>GetSnapshotGroupsFiltered</summary>
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null,
            bool? hasMarketWatch = null, double? minimumInterestScore = null,
/// <summary>UpdateMarketLastCandlestick</summary>
/// <summary>GetFinalizedMarketWatchesByBrainLock</summary>
            DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);
/// <summary>GetMarketsWithSnapshots</summary>
        Task<HashSet<MarketWatchDTO>> GetMarketWatchesFiltered(HashSet<string>? marketTickers = null,
/// <summary>GetTickers</summary>
/// <summary>GetInactiveMarketTickersWithoutSnapshots</summary>
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
/// <summary>AddOrUpdateMarket</summary>
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null);
/// <summary>AddOrUpdateMarkets</summary>
        Task<List<SnapshotGroupDTO>> GetSnapshotGroupsFiltered(List<string>? marketTickersToInclude = null, int? maxGroups = null);
/// <summary>GetLastCandlestick</summary>
        Task<List<SnapshotDTO>> GetSnapshotsFiltered(string? marketTicker = null, bool? isValidated = null,
/// <summary>RetrieveCandlesticksAsync</summary>
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);
/// <summary>UpdateMarketLastCandlestick</summary>
        Task<List<MarketWatchDTO>> GetFinalizedMarketWatchesByBrainLock(Guid brainLock);
        Task<HashSet<string>> GetMarketTickersWithSnapshots();
/// <summary>GetMarketWatch</summary>
        Task<HashSet<string>> GetMarketsWithSnapshots();
        Task<HashSet<string>> GetInactiveMarketsWithNoSnapshots();
/// <summary>GetTickers</summary>
        Task<HashSet<string>> GetInactiveMarketTickersWithoutSnapshots();
/// <summary>AddOrUpdateMarketWatch</summary>
/// <summary>AddOrUpdateTicker</summary>
        Task<HashSet<string>> GetProcessedMarkets();
/// <summary>RemoveOrphanedBrainLocks</summary>
        Task AddOrUpdateMarket(MarketDTO dto);
        Task DeleteMarket(string marketTicker);
/// <summary>GetCandlesticks</summary>
        Task AddOrUpdateMarkets(List<MarketDTO> dtos);
/// <summary>GetMarketPosition</summary>
/// <summary>AddOrUpdateCandlestick</summary>
        Task<List<MarketDTO>> GetMarkets(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
/// <summary>AddOrUpdateMarketPosition</summary>
/// <summary>ImportJsonCandlesticks</summary>
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null, bool? hasMarketWatch = null,
            double? minimumInterestScore = null, DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);
        Task UpdateMarketLastCandlestick(string marketTicker);
/// <summary>GetOrders</summary>
        Task<(bool MarketFound, bool EventFound, bool SeriesFound)> GetMarketStatus(string marketTicker);
        #endregion

        #region Tickers
/// <summary>GetSnapshots</summary>
        Task<List<TickerDTO>> GetTickers(string? marketTicker = null, DateTime? loggedDate = null);
/// <summary>AddOrUpdateSnapshot</summary>
        Task AddOrUpdateTickers(List<TickerDTO> dtos);
/// <summary>AddSnapshots</summary>
/// <summary>AddOrUpdateMarketWatches</summary>
        Task AddOrUpdateTicker(TickerDTO dto);
/// <summary>RemoveOrphanedBrainLocks</summary>
        #endregion
/// <summary>GetSnapshotSchema</summary>

/// <summary>AddSnapshotSchema</summary>
        #region Candlesticks
        Task<List<CandlestickDTO>> GetCandlesticks(string marketTicker, int? intervalType = null);
/// <summary>GetMarketPosition</summary>
        Task<CandlestickDTO?> GetLastCandlestick(string marketTicker, int? intervalType);
/// <summary>GetBrainInstance</summary>
        Task AddOrUpdateCandlestick(CandlestickDTO dto);
/// <summary>GetStaleBrains</summary>
/// <summary>AddOrUpdateMarketPosition</summary>
        Task<List<CandlestickData>> RetrieveCandlesticksAsync(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime);
/// <summary>LoadBrainPersistence</summary>
        Task ImportJsonCandlesticks();
/// <summary>GetAllBrainPersistenceNames</summary>
        #endregion

/// <summary>GetOrders</summary>
        #region Market Watches
/// <summary>GetSnapshotGroupNames</summary>
        Task<MarketWatchDTO?> GetMarketWatch(string marketTicker);
/// <summary>AddOrUpdateSnapshotGroups</summary>
        Task<HashSet<MarketWatchDTO>> GetMarketWatches(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
/// <summary>GetSnapshots</summary>
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null);
/// <summary>AddLogEntry</summary>
/// <summary>AddOrUpdateSnapshot</summary>
        Task<List<MarketWatchDTO>> GetFinalizedMarketWatches(Guid brainLock);
/// <summary>AddSnapshots</summary>
        Task AddOrUpdateMarketWatch(MarketWatchDTO dto);
        Task AddOrUpdateMarketWatches(List<MarketWatchDTO> watches);
        Task RemoveMarketWatches(List<MarketWatchDTO> dtoRange);
        Task RemoveOrphanedBrainLocks();
        Task RemoveClosedWatches();
/// <summary>GetWeightSetByStrategyName</summary>
/// <summary>GetSnapshotSchemas</summary>
        #endregion
/// <summary>AddOrUpdateWeightSet</summary>

/// <summary>DeleteWeightSet</summary>
        #region Market Positions
        Task<MarketPositionDTO?> GetMarketPosition(string marketTicker);
/// <summary>GetBrainInstanceByName</summary>
        Task<List<MarketPositionDTO>> GetMarketPositions(HashSet<string>? marketTickers = null,
/// <summary>GetBrainInstancesFiltered</summary>
            bool? hasPosition = null, bool? hasRestingOrder = null);
/// <summary>AddOrUpdateBrainInstance</summary>
        Task AddOrUpdateMarketPosition(MarketPositionDTO dto);
/// <summary>LoadBrainPersistence</summary>
        Task RemoveMarketPosition(string marketTicker);
/// <summary>GetAllBrainPersistenceNames</summary>
        #endregion

        #region Orders
/// <summary>GetSnapshotGroups</summary>
        Task<List<OrderDTO>> GetOrders(string? marketTicker = null, string? status = null);
/// <summary>GetSignalRClientById</summary>
/// <summary>GetUngroupedSnapshots</summary>
        Task AddOrUpdateOrder(OrderDTO dto);
/// <summary>UpdateSignalRClientConnection</summary>
/// <summary>AddOrUpdateSnapshotGroup</summary>
        #endregion

        #region Snapshot
/// <summary>AddLogEntry</summary>
        Task<List<SnapshotDTO>> GetSnapshots(string? marketTicker = null, bool? isValidated = null,
/// <summary>GetActiveOverseerInfos</summary>
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);
        Task AddOrUpdateSnapshot(SnapshotDTO dto);
        Task AddOrUpdateSnapshots(List<SnapshotDTO> dtos);
        Task AddSnapshots(List<SnapshotDTO> snapshots);
/// <summary>GetPerformanceMetrics</summary>
        Task<long> GetSnapshotCount(string marketTicker);
        #endregion

        #region Snapshot Schemas
/// <summary>GetWeightSets</summary>
        Task<SnapshotSchemaDTO?> GetSnapshotSchema(int version);
/// <summary>AddOrUpdateWeightSet</summary>
        Task<List<SnapshotSchemaDTO>> GetSnapshotSchemas();
/// <summary>DeleteWeightSet</summary>
        Task<SnapshotSchemaDTO> AddSnapshotSchema(SnapshotSchemaDTO dto);
        #endregion

/// <summary>AddAnnouncements</summary>
        #region Brain Instances
        Task<BrainInstanceDTO?> GetBrainInstanceByName(string? instanceName);
        Task<BrainInstanceDTO?> GetBrainInstance(string? instanceName);
/// <summary>AddExchangeSchedule</summary>
        Task<List<BrainInstanceDTO>> GetBrainInstancesFiltered(string? instanceName = null, bool? hasBrainLock = null);
        Task<List<BrainInstanceDTO>> GetStaleBrains(Guid brainLock);
        Task AddOrUpdateBrainInstance(BrainInstanceDTO dto);
/// <summary>GetMarketLiquidityStates</summary>
        Task SaveBrainPersistence(string brainInstanceName, string persistenceData);
        Task<string?> LoadBrainPersistence(string brainInstanceName);
        Task DeleteBrainPersistence(string brainInstanceName);
/// <summary>GetSignalRClients</summary>
        Task<List<string>> GetAllBrainPersistenceNames();
/// <summary>GetSignalRClient</summary>
        #endregion
/// <summary>UpdateSignalRClientConnection</summary>

/// <summary>DeactivateSignalRClient</summary>
        #region Snapshot Groups
        Task<List<SnapshotGroupDTO>> GetSnapshotGroups(List<string>? marketTickersToInclude = null, int? maxGroups = null);
        Task<HashSet<string>> GetSnapshotGroupNames();
/// <summary>AddOrUpdateOverseerInfo</summary>
        Task<List<SnapshotDTO>> GetUngroupedSnapshots(int maxMarkets);
/// <summary>GetOverseerInfoByHostName</summary>
        Task AddOrUpdateSnapshotGroups(List<SnapshotGroupDTO> dtoRange);
        Task AddOrUpdateSnapshotGroup(SnapshotGroupDTO dto);
        #endregion
/// <summary>GetPerformanceMetrics</summary>

        #region Log Entries
        Task AddLogEntry(LogEntryDTO dto);
        Task AddOverseerLogEntry(LogEntryDTO dto);
        Task<List<LogEntryDTO>> GetLogEntries(string? brainInstance = null, string? level = null,
            DateTime? startDate = null, DateTime? endDate = null, int? maxRecords = null);
        Task<List<LogEntryDTO>> GetLogEntriesFiltered(string? brainInstance = null, string? level = null,
            DateTime? startDate = null, DateTime? endDate = null, int? maxRecords = null);
        #endregion

        #region WeightSets
        Task<WeightSetDTO?> GetWeightSetByStrategyName(string strategyName);
        Task<List<WeightSetDTO>> GetWeightSets(HashSet<string>? strategyNames = null);
        Task<List<WeightSetDTO>> GetWeightSetsByMarketTicker(string marketTicker);
        Task AddOrUpdateWeightSet(WeightSetDTO dto);
        Task AddOrUpdateWeightSets(List<WeightSetDTO> dtos);
        Task DeleteWeightSet(string strategyName);
        #endregion

        #region Announcements
        Task AddAnnouncements(List<AnnouncementDTO> announcements);
        #endregion

        #region Exchange Schedule
        Task AddExchangeSchedule(ExchangeScheduleDTO exchangeSchedule);
        #endregion

        #region Other
        Task<List<MarketLiquidityStatsDTO>> GetMarketLiquidityStates();
        #endregion

        #region SignalR Clients
        Task<List<SignalRClient>> GetSignalRClients(string? clientId = null, string? ipAddress = null, bool? isActive = null);
        Task<SignalRClient?> GetSignalRClientById(string clientId);
        Task<SignalRClient?> GetSignalRClient(string clientId);
        Task AddOrUpdateSignalRClient(SignalRClient client);
        Task UpdateSignalRClientConnection(string clientId, string connectionId);
        Task UpdateSignalRClientLastSeen(string clientId);
        Task DeactivateSignalRClient(string clientId);
        #endregion

        #region Overseer Info
        Task AddOrUpdateOverseerInfo(BacklashDTOs.Data.OverseerInfo overseerInfo);
        Task<List<BacklashDTOs.Data.OverseerInfo>> GetActiveOverseerInfos();
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
        IReadOnlyDictionary<string, (int SuccessCount, int FailureCount, TimeSpan TotalTime, double AverageTimeMs)> GetPerformanceMetrics();
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
