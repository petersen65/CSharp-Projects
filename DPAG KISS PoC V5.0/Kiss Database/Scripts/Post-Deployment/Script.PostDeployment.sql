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

IF EXISTS (SELECT * FROM [sys].[certificates] WHERE name = 'ZoraClientWorkstationCertificate')
	DROP CERTIFICATE [ZoraClientWorkstationCertificate]

GO

-- makecert -pe -r -n "CN=KISS ZoraClient" -sv "ZoraClientWorkstationCertificate.pvk" "ZoraClientWorkstationCertificate.cer"
CREATE CERTIFICATE [ZoraClientWorkstationCertificate] 
	AUTHORIZATION [dbo]
	FROM FILE = '$(CertificatesRootPath)\ZoraClientWorkstationCertificate.cer' 
	WITH PRIVATE KEY (FILE = '$(CertificatesRootPath)\ZoraClientWorkstationCertificate.pvk', DECRYPTION BY PASSWORD = 'pass@word1')

GO

IF EXISTS (SELECT * FROM [sys].[certificates] WHERE name = 'KissProcessingServerCertificate')
	DROP CERTIFICATE [KissProcessingServerCertificate]

GO

-- makecert -pe -r -n "CN=KISS Processing" -sv "KissProcessingServerCertificate.pvk" "KissProcessingServerCertificate.cer"
CREATE CERTIFICATE [KissProcessingServerCertificate] 
	AUTHORIZATION [$(KissProcessingLogin)]
	FROM FILE = '$(CertificatesRootPath)\KissProcessingServerCertificate.cer' 

GO

CREATE ENDPOINT [Kiss Endpoint] 
	AUTHORIZATION [$(KissOwnerLogin)]
	STATE = STARTED
	AS TCP (LISTENER_PORT = $(ZoraClientWorkstationPort), 
			LISTENER_IP = ALL)
	FOR SERVICE_BROKER (MESSAGE_FORWARDING = DISABLED, 
						MESSAGE_FORWARD_SIZE = 32, 
						AUTHENTICATION = CERTIFICATE [ZoraClientWorkstationCertificate], 
						ENCRYPTION = SUPPORTED ALGORITHM AES)

GRANT CONNECT ON ENDPOINT::[Kiss Endpoint]
   TO [$(KissProcessingLogin)]

GO

USE [$(DatabaseName)]

PRINT N'Inserting data into Neighborhood...'

GO

SET NOCOUNT ON

INSERT INTO [kiss].[Neighborhood]
   VALUES ('$(ClientId)', '$(BranchId)', NULL, NULL, 1, NULL)

GO
