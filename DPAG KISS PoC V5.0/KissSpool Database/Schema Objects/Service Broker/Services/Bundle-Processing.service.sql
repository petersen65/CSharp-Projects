CREATE SERVICE [http://www.dpag.de/kiss/service/bundle-processing]  
	AUTHORIZATION [dbo]  
	ON QUEUE [bundle].[BundleProcessingQueue] 
		([http://www.dpag.de/kiss/contract/bundle-processing])
