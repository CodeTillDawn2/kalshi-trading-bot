USE [kalshibot-prd]
GO

/****** Object:  Table [dbo].[t_WeightSetMarkets]    Script Date: 7/23/2025 8:07:08 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_WeightSetMarkets](
	[WeightSetID] [int] NOT NULL,
	[MarketTicker] [varchar](100) NOT NULL,
	[PnL] [money] NOT NULL,
	[LastRun] [datetime] NULL,
 CONSTRAINT [PK_WeightSetMarkets] PRIMARY KEY CLUSTERED 
(
	[WeightSetID] ASC,
	[MarketTicker] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


