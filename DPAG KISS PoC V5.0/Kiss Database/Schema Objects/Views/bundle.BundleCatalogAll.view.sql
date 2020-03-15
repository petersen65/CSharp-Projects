CREATE VIEW [bundle].[BundleCatalogAll]	
	AS
	SELECT Id AS bundle_id, ClientId AS client_id
		FROM [bundle].[Bundle]
		WHERE Bundle IS NOT NULL
