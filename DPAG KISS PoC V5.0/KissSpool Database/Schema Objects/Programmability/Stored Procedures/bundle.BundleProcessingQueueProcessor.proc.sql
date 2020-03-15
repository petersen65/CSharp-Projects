CREATE PROCEDURE [bundle].[BundleProcessingQueueProcessor]
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @message_type AS sysname;
	DECLARE @message_body AS varbinary(max);
	DECLARE @sequence_number AS bigint;
	DECLARE @client_id AS varchar(16);
	DECLARE @bundle_sent_char AS char(23);
	DECLARE @bundle_header AS char(65);

	SET NOCOUNT ON;

	WHILE (1 = 1)
	BEGIN
		BEGIN TRY
			SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
			BEGIN TRANSACTION;

			WAITFOR (
				RECEIVE top(1)
					@message_type = message_type_name,
					@message_body = message_body,
					@dialog = conversation_handle
					FROM [bundle].[BundleProcessingQueue]
				), TIMEOUT 1000;

			IF (@@ROWCOUNT = 0)
			BEGIN
				ROLLBACK TRANSACTION;
				BREAK;
			END

			IF (@message_type = 'http://www.dpag.de/kiss/data/bundle')
			BEGIN
				SET @bundle_header = @message_body;
				SET @sequence_number = CAST(LEFT(@bundle_header, 26) AS bigint);
				SET @client_id = CAST(SUBSTRING(@bundle_header, 27, 16) AS varchar(16));
				SET @bundle_sent_char = CAST(SUBSTRING(@bundle_header, 43, 23) AS char(23));

				INSERT INTO [bundle].[BundleSpool]
					VALUES (@sequence_number, @client_id, @message_body, @bundle_sent_char, CURRENT_TIMESTAMP);
			END
			ELSE IF (@message_type = 'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
			BEGIN
				END CONVERSATION @dialog;
			END
			ELSE IF (@message_type = 'http://schemas.microsoft.com/SQL/ServiceBroker/Error')
			BEGIN
				INSERT INTO [kiss].[DeadLetter]
					VALUES (@dialog, @message_type, @message_body, 
							NULL, NULL, NULL, 
							NULL, NULL, N'[bundle].[BundleProcessingQueueProcessor]', CURRENT_TIMESTAMP);
				
				END CONVERSATION @dialog;
			END

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			IF (XACT_STATE() = 1)
			BEGIN
				IF (ERROR_NUMBER() <> 2627)
				BEGIN
					INSERT INTO [kiss].[DeadLetter]
						VALUES (@dialog, @message_type, @message_body, 
								ERROR_NUMBER(), ERROR_SEVERITY(), ERROR_STATE(), 
								ERROR_MESSAGE(), ERROR_LINE(), ERROR_PROCEDURE(), CURRENT_TIMESTAMP);
				END

				COMMIT TRANSACTION;
			END
			ELSE
				ROLLBACK TRANSACTION;
		END CATCH
	END
END
