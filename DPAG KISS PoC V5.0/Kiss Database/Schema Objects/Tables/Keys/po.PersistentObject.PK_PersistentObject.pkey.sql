﻿ALTER TABLE [po].[PersistentObject]
	ADD CONSTRAINT [PK_PersistentObject] 
		PRIMARY KEY CLUSTERED (Id ASC)
			WITH (PAD_INDEX = OFF, 
				  STATISTICS_NORECOMPUTE = OFF, 
				  IGNORE_DUP_KEY = OFF, 
				  ALLOW_ROW_LOCKS = ON, 
				  ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]