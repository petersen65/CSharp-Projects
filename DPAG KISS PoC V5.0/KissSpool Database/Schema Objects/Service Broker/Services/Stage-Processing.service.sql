CREATE SERVICE [http://www.dpag.de/kiss/service/stage-processing]  
	AUTHORIZATION [dbo]  
	ON QUEUE [bundle].[StageProcessingQueue] 
		([http://www.dpag.de/kiss/contract/stage-processing])
