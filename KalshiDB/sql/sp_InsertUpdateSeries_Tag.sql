CREATE OR ALTER PROCEDURE sp_InsertUpdateSeries_Tag
	@series_ticker varchar(50),
	@tag varchar(50)
AS
BEGIN

	SET NOCOUNT ON;

	Insert into t_Series_Tags (series_ticker, tag) VALUES (@series_ticker, @tag)
END
GO
