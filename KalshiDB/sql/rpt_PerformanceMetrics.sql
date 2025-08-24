CREATE TABLE rpt_PerformanceMetrics (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PerformanceReportId INT NOT NULL,
    ServiceName NVARCHAR(100) NOT NULL,
    LastExecutionTimeTicks BIGINT NOT NULL,
    MarketCount INT NOT NULL,
    UsagePercentage FLOAT NOT NULL,
    Timestamp DATETIME NOT NULL,
    IsUsageAcceptable BIT NOT NULL,
    CONSTRAINT FK_PerformanceMetrics_PerformanceReports FOREIGN KEY (PerformanceReportId) 
        REFERENCES rpt_PerformanceReports(Id) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX IX_PerformanceMetrics_ServiceName 
ON rpt_PerformanceMetrics (ServiceName);

CREATE NONCLUSTERED INDEX IX_PerformanceMetrics_PerformanceReportId 
ON rpt_PerformanceMetrics (PerformanceReportId);