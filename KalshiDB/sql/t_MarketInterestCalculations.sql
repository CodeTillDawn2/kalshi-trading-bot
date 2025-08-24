USE [kalshibot-dev]
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[t_MarketInterestCalculations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[t_MarketInterestCalculations] (
        Metric NVARCHAR(50) NOT NULL,
        P90 FLOAT,
        P95 FLOAT,
        P99 FLOAT,
        MaxValue FLOAT,
        LastUpdated DATETIME NOT NULL,
        PRIMARY KEY (Metric)
    );
END;
GO