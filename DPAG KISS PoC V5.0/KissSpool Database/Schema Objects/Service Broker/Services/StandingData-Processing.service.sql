CREATE SERVICE [http://www.dpag.de/kiss/service/standing-data-processing]  
	AUTHORIZATION [KissProcessingServiceUser]  
	ON QUEUE [sd].[StandingDataProcessingQueue] 
