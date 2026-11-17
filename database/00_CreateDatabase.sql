-- Выполнить в SSMS под учётной записью с правом CREATE DATABASE.
USE master;
GO
IF DB_ID(N'PhotoStudio') IS NULL
 EXEC(N'CREATE DATABASE [PhotoStudio] COLLATE Cyrillic_General_100_CI_AS');
GO
USE PhotoStudio;
GO
-- Теперь выполнить 01_StructureAndSeed.sql в этой базе один раз.
