CREATE REMOTE SERVICE BINDING [To ZoraClient FE-CXP]
	AUTHORIZATION [dbo]
	TO SERVICE 'TCP://WXP-ZORA-C:5524/http://www.dpag.de/kiss/service/zora-client'
	WITH USER = [ZoraClientServiceUser]
