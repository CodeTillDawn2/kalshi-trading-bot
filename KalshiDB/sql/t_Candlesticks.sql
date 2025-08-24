-- Create new table with computed columns
CREATE TABLE [dbo].[t_Candlesticks](
    [market_ticker] [nvarchar](150) NOT NULL,
    [interval_type] [int] NOT NULL,
    [end_period_ts] [bigint] NOT NULL,
    [end_period_datetime_utc]  AS (dateadd(second,[end_period_ts],CONVERT([datetime2](0),'1970-01-01T00:00:00',(126)))) PERSISTED,
	[year]  AS (datepart(year,dateadd(second,[end_period_ts],CONVERT([datetime2](0),'1970-01-01T00:00:00',(126))))) PERSISTED,
	[month]  AS (datepart(month,dateadd(second,[end_period_ts],CONVERT([datetime2](0),'1970-01-01T00:00:00',(126))))) PERSISTED,
	[day]  AS (datepart(day,dateadd(second,[end_period_ts],CONVERT([datetime2](0),'1970-01-01T00:00:00',(126))))) PERSISTED,
	[hour]  AS (datepart(hour,dateadd(second,[end_period_ts],CONVERT([datetime2](0),'1970-01-01T00:00:00',(126))))) PERSISTED,
	[minute]  AS (datepart(minute,dateadd(second,[end_period_ts],CONVERT([datetime2](0),'1970-01-01T00:00:00',(126))))) PERSISTED,
    [open_interest] [int] NOT NULL,
    [price_close] [int] NULL,
    [price_high] [int] NULL,
    [price_low] [int] NULL,
    [price_mean] [int] NULL,
    [price_open] [int] NULL,
    [price_previous] [int] NULL,
    [volume] [int] NOT NULL,
    [yes_ask_close] [int] NOT NULL,
    [yes_ask_high] [int] NOT NULL,
    [yes_ask_low] [int] NOT NULL,
    [yes_ask_open] [int] NOT NULL,
    [yes_bid_close] [int] NOT NULL,
    [yes_bid_high] [int] NOT NULL,
    [yes_bid_low] [int] NOT NULL,
    [yes_bid_open] [int] NOT NULL,
    CONSTRAINT [PK_t_Candlesticks] PRIMARY KEY CLUSTERED 
    (
        [market_ticker] ASC,
        [interval_type] ASC,
        [end_period_ts] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = ON) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_t_Candlesticks_market_ticker_interval_type]
ON [dbo].[t_Candlesticks] (
    [market_ticker] ASC,
    [interval_type] ASC
)
INCLUDE ([end_period_datetime_utc])
WITH (
    PAD_INDEX = OFF,
    STATISTICS_NORECOMPUTE = OFF,
    SORT_IN_TEMPDB = OFF,
    DROP_EXISTING = OFF,
    ONLINE = OFF,
    ALLOW_ROW_LOCKS = ON,
    ALLOW_PAGE_LOCKS = ON,
    FILLFACTOR = 90,
    OPTIMIZE_FOR_SEQUENTIAL_KEY = ON
) ON [PRIMARY]
GO