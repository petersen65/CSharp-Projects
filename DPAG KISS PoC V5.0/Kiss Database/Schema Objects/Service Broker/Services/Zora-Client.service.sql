CREATE SERVICE [TCP://$(LocalComputer):$(ZoraClientWorkstationPort)/http://www.dpag.de/kiss/service/zora-client]  
	AUTHORIZATION [ZoraClientServiceUser]  
	ON QUEUE [kiss].[ZoraClientQueue] 
		([http://www.dpag.de/kiss/contract/zoraclient-processing])
