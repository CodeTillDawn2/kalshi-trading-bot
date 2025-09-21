CREATE OR ALTER PROCEDURE [dbo].[sp_InsertFeed_OrderBook]
    @market_id uniqueidentifier,
    @sid int,
    @kalshi_seq int,
    @market_ticker varchar(50),
    @offer_type varchar(3),
    @price int,
    @delta int = NULL,
    @side varchar(3) = NULL,
    @resting_contracts int,
    @LoggedDate datetime
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Perform the insert directly into the table
    INSERT INTO dbo.t_feed_orderbook (market_id, sid, kalshi_seq, market_ticker, offer_type, price, delta, side, resting_contracts, LoggedDate)
    VALUES 
    (@market_id, @sid, @kalshi_seq, @market_ticker, @offer_type, @price, @delta, @side, @resting_contracts, @LoggedDate);
END
GO
