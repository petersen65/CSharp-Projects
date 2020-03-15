/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

PRINT N'Dropping AutoCreatedLocal...'

GO

IF EXISTS (SELECT * FROM [sys].[routes] WHERE name = 'AutoCreatedLocal')
	DROP ROUTE [AutoCreatedLocal]

GO

PRINT N'Dropping and Creating Kiss Endpoint...'

GO

USE [master]

GO

IF EXISTS (SELECT * FROM [sys].[databases] WHERE name = 'master' AND is_master_key_encrypted_by_server = 0)
	CREATE MASTER KEY 
		ENCRYPTION BY PASSWORD = 'pass@word1'

GO

IF EXISTS (SELECT * FROM [sys].[endpoints] WHERE name = 'Kiss Endpoint')
	DROP ENDPOINT [Kiss Endpoint]

GO

IF EXISTS (SELECT * FROM [sys].[certificates] WHERE name = 'KissProcessingServerCertificate')
	DROP CERTIFICATE [KissProcessingServerCertificate]

GO

-- makecert -pe -r -n "CN=KISS Processing" -sv "KissProcessingServerCertificate.pvk" "KissProcessingServerCertificate.cer"
CREATE CERTIFICATE [KissProcessingServerCertificate] 
	AUTHORIZATION [dbo]
	FROM FILE = '$(CertificatesRootPath)\KissProcessingServerCertificate.cer' 
	WITH PRIVATE KEY (FILE = '$(CertificatesRootPath)\KissProcessingServerCertificate.pvk', DECRYPTION BY PASSWORD = 'pass@word1')

GO

IF EXISTS (SELECT * FROM [sys].[certificates] WHERE name = 'ZoraClientWorkstationCertificate')
	DROP CERTIFICATE [ZoraClientWorkstationCertificate]

GO

-- makecert -pe -r -n "CN=KISS ZoraClient" -sv "ZoraClientWorkstationCertificate.pvk" "ZoraClientWorkstationCertificate.cer"
CREATE CERTIFICATE [ZoraClientWorkstationCertificate] 
	AUTHORIZATION [$(ZoraClientLogin)]
	FROM FILE = '$(CertificatesRootPath)\ZoraClientWorkstationCertificate.cer' 

GO

CREATE ENDPOINT [Kiss Endpoint] 
	AUTHORIZATION [$(KissOwnerLogin)]
	STATE = STARTED
	AS TCP (LISTENER_PORT = $(KissProcessingServerPort), 
			LISTENER_IP = ALL)
	FOR SERVICE_BROKER (MESSAGE_FORWARDING = DISABLED, 
						MESSAGE_FORWARD_SIZE = 256, 
						AUTHENTICATION = CERTIFICATE [KissProcessingServerCertificate], 
						ENCRYPTION = SUPPORTED ALGORITHM AES)

GRANT CONNECT ON ENDPOINT::[Kiss Endpoint]
   TO [$(ZoraClientLogin)]

GO

USE [$(DatabaseName)]

GO

PRINT N'Inserting data into NeighborhoodMaster...'

GO

SET NOCOUNT ON

INSERT INTO [kiss].[NeighborhoodMaster]
   VALUES ('FE-SQL', '1', 'VS2008SP1', 'formatname:direct=os:{0}\private$\kiss-sql/{1}', NULL, 0, NULL, CURRENT_TIMESTAMP)

INSERT INTO [kiss].[NeighborhoodMaster]
   VALUES ('FE-A', '2', 'VS2008SP1', 'formatname:direct=os:{0}\private$\kiss-a/{1}', NULL, 0, NULL, CURRENT_TIMESTAMP)
		   
INSERT INTO [kiss].[NeighborhoodMaster]
   VALUES ('FE-B', '2', 'VS2008SP1', 'formatname:direct=os:{0}\private$\kiss-b/{1}', NULL, 0, NULL, CURRENT_TIMESTAMP)
		   
INSERT INTO [kiss].[NeighborhoodMaster]
   VALUES ('FE-C', '2', 'VS2008SP1', 'formatname:direct=os:{0}\private$\kiss-c/{1}', NULL, 0, NULL, CURRENT_TIMESTAMP)

INSERT INTO [kiss].[NeighborhoodMaster]
   VALUES ('FE-AXP', '3', 'WXP-ZORA-A', 'formatname:direct=os:{0}\private$\kiss/{1}', NULL, 0, NULL, CURRENT_TIMESTAMP)
		   
INSERT INTO [kiss].[NeighborhoodMaster]
   VALUES ('FE-BXP', '3', 'WXP-ZORA-B', 'formatname:direct=os:{0}\private$\kiss/{1}', NULL, 0, NULL, CURRENT_TIMESTAMP)
		   
INSERT INTO [kiss].[NeighborhoodMaster]
   VALUES ('FE-CXP', '3', 'WXP-ZORA-C', 'formatname:direct=os:{0}\private$\kiss/{1}', NULL, 0, NULL, CURRENT_TIMESTAMP)

GO
