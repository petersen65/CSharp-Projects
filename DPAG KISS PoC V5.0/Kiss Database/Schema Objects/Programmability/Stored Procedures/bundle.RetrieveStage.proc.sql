CREATE PROCEDURE [bundle].[RetrieveStage]
	@self AS bit,
	@client_id AS varchar(16) = NULL,
	@sequence_number AS bigint OUTPUT,
	@last_update AS datetime OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	SET @sequence_number = -2;
	SET @last_update = NULL;

	IF (@self = 1)
	BEGIN
		SELECT @client_id = ClientId 
			FROM [kiss].[Neighborhood]
			WHERE Self = 1;
	END

	SELECT top(1) @sequence_number = Id, @last_update = Created
		FROM [bundle].[Stage]
		WHERE ClientId = @client_id;
END
