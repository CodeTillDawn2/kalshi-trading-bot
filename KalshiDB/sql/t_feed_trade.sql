SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_feed_trade](
	[table_seq] int IDENTITY(1,1) NOT NULL,
	[market_ticker] [varchar](150) NOT NULL,
	[yes_price] [int] NOT NULL,
	[no_price] [int] NOT NULL,
	[count] [int] NOT NULL,
	[taker_side] [varchar](3) NOT NULL,
	[ts] [bigint] NOT NULL,
	[LoggedDate] [datetime2](7) NOT NULL,
	[ProcessedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_t_feed_trade] PRIMARY KEY CLUSTERED 
(
	[table_seq] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_feed_trade_market_ticker] ON [dbo].[t_feed_trade]
(
	[market_ticker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_feed_trade_ProcessedDate_NotNull] ON [dbo].[t_feed_trade]
(
	[ProcessedDate] ASC
)
WHERE ([ProcessedDate] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


CREATE NONCLUSTERED INDEX IX_t_feed_trade_MarketTicker_LoggedDate
ON [dbo].[t_feed_trade] (market_ticker, LoggedDate)
INCLUDE (yes_price, no_price, count, taker_side, ts);
GO