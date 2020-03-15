﻿CREATE QUEUE [kiss].[ZoraClientQueue]
	WITH STATUS = ON, RETENTION = OFF, 
		 ACTIVATION (STATUS = ON, 
					 PROCEDURE_NAME = [kiss].[ZoraClientQueueProcessor], 
					 MAX_QUEUE_READERS = 1, EXECUTE AS OWNER) 
	ON [PRIMARY] 