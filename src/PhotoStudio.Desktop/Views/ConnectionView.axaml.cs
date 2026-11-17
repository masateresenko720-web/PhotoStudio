using Avalonia.Controls;
using Microsoft.Data.SqlClient;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class ConnectionView : BaseView
{
    public override string Title => "Настройка SQL Server";
    public override string Hint => "Первый запуск: укажите сервер и создайте учебную базу по исходной ER-диаграмме";
    public ConnectionView(MainWindow window)
    {
        InitializeComponent(); Setup(window);
        SqlConnectionStringBuilder current;
        try { current = new SqlConnectionStringBuilder(W.Service.Factory.Settings.ConnectionString); }
        catch (ArgumentException) { current = new SqlConnectionStringBuilder { DataSource = "localhost", InitialCatalog = "PhotoStudio" }; }
        var server = new TextBox { Text = current.DataSource };
        var database = new TextBox { Text = string.IsNullOrWhiteSpace(current.InitialCatalog) ? "PhotoStudio" : current.InitialCatalog };
        Panel.Children.Add(Text("1. Установите SQL Server Developer или Express на Windows.", 20, true));
        Panel.Children.Add(Text("SSMS и библиотека SqlClient сами по себе сервер не устанавливают. Используется вход Windows под вашим текущим пользователем."));
        Panel.Children.Add(Row(Field("Server", "Имя сервера — как при входе в SSMS", server, 400), Field("Database", "Имя базы данных", database, 300)));
        Panel.Children.Add(Row(
            Action("DefaultServer", "Обычный сервер", () => { server.Text = "localhost"; return Task.CompletedTask; }),
            Action("ExpressServer", "SQL Server Express", () => { server.Text = @".\SQLEXPRESS"; return Task.CompletedTask; }),
            Action("LocalServer", "SQL Server LocalDB", () => { server.Text = @"(localdb)\MSSQLLocalDB"; return Task.CompletedTask; })));
        Panel.Children.Add(Text("LocalDB — вариант SQL Server Express для локальной разработки. Выбирайте его, только если он установлен и такой вариант разрешён преподавателем."));
        StudioSettings Settings()
        {
            if (string.IsNullOrWhiteSpace(server.Text) || string.IsNullOrWhiteSpace(database.Text)) throw new AppException("Введите имя сервера и базы.");
            return new StudioSettings { ConnectionString = new SqlConnectionStringBuilder { DataSource = server.Text.Trim(), InitialCatalog = database.Text.Trim(), IntegratedSecurity = true, Encrypt = true, TrustServerCertificate = true, ConnectTimeout = 10 }.ConnectionString, DataDirectory = W.Service.Factory.Settings.DataDirectory };
        }
        async Task Run(bool create)
        {
            try
            {
                var settings = Settings();
                if (create) await DatabaseSetup.CreateAsync(settings, Path.Combine(AppContext.BaseDirectory, "database", "01_StructureAndSeed.sql"));
                else await new DbFactory(settings).InitializeAsync(new SystemClock());
                settings.SaveConnection();
                W.Service.Factory.Settings.ConnectionString = settings.ConnectionString;
                await W.InitializeAsync();
                W.Notice("Подключение сохранено. Вход: admin / PhotoStudio!2026. Измените пароль после первого входа.");
            }
            catch (Exception ex)
            {
                details.Text = "Подключение не выполнено: " + ex.Message + "\nПроверьте, запущен ли сервер, совпадает ли его имя с SSMS и есть ли у пользователя право создать базу. Существующие базы не удаляются.";
            }
        }
        Panel.Children.Add(Row(Action("CreateDatabase", "2. Создать / проверить базу и войти", () => Run(true), true), Action("ConnectDatabase", "Подключить готовую базу", () => Run(false))));
        Panel.Children.Add(Text("При первой установке создаются 7 таблиц и учебные записи. При повторном запуске существующие таблицы и заказы сохраняются. Если схема другой версии, укажите новое имя базы."));
        Panel.Children.Add(details);
    }
    private readonly TextBlock details = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
}
