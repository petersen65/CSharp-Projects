CREATE PROCEDURE [bundle].[TransmitBundles]
	@dialog_lifetime AS int
AS
BEGIN
	DECLARE @dialog AS uniqueidentifier;
	DECLARE @from_zora_client AS varchar(256);
	DECLARE @sequence_number AS bigint;
	DECLARE @bundle_sequence_number AS bigint;
	DECLARE @previous_bundle_sequence_number AS bigint;
	DECLARE @client AS varchar(16);
	DECLARE @bundle AS varbinary(max);

	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
		BEGIN TRANSACTION;

		SELECT @from_zora_client = name 
			FROM [sys].[services]
			WHERE name LIKE '%zora-client';

		DECLARE clients CURSOR LOCAL FAST_FORWARD
			FOR SELECT ClientId 
					FROM [kiss].[Neighborhood];

		OPEN clients;
		FETCH NEXT FROM clients INTO @client;

		WHILE @@FETCH_STATUS = 0
		BEGIN
			SET @sequence_number = -1;

			SELECT top(1) @sequence_number = Id 
				FROM [bundle].[Stage]
				WHERE ClientId = @client;

			IF (@sequence_number != -1)
			BEGIN
				DECLARE bundles CURSOR LOCAL FAST_FORWARD
					FOR SELECT Id, Bundle 
							FROM [bundle].[Bundle]
							WHERE ClientId = @client AND Id > @sequence_number AND Bundle IS NOT NULL
							ORDER BY Id ASC;

				OPEN bundles;
				FETCH NEXT FROM bundles INTO @bundle_sequence_number, @bundle;
				
				IF (@@FETCH_STATUS = 0)
				BEGIN
					BEGIN DIALOG CONVERSATION @dialog
						FROM SERVICE @from_zora_client
						TO SERVICE 'http://www.dpag.de/kiss/service/bundle-processing' 
						ON CONTRACT [http://www.dpag.de/kiss/contract/bundle-processing] 
						WITH ENCRYPTION = OFF, LIFETIME = @dialog_lifetime;

					WHILE @@FETCH_STATUS = 0
					BEGIN
						SEND ON CONVERSATION @dialog
							MESSAGE TYPE [http://www.dpag.de/kiss/data/bundle] (@bundle);

						SET @previous_bundle_sequence_number = @bundle_sequence_number;
						FETCH NEXT FROM bundles INTO @bundle_sequence_number, @bundle;

						IF (@@FETCH_STATUS = 0)
						BEGIN
							IF (@bundle_sequence_number <> @previous_bundle_sequence_number + 1)
								BREAK;
						END
					END

					END CONVERSATION @dialog;
				END

				CLOSE bundles;
				DEALLOCATE bundles;
			END

			FETCH NEXT FROM clients INTO @client;
		END
		
		CLOSE clients;
		DEALLOCATE clients;

		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION;
	END CATCH
END
