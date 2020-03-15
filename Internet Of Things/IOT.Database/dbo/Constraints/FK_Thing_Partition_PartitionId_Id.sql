ALTER TABLE [dbo].[Thing]
	ADD CONSTRAINT [FK_Thing_Partition_PartitionId_Id]
	FOREIGN KEY (PartitionId)
	REFERENCES [Partition] (Id)
	ON DELETE NO ACTION
	ON UPDATE NO ACTION
