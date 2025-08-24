CREATE TABLE rpt_TimerStatuses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PerformanceReportId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL,
    CONSTRAINT FK_TimerStatuses_PerformanceReports FOREIGN KEY (PerformanceReportId) 
        REFERENCES rpt_PerformanceReports(Id) ON DELETE CASCADE
);

CREATE NONCLUSTERED INDEX IX_TimerStatuses_Name 
ON rpt_TimerStatuses (Name);

CREATE NONCLUSTERED INDEX IX_TimerStatuses_PerformanceReportId 
ON rpt_TimerStatuses (PerformanceReportId);