CREATE TABLE t_Announcements
(
    AnnouncementID BIGINT IDENTITY(1,1) NOT NULL, -- Primary key with auto-increment
    DeliveryTime DATETIME NOT NULL, -- Stores the delivery time of the announcement
    Message NVARCHAR(4000) NOT NULL, -- Stores the announcement message, allowing up to 4000 characters
    Status NVARCHAR(50) NOT NULL, -- Stores the status of the announcement
    Type NVARCHAR(50) NOT NULL, -- Stores the type of the announcement
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was created
    LastModifiedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Tracks when the record was last modified
    CONSTRAINT PK_t_Announcements PRIMARY KEY (AnnouncementID)
);