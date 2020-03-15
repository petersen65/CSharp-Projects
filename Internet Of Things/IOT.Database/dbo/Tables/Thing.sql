CREATE TABLE [dbo].[Thing]
(
	[Id] UNIQUEIDENTIFIER NOT NULL, 
    [Description] NVARCHAR(50) NOT NULL, 
    [Active] BIT NOT NULL DEFAULT 0, 
    [PartitionId] INT NOT NULL, 
    [CommandTopicId] INT NOT NULL, 
    [LastUpdated] DATETIME2 NOT NULL DEFAULT SYSDATETIME()
)
