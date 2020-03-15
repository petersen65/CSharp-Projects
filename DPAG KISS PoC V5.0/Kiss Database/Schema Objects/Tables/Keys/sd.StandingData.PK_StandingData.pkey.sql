﻿ALTER TABLE [sd].[StandingData]
	ADD CONSTRAINT [PK_StandingData] 
		PRIMARY KEY CLUSTERED (Id ASC)
			WITH (PAD_INDEX = OFF, 
				  STATISTICS_NORECOMPUTE = OFF, 
				  IGNORE_DUP_KEY = OFF, 
				  ALLOW_ROW_LOCKS = ON, 
				  ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
