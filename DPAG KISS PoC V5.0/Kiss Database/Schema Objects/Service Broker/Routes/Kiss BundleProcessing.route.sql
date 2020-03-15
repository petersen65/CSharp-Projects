CREATE ROUTE [Kiss BundleProcessing] 
	AUTHORIZATION [dbo]
	WITH SERVICE_NAME = 'http://www.dpag.de/kiss/service/bundle-processing',
		 ADDRESS = 'TCP://$(KissProcessingServer):$(KissProcessingServerPort)'
