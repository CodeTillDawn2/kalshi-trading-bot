using BacklashDTOs;
using BacklashDTOs.Data;

namespace KalshiBotData.Data.Interfaces
{
    public interface IKalshiBotContext : IDisposable
    {
        #region Series
        Task<SeriesDTO?> GetSeriesByTicker(string seriesTicker);
        Task<SeriesDTO?> GetSeriesByTicker_cached(string seriesTicker);
        Task AddOrUpdateSeries(SeriesDTO dto);
        #endregion

        #region Events
        Task<EventDTO?> GetEventByTicker(string eventTicker);
        Task<EventDTO?> GetEventByTicker_cached(string eventTicker);
        Task AddOrUpdateEvent(EventDTO dto);
        #endregion

        #region Markets
        Task<MarketDTO?> GetMarketByTicker(string marketName);
        Task<MarketDTO?> GetMarketByTicker_cached(string marketName);
        Task<HashSet<string>> GetMarketsWithSnapshots();
        Task<HashSet<string>> GetInactiveMarketsWithNoSnapshots();
        Task<HashSet<string>> GetProcessedMarkets();
        Task AddOrUpdateMarket(MarketDTO dto);
        Task AddOrUpdateMarkets(List<MarketDTO> dtos);
        Task DeleteMarket(string marketTicker);
        Task<List<MarketDTO>> GetMarkets(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null, bool? hasMarketWatch = null,
            double? minimumInterestScore = null, DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);
        Task<List<MarketDTO>> GetMarkets_cached(HashSet<string>? includedStatuses = null, HashSet<string>? excludedStatuses = null,
            HashSet<string>? includedMarkets = null, HashSet<string>? excludedMarkets = null, bool? hasMarketWatch = null,
            double? minimumInterestScore = null, DateTime? maxInterestScoreDate = null, DateTime? maxAPILastFetchTime = null);
        Task UpdateMarketLastCandlestick(string marketTicker);
        Task<(bool MarketFound, bool EventFound, bool SeriesFound)> GetMarketStatus(string marketTicker);
        Task<(bool MarketFound, bool EventFound, bool SeriesFound)> GetMarketStatus_cached(string marketTicker);
        #endregion

        #region Tickers
        Task<List<TickerDTO>> GetTickers(string? marketTicker = null, DateTime? loggedDate = null);
        Task<List<TickerDTO>> GetTickers_cached(string? marketTicker = null, DateTime? loggedDate = null);
        Task AddOrUpdateTickers(List<TickerDTO> dto);
        Task AddOrUpdateTicker(TickerDTO dto);
        #endregion

        #region Candlesticks
        Task<List<CandlestickDTO>> GetCandlesticks(string marketTicker, int? intervalType = null);
        Task<List<CandlestickDTO>> GetCandlesticks_cached(string marketTicker, int? intervalType = null);
        Task<CandlestickDTO?> GetLastCandlestick(string marketTicker, int? intervalType);
        Task<CandlestickDTO?> GetLastCandlestick_cached(string marketTicker, int? intervalType);
        Task AddOrUpdateCandlestick(CandlestickDTO dto);
        Task<List<CandlestickData>> RetrieveCandlesticksAsync(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime);
        Task<List<CandlestickData>> RetrieveCandlesticksAsync_cached(CancellationToken token, int intervalType, string marketTicker, DateTime sqlStartTime);
        Task ImportJsonCandlesticks();
        #endregion

