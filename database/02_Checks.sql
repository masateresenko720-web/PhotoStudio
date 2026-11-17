-- Выполнить в базе PhotoStudio после установки.
SELECT 'roles' AS [Таблица],COUNT(*) AS [Строк] FROM dbo.roles
UNION ALL SELECT 'statuses',COUNT(*) FROM dbo.statuses
UNION ALL SELECT 'users',COUNT(*) FROM dbo.users
UNION ALL SELECT 'halls',COUNT(*) FROM dbo.halls
UNION ALL SELECT 'services',COUNT(*) FROM dbo.services
UNION ALL SELECT 'orders',COUNT(*) FROM dbo.orders
UNION ALL SELECT 'order_services',COUNT(*) FROM dbo.order_services;
SELECT t.name AS [Таблица],c.column_id,c.name AS [Поле],ty.name AS [Тип],c.max_length,c.precision,c.scale,c.is_nullable
FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id
JOIN sys.types ty ON ty.user_type_id=c.user_type_id WHERE t.is_ms_shipped=0
ORDER BY t.name,c.column_id;
SELECT name FROM sys.foreign_keys;
DBCC CHECKDB WITH NO_INFOMSGS;
