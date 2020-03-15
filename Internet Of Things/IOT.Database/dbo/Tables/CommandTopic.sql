CREATE TABLE [dbo].[CommandTopic]
(
	[Id] INT NOT NULL IDENTITY, 
    [RelativeId] INT NOT NULL,
    [MaximumSubscription] INT NOT NULL DEFAULT 10, 
    [CurrentSubscription] INT NOT NULL DEFAULT 1, 
    [PartitionId] INT NOT NULL
)
