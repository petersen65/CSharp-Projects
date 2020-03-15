CREATE ROUTE [Kiss NeighborhoodProcessing] 
	AUTHORIZATION [dbo]
	WITH SERVICE_NAME = 'http://www.dpag.de/kiss/service/neighborhood-processing',
		 ADDRESS = 'TCP://$(KissProcessingServer):$(KissProcessingServerPort)'
