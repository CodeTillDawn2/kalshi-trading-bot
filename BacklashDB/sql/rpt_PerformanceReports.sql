CREATE TABLE rpt_PerformanceReports (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Timestamp DATETIME NOT NULL
);

CREATE NONCLUSTERED INDEX IX_PerformanceReports_Timestamp 
ON rpt_PerformanceReports (Timestamp);