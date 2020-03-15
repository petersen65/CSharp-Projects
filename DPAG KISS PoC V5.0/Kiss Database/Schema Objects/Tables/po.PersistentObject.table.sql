CREATE TABLE [po].[PersistentObject]
(
	Id varchar(50) NOT NULL,
	Sent datetime NULL,
	Received datetime NULL,
	PersistentObject varbinary(max) NOT NULL,
	Created datetime NULL,
	Modified datetime NULL
) ON [PRIMARY]
