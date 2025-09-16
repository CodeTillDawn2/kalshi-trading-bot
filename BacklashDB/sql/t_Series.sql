SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_Series](
	[series_ticker] [varchar](50) NOT NULL,
	[frequency] [varchar](20) NOT NULL,
	[title] [varchar](255) NOT NULL,
	[category] [varchar](50) NOT NULL,
	[contract_url] [varchar](max) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[LastModifiedDate] [datetime] NULL
 CONSTRAINT [PK_t_Series] PRIMARY KEY CLUSTERED 
(
	[series_ticker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[t_Series] ADD  CONSTRAINT [DF_t_Series_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO

CREATE NONCLUSTERED INDEX [IX_t_Series_category] ON [dbo].[t_Series]
(
	[category] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_Series_title] ON [dbo].[t_Series]
(
	[title] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

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

CREATE TABLE [dbo].[t_Series_SettlementSources](
	[series_ticker] [varchar](50) NOT NULL,
	[name] [varchar](50) NOT NULL,
	[url] [varchar](max) NOT NULL,
 CONSTRAINT [PK_t_Series_SettlementSources] PRIMARY KEY CLUSTERED 
(
	[series_ticker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[t_Series_SettlementSources]  WITH CHECK ADD  CONSTRAINT [FK_settlementsourcesrequireseries] FOREIGN KEY([series_ticker])
REFERENCES [dbo].[t_Series] ([series_ticker])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[t_Series_SettlementSources] CHECK CONSTRAINT [FK_settlementsourcesrequireseries]
GO




CREATE TRIGGER [dbo].[trg_Series_LastModifiedDate]
ON [dbo].[t_Series]
AFTER UPDATE
AS
BEGIN
    UPDATE [dbo].[t_Series]
    SET LastModifiedDate = GETDATE()
    FROM [dbo].[t_Series] AS e
    INNER JOIN inserted AS i
    ON e.series_ticker = i.series_ticker
END


CREATE OR ALTER TRIGGER [dbo].[trg_t_Series_Tags_InsertLastModifiedDate]
ON [dbo].[t_Series_Tags]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE t_Series
    SET LastModifiedDate = GETDATE()
    WHERE EXISTS (
        SELECT 1
        FROM Inserted i
        WHERE i.series_ticker = t_Series.series_ticker
    );
END;



CREATE OR ALTER TRIGGER [dbo].[trg_t_Series_SettlementSources_InsertLastModifiedDate]
ON [dbo].[t_Series_SettlementSources]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE t_Series
    SET LastModifiedDate = GETDATE()
    WHERE EXISTS (
        SELECT 1
        FROM Inserted i
        WHERE i.series_ticker = t_Series.series_ticker
    );
END;
