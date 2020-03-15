CREATE REMOTE SERVICE BINDING [To ZoraClient FE-B]
	AUTHORIZATION [dbo]
	TO SERVICE 'TCP://VS2008SP1:5526/http://www.dpag.de/kiss/service/zora-client'
	WITH USER = [ZoraClientServiceUser]
