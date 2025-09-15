using BacklashDTOs;
using BacklashDTOs.Data;
using KalshiBotData.Data;

namespace KalshiBotData.Data.Interfaces
{
    public interface IKalshiBotContext : IDisposable
    {
        #region Series
        Task<SeriesDTO?> GetSeriesByTicker(string seriesTicker);
        Task AddOrUpdateSeries(SeriesDTO dto);
        #endregion

        #region Events
        Task<EventDTO?> GetEventByTicker(string eventTicker);
        Task AddOrUpdateEvent(EventDTO dto);
        Task AddOrUpdateEvents(List<EventDTO> dtos);
        #endregion

        #region Markets
        Task<MarketDTO?> GetMarketByTicker(string marketTicker);
        Task<CandlestickDTO?> GetLatestCandlestick(string marketTicker, int? intervalType);
        Task<MarketWatchDTO?> GetMarketWatchByTicker(string marketTicker);
        Task<List<MarketDTO>> GetMarketsFiltered(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null,
            bool? hasMarketWatch = null, double? minimumInterestScore = null,
            DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);
        Task<HashSet<MarketWatchDTO>> GetMarketWatchesFiltered(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null);
        Task<List<SnapshotGroupDTO>> GetSnapshotGroupsFiltered(List<string>? marketTickersToInclude = null, int? maxGroups = null);
        Task<List<SnapshotDTO>> GetSnapshotsFiltered(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);
        Task<List<MarketWatchDTO>> GetFinalizedMarketWatchesByBrainLock(Guid brainLock);
        Task<HashSet<string>> GetMarketTickersWithSnapshots();
        Task<HashSet<string>> GetMarketsWithSnapshots();
        Task<HashSet<string>> GetInactiveMarketsWithNoSnapshots();
        Task<HashSet<string>> GetInactiveMarketTickersWithoutSnapshots();
        Task<HashSet<string>> GetProcessedMarkets();
        Task AddOrUpdateMarket(MarketDTO dto);
        Task DeleteMarket(string marketTicker);
        Task AddOrUpdateMarkets(List<MarketDTO> dtos);
        Task<List<MarketDTO>> GetMarkets(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null, bool? hasMarketWatch = null,
            double? minimumInterestScore = null, DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);
        Task UpdateMarketLastCandlestick(string marketTicker);
        Task<(bool MarketFound, bool EventFound, bool SeriesFound)> GetMarketStatus(string marketTicker);
        #endregion

        #region Tickers
        Task<List<TickerDTO>> GetTickers(string? marketTicker = null, DateTime? loggedDate = null);
        Task AddOrUpdateTickers(List<TickerDTO> dtos);
        Task AddOrUpdateTicker(TickerDTO dto);
        #endregion

        #region Candlesticks
        Task<List<CandlestickDTO>> GetCandlesticks(string marketTicker, int? intervalType = null);
        Task<CandlestickDTO?> GetLastCandlestick(string marketTicker, int? intervalType);
        Task AddOrUpdateCandlestick(CandlestickDTO dto);
        Task<List<CandlestickData>> RetrieveCandlesticksAsync(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime);
        Task ImportJsonCandlesticks();
        #endregion

        #region Market Watches
        Task<MarketWatchDTO?> GetMarketWatch(string marketTicker);
        Task<HashSet<MarketWatchDTO>> GetMarketWatches(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null);
        Task<List<MarketWatchDTO>> GetFinalizedMarketWatches(Guid brainLock);
        Task AddOrUpdateMarketWatch(MarketWatchDTO dto);
        Task AddOrUpdateMarketWatches(List<MarketWatchDTO> watches);
        Task RemoveMarketWatches(List<MarketWatchDTO> dtoRange);
        Task RemoveOrphanedBrainLocks();
        Task RemoveClosedWatches();
        #endregion

        #region Market Positions
        Task<MarketPositionDTO?> GetMarketPosition(string marketTicker);
        Task<List<MarketPositionDTO>> GetMarketPositions(HashSet<string>? marketTickers = null,
            bool? hasPosition = null, bool? hasRestingOrder = null);
        Task AddOrUpdateMarketPosition(MarketPositionDTO dto);
        Task RemoveMarketPosition(string marketTicker);
        #endregion

        #region Orders
        Task<List<OrderDTO>> GetOrders(string? marketTicker = null, string? status = null);
        Task AddOrUpdateOrder(OrderDTO dto);
        #endregion

        #region Snapshot
        Task<List<SnapshotDTO>> GetSnapshots(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);
        Task AddOrUpdateSnapshot(SnapshotDTO dto);
        Task AddOrUpdateSnapshots(List<SnapshotDTO> dtos);
        Task AddSnapshots(List<SnapshotDTO> snapshots);
        Task<long> GetSnapshotCount(string marketTicker);
        #endregion

        #region Snapshot Schemas
        Task<SnapshotSchemaDTO?> GetSnapshotSchema(int version);
        Task<List<SnapshotSchemaDTO>> GetSnapshotSchemas();
        Task<SnapshotSchemaDTO> AddSnapshotSchema(SnapshotSchemaDTO dto);
        #endregion

        #region Brain Instances
        Task<BrainInstanceDTO?> GetBrainInstanceByName(string? instanceName);
        Task<BrainInstanceDTO?> GetBrainInstance(string? instanceName);
        Task<List<BrainInstanceDTO>> GetBrainInstancesFiltered(string? instanceName = null, bool? hasBrainLock = null);
        Task<List<BrainInstanceDTO>> GetStaleBrains(Guid brainLock);
        Task AddOrUpdateBrainInstance(BrainInstanceDTO dto);
        Task SaveBrainPersistence(string brainInstanceName, string persistenceData);
        Task<string?> LoadBrainPersistence(string brainInstanceName);
        Task DeleteBrainPersistence(string brainInstanceName);
        Task<List<string>> GetAllBrainPersistenceNames();
        #endregion

        #region Snapshot Groups
        Task<List<SnapshotGroupDTO>> GetSnapshotGroups(List<string>? marketTickersToInclude = null, int? maxGroups = null);
        Task<HashSet<string>> GetSnapshotGroupNames();
        Task<List<SnapshotDTO>> GetUngroupedSnapshots(int maxMarkets);
        Task AddOrUpdateSnapshotGroups(List<SnapshotGroupDTO> dtoRange);
        Task AddOrUpdateSnapshotGroup(SnapshotGroupDTO dto);
        #endregion

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
