
CREATE TABLE [dbo].[t_SnapshotGroups](
	SnapshotGroupID int IDENTITY(1,1),
	[MarketTicker] [nvarchar](50) NOT NULL,
	[StartTime] [datetime2](7) NOT NULL,
	[EndTime] [datetime2](7) NOT NULL,
	[YesStart] [int] NOT NULL,
	[NoStart] [int] NOT NULL,
	[YesEnd] [int] NOT NULL,
	[NoEnd] [int] NOT NULL,
	[AverageLiquidity] [float] NOT NULL,
	[SnapshotSchema] int NOT NULL,
	[JsonPath] varchar(MAX) NOT NULL,
	[ProcessedDttm] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_t_SnapshotGroups] PRIMARY KEY CLUSTERED 
(
	[MarketTicker] ASC,
	[StartTime] ASC,
	[EndTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


