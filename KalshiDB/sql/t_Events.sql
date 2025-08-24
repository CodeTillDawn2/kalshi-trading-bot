SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_Events](
	[event_ticker] [varchar](50) NOT NULL,
	[series_ticker] [varchar](50) NOT NULL,
	[title] [varchar](255) NOT NULL,
	[sub_title] [varchar](255) NULL,
	[collateral_return_type] [varchar](25) NOT NULL,
	[mutually_exclusive] [bit] NOT NULL,
	[category] [varchar](50) NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
	[LastModifiedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_Events] PRIMARY KEY CLUSTERED 
(
	[event_ticker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[t_Events] ADD  CONSTRAINT [DF_Events_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO

CREATE NONCLUSTERED INDEX [IX_t_Events_category] ON [dbo].[t_Events]
(
	[category] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_Events_series_ticker] ON [dbo].[t_Events]
(
	[series_ticker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE TRIGGER [dbo].[trg_Events_LastModifiedDate]
ON [dbo].[t_Events]
AFTER UPDATE
AS
BEGIN
    UPDATE [dbo].[t_Events]
    SET LastModifiedDate = GETDATE()
    FROM [dbo].[t_Events] AS e
    INNER JOIN inserted AS i
    ON e.event_ticker = i.event_ticker
END
