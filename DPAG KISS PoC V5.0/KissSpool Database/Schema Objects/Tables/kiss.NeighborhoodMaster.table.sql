CREATE TABLE [kiss].[NeighborhoodMaster]
(
	ClientId varchar(16) NOT NULL,
	BranchId varchar(50) NOT NULL,
	Dns varchar(50) NOT NULL,
	Queue varchar(50) NOT NULL,
	Service varchar(256) NULL,
	Active bit NOT NULL,
	Ping datetime NULL,
	Created datetime NULL
) ON [PRIMARY]
