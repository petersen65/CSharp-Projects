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

PRINT '';
PRINT 'Loading Data...';
GO

PRINT 'Loading [dbo].[Partition]....';

BULK INSERT [dbo].[Partition] FROM N'$(SampleDataPath)Partition.csv'
	WITH (CODEPAGE='ACP', DATAFILETYPE = 'char', FIELDTERMINATOR= '\t', ROWTERMINATOR = '\n', KEEPIDENTITY, TABLOCK);

PRINT 'Loading [dbo].[Thing]....';

BULK INSERT [dbo].[Thing] FROM N'$(SampleDataPath)Thing.csv'
	WITH (CODEPAGE='ACP', DATAFILETYPE = 'char', FIELDTERMINATOR= '\t', ROWTERMINATOR = '\n', TABLOCK);

PRINT 'Loading [dbo].[CommandTopic]....';

BULK INSERT [dbo].[CommandTopic] FROM N'$(SampleDataPath)CommandTopic.csv'
	WITH (CODEPAGE='ACP', DATAFILETYPE = 'char', FIELDTERMINATOR= '\t', ROWTERMINATOR = '\n', KEEPIDENTITY, TABLOCK);

GO
