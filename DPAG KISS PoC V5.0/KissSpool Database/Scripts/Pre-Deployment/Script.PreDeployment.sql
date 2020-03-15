/*
 Pre-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script.	
 Use SQLCMD syntax to include a file in the pre-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the pre-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

USE [master]

GO

IF NOT EXISTS (SELECT * FROM [sys].[server_principals] WHERE name = N'$(KissOwnerLogin)')
	CREATE LOGIN [$(KissOwnerLogin)] 
		WITH PASSWORD = 'pass@word1', DEFAULT_DATABASE=[master], DEFAULT_LANGUAGE=[us_english]

GO

IF NOT EXISTS (SELECT * FROM [sys].[database_principals] WHERE name = N'$(KissOwnerLogin)')
	CREATE USER [$(KissOwnerLogin)] 
		FOR LOGIN [$(KissOwnerLogin)] 
		WITH DEFAULT_SCHEMA=[dbo]

GO

IF NOT EXISTS (SELECT * FROM [sys].[server_principals] WHERE name = N'$(ZoraClientLogin)')
	CREATE LOGIN [$(ZoraClientLogin)] 
		WITH PASSWORD = 'pass@word1', DEFAULT_DATABASE=[master], DEFAULT_LANGUAGE=[us_english]

IF NOT EXISTS (SELECT * FROM [sys].[database_principals] WHERE name = N'$(ZoraClientLogin)')
	CREATE USER [$(ZoraClientLogin)] 
		FOR LOGIN [$(ZoraClientLogin)]
		WITH DEFAULT_SCHEMA=[dbo]

GO

USE [$(DatabaseName)]

GO
