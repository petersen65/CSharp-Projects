CREATE PROCEDURE [dbo].[UpdatePartition]
	@id int,
	@description nvarchar(50),
	@active bit,
	@eventStore nvarchar(50),
	@lastUpdated datetime2,
	@rowsAffected int OUTPUT
AS
	SET NOCOUNT ON;

	DECLARE @updated datetime2 = SYSDATETIME();

	UPDATE [dbo].[Partition]
	SET Description = @description, Active = @active, EventStore = @eventStore, LastUpdated = @updated
	WHERE Id = @id AND LastUpdated = @lastUpdated;

	SET @rowsAffected = @@ROWCOUNT;
	SELECT @updated AS LastUpdated;
