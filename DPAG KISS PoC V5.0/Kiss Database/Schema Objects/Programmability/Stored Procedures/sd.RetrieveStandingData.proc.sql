CREATE PROCEDURE [sd].[RetrieveStandingData]
	@sd_id AS bigint
AS
BEGIN
	SET NOCOUNT ON;

	SELECT Instruction AS sd_instr, StandingData AS sd
		FROM [sd].[StandingData]
		WHERE Id = @sd_id;
END
