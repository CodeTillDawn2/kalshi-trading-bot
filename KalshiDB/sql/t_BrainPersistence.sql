USE [kalshibot-prd]
GO

/****** Object:  Table [dbo].[t_BrainPersistence]    Script Date: 9/13/2025 6:27:00 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[t_BrainPersistence](
	[BrainInstanceName] [varchar](25) NOT NULL,
	[PersistenceData] [nvarchar](max) NOT NULL,
	[LastUpdated] [datetime] NOT NULL,
	[Version] [int] NOT NULL,
 CONSTRAINT [PK_t_BrainPersistence] PRIMARY KEY CLUSTERED
(
	[BrainInstanceName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[t_BrainPersistence] ADD  CONSTRAINT [DF_t_BrainPersistence_LastUpdated]  DEFAULT (getutcdate()) FOR [LastUpdated]
GO

ALTER TABLE [dbo].[t_BrainPersistence] ADD  CONSTRAINT [DF_t_BrainPersistence_Version]  DEFAULT ((1)) FOR [Version]
GO