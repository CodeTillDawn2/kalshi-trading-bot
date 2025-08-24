USE [kalshibot-prd]
GO

/****** Object:  Table [dbo].[t_WeightSets]    Script Date: 7/22/2025 4:10:24 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_WeightSets](
	[WeightSetID] [int] IDENTITY(1,1) NOT NULL,
	[StrategyName] [varchar](100) NOT NULL,
	[Weights] [varchar](max) NOT NULL,
	[LastRun] [datetime] NULL,
 CONSTRAINT [PK_WeightSets] PRIMARY KEY CLUSTERED 
(
	[WeightSetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


