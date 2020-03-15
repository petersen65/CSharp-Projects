CREATE SERVICE [http://www.dpag.de/kiss/service/neighborhood-processing]  
	AUTHORIZATION [KissProcessingServiceUser]  
	ON QUEUE [kiss].[NeighborhoodProcessingQueue] 
		([http://www.dpag.de/kiss/contract/neighborhood-processing])
