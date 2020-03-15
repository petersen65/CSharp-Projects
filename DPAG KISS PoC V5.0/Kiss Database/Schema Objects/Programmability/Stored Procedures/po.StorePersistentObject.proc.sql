CREATE PROCEDURE [po].[StorePersistentObject]
	@po_id AS varchar(50),
	@po_sent AS datetime,
	@po_received AS datetime,
	@po AS varbinary(max)
AS
BEGIN
	DECLARE @sent AS datetime;

	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
		BEGIN TRANSACTION

		IF (EXISTS(SELECT Id FROM [po].[PersistentObject] WHERE Id = @po_id))
		BEGIN
			SELECT @sent = Sent
				FROM [po].[PersistentObject] 
				WHERE Id = @po_id;
				
			IF (@po_sent > @sent)
			BEGIN
				UPDATE [po].[PersistentObject]
					SET Sent = @po_sent,
						Received = @po_received,
						PersistentObject = @po,
						Modified = CURRENT_TIMESTAMP
					WHERE Id = @po_id;
				
				COMMIT TRANSACTION;
			END
			ELSE
				ROLLBACK TRANSACTION;
		END
		ELSE
		BEGIN
			INSERT INTO [po].[PersistentObject]
				VALUES (@po_id, @po_sent, @po_received, @po, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
		
			COMMIT TRANSACTION;
		END
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
	END CATCH
END
