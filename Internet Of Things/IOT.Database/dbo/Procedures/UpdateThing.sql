CREATE PROCEDURE [dbo].[UpdateThing]
	@id uniqueidentifier,
	@description nvarchar(50),
	@active bit,
	@lastUpdated datetime2,
	@rowsAffected int OUTPUT
AS
	SET NOCOUNT ON;

	DECLARE @updated datetime2 = SYSDATETIME();

	UPDATE [dbo].[Thing]
	SET Description = @description, Active = @active, LastUpdated = @updated
	WHERE Id = @id AND LastUpdated = @lastUpdated;

	SET @rowsAffected = @@ROWCOUNT;
	SELECT @updated AS LastUpdated;
