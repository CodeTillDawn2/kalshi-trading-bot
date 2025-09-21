USE [kalshibot-prd]
GO

/****** Object:  Table [dbo].[t_BrainInstances]    Script Date: 5/17/2025 3:00:02 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_BrainInstances](
	[BrainInstanceName] [varchar](25) NOT NULL,
	[BrainLock] [uniqueidentifier] NULL,
	[WatchPositions] bit NOT NULL,
	[WatchOrders] bit NOT NULL,
	[ManagedWatchList] bit NOT NULL,
	[CaptureSnapshots] bit NOT NULL,
	[TargetWatches] int NOT NULL,
	[UsageMin] [float] NOT NULL,
	[UsageMax] [float] NOT NULL,
	[MinimumInterest] [float] NOT NULL,
	[LastSeen] [datetime] NULL,
 CONSTRAINT [PK_t_BrainInstances] PRIMARY KEY CLUSTERED 
(
	[BrainInstanceName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


