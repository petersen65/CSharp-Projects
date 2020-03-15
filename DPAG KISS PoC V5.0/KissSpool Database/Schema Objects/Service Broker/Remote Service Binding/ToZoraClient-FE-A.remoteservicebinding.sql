CREATE REMOTE SERVICE BINDING [To ZoraClient FE-A]
	AUTHORIZATION [dbo]
	TO SERVICE 'TCP://VS2008SP1:5525/http://www.dpag.de/kiss/service/zora-client'
	WITH USER = [ZoraClientServiceUser]
