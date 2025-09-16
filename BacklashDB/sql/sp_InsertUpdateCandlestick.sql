CREATE OR ALTER PROCEDURE sp_InsertUpdateCandlestick
    @market_ticker NVARCHAR(50),
    @interval_type INT,
    @open_period_ts BIGINT,
    @end_period_ts bigint,
    @open_interest INT,
    @price_close INT,
    @price_high INT,
    @price_low INT,
    @price_mean INT,
    @price_open INT,
    @price_previous INT,
    @volume INT,
    @yes_ask_close INT,
    @yes_ask_high INT,
    @yes_ask_low INT,
    @yes_ask_open INT,
    @yes_bid_close INT,
    @yes_bid_high INT,
    @yes_bid_low INT,
    @yes_bid_open INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if the record exists
    IF EXISTS (
        SELECT 1
        FROM t_Candlesticks
        WHERE market_ticker = @market_ticker
          AND interval_type = @interval_type
          AND open_period_ts = @open_period_ts
    )
    BEGIN
        -- Update the existing record
        UPDATE t_Candlesticks
        SET 
            end_period_ts = @end_period_ts,
            open_interest = @open_interest,
            price_close = @price_close,
            price_high = @price_high,
            price_low = @price_low,
            price_mean = @price_mean,
            price_open = @price_open,
            price_previous = @price_previous,
            volume = @volume,
            yes_ask_close = @yes_ask_close,
            yes_ask_high = @yes_ask_high,
            yes_ask_low = @yes_ask_low,
            yes_ask_open = @yes_ask_open,
            yes_bid_close = @yes_bid_close,
            yes_bid_high = @yes_bid_high,
            yes_bid_low = @yes_bid_low,
            yes_bid_open = @yes_bid_open
        WHERE 
            market_ticker = @market_ticker
            AND interval_type = @interval_type
            AND open_period_ts = @open_period_ts;
    END
    ELSE
    BEGIN
        -- Insert a new record
        INSERT INTO t_Candlesticks (
            market_ticker,
            interval_type,
            open_period_ts,
            end_period_ts,
            open_interest,
            price_close,
            price_high,
            price_low,
            price_mean,
            price_open,
            price_previous,
            volume,
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
            @market_ticker,
            @interval_type,
            @open_period_ts,
            @end_period_ts,
            @open_interest,
            @price_close,
            @price_high,
            @price_low,
            @price_mean,
            @price_open,
            @price_previous,
            @volume,
            @yes_ask_close,
            @yes_ask_high,
            @yes_ask_low,
            @yes_ask_open,
            @yes_bid_close,
            @yes_bid_high,
            @yes_bid_low,
            @yes_bid_open
        );
    END
END;
GO
