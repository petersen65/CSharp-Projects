CREATE VIEW [sd].[StandingDataCatalogAll]
	AS 
	SELECT Id AS sd_id
		FROM [sd].[StandingData]
		WHERE StandingData IS NOT NULL
