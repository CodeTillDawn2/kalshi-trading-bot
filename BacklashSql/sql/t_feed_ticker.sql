SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_feed_ticker](
	[market_id] [uniqueidentifier] NOT NULL,
	[market_ticker] [varchar](150) NOT NULL,
	[price] [int] NOT NULL,
	[yes_bid] [int] NOT NULL,
	[yes_ask] [int] NOT NULL,
	[volume] [int] NOT NULL,
	[open_interest] [int] NOT NULL,
	[dollar_volume] [int] NOT NULL,
	[dollar_open_interest] [int] NOT NULL,
	[ts] [bigint] NOT NULL,
	[LoggedDate] [datetime2](7) NOT NULL,
	[ProcessedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_Feed_Ticker] PRIMARY KEY CLUSTERED
(
 [market_ticker] ASC,
 [ts] ASC,
 [price] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX IX_t_feed_ticker_ProcessedDate_NotNull
ON [dbo].[t_feed_ticker] ([ProcessedDate])
WHERE [ProcessedDate] IS NOT NULL;