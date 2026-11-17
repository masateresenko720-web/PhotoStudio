using Avalonia.Controls;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class HallView : BaseView
{
    private readonly Hall hall;
    public override string Title => "Зал «" + hall.Name + "»";
    public override string Hint => "Карточка пространства и правила бронирования";

    public HallView(MainWindow w, Hall h)
    {
        hall = h;
        InitializeComponent();
        Setup(w);
        var image = CatalogView.Picture(h.Id);
        image.Width = 500;
        image.Height = 300;
        Panel.Children.Add(Card(new StackPanel { Spacing = 18, Children = { Row(image, new StackPanel { Width = 390, Spacing = 20, Children = { Text(h.Name, 28, true), Text(h.Description), Text($"Базовый тариф: {h.PricePerHour:N2} ₽ / час", 22, true), Text("Выходные × 1,20\nПосле 18:00 дополнительно × 1,15\nМинимум 30 минут • 09:00–22:00", 15) } }), Text("Иллюстрация: авторская схема зала. Финальная занятость проверяется при сохранении.", 13), Action("BookButton", w.Session?.Role == Names.Client ? "Выбрать время и услуги" : "Войти для бронирования", () => W.Navigate(w.Session?.Role == Names.Client ? new BookingView(w, h) : new LoginView(w)), true) } }));
    }
}
