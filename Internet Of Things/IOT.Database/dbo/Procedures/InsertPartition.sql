CREATE PROCEDURE [dbo].[InsertPartition]
	@description nvarchar(50),
	@active bit,
	@area nvarchar(50),
	@maximumCommandTopic int,
	@maximumSubscription int,
	@namespace nvarchar(50),
	@eventStore nvarchar(50),
	@owner nvarchar(50) = NULL,
	@ownerSecret nvarchar(100) = NULL,
	@storageAccount nvarchar(255) = NULL,
	@accessControl nvarchar(50) = NULL,
	@accessControlSecret nvarchar(100)  = NULL, 
	@rowsAffected int OUTPUT
AS
	SET NOCOUNT ON;
	SET XACT_ABORT ON;

	DECLARE @errorMessage nvarchar(4000),  
			@errorSeverity int, 
			@errorState int,
			@tranCount int = @@TRANCOUNT,
			@lastUpdated datetime2 = SYSDATETIME();

	DECLARE @partitionId int, 
			@i int = 1; 
	
	SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

	IF @tranCount > 0
		SAVE TRANSACTION insertPartition;
	ELSE
		BEGIN TRANSACTION;

	BEGIN TRY
		INSERT INTO [dbo].[Partition] (Description, Active, Area, MaximumCommandTopic, 
									   Namespace, EventStore, 
									   Owner, OwnerSecret, StorageAccount, AccessControl, AccessControlSecret, 
									   LastUpdated)
		VALUES (@description, @active, @area, @maximumCommandTopic, 
				@namespace, @eventStore, 
				@owner, @ownerSecret, @storageAccount, @accessControl, @accessControlSecret, 
				@lastUpdated);

		SET @rowsAffected = @@ROWCOUNT;
		SET @partitionId = SCOPE_IDENTITY();

		WHILE @i <= @maximumCommandTopic 
		BEGIN
			INSERT INTO [dbo].[CommandTopic] (RelativeId, MaximumSubscription, CurrentSubscription, PartitionId)
			VALUES (@i, @maximumSubscription, 0, @partitionId);

			SET @i += 1;
		END;

		IF @tranCount = 0
			COMMIT TRANSACTION;

		SELECT @partitionId AS PartitionId, @lastUpdated AS LastUpdated;
	END TRY
	BEGIN CATCH
		IF @tranCount = 0 
			ROLLBACK TRANSACTION;
		ELSE IF XACT_STATE() <> -1
			ROLLBACK TRANSACTION insertPartition;

		SELECT @errorMessage = ERROR_MESSAGE(), 
			   @errorSeverity = ERROR_SEVERITY(),
			   @errorState = ERROR_STATE();

		RAISERROR(@errorMessage, @errorSeverity, @errorState);
	END CATCH;