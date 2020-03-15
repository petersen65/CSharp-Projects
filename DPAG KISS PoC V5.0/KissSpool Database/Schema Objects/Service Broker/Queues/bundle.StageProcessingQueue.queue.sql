CREATE QUEUE [bundle].[StageProcessingQueue]
	WITH STATUS = ON, RETENTION = OFF, 
		 ACTIVATION (STATUS = ON, 
					 PROCEDURE_NAME = [bundle].[StageProcessingQueueProcessor], 
					 MAX_QUEUE_READERS = 1, EXECUTE AS OWNER) 
	ON [PRIMARY] 
