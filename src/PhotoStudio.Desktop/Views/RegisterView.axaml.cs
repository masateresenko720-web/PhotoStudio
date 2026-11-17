using Avalonia.Controls;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class RegisterView : BaseView
{
    public override string Title => "Регистрация клиента";
    public override string Hint => "Новая учётная запись получает только роль клиента";

    public RegisterView(MainWindow w)
    {
        InitializeComponent();
        Setup(w);
        var last = new TextBox();
        var first = new TextBox();
        var phone = new TextBox
        {
            PlaceholderText = "+7 ..."
        };
        var login = new TextBox
        {
            PlaceholderText = "Латинские буквы и цифры"
        };
        var pass = new TextBox
        {
            PasswordChar = '●'
        };
        var repeat = new TextBox
        {
            PasswordChar = '●'
        };
        Panel.Children.Add(Card(new StackPanel { Spacing = 10, Children = { Row(Field("LastName", "Фамилия *", last), Field("FirstName", "Имя *", first)), Row(Field("Login", "Логин *", login), Field("Phone", "Телефон", phone)), Row(Field("Password", "Пароль, 12–128 символов *", pass), Field("RepeatPassword", "Повторите пароль *", repeat)), Action("SaveButton", "Создать аккаунт", async () =>
        {
            if (pass.Text != repeat.Text)
                throw new AppException("Пароли не совпадают.");
            await W.Service.RegisterAsync(login.Text ?? "", pass.Text ?? "", last.Text ?? "", first.Text ?? "", phone.Text ?? "");
            await W.Navigate(new LoginView(W), false, true);
            W.Notice("Аккаунт создан. Теперь войдите в систему.");
        }, true) } }));
    }
}
