CREATE OR ALTER   PROCEDURE [dbo].[sp_InsertFeed_Trade]
	@market_ticker varchar(50),
    @yes_price int,
    @no_price int,
    @count int,
    @taker_side varchar(3),
    @ts bigint,
    @LoggedDate datetime
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Perform the insert directly into the table
    INSERT INTO dbo.t_feed_trade (market_ticker, yes_price, no_price, count, taker_side, ts, LoggedDate)
    VALUES 
    (@market_ticker, @yes_price, @no_price, @count, @taker_side, @ts, @LoggedDate);
END