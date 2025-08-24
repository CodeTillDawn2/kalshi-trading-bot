SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Peter Foley
-- Create date: 1/18/25
-- Description:	Sproc to be run by sql agent which will look for missing links and ultimately inform the synchronization app to refresh data
-- =============================================
CREATE OR ALTER PROCEDURE sp_MaintainRelationships
	-- Add the parameters for the stored procedure here
AS
BEGIN
	SET NOCOUNT ON;


	
END
GO
