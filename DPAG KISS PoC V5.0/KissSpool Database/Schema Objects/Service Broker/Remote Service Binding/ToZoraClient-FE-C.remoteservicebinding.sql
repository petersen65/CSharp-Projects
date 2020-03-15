CREATE REMOTE SERVICE BINDING [To ZoraClient FE-C]
	AUTHORIZATION [dbo]
	TO SERVICE 'TCP://VS2008SP1:5527/http://www.dpag.de/kiss/service/zora-client'
	WITH USER = [ZoraClientServiceUser]
