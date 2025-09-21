CREATE OR ALTER PROCEDURE [dbo].[sp_InsertFeed_Fill]
    @trade_id uniqueidentifier,
    @order_id uniqueidentifier,
    @market_ticker varchar(50),
    @is_taker bit,
	@side varchar(3),
    @yes_price int NULL,
    @no_price int NULL,
    @count int,
    @action varchar(4),
    @ts bigint,
    @LoggedDate datetime
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.t_feed_fill (trade_id, order_id, market_ticker, is_taker, side, yes_price, no_price, [count], [action], ts, LoggedDate)
    VALUES 
    (@trade_id, @order_id, @market_ticker, @is_taker, @side, @yes_price, @no_price, @count, @action, @ts, @LoggedDate);
END
