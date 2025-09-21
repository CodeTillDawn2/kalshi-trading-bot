CREATE OR ALTER PROCEDURE sp_InsertUpdateSeries_SettlementSource
	@series_ticker varchar(50),
	@name varchar(50),
	@url varchar(MAX)
AS
BEGIN

	SET NOCOUNT ON;

	Insert into t_Series_SettlementSources (series_ticker, name, url) VALUES (@series_ticker, @name, @url)
END
GO