        #region Market Watches
        Task<MarketWatchDTO?> GetMarketWatch(string marketTicker);
        Task<HashSet<MarketWatchDTO>> GetMarketWatches(HashSet<string>? marketTickers = null,
            HashSet<Guid>? brainLocksIncluded = null, HashSet<Guid>? brainLocksExcluded = null, bool? brainLockIsNull = null,
            double? minInterestScore = null, double? maxInterestScore = null, DateTime? maxInterestScoreDate = null);
        Task<HashSet<MarketWatchDTO>> GetMarketWatches_cached(HashSet<string>? marketTickers = null,
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
        Task<List<MarketPositionDTO>> GetMarketPositions(HashSet<string>? marketTickers = null
            , bool? hasPosition = null, bool? hasRestingOrder = null);
        Task<List<MarketPositionDTO>> GetMarketPositions_cached(HashSet<string>? marketTickers = null
            , bool? hasPosition = null, bool? hasRestingOrder = null);
        Task AddOrUpdateMarketPosition(MarketPositionDTO dto);
        Task RemoveMarketPosition(string marketTicker);
        #endregion

        #region Orders
        Task<List<OrderDTO>> GetOrders(string? marketTicker = null, string? status = null);
        Task<List<OrderDTO>> GetOrders_cached(string? marketTicker = null, string? status = null);
        Task AddOrUpdateOrder(OrderDTO dto);
        #endregion

        #region Snapshot
        Task<List<SnapshotDTO>> GetSnapshots(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);
        Task<List<SnapshotDTO>> GetSnapshots_cached(string? marketTicker = null, bool? isValidated = null,
            DateTime? startDate = null, DateTime? endDate = null, int? MaxRecords = null, int? MaxSnapshotVersion = null);
        Task AddOrUpdateSnapshot(SnapshotDTO dto);
        Task AddOrUpdateSnapshots(List<SnapshotDTO> dtos);

        Task AddSnapshots(List<SnapshotDTO> snapshots);

        Task<long> GetSnapshotCount(string marketTicker);
        #endregion

        #region Snapshot Schemas
        Task<SnapshotSchemaDTO?> GetSnapshotSchema(int version);
        Task<SnapshotSchemaDTO?> GetSnapshotSchema_cached(int version);
        Task<List<SnapshotSchemaDTO>> GetSnapshotSchemas();
        Task<SnapshotSchemaDTO> AddSnapshotSchema(SnapshotSchemaDTO dto);
        #endregion

        #region Brain Instances
        Task<BrainInstanceDTO?> GetBrainInstance(string? instanceName);
        Task<List<BrainInstanceDTO>> GetBrainInstances(string? instanceName = null, bool? hasBrainLock = null);
        Task<List<BrainInstanceDTO>> GetBrainInstances_cached(string? instanceName = null, bool? hasBrainLock = null);
        Task<List<BrainInstanceDTO>> GetStaleBrains(Guid brainLock);
        Task AddOrUpdateBrainInstance(BrainInstanceDTO dto);
        #endregion

        #region Snapshot Groups
        Task<List<SnapshotGroupDTO>> GetSnapshotGroups(List<string>? marketTickersToInclude = null, int? maxGroups = null);
        Task<List<SnapshotGroupDTO>> GetSnapshotGroups_cached(List<string>? marketTickersToInclude = null, int? maxGroups = null);
        Task<HashSet<string>> GetSnapshotGroupNames();
        Task<HashSet<string>> GetSnapshotGroupNames_cached();
        Task<List<SnapshotDTO>> GetUngroupedSnapshots(int maxMarkets);
        Task AddOrUpdateSnapshotGroups(List<SnapshotGroupDTO> dtoRange);
        Task AddOrUpdateSnapshotGroup(SnapshotGroupDTO dto);
        #endregion

        #region Log Entries
        Task AddLogEntry(LogEntryDTO dto);
        Task<List<LogEntryDTO>> GetLogEntries(string? brainInstance = null, string? level = null,
            DateTime? startDate = null, DateTime? endDate = null, int? maxRecords = null);
        #endregion

        #region WeightSets
        Task<WeightSetDTO?> GetWeightSetByStrategyName(string strategyName);
        Task<WeightSetDTO?> GetWeightSetByStrategyName_cached(string strategyName);
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
        Task<List<MarketLiquidityStatsDTO>> GetMarketLiquidityStates_cached();
        #endregion
    }
}
