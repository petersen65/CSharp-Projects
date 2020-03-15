ALTER TABLE [dbo].[CommandTopic]
	ADD CONSTRAINT [FK_CommandTopic_Partition_PartitionId_Id]
	FOREIGN KEY (PartitionId)
	REFERENCES [Partition] (Id)
	ON DELETE CASCADE
	ON UPDATE CASCADE
