CREATE VIEW [dbo].[PartitionAll]
	AS 
	SELECT p.Id, p.Description, p.Active, p.Area, 
		   p.MaximumCommandTopic, ct.MaximumSubscription, 
		   p.Namespace, p.EventStore, 
		   p.Owner, p.OwnerSecret, p.StorageAccount, p.AccessControl, p.AccessControlSecret,
		   p.LastUpdated 
	FROM [dbo].[Partition] AS p
	LEFT OUTER JOIN (SELECT DISTINCT PartitionId, MaximumSubscription FROM [dbo].[CommandTopic]) AS ct
	ON p.Id = ct.PartitionId
