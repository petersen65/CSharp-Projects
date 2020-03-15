USE KissSpool
GO

SELECT * FROM [kiss].[NeighborhoodProcessingQueue]
SELECT * FROM [bundle].[StageProcessingQueue]
SELECT * FROM [bundle].[BundleProcessingQueue]
SELECT * FROM [sd].[StandingDataProcessingQueue]

SELECT * FROM [kiss].[NeighborhoodMaster]
SELECT * FROM [bundle].[BundleSpool]
SELECT * FROM [kiss].[DeadLetter]

SELECT * FROM [sys].[transmission_queue]
SELECT * FROM [msdb].[sys].[transmission_queue]
SELECT * FROM [sys].[conversation_endpoints]
SELECT * FROM [sys].[routes]
SELECT * FROM [sys].[remote_service_bindings]

DELETE FROM [kiss].[NeighborhoodMaster]
DELETE FROM [bundle].[BundleSpool]
DELETE FROM [kiss].[DeadLetter]

EXECUTE [sd].[DistributeStandingDataInstruction] 1, 4711, 'StandingData-1K.txt', 20
