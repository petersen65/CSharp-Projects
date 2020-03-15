CREATE TABLE [sd].[StandingData]
(
	Id bigint NOT NULL,
	Sent datetime NULL,
	Received datetime NULL,
	Instruction xml NULL,
	BlobSent datetime NULL,
	BlobReceived datetime NULL,
	StandingData varbinary(max) NULL,
	Created datetime NULL
) ON [PRIMARY]
