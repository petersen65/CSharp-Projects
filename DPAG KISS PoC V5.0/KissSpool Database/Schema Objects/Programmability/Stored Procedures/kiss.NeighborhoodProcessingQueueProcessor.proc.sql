CREATE PROCEDURE [kiss].[NeighborhoodProcessingQueueProcessor]
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @message_type AS sysname;
	DECLARE @message_body AS varbinary(max);
	DECLARE @client_id AS varchar(16);
	DECLARE @branch_id AS varchar(50);
	DECLARE @pc AS xml;
	DECLARE @ping AS xml;
	DECLARE @neighborhood AS xml;
	DECLARE @active AS bit;
	DECLARE @client AS varchar(16);
	DECLARE @branch AS varchar(50);
	DECLARE @dns AS varchar(50);
	DECLARE @queue AS varchar(50);
	DECLARE @service AS varchar(256);
	
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
					FROM [kiss].[NeighborhoodProcessingQueue]
				), TIMEOUT 1000;

			IF (@@ROWCOUNT = 0)
			BEGIN
				ROLLBACK TRANSACTION;
				BREAK;
			END

			IF (@message_type = 'http://www.dpag.de/kiss/data/pc-registration' OR 
				@message_type = 'http://www.dpag.de/kiss/data/pc-deregistration')
			BEGIN
				SET @pc = CAST(@message_body AS xml);
				
				IF (@message_type = 'http://www.dpag.de/kiss/data/pc-registration')
				BEGIN
					SET @active = 1;
					
					SET @client_id =  @pc.value('/PcRegistration[1]/@ClientId', 'varchar(16)');
					SET @branch_id =  @pc.value('/PcRegistration[1]/@BranchId', 'varchar(50)');
					SET @service = @pc.value('/PcRegistration[1]/@Service', 'varchar(256)');
				END
				ELSE
				BEGIN
					SET @active = 0;
					
					SET @client_id =  @pc.value('/PcDeregistration[1]/@ClientId', 'varchar(16)');
					SET @branch_id =  @pc.value('/PcDeregistration[1]/@BranchId', 'varchar(50)');
					SET @service = NULL;
				END
				
				IF (EXISTS(SELECT ClientId FROM [kiss].[NeighborhoodMaster] 
						       WHERE ClientId = @client_id AND BranchId = @branch_id))
				BEGIN
					SET @neighborhood = '<Neighborhood/>';
					
					UPDATE [kiss].[NeighborhoodMaster]
						SET Active = @active,
						    Service = @service,
						    Created = CURRENT_TIMESTAMP
						WHERE ClientId = @client_id AND BranchId = @branch_id;

					DECLARE clients CURSOR LOCAL FAST_FORWARD
						FOR SELECT ClientId, BranchId, Dns, Queue
								FROM [kiss].[NeighborhoodMaster]
								WHERE BranchId = @branch_id AND Active = 1;

					OPEN clients;
					FETCH NEXT FROM clients INTO @client, @branch, @dns, @queue;

					WHILE @@FETCH_STATUS = 0
					BEGIN
						SET @neighborhood.modify('insert <Client clientId="{sql:variable("@client")}" branchId="{sql:variable("@branch")}" dns="{sql:variable("@dns")}" queue="{sql:variable("@queue")}"/> into /Neighborhood[1]');
						FETCH NEXT FROM clients INTO @client, @branch, @dns, @queue;
					END

					CLOSE clients;
					DEALLOCATE clients;

					DECLARE services CURSOR LOCAL FAST_FORWARD
						FOR SELECT Service
								FROM [kiss].[NeighborhoodMaster]
								WHERE BranchId = @branch_id AND Active = 1;

					OPEN services;
					FETCH NEXT FROM services INTO @service;

					WHILE @@FETCH_STATUS = 0
					BEGIN
						BEGIN DIALOG CONVERSATION @dialog
							FROM SERVICE [http://www.dpag.de/kiss/service/neighborhood-processing]
							TO SERVICE @service 
							ON CONTRACT [http://www.dpag.de/kiss/contract/zoraclient-processing] 
							WITH ENCRYPTION = OFF, LIFETIME = 10 * 60;
						
						SEND ON CONVERSATION @dialog
							MESSAGE TYPE [http://www.dpag.de/kiss/data/neighborhood] (@neighborhood);

						END CONVERSATION @dialog;
						FETCH NEXT FROM services INTO @service;
					END

					CLOSE services;
					DEALLOCATE services;
				END
			END
			ELSE IF (@message_type = 'http://www.dpag.de/kiss/data/ping')
			BEGIN
				SET @ping = CAST(@message_body AS xml);
			
				SET @client_id =  @ping.value('/Ping[1]/@ClientId', 'varchar(16)');
				SET @branch_id =  @ping.value('/Ping[1]/@BranchId', 'varchar(50)');

				UPDATE [kiss].[NeighborhoodMaster]
					SET Ping = CURRENT_TIMESTAMP
					WHERE ClientId = @client_id AND BranchId = @branch_id;
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
							NULL, NULL, N'[kiss].[NeighborhoodProcessingQueueProcessor]', CURRENT_TIMESTAMP);
				
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
