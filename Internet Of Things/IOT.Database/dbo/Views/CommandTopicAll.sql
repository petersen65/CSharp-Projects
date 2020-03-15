CREATE VIEW [dbo].[CommandTopicAll]
	AS 
	SELECT Id, RelativeId, MaximumSubscription, CurrentSubscription, PartitionId 
	FROM [dbo].[CommandTopic]
