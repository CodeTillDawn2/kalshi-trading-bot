USE [kalshibot-dev]
GO

/****** Object:  Table [dbo].[t_Snapshots]    Script Date: 5/8/2025 6:32:48 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_Snapshots](
	[MarketTicker] [varchar](150) NOT NULL,
	[SnapshotDate] [datetime2] NOT NULL,
	[JSONSchemaVersion] [int] NOT NULL,
	[PositionSize] [int] NOT NULL,
	[ChangeMetricsMature] [bit] NOT NULL,
	[VelocityPerMinute_Top_Yes_Bid] [float] NULL,
	[VelocityPerMinute_Top_No_Bid] [float] NULL,
	[VelocityPerMinute_Bottom_Yes_Bid] [float] NULL,
	[VelocityPerMinute_Bottom_No_Bid] [float] NULL,
	[OrderVolume_Yes_Bid] [float] NULL,
	[OrderVolume_No_Bid] [float] NULL,
	[TradeVolume_Yes] [float] NULL,
	[TradeVolume_No] [float] NULL,
	[AverageTradeSize_Yes] [float] NULL,
	[AverageTradeSize_No] [float] NULL,
	[MarketType] [int] NULL,
	[IsValidated] [bit] NULL,
	[BrainInstance] [varchar(50)] NULL,
	[RawJSON] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_t_Snapshots] PRIMARY KEY CLUSTERED 
(
	[MarketTicker] ASC,
	[SnapshotDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- Add composite nonclustered index for query efficiency
CREATE NONCLUSTERED INDEX IX_t_Snapshots_SnapshotDate
ON [dbo].[t_Snapshots] (SnapshotDate)
INCLUDE (MarketTicker, JSONSchemaVersion, PositionSize, ChangeMetricsMature);
GO

CREATE NONCLUSTERED INDEX IX_t_Snapshots_IsValidated
ON [dbo].[t_Snapshots] (IsValidated)
INCLUDE (MarketTicker, SnapshotDate);