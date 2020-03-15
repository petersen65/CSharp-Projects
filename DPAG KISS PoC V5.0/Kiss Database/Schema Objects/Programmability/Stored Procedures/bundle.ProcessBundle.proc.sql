CREATE PROCEDURE [bundle].[ProcessBundle]
	@client_id AS varchar(16), 
	@bundle_sent AS datetime,
	@bundle_sent_char AS char(23),
	@bundle AS varbinary(max),
	@dialog_lifetime AS int
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @from_zora_client AS varchar(256);
	DECLARE @sequence_number AS bigint;
	DECLARE @bundle_header AS char(65);
   
	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
		BEGIN TRANSACTION;

		SET @sequence_number = 0;

		SELECT TOP(1) @sequence_number = Id + 1
			FROM [bundle].[Bundle] WITH (TABLOCKX)
			WHERE ClientId = @client_id;

		SET @bundle_header = CAST(@sequence_number AS char(26)) + CAST(@client_id AS char(16)) + @bundle_sent_char;
		SET @bundle.WRITE(CAST(@bundle_header AS varbinary(max)), 0, 65);

		INSERT INTO [bundle].[Bundle]
			VALUES (@sequence_number, @client_id, @bundle_sent, NULL, @bundle, CURRENT_TIMESTAMP);

		SELECT @from_zora_client = name 
			FROM [sys].[services]
			WHERE name LIKE '%zora-client';

		BEGIN DIALOG CONVERSATION @dialog
			FROM SERVICE @from_zora_client
			TO SERVICE 'http://www.dpag.de/kiss/service/bundle-processing' 
			ON CONTRACT [http://www.dpag.de/kiss/contract/bundle-processing] 
			WITH ENCRYPTION = OFF, LIFETIME = @dialog_lifetime;

		SEND ON CONVERSATION @dialog
			MESSAGE TYPE [http://www.dpag.de/kiss/data/bundle] (@bundle);

		END CONVERSATION @dialog;
		
		COMMIT TRANSACTION;
		SELECT @sequence_number;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
	END CATCH
END
