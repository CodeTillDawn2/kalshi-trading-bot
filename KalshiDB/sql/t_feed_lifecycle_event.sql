SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_feed_lifecycle_event](
    [event_ticker] [varchar](150) NOT NULL,
    [title] [varchar](500) NOT NULL,
    [sub_title] [varchar](500) NULL,
    [collateral_return_type] [varchar](50) NULL,
    [series_ticker] [varchar](150) NOT NULL,
    [strike_date] [bigint] NULL,
    [strike_period] [varchar](50) NULL,
    [LoggedDate] [datetime2](7) NOT NULL,
    [ProcessedDate] [datetime2](7) NULL
 CONSTRAINT [PK_t_feed_lifecycle_event] PRIMARY KEY CLUSTERED 
(
    [event_ticker] ASC,
    [LoggedDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_feed_lifecycle_event_ProcessedDate_NotNull]
ON [dbo].[t_feed_lifecycle_event] ([ProcessedDate])
WHERE [ProcessedDate] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX IX_t_feed_lifecycle_event_LoggedDate
ON [dbo].[t_feed_lifecycle_event] (LoggedDate)
INCLUDE (event_ticker, series_ticker, title, sub_title, collateral_return_type, strike_date, strike_period);
GO