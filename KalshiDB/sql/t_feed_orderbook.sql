SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_feed_orderbook](
	[market_id] [uniqueidentifier] NOT NULL,
	[sid] [int] NOT NULL,
	[kalshi_seq] [int] NOT NULL,
	[market_ticker] [varchar](150) NULL,
	[offer_type] [varchar](3) NOT NULL,
	[price] [int] NOT NULL,
	[delta] [int] NULL,
	[side] [varchar](3) NULL,
	[resting_contracts] [int] NOT NULL,
	[LoggedDate] [datetime2](7) NOT NULL,
	[table_seq] [int] IDENTITY(1,1) NOT NULL,
	[ProcessedDate] [datetime2](7) NULL
 CONSTRAINT [PK_t_feed_orderbook] PRIMARY KEY CLUSTERED 
(
	[table_seq] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX IX_t_feed_orderbook_MarketTicker_LoggedDate
ON [dbo].[t_feed_orderbook] (market_ticker, LoggedDate)
INCLUDE (market_id, sid, kalshi_seq, offer_type, price, delta, side, resting_contracts);
GO