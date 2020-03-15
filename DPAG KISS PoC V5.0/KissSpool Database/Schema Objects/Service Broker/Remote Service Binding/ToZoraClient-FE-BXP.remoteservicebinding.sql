CREATE REMOTE SERVICE BINDING [To ZoraClient FE-BXP]
	AUTHORIZATION [dbo]
	TO SERVICE 'TCP://WXP-ZORA-B:5524/http://www.dpag.de/kiss/service/zora-client'
	WITH USER = [ZoraClientServiceUser]
