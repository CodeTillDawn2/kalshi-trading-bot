CREATE TABLE [dbo].[OverseerLogs] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [Timestamp] DATETIME NOT NULL,
    [Level] NVARCHAR(50) NOT NULL,
    [Message] NVARCHAR(4000) NOT NULL,
    [Exception] NVARCHAR(4000) NULL,
    [Environment] NVARCHAR(MAX) NOT NULL,
    [BrainInstance] NVARCHAR(MAX) NOT NULL,
    [SessionIdentifier] NVARCHAR(5) NOT NULL,
    [Source] NVARCHAR(255) NOT NULL,
    CONSTRAINT [PK_OverseerLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);