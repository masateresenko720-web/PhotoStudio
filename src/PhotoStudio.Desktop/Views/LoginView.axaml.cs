using Avalonia.Controls;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class LoginView : BaseView
{
    public override string Title => "Добро пожаловать";
    public override string Hint => "Войдите в аккаунт или продолжите просмотр каталога";

    public LoginView(MainWindow w)
    {
        InitializeComponent();
        Setup(w);
        var login = new TextBox
        {
            PlaceholderText = "Логин"
        };
        var password = new TextBox
        {
            PasswordChar = '●',
            PlaceholderText = "Пароль"
        };
        var form = new StackPanel
        {
            Spacing = 20
        };
        form.Children.Add(Text("Вход в систему", 24, true));
        form.Children.Add(Field("Login", "Логин", login, 480));
        form.Children.Add(Field("Password", "Пароль", password, 480));
        form.Children.Add(Row(Action("LoginButton", "Войти", async () => await W.SignedIn(await W.Service.LoginAsync(login.Text ?? "", password.Text ?? "")), true), Action("RegisterButton", "Регистрация", () => W.Navigate(new RegisterView(W)))));
        form.Children.Add(Action("GuestButton", "Просмотреть каталог без входа", () => W.Navigate(new CatalogView(W))));
        Panel.Children.Add(Card(form));
        Panel.Children.Add(Text("Учебная версия • администрация, клиенты и фотографы", 14));
    }
}
