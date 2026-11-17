using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop;
public partial class App : Application
{
    public override void Initialize()
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.MainWindow = new MainWindow(new StudioService(new DbFactory(StudioSettings.Load()), new SystemClock()));
            }
            catch (Exception ex)
            {
                desktop.MainWindow = new Avalonia.Controls.Window
                {
                    Title = "Фотостудия — настройка",
                    Width = 720,
                    Height = 300,
                    Content = new Avalonia.Controls.TextBlock
                    {
                        Text = "Ошибка настройки: " + ex.Message,
                        Margin = new Thickness(30)
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
