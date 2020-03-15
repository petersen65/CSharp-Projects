CREATE PROCEDURE [dbo].[InsertThing]
	@id uniqueidentifier,
	@description nvarchar(50),
	@active bit,
	@area nvarchar(50),
	@rowsAffected int OUTPUT
AS
	SET NOCOUNT ON;
	SET XACT_ABORT ON;

	DECLARE @errorMessage nvarchar(4000),  
			@errorSeverity int, 
			@errorState int, 
			@tranCount int = @@TRANCOUNT;

	DECLARE @partitionId int = NULL,
			@commandTopicId int = NULL, 
			@lastUpdated datetime2 = SYSDATETIME();

	DECLARE @relativeIds table (Id int);

	SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
	
	IF @tranCount > 0
		SAVE TRANSACTION insertThing;
	ELSE
		BEGIN TRANSACTION;
	
	BEGIN TRY
		SELECT @partitionId = Id 
		FROM [dbo].[Partition]
		WHERE Area = @area;

		IF @partitionId IS NOT NULL
			SELECT TOP (1) @commandTopicId = Id
			FROM [dbo].[CommandTopic] WITH (TABLOCKX, XLOCK)
			WHERE PartitionId = @partitionId AND CurrentSubscription < MaximumSubscription
			ORDER BY RelativeId ASC;

		IF @commandTopicId IS NOT NULL
		BEGIN
			UPDATE [dbo].[CommandTopic]
			SET CurrentSubscription += 1
			OUTPUT deleted.RelativeId INTO @relativeIds
			WHERE Id = @commandTopicId;

			INSERT INTO [dbo].[Thing] (Id, Description, Active, PartitionId, CommandTopicId, LastUpdated)
			VALUES (@id, @description, @active, @partitionId, @commandTopicId, @lastUpdated);

			SET @rowsAffected = @@ROWCOUNT;
			
			IF @tranCount = 0
				COMMIT TRANSACTION;

			SELECT Id AS RelativeId, @lastUpdated AS LastUpdated
			FROM @relativeIds;
		END
		ELSE
			RAISERROR(N'Partition does not contain Command Topics with free capacity.', 11, 1);
	END TRY
	BEGIN CATCH
		IF @tranCount = 0 
			ROLLBACK TRANSACTION;
		ELSE IF XACT_STATE() <> -1
			ROLLBACK TRANSACTION insertThing;

		SELECT @errorMessage = ERROR_MESSAGE(), 
			   @errorSeverity = ERROR_SEVERITY(),
			   @errorState = ERROR_STATE();

		RAISERROR(@errorMessage, @errorSeverity, @errorState);
	END CATCH;