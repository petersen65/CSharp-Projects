CREATE QUEUE [kiss].[NeighborhoodProcessingQueue]
	WITH STATUS = ON, RETENTION = OFF, 
		 ACTIVATION (STATUS = ON, 
					 PROCEDURE_NAME = [kiss].[NeighborhoodProcessingQueueProcessor], 
					 MAX_QUEUE_READERS = 1, EXECUTE AS OWNER) 
	ON [PRIMARY] 
