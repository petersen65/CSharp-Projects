CREATE PROCEDURE [po].[RemovePersistentObject]
	@po_id AS varchar(50)
AS
BEGIN
	SET NOCOUNT ON;

	DELETE FROM [po].[PersistentObject]
		WHERE Id = @po_id;
END
