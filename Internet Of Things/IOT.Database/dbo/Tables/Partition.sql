CREATE TABLE [dbo].[Partition]
(
	[Id] INT NOT NULL IDENTITY, 
    [Description] NVARCHAR(50) NOT NULL, 
    [Active] BIT NOT NULL DEFAULT 0, 
    [Area] NVARCHAR(50) NOT NULL,
    [MaximumCommandTopic] INT NOT NULL DEFAULT 1, 
    [Namespace] NVARCHAR(50) NOT NULL, 
	[EventStore] NVARCHAR(50) NOT NULL,
    [Owner] NVARCHAR(50) NULL, 
    [OwnerSecret] NVARCHAR(100) NULL, 
    [StorageAccount] NVARCHAR(255) NULL, 
    [AccessControl] NVARCHAR(50) NULL, 
    [AccessControlSecret] NVARCHAR(100) NULL,
	[LastUpdated] DATETIME2 NOT NULL DEFAULT SYSDATETIME()
)
