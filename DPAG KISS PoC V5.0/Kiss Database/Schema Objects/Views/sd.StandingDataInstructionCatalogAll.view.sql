CREATE VIEW [sd].[StandingDataInstructionCatalogAll]
	AS 
	SELECT Id AS sd_id
		FROM [sd].[StandingData]
		WHERE Instruction IS NOT NULL AND StandingData IS NULL
