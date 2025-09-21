SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_Markets](
    [market_ticker] [varchar](150) NOT NULL,
    [event_ticker] [varchar](50) NOT NULL,
    [market_type] [varchar](20) NOT NULL,
    [title] [varchar](1000) NOT NULL,
    [subtitle] [varchar](255) NULL,
    [yes_sub_title] [varchar](255) NOT NULL,
    [no_sub_title] [varchar](255) NOT NULL,
    [open_time] [datetime] NOT NULL,
    [close_time] [datetime] NOT NULL,
    [expected_expiration_time] [datetime] NULL,
    [expiration_time] [datetime] NOT NULL,
    [latest_expiration_time] [datetime] NOT NULL,
    [settlement_timer_seconds] [int] NOT NULL,
    [status] [varchar](20) NOT NULL,
    [response_price_units] [varchar](20) NOT NULL,
    [notional_value] [int] NOT NULL,
    [tick_size] [int] NOT NULL,
    [yes_bid] [int] NOT NULL,
    [yes_ask] [int] NOT NULL,
    [no_bid] [int] NOT NULL,
    [no_ask] [int] NOT NULL,
    [last_price] [int] NOT NULL,
    [previous_yes_bid] [int] NOT NULL,
    [previous_yes_ask] [int] NOT NULL,
    [previous_price] [int] NOT NULL,
    [volume] [bigint] NOT NULL,
    [volume_24h] [int] NOT NULL,
    [liquidity] [bigint] NOT NULL,
    [open_interest] [int] NOT NULL,
    [result] [varchar](20) NOT NULL,
    [can_close_early] [bit] NOT NULL,
    [expiration_value] [varchar](255) NOT NULL,
    [category] [varchar](50) NOT NULL,
    [risk_limit_cents] [int] NOT NULL,
    [strike_type] [varchar](30) NOT NULL,
    [floor_strike] [float] NULL,
    [rules_primary] [varchar](MAX) NOT NULL,
    [rules_secondary] [varchar](MAX) NULL,
    [CreatedDate] [datetime] NOT NULL CONSTRAINT [DF_t_Markets_CreatedDate] DEFAULT (GETDATE()),
    [LastModifiedDate] [datetime] NULL,
    [LastCandlestickUTC] [datetime] NULL,
	APILastFetchedDate [datetime] NULL
    CONSTRAINT [PK_t_Markets] PRIMARY KEY CLUSTERED 
    (
        [market_ticker] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_Markets_market_ticker_key_columns]
ON [dbo].[t_Markets] (
    [market_ticker] ASC
)
INCLUDE (
    [event_ticker], [market_type], [title], [subtitle], [yes_sub_title], [no_sub_title],
    [open_time], [close_time], [expected_expiration_time], [expiration_time], [latest_expiration_time],
    [settlement_timer_seconds], [status], [response_price_units], [notional_value], [tick_size],
    [yes_bid], [yes_ask], [no_bid], [no_ask], [last_price], [previous_yes_bid], [previous_yes_ask],
    [previous_price], [volume], [volume_24h], [liquidity], [open_interest], [result],
    [can_close_early], [expiration_value], [category], [risk_limit_cents], [strike_type],
    [floor_strike], [rules_primary], [rules_secondary], [CreatedDate], [APILastFetchedDate],
    [LastCandlestickUTC], [LastModifiedDate]
)
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

CREATE NONCLUSTERED INDEX [IX_t_Markets_status_close_time]
ON [dbo].[t_Markets] (
    [status] ASC,
    [close_time] ASC
)
INCLUDE ([market_ticker], [event_ticker], [category], [last_price], [APILastFetchedDate])
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