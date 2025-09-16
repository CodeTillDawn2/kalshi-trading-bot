CREATE OR ALTER PROCEDURE [dbo].[sp_InsertFeed_Ticker]
    @market_id uniqueidentifier,
    @market_ticker varchar(50),
    @price int,
    @yes_bid int,
    @yes_ask int,
    @volume int,
    @open_interest int,
    @dollar_volume int,
    @dollar_open_interest int,
    @ts bigint,
    @LoggedDate datetime
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.t_feed_ticker (market_id, market_ticker, price, yes_bid, yes_ask, volume, open_interest, dollar_volume, dollar_open_interest, ts, LoggedDate)
    VALUES 
    (@market_id, @market_ticker, @price, @yes_bid, @yes_ask, @volume, @open_interest, @dollar_volume, @dollar_open_interest, @ts, @LoggedDate);
END
GO
