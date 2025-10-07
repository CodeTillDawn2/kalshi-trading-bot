CREATE TABLE [dbo].[t_Milestones](
	[id] [varchar](255) NOT NULL,
	[category] [varchar](100) NOT NULL,
	[details] [nvarchar](max) NULL,
	[end_date] [datetime2](7) NULL,
	[last_updated_ts] [datetime2](7) NOT NULL,
	[notification_message] [nvarchar](1000) NOT NULL,
	[primary_event_tickers] [nvarchar](max) NULL,
	[related_event_tickers] [nvarchar](max) NULL,
	[source_id] [varchar](255) NULL,
	[start_date] [datetime2](7) NOT NULL,
	[title] [nvarchar](500) NOT NULL,
	[type] [varchar](100) NOT NULL,
	[CreatedDate] [datetime] NOT NULL CONSTRAINT [DF_t_Milestones_CreatedDate] DEFAULT (GETDATE()),
	[LastModifiedDate] [datetime] NULL,
 CONSTRAINT [PK_t_Milestones] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_Milestones_category] ON [dbo].[t_Milestones]
(
	[category] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_Milestones_type] ON [dbo].[t_Milestones]
(
	[type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_t_Milestones_start_date] ON [dbo].[t_Milestones]
(
	[start_date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
