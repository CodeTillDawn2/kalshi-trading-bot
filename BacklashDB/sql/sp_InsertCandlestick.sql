USE [kalshibot-dev]
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_InsertCandlestick]
    @market_ticker nvarchar(150),
    @interval_type int,
    @end_period_ts bigint,
    @open_interest int,
    @volume int,
    @price_close int = NULL,
    @price_high int = NULL,
    @price_low int = NULL,
    @price_mean int = NULL,
    @price_open int = NULL,
    @price_previous int = NULL,
    @yes_ask_close int,
    @yes_ask_high int,
    @yes_ask_low int,
    @yes_ask_open int,
    @yes_bid_close int,
    @yes_bid_high int,
    @yes_bid_low int,
    @yes_bid_open int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EndPeriodDate datetime = DATEADD(SECOND, @end_period_ts, '1970-01-01')

    MERGE [dbo].[t_Candlesticks] AS target
    USING (
        VALUES (
            @market_ticker,
            @interval_type,
            @end_period_ts,
            DATEPART(YEAR, @EndPeriodDate),
            DATEPART(MONTH, @EndPeriodDate),
            DATEPART(DAY, @EndPeriodDate),
            DATEPART(HOUR, @EndPeriodDate),
            DATEPART(MINUTE, @EndPeriodDate),
            @open_interest,
            @volume,
            @price_close,
            @price_high,
            @price_low,
            @price_mean,
            @price_open,
            @price_previous,
            @yes_ask_close,
            @yes_ask_high,
            @yes_ask_low,
            @yes_ask_open,
            @yes_bid_close,
            @yes_bid_high,
            @yes_bid_low,
            @yes_bid_open
        )
    ) AS source (
        market_ticker,
        interval_type,
        end_period_ts,
        year,
        month,
        day,
        hour,
        minute,
        open_interest,
        volume,
        price_close,
        price_high,
        price_low,
        price_mean,
        price_open,
        price_previous,
        yes_ask_close,
        yes_ask_high,
        yes_ask_low,
        yes_ask_open,
        yes_bid_close,
        yes_bid_high,
        yes_bid_low,
        yes_bid_open
    )
    ON (
        target.market_ticker = source.market_ticker AND
        target.interval_type = source.interval_type AND
        target.end_period_ts = source.end_period_ts
    )
    WHEN MATCHED THEN
        UPDATE SET
            target.year = source.year,
            target.month = source.month,
            target.day = source.day,
            target.hour = source.hour,
            target.minute = source.minute,
            target.open_interest = source.open_interest,
            target.volume = source.volume,
            target.price_close = source.price_close,
            target.price_high = source.price_high,
            target.price_low = source.price_low,
            target.price_mean = source.price_mean,
            target.price_open = source.price_open,
            target.price_previous = source.price_previous,
            target.yes_ask_close = source.yes_ask_close,
            target.yes_ask_high = source.yes_ask_high,
            target.yes_ask_low = source.yes_ask_low,
            target.yes_ask_open = source.yes_ask_open,
            target.yes_bid_close = source.yes_bid_close,
            target.yes_bid_high = source.yes_bid_high,
            target.yes_bid_low = source.yes_bid_low,
            target.yes_bid_open = source.yes_bid_open
    WHEN NOT MATCHED THEN
        INSERT (
            market_ticker,
            interval_type,
            end_period_ts,
            year,
            month,
            day,
            hour,
            minute,
            open_interest,
            volume,
            price_close,
            price_high,
            price_low,
            price_mean,
            price_open,
            price_previous,
            yes_ask_close,
            yes_ask_high,
            yes_ask_low,
            yes_ask_open,
            yes_bid_close,
            yes_bid_high,
            yes_bid_low,
            yes_bid_open
        )
        VALUES (
            source.market_ticker,
            source.interval_type,
            source.end_period_ts,
            source.year,
            source.month,
            source.day,
            source.hour,
            source.minute,
            source.open_interest,
            source.volume,
            source.price_close,
            source.price_high,
            source.price_low,
            source.price_mean,
            source.price_open,
            source.price_previous,
            source.yes_ask_close,
            source.yes_ask_high,
            source.yes_ask_low,
            source.yes_ask_open,
            source.yes_bid_close,
            source.yes_bid_high,
            source.yes_bid_low,
            source.yes_bid_open
        );
END
GO