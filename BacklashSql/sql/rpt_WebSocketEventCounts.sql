CREATE TABLE rpt_WebSocketEventCounts (
    Id INT IDENTITY(1,1) NOT NULL,
    PerformanceReportId INT NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    Count BIGINT NOT NULL,
    CONSTRAINT PK_rpt_WebSocketEventCounts PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_rpt_WebSocketEventCounts_rpt_PerformanceReports FOREIGN KEY (PerformanceReportId)
        REFERENCES rpt_PerformanceReports (Id) ON DELETE CASCADE
);

-- Create index on EventType
CREATE NONCLUSTERED INDEX IX_rpt_WebSocketEventCounts_EventType
ON rpt_WebSocketEventCounts (EventType);