CREATE QUEUE [bundle].[BundleProcessingQueue]
	WITH STATUS = ON, RETENTION = OFF, 
		 ACTIVATION (STATUS = ON, 
					 PROCEDURE_NAME = [bundle].[BundleProcessingQueueProcessor], 
					 MAX_QUEUE_READERS = 1, EXECUTE AS OWNER) 
	ON [PRIMARY] 
