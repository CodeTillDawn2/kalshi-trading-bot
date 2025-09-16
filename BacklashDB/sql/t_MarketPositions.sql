CREATE TABLE t_MarketPositions (
    Ticker NVARCHAR(50) PRIMARY KEY,
    TotalTraded BIGINT NOT NULL,
    Position INT NOT NULL,
    MarketExposure BIGINT NOT NULL,
    RealizedPnl BIGINT NOT NULL,
    RestingOrdersCount INT NOT NULL,
    FeesPaid BIGINT NOT NULL,
    LastUpdatedUTC DATETIME NOT NULL,
    LastModified DATETIME NULL
);

CREATE NONCLUSTERED INDEX [IX_t_MarketPositions_Ticker_Position]
ON [dbo].[t_MarketPositions] (
    [Ticker] ASC,
    [Position] ASC
)
INCLUDE ([TotalTraded], [MarketExposure], [RealizedPnl], [RestingOrdersCount], [FeesPaid], [LastUpdatedUTC], [LastModified])
WITH (
    PAD_INDEX = OFF,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON,
    FILLFACTOR = 90,
    OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
) ON [PRIMARY]
GO