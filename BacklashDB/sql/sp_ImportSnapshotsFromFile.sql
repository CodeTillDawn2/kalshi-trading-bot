USE [kalshibot-prd]
GO
/****** Object: StoredProcedure [dbo].[sp_ImportSnapshotsFromFile] Script Date: 8/3/2025 8:30:00 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_ImportSnapshotsFromFile]
AS
BEGIN
    SET NOCOUNT ON;
    -- Temporary table to store file list
    CREATE TABLE #FileList (
        FileName NVARCHAR(255),
        Depth INT,
        IsFile BIT
    );
    -- Temporary table for staging JSON data
    CREATE TABLE #TempSnapshots (
        MarketTicker [varchar](150) NULL,
        SnapshotDate [datetime2](7) NULL,
        JSONSchemaVersion [int] NULL,
        PositionSize [int] NULL,
        ChangeMetricsMature [bit] NULL,
        VelocityPerMinute_Top_Yes_Bid [float] NULL,
        VelocityPerMinute_Top_No_Bid [float] NULL,
        VelocityPerMinute_Bottom_Yes_Bid [float] NULL,
        VelocityPerMinute_Bottom_No_Bid [float] NULL,
        OrderVolume_Yes_Bid [float] NULL,
        OrderVolume_No_Bid [float] NULL,
        TradeVolume_Yes [float] NULL,
        TradeVolume_No [float] NULL,
        AverageTradeSize_Yes [float] NULL,
        AverageTradeSize_No [float] NULL,
        RawJSON [nvarchar](max) NULL,
        MarketTypeID [int] NULL,
        IsValidated [bit] NULL,
        BrainInstance [varchar](50) NULL
    );
    -- Variables for dynamic SQL and JSON data
    DECLARE @JsonData NVARCHAR(MAX);
    DECLARE @Sql NVARCHAR(MAX);
    DECLARE @FullFilePath NVARCHAR(255);
    DECLARE @DirectoryPath NVARCHAR(255) = N'\\DESKTOP-ITC50UT\SmokehouseDataStorage\NewSnapshots'; -- Hardcoded path
    DECLARE @DeleteCmd NVARCHAR(512);
    -- Enable xp_cmdshell if not already enabled (run once by an admin if needed)
    -- EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
    -- EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;
    BEGIN TRY
        -- Get list of files in the directory using xp_dirtree
        INSERT INTO #FileList (FileName, Depth, IsFile)
        EXEC xp_dirtree @DirectoryPath, 1, 1; -- Depth 1, include files only
        -- Cursor to loop through JSON files
        DECLARE file_cursor CURSOR FOR
            SELECT @DirectoryPath + N'\' + FileName
            FROM #FileList
            WHERE IsFile = 1 AND FileName LIKE '%.json';
        OPEN file_cursor;
        FETCH NEXT FROM file_cursor INTO @FullFilePath;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            PRINT 'Processing file: ' + @FullFilePath;
            -- Construct dynamic SQL to load JSON from the file
            SET @Sql = N'
                SELECT @JsonData = BulkColumn
                FROM OPENROWSET(BULK ''' + @FullFilePath + ''', SINGLE_NCLOB) AS j;
            ';
            -- Execute the dynamic SQL to load the JSON
            EXEC sp_executesql @Sql, N'@JsonData NVARCHAR(MAX) OUTPUT', @JsonData OUTPUT;
            -- Process the data if loaded
            IF @JsonData IS NOT NULL
            BEGIN
                -- Clear the temporary table
                TRUNCATE TABLE #TempSnapshots;
                -- Stage JSON data into temporary table, assuming JSON is an array of snapshot objects
                INSERT INTO #TempSnapshots (
                    MarketTicker, SnapshotDate, JSONSchemaVersion, PositionSize, ChangeMetricsMature,
                    VelocityPerMinute_Top_Yes_Bid, VelocityPerMinute_Top_No_Bid, VelocityPerMinute_Bottom_Yes_Bid,
                    VelocityPerMinute_Bottom_No_Bid, OrderVolume_Yes_Bid, OrderVolume_No_Bid, TradeVolume_Yes,
                    TradeVolume_No, AverageTradeSize_Yes, AverageTradeSize_No, RawJSON, MarketTypeID,
                    IsValidated, BrainInstance
                )
                SELECT
                    MarketTicker,
                    SnapshotDate,
                    JSONSchemaVersion,
                    PositionSize,
                    ChangeMetricsMature,
                    VelocityPerMinute_Top_Yes_Bid,
                    VelocityPerMinute_Top_No_Bid,
                    VelocityPerMinute_Bottom_Yes_Bid,
                    VelocityPerMinute_Bottom_No_Bid,
                    OrderVolume_Yes_Bid,
                    OrderVolume_No_Bid,
                    TradeVolume_Yes,
                    TradeVolume_No,
                    AverageTradeSize_Yes,
                    AverageTradeSize_No,
                    RawJSON,
                    MarketTypeID,
                    IsValidated,
                    BrainInstance
                FROM OPENJSON(@JsonData)
                WITH (
                    MarketTicker [varchar](150) '$.MarketTicker',
                    SnapshotDate [datetime2](7) '$.SnapshotDate',
                    JSONSchemaVersion [int] '$.JSONSchemaVersion',
                    PositionSize [int] '$.PositionSize',
                    ChangeMetricsMature [bit] '$.ChangeMetricsMature',
                    VelocityPerMinute_Top_Yes_Bid [float] '$.VelocityPerMinute_Top_Yes_Bid',
                    VelocityPerMinute_Top_No_Bid [float] '$.VelocityPerMinute_Top_No_Bid',
                    VelocityPerMinute_Bottom_Yes_Bid [float] '$.VelocityPerMinute_Bottom_Yes_Bid',
                    VelocityPerMinute_Bottom_No_Bid [float] '$.VelocityPerMinute_Bottom_No_Bid',
                    OrderVolume_Yes_Bid [float] '$.OrderVolume_Yes_Bid',
                    OrderVolume_No_Bid [float] '$.OrderVolume_No_Bid',
                    TradeVolume_Yes [float] '$.TradeVolume_Yes',
                    TradeVolume_No [float] '$.TradeVolume_No',
                    AverageTradeSize_Yes [float] '$.AverageTradeSize_Yes',
                    AverageTradeSize_No [float] '$.AverageTradeSize_No',
                    RawJSON [nvarchar](max) '$.RawJSON',
                    MarketTypeID [int] '$.MarketTypeID',
                    IsValidated [bit] '$.IsValidated',
                    BrainInstance [varchar](50) '$.BrainInstance'
                );
                -- Check for invalid rows
                IF EXISTS (
                    SELECT 1
                    FROM #TempSnapshots
                    WHERE MarketTicker IS NULL
                       OR SnapshotDate IS NULL
                       OR JSONSchemaVersion IS NULL
                       OR PositionSize IS NULL
                       OR ChangeMetricsMature IS NULL
                       OR RawJSON IS NULL
                )
                BEGIN
                    RAISERROR ('Invalid data: NULL found in required field for file %s', 16, 1, @FullFilePath);
                END
                -- Insert valid rows with TRY-CATCH
                BEGIN TRY
                    INSERT INTO [dbo].[t_Snapshots] (
                        MarketTicker, SnapshotDate, JSONSchemaVersion, PositionSize, ChangeMetricsMature,
                        VelocityPerMinute_Top_Yes_Bid, VelocityPerMinute_Top_No_Bid, VelocityPerMinute_Bottom_Yes_Bid,
                        VelocityPerMinute_Bottom_No_Bid, OrderVolume_Yes_Bid, OrderVolume_No_Bid, TradeVolume_Yes,
                        TradeVolume_No, AverageTradeSize_Yes, AverageTradeSize_No, RawJSON, MarketTypeID,
                        IsValidated, BrainInstance
                    )
                    SELECT
                        MarketTicker,
                        SnapshotDate,
                        JSONSchemaVersion,
                        PositionSize,
                        ChangeMetricsMature,
                        VelocityPerMinute_Top_Yes_Bid,
                        VelocityPerMinute_Top_No_Bid,
                        VelocityPerMinute_Bottom_Yes_Bid,
                        VelocityPerMinute_Bottom_No_Bid,
                        OrderVolume_Yes_Bid,
                        OrderVolume_No_Bid,
                        TradeVolume_Yes,
                        TradeVolume_No,
                        AverageTradeSize_Yes,
                        AverageTradeSize_No,
                        RawJSON,
                        MarketTypeID,
                        IsValidated,
                        BrainInstance
                    FROM #TempSnapshots;
                    PRINT 'Imported: ' + @FullFilePath;
                    -- Delete the file only if all rows were inserted successfully
                    SET @DeleteCmd = N'DEL "' + @FullFilePath + '"';
                    EXEC xp_cmdshell @DeleteCmd;
                    PRINT 'Deleted: ' + @FullFilePath;
                END TRY
                BEGIN CATCH
                    PRINT 'Error inserting data from: ' + @FullFilePath + '. Error: ' + ERROR_MESSAGE();
                    THROW;
                END CATCH;
            END
            ELSE
            BEGIN
                PRINT 'No data loaded from: ' + @FullFilePath;
            END
            FETCH NEXT FROM file_cursor INTO @FullFilePath;
        END;
        CLOSE file_cursor;
        DEALLOCATE file_cursor;
    END TRY
    BEGIN CATCH
        SELECT
            ERROR_NUMBER() AS ErrorNumber,
            ERROR_SEVERITY() AS ErrorSeverity,
            ERROR_STATE() AS ErrorState,
            ERROR_PROCEDURE() AS ErrorProcedure,
            ERROR_LINE() AS ErrorLine,
            ERROR_MESSAGE() AS ErrorMessage;
        IF CURSOR_STATUS('global', 'file_cursor') >= 0
        BEGIN
            CLOSE file_cursor;
            DEALLOCATE file_cursor;
        END;
        THROW;
    END CATCH;
    DROP TABLE #TempSnapshots;
    DROP TABLE #FileList;
END;