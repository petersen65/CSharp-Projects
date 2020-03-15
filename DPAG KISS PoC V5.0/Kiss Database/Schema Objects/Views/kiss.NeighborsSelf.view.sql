CREATE VIEW [kiss].[NeighborsSelf]
	AS 
	SELECT ClientId AS client_id, BranchId AS branch_id, Dns AS dns, Queue AS queue
		FROM [kiss].[Neighborhood]
		WHERE Self = 1
