CREATE TABLE [dbo].[t_Orderbook](
	[market_ticker] [varchar](150) NOT NULL,
	[price] [int] NOT NULL,
	[side] [varchar](3) NOT NULL,
	[resting_contracts] [int] NOT NULL,
	[LastModifiedDate] [datetime] NULL,
 CONSTRAINT [PK_t_Orderbook] PRIMARY KEY CLUSTERED 
(
	[market_ticker] ASC,
	[price] ASC,
	[side] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[t_Orderbook] SET (LOCK_ESCALATION = AUTO)