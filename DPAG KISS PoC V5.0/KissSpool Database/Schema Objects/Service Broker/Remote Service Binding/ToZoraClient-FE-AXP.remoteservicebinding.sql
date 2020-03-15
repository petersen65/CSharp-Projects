CREATE REMOTE SERVICE BINDING [To ZoraClient FE-AXP]
	AUTHORIZATION [dbo]
	TO SERVICE 'TCP://WXP-ZORA-A:5524/http://www.dpag.de/kiss/service/zora-client'
	WITH USER = [ZoraClientServiceUser]
