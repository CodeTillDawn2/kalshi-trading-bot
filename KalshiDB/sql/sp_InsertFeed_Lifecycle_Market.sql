USE [kalshibot-dev]
GO
/****** Object:  StoredProcedure [dbo].[sp_InsertFeed_LifeCycle_Market]    Script Date: 5/7/2025 2:30:01 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_InsertFeed_LifeCycle_Market]
    @market_ticker varchar(50),
    @open_ts bigint = 0,
    @close_ts bigint = 0,
    @determination_ts bigint = NULL,
    @settled_ts bigint = NULL,
    @result varchar(3) = '',
    @is_deactivated bit,
    @LoggedDate datetime
AS
BEGIN
    SET NOCOUNT ON;

    -- Update if record exists, otherwise insert
    IF EXISTS (SELECT 1 FROM dbo.t_feed_lifecycle_market WHERE market_ticker = @market_ticker)
    BEGIN
        UPDATE dbo.t_feed_lifecycle_market
        SET open_ts = @open_ts,
            close_ts = @close_ts,
            determination_ts = @determination_ts,
            settled_ts = @settled_ts,
            result = @result,
            is_deactivated = @is_deactivated,
            LoggedDate = @LoggedDate
        WHERE market_ticker = @market_ticker;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.t_feed_lifecycle_market (market_ticker, open_ts, close_ts, determination_ts, settled_ts, result, is_deactivated, LoggedDate)
        VALUES (@market_ticker, @open_ts, @close_ts, @determination_ts, @settled_ts, @result, @is_deactivated, @LoggedDate);
    END
END