CREATE TABLE [bundle].[BundleSpool]
(
	Id bigint NOT NULL,
    ClientId varchar(16) NOT NULL,
    Bundle varbinary(max) NOT NULL,
	Sent datetime NULL,
	Received datetime NULL
) ON [PRIMARY]
