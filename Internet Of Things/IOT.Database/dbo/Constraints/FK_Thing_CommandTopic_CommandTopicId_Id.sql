ALTER TABLE [dbo].[Thing]
	ADD CONSTRAINT [FK_Thing_CommandTopic_CommandTopicId_Id]
	FOREIGN KEY (CommandTopicId)
	REFERENCES [CommandTopic] (Id)
	ON DELETE NO ACTION
	ON UPDATE NO ACTION
