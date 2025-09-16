CREATE TABLE t_ExchangeSchedule
(
    ExchangeScheduleID BIGINT IDENTITY(1,1) NOT NULL, -- Primary key with auto-increment
    LastUpdated DATETIME NOT NULL DEFAULT GETDATE(), -- When this schedule was last fetched
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was created
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was last modified
    CONSTRAINT PK_t_ExchangeSchedule PRIMARY KEY (ExchangeScheduleID)
);