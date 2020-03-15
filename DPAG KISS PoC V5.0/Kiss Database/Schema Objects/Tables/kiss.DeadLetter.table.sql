CREATE TABLE [kiss].[DeadLetter]
(
	Id bigint IDENTITY (1, 1) NOT NULL,
	Dialog uniqueidentifier NULL,
	MessageType sysname NULL,
	MessageBody varbinary(max) NULL,
	ErrorNumber int NULL,
	ErrorSeverity int NULL,
	ErrorState int NULL,
	ErrorMessage nvarchar(2048) NULL,
	ErrorLine int NULL,
	ErrorProcedure nvarchar(126) NULL,
	Created datetime NULL
) ON [PRIMARY]
