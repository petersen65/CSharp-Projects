USE Kiss
GO

SELECT * FROM [kiss].[ZoraClientQueue]

SELECT * FROM [bundle].[Stage]
SELECT * FROM [bundle].[Bundle]
SELECT * FROM [po].[PersistentObject]
SELECT * FROM [sd].[StandingData]
SELECT * FROM [kiss].[Neighborhood]
SELECT * FROM [kiss].[DeadLetter]

SELECT * FROM [sys].[transmission_queue]
SELECT * FROM [msdb].[sys].[transmission_queue]
SELECT * FROM [sys].[conversation_endpoints]
SELECT * FROM [sys].[routes]

DELETE FROM [bundle].[Stage]
DELETE FROM [bundle].[Bundle]
DELETE FROM [po].[PersistentObject]
DELETE FROM [sd].[StandingData]
DELETE FROM [kiss].[Neighborhood]
DELETE FROM [kiss].[DeadLetter]
