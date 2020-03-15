CREATE TABLE [bundle].[Bundle]
(
	Id bigint NOT NULL,
	ClientId varchar(16) NOT NULL,
	Sent datetime NULL,
	Received datetime NULL,
	Bundle varbinary(max) NULL,
	Created datetime NULL
) ON [PRIMARY]
