CREATE ROUTE [Kiss StandingDataProcessing]
	AUTHORIZATION [dbo]
	WITH SERVICE_NAME = 'http://www.dpag.de/kiss/service/standing-data-processing',
		 ADDRESS = 'TCP://$(KissProcessingServer):$(KissProcessingServerPort)'
