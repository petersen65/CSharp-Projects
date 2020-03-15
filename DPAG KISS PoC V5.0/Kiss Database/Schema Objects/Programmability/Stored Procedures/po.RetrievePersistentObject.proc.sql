CREATE PROCEDURE [po].[RetrievePersistentObject]
	@po_id AS varchar(50)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT PersistentObject AS po
		FROM [po].[PersistentObject]
		WHERE Id = @po_id;
END
