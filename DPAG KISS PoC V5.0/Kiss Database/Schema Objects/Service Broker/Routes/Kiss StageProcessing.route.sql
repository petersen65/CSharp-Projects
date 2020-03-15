CREATE ROUTE [Kiss StageProcessing] 
	AUTHORIZATION [dbo]
	WITH SERVICE_NAME = 'http://www.dpag.de/kiss/service/stage-processing',
		 ADDRESS = 'TCP://$(KissProcessingServer):$(KissProcessingServerPort)'
