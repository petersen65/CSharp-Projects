CREATE CONTRACT [http://www.dpag.de/kiss/contract/neighborhood-processing] 
	AUTHORIZATION [dbo] 
	([http://www.dpag.de/kiss/data/pc-registration] SENT BY INITIATOR,
	 [http://www.dpag.de/kiss/data/pc-deregistration] SENT BY INITIATOR,
	 [http://www.dpag.de/kiss/data/ping] SENT BY INITIATOR)
