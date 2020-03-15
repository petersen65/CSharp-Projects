﻿ALTER TABLE [bundle].[BundleSpool]
	ADD CONSTRAINT [PK_BundleSpool] 
		PRIMARY KEY CLUSTERED (Id DESC, ClientId ASC)
			WITH (PAD_INDEX = OFF, 
				  STATISTICS_NORECOMPUTE = OFF, 
				  IGNORE_DUP_KEY = OFF, 
				  ALLOW_ROW_LOCKS = ON, 
				  ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]