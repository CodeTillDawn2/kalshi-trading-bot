USE [kalshibot-prd]
GO

/****** Object:  Table [dbo].[t_SignalRClients]    Script Date: 9/9/2025 3:57:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_SignalRClients](
	[ClientId] [varchar](100) NOT NULL,
	[ClientName] [varchar](100) NOT NULL,
	[IPAddress] [varchar](45) NOT NULL,
	[AuthToken] [varchar](256) NOT NULL,
	[ClientType] [varchar](50) NOT NULL, -- 'overseer', 'dashboard', etc.
	[IsActive] bit NOT NULL DEFAULT 1,
	[LastSeen] [datetime] NULL,
	[RegisteredAt] [datetime] NOT NULL DEFAULT GETUTCDATE(),
	[ConnectionId] [varchar](100) NULL, -- SignalR connection ID
 CONSTRAINT [PK_t_SignalRClients] PRIMARY KEY CLUSTERED
(
	[ClientId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Index for IP address lookups
CREATE NONCLUSTERED INDEX [IX_t_SignalRClients_IPAddress] ON [dbo].[t_SignalRClients]
(
	[IPAddress] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- Index for active clients
CREATE NONCLUSTERED INDEX [IX_t_SignalRClients_IsActive] ON [dbo].[t_SignalRClients]
(
	[IsActive] ASC,
	[LastSeen] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO