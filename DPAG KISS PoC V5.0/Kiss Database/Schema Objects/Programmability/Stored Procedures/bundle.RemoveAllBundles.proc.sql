CREATE PROCEDURE [bundle].[RemoveAllBundles]
AS
BEGIN
	SET NOCOUNT ON;

	TRUNCATE TABLE [bundle].[Bundle];
END
