CREATE TABLE [dbo].[t_Series_SettlementSources](
	[series_ticker] [varchar](50) NOT NULL,
	[name] [varchar](255) NOT NULL,
	[url] [varchar](max) NOT NULL,
 CONSTRAINT [PK_t_Series_SettlementSources] PRIMARY KEY CLUSTERED 
(
	[series_ticker] ASC,
	name ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[t_Series_SettlementSources]  WITH CHECK ADD  CONSTRAINT [FK_settlementsourcesrequireseries] FOREIGN KEY([series_ticker])
REFERENCES [dbo].[t_Series] ([series_ticker])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[t_Series_SettlementSources] CHECK CONSTRAINT [FK_settlementsourcesrequireseries]
GO

CREATE   TRIGGER [dbo].[trg_t_Series_SettlementSources_InsertLastModifiedDate]
ON [dbo].[t_Series_SettlementSources]
AFTER INSERT
AS
BEGIN
    -- Update the LastModifiedDate column for the newly inserted rows
    SET NOCOUNT ON;
    
    UPDATE t_Series
    SET LastModifiedDate = GETDATE()
    WHERE EXISTS (
        SELECT 1
        FROM Inserted i
        WHERE i.series_ticker = t_Series.series_ticker
    );
END;