using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using PhotoStudio.Core;
using PhotoStudio.Desktop.Views;

namespace PhotoStudio.Desktop;
public partial class MainWindow : Window
{
    public StudioService Service { get; }
    public Session? Session { get; private set; }
    public BaseView? CurrentPage { get; private set; }
    public Task LastOperation { get; private set; } = Task.CompletedTask;
    public Task Initialization { get; private set; } = Task.CompletedTask;
    public string Message => MessageText.Text ?? "";

    private readonly Stack<BaseView> history = [];
    private BaseView? pending;
    public MainWindow(StudioService service)
    {
        InitializeComponent();
        Service = service;
        ProviderLabel.Text = "База: " + service.Factory.Settings.Provider;
        Opened += (_, _) =>
        {
            Initialization = InitializeAsync();
        };
    }

    public async Task InitializeAsync()
    {
        try
        {
            await Service.Factory.InitializeAsync(Service.Clock);
            await Navigate(new LoginView(this), false, true);
        }
        catch (Exception ex)
        {
            Error(ex);
            await Navigate(new ConnectionView(this), false, true);
        }
    }

    public void Execute(Func<Task> action) => LastOperation = SafeAsync(action);
    private async Task SafeAsync(Func<Task> action)
    {
        MessageText.Text = "";
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }

    private void Error(Exception ex)
    {
        MessageText.Foreground = Brushes.Firebrick;
        MessageText.Text = ex is AppException or ArgumentException ? ex.Message : ex is Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ? "Данные изменены. Обновите карточку." : "Не удалось выполнить операцию. Проверьте подключение к базе.";
        try
        {
            var dir = Service.Factory.Settings.DataDirectory;
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "application.log"), DateTime.Now + " " + ex + Environment.NewLine);
            }
        }
        catch
        {
        }
    }

    public void Notice(string text)
    {
        MessageText.Foreground = Brush.Parse("#16794A");
        MessageText.Text = text;
    }

    public async Task Navigate(BaseView page, bool remember = true, bool force = false)
    {
        if (!force && CurrentPage?.IsDirty == true)
        {
            pending = page;
            LeaveButton.IsVisible = true;
            Notice("Есть несохранённые изменения. Подтвердите переход или продолжите заполнение.");
            return;
        }

        pending = null;
        LeaveButton.IsVisible = false;
        if (remember && CurrentPage != null)
            history.Push(CurrentPage);
        CurrentPage = page;
        PageHost.Content = page;
        PageTitle.Text = page.Title;
        PageHint.Text = page.Hint;
        RefreshNavigation();
        await page.LoadAsync();
    }

    public async Task SignedIn(Session session)
    {
        Session = session;
        history.Clear();
        await Navigate(new CatalogView(this), false, true);
        Notice("Вход выполнен: " + session.Role);
    }

    public async Task Logout()
    {
        Session = null;
        history.Clear();
        await Navigate(new LoginView(this), false, true);
        Notice("Вы вышли из системы.");
    }

    private void RefreshNavigation()
    {
        Navigation.Children.Clear();
        UserLabel.Text = Session == null ? "Посетитель" : Session.FullName + "\n" + Session.Role;
        void Nav(string label, Func<Task> action)
        {
            var b = new Button
            {
                Content = label,
                Classes =
                {
                    "nav"
                }
            };
            b.Click += (_, _) => Execute(action);
            Navigation.Children.Add(b);
        }

        Nav("Каталог залов", () => Navigate(new CatalogView(this)));
        if (Session != null)
            Nav(Session.Role == Names.Photographer ? "Мои съёмки" : "Заказы", () => Navigate(new OrdersView(this)));
        if (Session?.Role == Names.Admin)
        {
            Nav("Справочники", () => Navigate(new ReferenceView(this)));
            Nav("Отчёты", () => Navigate(new ReportView(this)));
        }

        Nav(Session == null ? "Вход / регистрация" : "Выйти", Logout);
        BackButton.IsEnabled = history.Count > 0;
    }

    private void BackClick(object? sender, RoutedEventArgs e)
    {
        if (history.Count > 0)
            Execute(() => Navigate(history.Pop(), false));
    }

    private void LeaveClick(object? sender, RoutedEventArgs e)
    {
        if (pending != null)
        {
            var p = pending;
            Execute(() => Navigate(p, true, true));
        }
    }
}
