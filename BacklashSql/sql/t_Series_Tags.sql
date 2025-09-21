CREATE TABLE [dbo].[t_Series_Tags](
	[series_ticker] [varchar](50) NOT NULL,
	[tag] [varchar](50) NOT NULL,
 CONSTRAINT [PK_t_Series_Tags] PRIMARY KEY CLUSTERED 
(
	[series_ticker] ASC,
	[tag] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[t_Series_Tags]  WITH CHECK ADD  CONSTRAINT [FK_tagsrequireseries] FOREIGN KEY([series_ticker])
REFERENCES [dbo].[t_Series] ([series_ticker])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[t_Series_Tags] CHECK CONSTRAINT [FK_tagsrequireseries]
GO

CREATE   TRIGGER [dbo].[trg_t_Series_Tags_InsertLastModifiedDate]
ON [dbo].[t_Series_Tags]
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
GO

ALTER TABLE [dbo].[t_Series_Tags] ENABLE TRIGGER [trg_t_Series_Tags_InsertLastModifiedDate]
GO