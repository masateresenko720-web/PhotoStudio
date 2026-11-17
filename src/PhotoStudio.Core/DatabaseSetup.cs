using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace PhotoStudio.Core;
public static class DatabaseSetup
{
    // Проверяет существующую базу. Не удаляет и не заменяет пользовательские данные.
    public static async Task CreateAsync(StudioSettings settings, string script)
    {
        var target = new SqlConnectionStringBuilder(settings.ConnectionString);
        string name = target.InitialCatalog;
        if (string.IsNullOrWhiteSpace(name) || name.Length>128)
            throw new AppException("Задайте Database в строке подключения.");
        var master = new SqlConnectionStringBuilder(settings.ConnectionString) { InitialCatalog = "master" };
        await using (var c = new SqlConnection(master.ConnectionString))
        {
            await c.OpenAsync();
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT DB_ID(@name)";
            cmd.Parameters.AddWithValue("@name", name);
            if (await cmd.ExecuteScalarAsync() is DBNull)
            {
                cmd.CommandText = "CREATE DATABASE ["+name.Replace("]", "]]")+"] COLLATE Cyrillic_General_100_CI_AS";
                cmd.Parameters.Clear();
                await cmd.ExecuteNonQueryAsync();
            }
        }
        await using var db = new DbFactory(settings).Create();
        await db.Database.OpenConnectionAsync();
        await using var check = db.Database.GetDbConnection().CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0";
        int count = Convert.ToInt32(await check.ExecuteScalarAsync());
        if (count==0)
            await db.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync(script));
        await ValidateAsync(db);
    }

    public static async Task ValidateAsync(StudioDb db)
    {
        string[] expected = ["halls:hall_id,hall_name,price_per_hour,description", "order_services:order_service_id,order_id,service_id,quantity", "orders:order_id,order_date,client_id,hall_id,photographer_id,start_time,end_time,status_id", "roles:role_id,role_name", "services:service_id,service_name,price", "statuses:status_id,status_name", "users:user_id,login,password_hash,last_name,first_name,middle_name,phone,role_id"];
        if(db.Database.GetDbConnection().State!=ConnectionState.Open) await db.Database.OpenConnectionAsync();
        await using var cmd=db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText="SELECT t.name,c.name FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id WHERE t.is_ms_shipped=0 ORDER BY t.name,c.column_id";
        await using var reader=await cmd.ExecuteReaderAsync();
        var tables=new Dictionary<string,List<string>>();
        while(await reader.ReadAsync())
        {
            var t=reader.GetString(0); if(!tables.ContainsKey(t)) tables[t]=[]; tables[t].Add(reader.GetString(1));
        }
        var actual=tables.Select(x=>x.Key+":"+string.Join(",",x.Value)).Order(StringComparer.Ordinal).ToArray();
        if(!actual.SequenceEqual(expected)) throw new AppException("Структура базы отличается от исходной ER-диаграммы. Выберите новую пустую базу; существующие данные не изменены.");
    }
}
