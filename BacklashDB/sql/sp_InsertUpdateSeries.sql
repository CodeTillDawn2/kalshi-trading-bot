USE [kalshibot-dev]
GO
/****** Object:  StoredProcedure [dbo].[sp_InsertUpdateSeries]    Script Date: 1/24/2025 7:11:13 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [dbo].[sp_InsertUpdateSeries]
    @series_ticker        VARCHAR(50),
    @frequency            VARCHAR(20),
    @title                VARCHAR(255),
    @category             VARCHAR(50),
    @contract_url         VARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;


        -- Upsert the main series record
        IF EXISTS (SELECT 1 FROM t_Series WHERE series_ticker = @series_ticker)
        BEGIN
            UPDATE t_Series
            SET 
                frequency = @frequency,
                title = @title,
                category = @category,
                contract_url = @contract_url
            WHERE 
                series_ticker = @series_ticker;
        END
        ELSE
        BEGIN
            INSERT INTO t_Series (series_ticker, frequency, title, category, contract_url)
            VALUES (@series_ticker, @frequency, @title, @category, @contract_url);
        END

		DELETE FROM t_Series_SettlementSources where series_ticker = @series_ticker
		DELETE FROM t_Series_Tags where series_ticker = @series_ticker
END