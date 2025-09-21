CREATE TABLE t_Orders (
    OrderId NVARCHAR(255) PRIMARY KEY,
    Ticker NVARCHAR(255),
    UserId NVARCHAR(255),
    Action NVARCHAR(50),
    Side NVARCHAR(50),
    Type NVARCHAR(50),
    Status NVARCHAR(50),
    YesPrice BIGINT,
    NoPrice BIGINT,
    CreatedTime DATETIME,
    LastUpdateTime DATETIME,
    ExpirationTime DATETIME,
    ClientOrderId NVARCHAR(255),
    PlaceCount INT,
    DecreaseCount INT,
    AmendCount INT,
    AmendTakerFillCount INT,
    MakerFillCount INT,
    TakerFillCount INT,
    RemainingCount INT,
    QueuePosition INT,
    MakerFillCost BIGINT,
    TakerFillCost BIGINT,
    MakerFees BIGINT,
    TakerFees BIGINT,
    FccCancelCount INT,
    CloseCancelCount INT,
    TakerSelfTradeCancelCount INT,
    LastModified DATETIME DEFAULT GETDATE()
);
GO

CREATE TRIGGER trg_Orders_LastModifiedDate
ON t_Orders
AFTER UPDATE
AS
BEGIN
    UPDATE t_Orders
    SET LastModified = GETDATE()
    FROM t_Orders o
    INNER JOIN inserted i ON o.OrderId = i.OrderId;
END;
GO