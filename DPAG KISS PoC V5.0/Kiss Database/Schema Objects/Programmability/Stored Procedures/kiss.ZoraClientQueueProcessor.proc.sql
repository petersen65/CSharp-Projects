CREATE PROCEDURE [kiss].[ZoraClientQueueProcessor]
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @message_type AS sysname;
	DECLARE @message_body AS varbinary(max);
	DECLARE @top_created AS datetime;
	DECLARE @idoc AS int;
	DECLARE @sent AS datetime;
	DECLARE @sd_id AS bigint;
	DECLARE @stage_list AS xml;
	DECLARE @neighborhood AS xml;
	DECLARE @sd_instr AS xml;
	DECLARE @stage AS varchar(26);
	DECLARE @self AS varchar(16);
	DECLARE @client AS varchar(16);
	DECLARE @branch AS varchar(50);
	DECLARE @dns AS varchar(50);
	DECLARE @queue AS varchar(50);

	SET NOCOUNT ON;

	WHILE (1 = 1)
	BEGIN
		BEGIN TRY
			SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
			BEGIN TRANSACTION;

			WAITFOR (
				RECEIVE top(1)
					@message_type = message_type_name,
					@message_body = message_body,
					@dialog = conversation_handle
					FROM [kiss].[ZoraClientQueue]
				), TIMEOUT 1000;

			IF (@@ROWCOUNT = 0)
			BEGIN
				ROLLBACK TRANSACTION;
				BREAK;
			END

			IF (@message_type = 'http://www.dpag.de/kiss/data/stage-list')
			BEGIN
				SET @stage_list = CAST(@message_body AS xml);
				EXECUTE [dbo].[sp_xml_preparedocument] @idoc OUTPUT, @stage_list;

				DECLARE stages CURSOR LOCAL
					FOR SELECT text 
							FROM OPENXML (@idoc, '/StageList/Stage')
							WHERE text IS NOT NULL;

				OPEN stages;

				FETCH NEXT FROM stages INTO @client;
				FETCH NEXT FROM stages INTO @stage;

				WHILE @@FETCH_STATUS = 0
				BEGIN
					IF (EXISTS(SELECT Id FROM [bundle].[Stage] WHERE Id = @stage AND ClientId = @client))
					BEGIN
						UPDATE [bundle].[Stage]
							SET Created = CURRENT_TIMESTAMP
							WHERE Id = @stage AND ClientId = @client;
					END
					ELSE
					BEGIN
						INSERT INTO [bundle].[Stage]
							VALUES (@stage, @client, CURRENT_TIMESTAMP);
					END

					SELECT top(1) @top_created = Created
						FROM [bundle].[Stage]
						WHERE ClientId = @client;

					DELETE FROM [bundle].[Stage]
						WHERE ClientId = @client AND Created < dateadd(hour, -24, @top_created);

					FETCH NEXT FROM stages INTO @client;
					FETCH NEXT FROM stages INTO @stage;
				END

				CLOSE stages;

				DEALLOCATE stages;
				EXECUTE [dbo].[sp_xml_removedocument] @idoc;
			END
			ELSE IF (@message_type = 'http://www.dpag.de/kiss/data/neighborhood')
			BEGIN
				SET @neighborhood = CAST(@message_body AS xml);

				SELECT @self = ClientId 
					FROM [kiss].[Neighborhood]
					WHERE Self = 1;

				DELETE FROM [kiss].[neighborhood];
				EXECUTE [dbo].[sp_xml_preparedocument] @idoc OUTPUT, @neighborhood;

				DECLARE clients CURSOR LOCAL
					FOR SELECT text 
							FROM OPENXML (@idoc, '/Neighborhood/Client')
							WHERE text IS NOT NULL;

				OPEN clients;

				FETCH NEXT FROM clients INTO @client;
				FETCH NEXT FROM clients INTO @branch;
				FETCH NEXT FROM clients INTO @dns;
				FETCH NEXT FROM clients INTO @queue;

				WHILE @@FETCH_STATUS = 0
				BEGIN
					INSERT INTO [kiss].[Neighborhood]
					   VALUES (@client, @branch, @dns, @queue, 0, CURRENT_TIMESTAMP);

					FETCH NEXT FROM clients INTO @client;
					FETCH NEXT FROM clients INTO @branch;
					FETCH NEXT FROM clients INTO @dns;
					FETCH NEXT FROM clients INTO @queue;
				END

				CLOSE clients;

				DEALLOCATE clients;
				EXECUTE [dbo].[sp_xml_removedocument] @idoc;

				UPDATE [kiss].[Neighborhood]
					SET Self = 1,
						Created = CURRENT_TIMESTAMP
					WHERE ClientId = @self;
			END
			ELSE IF (@message_type = 'http://www.dpag.de/kiss/data/standing-data-instr')
			BEGIN
				SET @sd_instr = CAST(@message_body AS xml);
				
				SET @sent = @sd_instr.value('/StandingData[1]/@Sent', 'datetime');
				SET @sd_id = @sd_instr.value('/StandingData[1]/@Id', 'bigint');
				SET @sd_instr.modify('delete StandingData[1]/@Sent');
				
				IF (EXISTS(SELECT Id FROM [sd].[StandingData] WITH (TABLOCKX) WHERE Id = @sd_id))
				BEGIN
					UPDATE [sd].[StandingData]
						SET Sent = @sent, 
							Received = CURRENT_TIMESTAMP,
							Instruction = @sd_instr
						WHERE Id = @sd_id;
				END
				ELSE
				BEGIN
					INSERT INTO [sd].[StandingData]
						VALUES (@sd_id, @sent, CURRENT_TIMESTAMP, @sd_instr, NULL, NULL, NULL, NULL);
				END
				
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
							NULL, NULL, N'[kiss].[ZoraClientQueueProcessor]', CURRENT_TIMESTAMP);
				
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

				COMMIT TRANSACTION;
			END
			ELSE
				ROLLBACK TRANSACTION;
		END CATCH
	END
END
