-- Выполнять в SSMS на SQL Server 2025 (не на SQL Server 2022).
-- Измените только путь к BAK ниже. Путь должен быть доступен службе SQL Server.
USE master;
GO
DECLARE @backup nvarchar(4000) = N'C:\Coursework\PhotoStudio\database\backup\PhotoStudio_SQLServer2025.bak';
IF DB_ID(N'PhotoStudio_Restored') IS NOT NULL
    THROW 50001, N'PhotoStudio_Restored уже существует. Данные не изменены. Используйте другую пустую базу или основной способ установки скриптами.', 1;
DECLARE @data nvarchar(4000) = CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultDataPath'));
DECLARE @log nvarchar(4000) = CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultLogPath'));
IF @data IS NULL OR @log IS NULL
    THROW 50002, N'Не удалось определить папку SQL Server. Восстановите копию через мастер SSMS с Relocate all files.', 1;
DECLARE @mdf nvarchar(4000) = @data + N'PhotoStudio_Restored.mdf';
DECLARE @ldf nvarchar(4000) = @log + N'PhotoStudio_Restored_log.ldf';
RESTORE VERIFYONLY FROM DISK = @backup WITH CHECKSUM;
RESTORE DATABASE PhotoStudio_Restored FROM DISK = @backup
WITH MOVE N'PhotoStudio' TO @mdf,
     MOVE N'PhotoStudio_log' TO @ldf,
     CHECKSUM, RECOVERY, STATS = 10;
DBCC CHECKDB (N'PhotoStudio_Restored') WITH NO_INFOMSGS;
-- После успеха в окне приложения укажите имя базы PhotoStudio_Restored.
