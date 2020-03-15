CREATE PROCEDURE [kiss].[RegisterPC]
	@deregister AS bit,
	@dialog_lifetime AS int
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @message_type AS sysname;
	DECLARE @from_zora_client AS varchar(256);
	DECLARE @pc AS xml;

	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
		BEGIN TRANSACTION;

		SELECT @from_zora_client = name 
			FROM [sys].[services]
			WHERE name LIKE '%zora-client';

		IF (@deregister = 1)
		BEGIN
			SET @pc = (
				SELECT ClientId, BranchId 
					FROM [kiss].[Neighborhood] AS [PcDeregistration]
					WHERE Self = 1
					FOR XML AUTO, TYPE);
					
			SET @message_type = 'http://www.dpag.de/kiss/data/pc-deregistration';
			
			DELETE FROM [kiss].[Neighborhood]
				WHERE Self = 0;
				
			UPDATE [kiss].[Neighborhood]
				SET Dns = NULL,
					Queue = NULL,
					Created = NULL
				WHERE Self = 1;
		END
		ELSE
		BEGIN
			SET @pc = (
				SELECT ClientId, BranchId, Service = @from_zora_client 
					FROM [kiss].[Neighborhood] AS [PcRegistration]
					WHERE Self = 1
					FOR XML AUTO, TYPE);
					
			SET @message_type = 'http://www.dpag.de/kiss/data/pc-registration';
		END

		BEGIN DIALOG CONVERSATION @dialog
			FROM SERVICE @from_zora_client
			TO SERVICE 'http://www.dpag.de/kiss/service/neighborhood-processing' 
			ON CONTRACT [http://www.dpag.de/kiss/contract/neighborhood-processing] 
			WITH ENCRYPTION = OFF, LIFETIME = @dialog_lifetime;

		SEND ON CONVERSATION @dialog
			MESSAGE TYPE @message_type (@pc);

		END CONVERSATION @dialog;
		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
	END CATCH
END
