USE [kalshibot-dev]
GO

/****** Object:  Table [dbo].[t_feed_fill]    Script Date: 4/27/2025 1:23:24 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_feed_fill](
	[trade_id] [uniqueidentifier] NOT NULL,
	[order_id] [uniqueidentifier] NOT NULL,
	[market_ticker] [varchar](150) NOT NULL,
	[is_taker] [bit] NOT NULL,
	[side] varchar(3) NOT NULL,
	[ts] [bigint] NOT NULL,
	[yes_price] int NULL,
	[no_price] int NULL,
	[count] [int] NOT NULL,
	[action] [varchar](4) NULL,
	[LoggedDate] [datetime2](7) NOT NULL,
	[ProcessedDate] [datetime2](7) NULL,
 CONSTRAINT [PK_t_feed_fill] PRIMARY KEY CLUSTERED 
(
	[market_ticker] ASC,
	[ts] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE NONCLUSTERED INDEX IX_t_feed_fill_ProcessedDate_NotNull
ON [dbo].[t_feed_fill] ([ProcessedDate])
WHERE [ProcessedDate] IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_t_feed_fill_LoggedDate
ON [dbo].[t_feed_fill] (LoggedDate)
INCLUDE (trade_id, order_id, market_ticker, is_taker, side, ts, yes_price, no_price, count, action);
GO