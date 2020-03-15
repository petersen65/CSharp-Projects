CREATE PROCEDURE [bundle].[StageProcessingQueueProcessor]
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @message_type AS sysname;
	DECLARE @message_body AS varbinary(max);
	DECLARE @idoc AS int;
	DECLARE @stage AS varchar(26);
	DECLARE @client AS varchar(16);
	DECLARE @client_list AS xml;
	DECLARE @stage_list AS xml;

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
					FROM [bundle].[StageProcessingQueue]
				), TIMEOUT 1000;

			IF (@@ROWCOUNT = 0)
			BEGIN
				ROLLBACK TRANSACTION;
				BREAK;
			END

			IF (@message_type = 'http://www.dpag.de/kiss/data/client-list')
			BEGIN
				SET @client_list = CAST(@message_body AS xml);
				EXECUTE [dbo].[sp_xml_preparedocument] @idoc OUTPUT, @client_list;
				SET @stage_list = '<StageList/>';

				DECLARE clients CURSOR LOCAL
					FOR 
					SELECT text 
						FROM OPENXML (@idoc, '/ClientList/Client')
						WHERE text IS NOT NULL;

				OPEN clients;
				FETCH NEXT FROM clients INTO @client;

				WHILE @@FETCH_STATUS = 0
				BEGIN
					SET @stage = '-1';

					SELECT TOP(1) @stage = Id 
						FROM [bundle].[BundleSpool]
						WHERE ClientId = @client;

					SET @stage_list.modify('insert <Stage client="{sql:variable("@client")}">{sql:variable("@stage")}</Stage> into /StageList[1]');
					FETCH NEXT FROM clients INTO @client;
				END

				CLOSE clients;

				DEALLOCATE clients;
				EXECUTE [dbo].[sp_xml_removedocument] @idoc;

				SEND ON CONVERSATION @dialog
					MESSAGE TYPE [http://www.dpag.de/kiss/data/stage-list] (@stage_list);
				
				END CONVERSATION @dialog;
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
							NULL, NULL, N'[bundle].[StageProcessingQueueProcessor]', CURRENT_TIMESTAMP);
				
				END CONVERSATION @dialog;
			END

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			IF (XACT_STATE() = 1)
			BEGIN
				INSERT INTO [kiss].[DeadLetter]
					VALUES (@dialog, @message_type, @message_body, 
							ERROR_NUMBER(), ERROR_SEVERITY(), ERROR_STATE(), 
							ERROR_MESSAGE(), ERROR_LINE(), ERROR_PROCEDURE(), CURRENT_TIMESTAMP);
				
				IF (@dialog IS NOT NULL AND @message_type = 'http://www.dpag.de/kiss/data/client-list')
				BEGIN
					SET @stage_list = '<StageList/>';
					
					SEND ON CONVERSATION @dialog
						MESSAGE TYPE [http://www.dpag.de/kiss/data/stage-list] (@stage_list);
					
					END CONVERSATION @dialog;
				END
				
				COMMIT TRANSACTION;
			END
			ELSE
				ROLLBACK TRANSACTION;
		END CATCH
	END
END
