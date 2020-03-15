CREATE REMOTE SERVICE BINDING [To ZoraClient FE-SQL]
	AUTHORIZATION [dbo]
	TO SERVICE 'TCP://VS2008SP1:5524/http://www.dpag.de/kiss/service/zora-client'
	WITH USER = [ZoraClientServiceUser]
