CREATE TABLE t_StandardHoursSessions
(
    SessionID BIGINT IDENTITY(1,1) NOT NULL, -- Primary key with auto-increment
    StandardHoursID BIGINT NOT NULL, -- Foreign key to t_StandardHours
    DayOfWeek NVARCHAR(20) NOT NULL, -- Day of the week (Monday, Tuesday, etc.)
    StartTime TIME NOT NULL, -- Start time of the trading session
    EndTime TIME NOT NULL, -- End time of the trading session
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was created
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was last modified
    CONSTRAINT PK_t_StandardHoursSessions PRIMARY KEY (SessionID),
    CONSTRAINT FK_t_StandardHoursSessions_StandardHours FOREIGN KEY (StandardHoursID)
        REFERENCES t_StandardHours (StandardHoursID) ON DELETE CASCADE
);