USE [kalshibot-dev]
GO

/****** Object:  Table [dbo].[t_MarketWatches]    Script Date: 5/17/2025 2:56:17 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_MarketWatches](
	[market_ticker] [varchar](150) NOT NULL,
	[BrainLock] [uniqueidentifier] NULL,
	[InterestScore] [float] NULL,
	[AverageWebsocketEventsPerMinute] [float] NULL,
	[InterestScoreDate] [datetime] NULL,
	[LastWatched] [datetime] NULL,
 CONSTRAINT [PK_t_MarketWatches] PRIMARY KEY CLUSTERED 
(
	[market_ticker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE NONCLUSTERED INDEX [IX_t_MarketWatches_market_ticker_interest]
ON [dbo].[t_MarketWatches] (
    [market_ticker] ASC,
    [InterestScore] ASC,
    [InterestScoreDate] ASC
)
INCLUDE ([BrainLock], [LastWatched])
WITH (
    PAD_INDEX = OFF,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON,
    FILLFACTOR = 80,
    OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
) ON [PRIMARY]
GO