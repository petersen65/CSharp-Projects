CREATE PROCEDURE [bundle].[QueryStageList]
	@self AS bit, 
	@dialog_lifetime AS int
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @from_zora_client AS varchar(256);
	DECLARE @client_list AS xml;

	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
		BEGIN TRANSACTION;

		SET @client_list = (
			SELECT ClientId AS 'text()' 
				FROM [kiss].[Neighborhood]
				WHERE Self BETWEEN @self AND 1
				FOR XML PATH('Client'), TYPE, ROOT('ClientList'));

		SELECT @from_zora_client = name 
			FROM [sys].[services]
			WHERE name LIKE '%zora-client';

		BEGIN DIALOG CONVERSATION @dialog
			FROM SERVICE @from_zora_client
			TO SERVICE 'http://www.dpag.de/kiss/service/stage-processing' 
			ON CONTRACT [http://www.dpag.de/kiss/contract/stage-processing] 
			WITH ENCRYPTION = OFF, LIFETIME = @dialog_lifetime;

		SEND ON CONVERSATION @dialog
			MESSAGE TYPE [http://www.dpag.de/kiss/data/client-list] (@client_list);
		
		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
	END CATCH
END
