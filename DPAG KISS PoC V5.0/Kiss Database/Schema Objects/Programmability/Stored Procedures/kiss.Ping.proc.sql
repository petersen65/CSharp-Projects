CREATE PROCEDURE [kiss].[Ping]
	@dialog_lifetime AS int
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @from_zora_client AS varchar(256);
	DECLARE @ping AS xml;

	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
		BEGIN TRANSACTION;

		SET @ping = (
			SELECT ClientId, BranchId 
				FROM [kiss].[Neighborhood] AS [Ping]
				WHERE Self = 1
				FOR XML AUTO, TYPE);

		SELECT @from_zora_client = name 
			FROM [sys].[services]
			WHERE name LIKE '%zora-client';

		BEGIN DIALOG CONVERSATION @dialog
			FROM SERVICE @from_zora_client
			TO SERVICE 'http://www.dpag.de/kiss/service/neighborhood-processing' 
			ON CONTRACT [http://www.dpag.de/kiss/contract/neighborhood-processing] 
			WITH ENCRYPTION = OFF, LIFETIME = @dialog_lifetime;

		SEND ON CONVERSATION @dialog
			MESSAGE TYPE [http://www.dpag.de/kiss/data/ping] (@ping);

		END CONVERSATION @dialog;
		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
	END CATCH
END
