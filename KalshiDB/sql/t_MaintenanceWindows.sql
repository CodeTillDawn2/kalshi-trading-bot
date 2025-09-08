CREATE TABLE t_MaintenanceWindows
(
    MaintenanceWindowID BIGINT IDENTITY(1,1) NOT NULL, -- Primary key with auto-increment
    ExchangeScheduleID BIGINT NOT NULL, -- Foreign key to t_ExchangeSchedule
    StartDateTime DATETIME NOT NULL, -- Start date and time of the maintenance window
    EndDateTime DATETIME NOT NULL, -- End date and time of the maintenance window
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was created
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was last modified
    CONSTRAINT PK_t_MaintenanceWindows PRIMARY KEY (MaintenanceWindowID),
    CONSTRAINT FK_t_MaintenanceWindows_ExchangeSchedule FOREIGN KEY (ExchangeScheduleID)
        REFERENCES t_ExchangeSchedule (ExchangeScheduleID) ON DELETE CASCADE
);