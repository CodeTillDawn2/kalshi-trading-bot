USE [kalshibot-prd]
GO

/****** Object:  Table [dbo].[t_OverseerInfo]    Script Date: 9/9/2025 6:43:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_OverseerInfo](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[HostName] [varchar](255) NOT NULL,
	[IPAddress] [varchar](45) NOT NULL,
	[Port] [int] NOT NULL,
	[StartTime] [datetime] NOT NULL,
	[LastHeartbeat] [datetime] NULL,
	[IsActive] [bit] NOT NULL,
	[ServiceName] [varchar](100) NULL,
	[Version] [varchar](50) NULL,
 CONSTRAINT [PK_t_OverseerInfo] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO