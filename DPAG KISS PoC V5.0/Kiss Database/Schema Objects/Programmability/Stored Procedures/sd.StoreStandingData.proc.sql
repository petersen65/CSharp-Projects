CREATE PROCEDURE [sd].[StoreStandingData]
	@sd_id AS bigint,
	@blob_sent AS datetime,
	@blob_received AS datetime,
	@sd AS varbinary(max)
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
		BEGIN TRANSACTION
		
		IF (EXISTS(SELECT Id FROM [sd].[StandingData] WITH (TABLOCKX) WHERE Id = @sd_id))
		BEGIN
			UPDATE [sd].[StandingData]
				SET BlobSent = @blob_sent,
					BlobReceived = @blob_received,
					StandingData = @sd,
					Created = CURRENT_TIMESTAMP
				WHERE Id = @sd_id;
		END
		ELSE
		BEGIN
			INSERT INTO [sd].[StandingData]
				VALUES (@sd_id, NULL, NULL, NULL, @blob_sent, @blob_received, @sd, CURRENT_TIMESTAMP);
		END
		
		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
	END CATCH
END
