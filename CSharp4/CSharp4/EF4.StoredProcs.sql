USE [EF4Test]
GO

/****** Object:  StoredProcedure [dbo].[DeleteCustomer]    Script Date: 01/03/2011 09:32:45 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeleteCustomer]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[DeleteCustomer]
GO

/****** Object:  StoredProcedure [dbo].[InsertCustomer]    Script Date: 01/03/2011 09:32:45 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InsertCustomer]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[InsertCustomer]
GO

/****** Object:  StoredProcedure [dbo].[UpdateCustomer]    Script Date: 01/03/2011 09:32:45 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UpdateCustomer]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[UpdateCustomer]
GO

USE [EF4Test]
GO

/****** Object:  StoredProcedure [dbo].[DeleteCustomer]    Script Date: 01/03/2011 09:32:45 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[DeleteCustomer]
(
	@customerId int
)
AS
BEGIN
	SET NOCOUNT ON

	DELETE FROM dbo.Customer
		WHERE CustomerId = @customerId

	RETURN
END

GO

/****** Object:  StoredProcedure [dbo].[InsertCustomer]    Script Date: 01/03/2011 09:32:45 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[InsertCustomer]
(
	@firstName nvarchar(50),
	@lastName nvarchar(50),
	@emailAddress nvarchar(50)
)
AS
BEGIN
	SET NOCOUNT ON

	INSERT INTO dbo.Customer (FirstName, LastName, EmailAddress)
		VALUES (@firstName, @lastName, @emailAddress)

	SELECT @@IDENTITY AS CustomerId
	
	RETURN
END

GO

/****** Object:  StoredProcedure [dbo].[UpdateCustomer]    Script Date: 01/03/2011 09:32:45 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[UpdateCustomer]
(
	@customerId int,
	@firstName nvarchar(50),
	@lastName nvarchar(50),
	@emailAddress nvarchar(50)
)
AS
BEGIN
	SET NOCOUNT ON
	
	UPDATE dbo.Customer
		SET FirstName = @firstName,
			LastName = @lastName,
			EmailAddress = @emailAddress
		WHERE CustomerId = @customerId

	RETURN
END

GO

