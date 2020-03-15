CREATE TABLE [kiss].[Neighborhood]
(
	ClientId varchar(16) NOT NULL,
	BranchId varchar(50) NOT NULL,
	Dns varchar(50) NULL,
	Queue varchar(50) NULL,
	Self bit NOT NULL,
	Created datetime NULL
) ON [PRIMARY]
