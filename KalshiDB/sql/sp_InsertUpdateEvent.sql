CREATE OR ALTER PROCEDURE [dbo].[sp_InsertUpdateEvent]
    @event_ticker            VARCHAR(50),
    @series_ticker           VARCHAR(50),
    @title                   VARCHAR(255),
    @sub_title               VARCHAR(255) = NULL,
    @collateral_return_type  VARCHAR(25),
    @mutually_exclusive      BIT,
    @category                VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    BEGIN TRY
        -- Update the existing event if it exists
        UPDATE t_Events
        SET 
            series_ticker = @series_ticker,
            title = @title,
            sub_title = @sub_title,
            collateral_return_type = @collateral_return_type,
            mutually_exclusive = @mutually_exclusive,
            category = @category,
            LastModifiedDate = GETDATE()
        WHERE event_ticker = @event_ticker;

        -- Insert a new event if it does not exist
        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO t_Events (event_ticker, series_ticker, title, sub_title, collateral_return_type,
                                  mutually_exclusive, category, CreatedDate, LastModifiedDate)
            VALUES (@event_ticker, @series_ticker, @title, @sub_title, @collateral_return_type,
                    @mutually_exclusive, @category, GETDATE(), GETDATE());
        END

        -- Commit the transaction
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        -- Rollback transaction in case of errors
        ROLLBACK TRANSACTION;

        -- Re-throw the error to the caller
        THROW;
    END CATCH
END;
GO
