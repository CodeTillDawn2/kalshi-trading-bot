CREATE TABLE rpt_SemaphoreStatuses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PerformanceReportId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Count INT NOT NULL,
    CONSTRAINT FK_SemaphoreStatuses_PerformanceReports FOREIGN KEY (PerformanceReportId) 
        REFERENCES rpt_PerformanceReports(Id) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX IX_SemaphoreStatuses_Name 
ON rpt_SemaphoreStatuses (Name);

CREATE NONCLUSTERED INDEX IX_SemaphoreStatuses_PerformanceReportId 
ON rpt_SemaphoreStatuses (PerformanceReportId);