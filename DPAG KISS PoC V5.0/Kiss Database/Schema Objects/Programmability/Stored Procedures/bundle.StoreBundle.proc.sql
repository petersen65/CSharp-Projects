CREATE PROCEDURE [bundle].[StoreBundle]
	@bundle_id AS bigint,
	@client_id AS varchar(16),
	@bundle_sent AS datetime = NULL,
	@bundle_received AS datetime = NULL,
	@bundle_sent_char AS char(23) = NULL,
	@bundle AS varbinary(max) = NULL
AS
BEGIN
	DECLARE @bundle_header AS char(65);

	SET NOCOUNT ON;

	IF (NOT EXISTS(SELECT Id FROM [bundle].[Bundle] WHERE Id = @bundle_id AND ClientId = @client_id))
	BEGIN
		IF @bundle IS NOT NULL
		BEGIN
			IF @bundle_sent_char IS NULL
				SET @bundle_sent_char = SPACE(23);
		
			SET @bundle_header = CAST(@bundle_id AS char(26)) + CAST(@client_id AS char(16)) + @bundle_sent_char;
			SET @bundle.WRITE(CAST(@bundle_header AS varbinary(max)), 0, 65);
		END

		INSERT INTO [bundle].[Bundle]
			VALUES (@bundle_id, @client_id, @bundle_sent, @bundle_received, @bundle, CURRENT_TIMESTAMP);
	END
END
