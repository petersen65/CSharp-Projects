USE [master]
GO

/****** Object:  Database [EF4Test]    Script Date: 01/03/2011 09:36:17 ******/
IF  EXISTS (SELECT name FROM sys.databases WHERE name = N'EF4Test')
DROP DATABASE [EF4Test]
GO

USE [master]
GO

/****** Object:  Database [EF4Test]    Script Date: 01/03/2011 09:36:17 ******/
CREATE DATABASE [EF4Test] ON  PRIMARY 
( NAME = N'EF4Test', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\EF4Test.mdf' , SIZE = 2304KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'EF4Test_log', FILENAME = N'c:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLEXPRESS\MSSQL\DATA\EF4Test_log.LDF' , SIZE = 576KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [EF4Test] SET COMPATIBILITY_LEVEL = 100
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [EF4Test].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [EF4Test] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [EF4Test] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [EF4Test] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [EF4Test] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [EF4Test] SET ARITHABORT OFF 
GO

ALTER DATABASE [EF4Test] SET AUTO_CLOSE ON 
GO

ALTER DATABASE [EF4Test] SET AUTO_CREATE_STATISTICS ON 
GO

ALTER DATABASE [EF4Test] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [EF4Test] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [EF4Test] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [EF4Test] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [EF4Test] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [EF4Test] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [EF4Test] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [EF4Test] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [EF4Test] SET  ENABLE_BROKER 
GO

ALTER DATABASE [EF4Test] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [EF4Test] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [EF4Test] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [EF4Test] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [EF4Test] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [EF4Test] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [EF4Test] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [EF4Test] SET  READ_WRITE 
GO

ALTER DATABASE [EF4Test] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [EF4Test] SET  MULTI_USER 
GO

ALTER DATABASE [EF4Test] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [EF4Test] SET DB_CHAINING OFF 
GO
