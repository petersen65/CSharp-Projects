CREATE PROCEDURE [bundle].[RemoveAllStages]
AS
BEGIN
	SET NOCOUNT ON;

	TRUNCATE TABLE [bundle].[Stage];
END
