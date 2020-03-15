CREATE PROCEDURE [dbo].[DeleteThing]
	@id uniqueidentifier, 
	@lastUpdated datetime2,
	@rowsAffected int OUTPUT
AS
	SET NOCOUNT ON;

	DELETE FROM [dbo].[Thing]
	WHERE Id = @id AND LastUpdated = @lastUpdated;

	SET @rowsAffected = @@ROWCOUNT;