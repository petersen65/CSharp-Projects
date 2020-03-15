CREATE PROCEDURE [bundle].[GarbageCollectBundles]
AS
BEGIN
	DECLARE @sequence_number AS bigint;
	DECLARE @bundle_sequence_number AS bigint;
	DECLARE @self AS varchar(16);
	DECLARE @client AS varchar(16);

	SET NOCOUNT ON;

	BEGIN TRY
		SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
		BEGIN TRANSACTION;

		SET @bundle_sequence_number = -1;

		SELECT @self = ClientId 
			FROM [kiss].[Neighborhood]
			WHERE Self = 1;

		SELECT top(1) @bundle_sequence_number = Id 
			FROM [bundle].[Bundle] WITH (TABLOCKX)
			WHERE ClientId = @self;

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
				IF (@client = @self AND @sequence_number = @bundle_sequence_number)
				BEGIN
					UPDATE [bundle].[Bundle]
						SET Sent = NULL, 
							Received = NULL,
							Bundle = NULL,
							Created = CURRENT_TIMESTAMP
						WHERE ClientId = @self AND Id = @bundle_sequence_number;

					SET @sequence_number = @bundle_sequence_number - 1;
				END

				DELETE FROM [bundle].[Bundle]
					WHERE ClientId = @client AND Id <= @sequence_number;
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
