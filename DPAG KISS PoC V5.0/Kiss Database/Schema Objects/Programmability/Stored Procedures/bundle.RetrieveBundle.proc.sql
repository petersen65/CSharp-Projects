CREATE PROCEDURE [bundle].[RetrieveBundle]
	@bundle_id AS bigint,
	@client_id AS varchar(16)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Bundle AS bundle
		FROM [bundle].[Bundle]
		WHERE Id = @bundle_id AND ClientId = @client_id;
END
