SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_feed_lifecycle_market](
	[market_ticker] [varchar](150) NOT NULL,
	[open_ts] [bigint] NOT NULL,
	[close_ts] [bigint] NOT NULL,
	[determination_ts] [bigint] NULL,
	[settled_ts] [bigint] NULL,
	[result] [varchar](3) NULL,
	[is_deactivated] [bit] NOT NULL,
	[LoggedDate] [datetime2](7) NOT NULL,
	[ProcessedDate] [datetime2](7) NULL
 CONSTRAINT [PK_t_feed_lifecycle_market] PRIMARY KEY CLUSTERED 
(
	[market_ticker] ASC,
	[LoggedDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX IX_t_feed_lifecycle_market_LoggedDate
ON [dbo].[t_feed_lifecycle_market] (LoggedDate)
INCLUDE (open_ts, close_ts, determination_ts, settled_ts, result, is_deactivated);
GO