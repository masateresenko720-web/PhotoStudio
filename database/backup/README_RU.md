# Настоящая резервная копия базы

PhotoStudio_SQLServer2025.bak создана SQL Server 2025 Developer 17.0.5035.5 командой BACKUP DATABASE WITH COPY_ONLY, COMPRESSION, CHECKSUM. Проверка RESTORE VERIFYONLY WITH CHECKSUM успешно выполнена. В копии исходные семь таблиц и учебные данные: 3 роли, 4 статуса, 10 пользователей, 4 зала, 4 услуги, 15 заказов, 15 строк услуг заказа. Это исходная база PhotoStudio, а не база испытаний.

Восстановление требует SQL Server 2025. Основной способ установки для SQL Server 2022 и 2025 — SQL-скрипты или окно настройки программы; BAK не обязателен.

Для восстановления на SQL Server 2025 можно использовать SSMS → Databases → Restore Database → Device → выбрать BAK. В Files включите Relocate all files to folder, задайте папки данных и журнала своего сервера. Используйте новое имя PhotoStudio_Restored. Пункт Overwrite existing database не включайте. В приложении укажите новое имя базы.

Дополнительный пример — ../03_RestoreBackup_SQLServer2025.sql. Измените в нём путь к BAK. Скрипт восстановления на другом компьютере здесь не выполнялся; сама копия проверена RESTORE VERIFYONLY. Не восстанавливайте поверх существующей базы с вашими заказами.
