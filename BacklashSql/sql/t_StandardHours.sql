CREATE TABLE t_StandardHours
(
    StandardHoursID BIGINT IDENTITY(1,1) NOT NULL, -- Primary key with auto-increment
    ExchangeScheduleID BIGINT NOT NULL, -- Foreign key to t_ExchangeSchedule
    StartTime DATETIME NOT NULL, -- Start date and time for when this weekly schedule is effective
    EndTime DATETIME NOT NULL, -- End date and time for when this weekly schedule is no longer effective
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was created
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was last modified
    CONSTRAINT PK_t_StandardHours PRIMARY KEY (StandardHoursID),
    CONSTRAINT FK_t_StandardHours_ExchangeSchedule FOREIGN KEY (ExchangeScheduleID)
        REFERENCES t_ExchangeSchedule (ExchangeScheduleID) ON DELETE CASCADE
);