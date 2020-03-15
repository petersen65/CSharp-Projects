CREATE VIEW [dbo].[ThingAll]
	AS 
	SELECT t.Id, t.Description, t.Active, p.Area, ct.RelativeId, t.PartitionId, t.LastUpdated
	FROM [dbo].[Thing] AS t
	INNER JOIN [dbo].[Partition] AS p
	ON t.PartitionId = p.Id
	INNER JOIN [dbo].[CommandTopic] as ct
	ON t.CommandTopicId = ct.Id