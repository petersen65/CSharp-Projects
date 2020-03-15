CREATE PROCEDURE [po].[RemoveAllPersistentObjects]
AS
BEGIN
	SET NOCOUNT ON;

	TRUNCATE TABLE [po].[PersistentObject];
END
