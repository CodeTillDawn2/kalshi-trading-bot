CREATE TABLE t_StratData (
    StratID INT IDENTITY(1,1) PRIMARY KEY,
    StratName VARCHAR(50) NOT NULL,
    StratType INT NOT NULL,
    RawJSON NVARCHAR(MAX) NOT NULL
);

-- Create a unique index on StratName
CREATE UNIQUE INDEX IX_t_StratData_StratName ON t_StratData (StratName);
