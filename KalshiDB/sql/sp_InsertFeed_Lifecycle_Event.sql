USE [kalshibot-dev]
GO
/****** Object:  StoredProcedure [dbo].[sp_InsertFeed_Lifecycle_Event]    Script Date: 4/22/2025 3:38:07 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_InsertFeed_Lifecycle_Event]
    @event_ticker varchar(150),
    @title varchar(500),
    @sub_title varchar(500) = NULL,
    @collateral_return_type varchar(50) = NULL,
    @series_ticker varchar(150),
    @strike_date bigint = NULL,
    @strike_period varchar(50) = NULL,
    @LoggedDate datetime
AS
BEGIN
    SET NOCOUNT ON;

    -- Update if record exists, otherwise insert
    IF EXISTS (SELECT 1 FROM [dbo].[t_feed_lifecycle_event] WHERE event_ticker = @event_ticker)
    BEGIN
        UPDATE [dbo].[t_feed_lifecycle_event]
        SET title = @title,
            sub_title = @sub_title,
            collateral_return_type = @collateral_return_type,
            series_ticker = @series_ticker,
            strike_date = @strike_date,
            strike_period = @strike_period,
            LoggedDate = @LoggedDate
        WHERE event_ticker = @event_ticker;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[t_feed_lifecycle_event] (
            event_ticker,
            title,
            sub_title,
            collateral_return_type,
            series_ticker,
            strike_date,
            strike_period,
            LoggedDate
        )
        VALUES (
            @event_ticker,
            @title,
            @sub_title,
            @collateral_return_type,
            @series_ticker,
            @strike_date,
            @strike_period,
            @LoggedDate
        );
    END
END