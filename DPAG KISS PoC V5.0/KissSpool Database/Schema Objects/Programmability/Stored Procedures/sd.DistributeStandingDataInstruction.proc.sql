CREATE PROCEDURE [sd].[DistributeStandingDataInstruction]
	@branch_id AS varchar(50),
	@id AS bigint,
	@path AS varchar(256),
	@dialog_lifetime AS int
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @message_type AS sysname;
	DECLARE @sent AS datetime;
	DECLARE @sd_instr AS xml;
	DECLARE @service AS varchar(256);
	DECLARE @receive_timeout AS int;
	
	SET NOCOUNT ON;
	
	SET @sent = CURRENT_TIMESTAMP;
	SET @receive_timeout = @dialog_lifetime * 1000 + 1000;

	SET @sd_instr = (
		SELECT ClientId AS Id 
			FROM [kiss].[NeighborhoodMaster] AS [Client]
			WHERE BranchId = @branch_id AND Active = 1
			FOR XML AUTO, TYPE, ROOT('StandingData'));

	IF (@sd_instr IS NOT NULL)
	BEGIN
		SET @sd_instr.modify('insert attribute Id {sql:variable("@id")} into StandingData[1]');
		SET @sd_instr.modify('insert attribute BranchId {sql:variable("@branch_id")} into StandingData[1]');
		SET @sd_instr.modify('insert attribute Path {sql:variable("@path")} into StandingData[1]');
		SET @sd_instr.modify('insert attribute Sent {sql:variable("@sent")} into StandingData[1]');
	END

	DECLARE services CURSOR LOCAL FAST_FORWARD
		FOR SELECT Service
				FROM [kiss].[NeighborhoodMaster]
				WHERE BranchId = @branch_id AND Active = 1;

	OPEN services;
	FETCH NEXT FROM services INTO @service;

	WHILE @@FETCH_STATUS = 0
	BEGIN
		BEGIN TRY
			SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
			BEGIN TRANSACTION;
		
			BEGIN DIALOG CONVERSATION @dialog
				FROM SERVICE [http://www.dpag.de/kiss/service/standing-data-processing]
				TO SERVICE @service 
				ON CONTRACT [http://www.dpag.de/kiss/contract/zoraclient-processing] 
				WITH ENCRYPTION = OFF, LIFETIME = @dialog_lifetime;
			
			SEND ON CONVERSATION @dialog
				MESSAGE TYPE [http://www.dpag.de/kiss/data/standing-data-instr] (@sd_instr);

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			ROLLBACK TRANSACTION;
		END CATCH

		IF (@dialog IS NOT NULL)
		BEGIN		
			WAITFOR (
				RECEIVE top(1)
					@message_type = message_type_name
					FROM [sd].[StandingDataProcessingQueue]
					WHERE conversation_handle = @dialog
				), TIMEOUT @receive_timeout;
		
			IF (@@ROWCOUNT > 0 AND @message_type = 'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
			BEGIN
				END CONVERSATION @dialog;
				BREAK;
			END
			ELSE
				END CONVERSATION @dialog;
		END
		
		FETCH NEXT FROM services INTO @service;
	END

	CLOSE services;
	DEALLOCATE services;
END
