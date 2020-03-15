CREATE PROCEDURE [dbo].[DeletePartition]
	@id int,
	@lastUpdated datetime2,
	@rowsAffected int OUTPUT
AS
	SET NOCOUNT ON;

	DELETE FROM [dbo].[Partition]
	WHERE Id = @id AND LastUpdated = @lastUpdated;

	SET @rowsAffected = @@ROWCOUNT;