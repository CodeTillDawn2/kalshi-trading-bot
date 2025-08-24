CREATE TABLE rpt_BroadcastEventCounts (
    Id INT IDENTITY(1,1) NOT NULL,
    PerformanceReportId INT NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    Count BIGINT NOT NULL,
    CONSTRAINT PK_rpt_BroadcastEventCounts PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_rpt_BroadcastEventCounts_rpt_PerformanceReports FOREIGN KEY (PerformanceReportId)
        REFERENCES rpt_PerformanceReports (Id) ON DELETE CASCADE
);

-- Create index on EventType
CREATE NONCLUSTERED INDEX IX_rpt_BroadcastEventCounts_EventType
ON rpt_BroadcastEventCounts (EventType);