select * from dbo.Spatial

select GeomCol1.STX as X, GeomCol1.STY as Y from dbo.Spatial

insert into dbo.Spatial (GeomCol1) values (geometry::STGeomFromText('LINESTRING (100 100, 20 180, 180 180)', 0))
insert into dbo.Spatial (GeomCol1) values (geometry::STGeomFromText('POLYGON ((0 0, 150 0, 150 150, 0 150, 0 0))', 0))
insert into dbo.Spatial (GeomCol1) values (geometry::STGeomFromText('POINT (3 4)', 0))
insert into dbo.Spatial (GeomCol1) values (geometry::STGeomFromText('POINT (5 6)', 0))

delete from dbo.Spatial

select * from dbo.SpatialPoint
