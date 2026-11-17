using Avalonia.Controls;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class BookingView : BaseView
{
    private readonly Hall hall;
    private readonly TextBox day = new(), start = new()
    {
        Text = "17:00"
    }, end = new()
    {
        Text = "19:00"
    };
    private readonly ComboBox photographer = new()
    {
        MinWidth = 300
    };
    private readonly Dictionary<int, NumericUpDown> quantities = [];
    private readonly TextBlock quote = Text("Нажмите «Рассчитать стоимость»", 24, true);
    private bool dirty;
    public override bool IsDirty => dirty;
    public override string Title => "Бронирование • " + hall.Name;
    public override string Hint => "Заполните интервал и выберите дополнительные услуги";

    public BookingView(MainWindow w, Hall h)
    {
        hall = h;
        InitializeComponent();
        Setup(w);
        day.Text = w.Service.Clock.Now.Date.AddDays(3).ToString("yyyy-MM-dd");
        Panel.Children.Add(Card(new StackPanel { Spacing = 8, Children = { Row(Field("Day", "Дата, ГГГГ-ММ-ДД", day), Field("Photographer", "Фотограф (необязательно)", photographer)), Row(Field("Start", "Начало, ЧЧ:ММ", start), Field("End", "Окончание, ЧЧ:ММ", end)), Text("Дополнительные услуги • количество 0 означает «не добавлять»", 18, true) } }));
        foreach (var b in new[]
        {
            day,
            start,
            end
        }

        )
            b.TextChanged += (_, _) => dirty = true;
        photographer.SelectionChanged += (_, _) => dirty = true;
    }

    public override async Task LoadAsync()
    {
        var people = await W.Service.PhotographersAsync();
        photographer.ItemsSource = new[]
        {
            new User
            {
                Id = 0,
                LastName = "Без фотографа"
            }
        }.Concat(people);
        photographer.SelectedIndex = 0;
        var panel = new StackPanel
        {
            Spacing = 12
        };
        foreach (var extra in await W.Service.ExtrasAsync())
        {
            var n = Track("Qty_" + extra.Id, new NumericUpDown { Minimum = 0, Maximum = 100, Value = 0, Increment = 1, FormatString = "0", Width = 120 });
            n.ValueChanged += (_, _) => dirty = true;
            quantities[extra.Id] = n;
            panel.Children.Add(Row(Text($"{extra.Name} • {extra.Price:N2} ₽", 17), n));
        }

        Panel.Children.Add(Card(panel));
        Panel.Children.Add(quote);
        Panel.Children.Add(Row(Action("QuoteButton", "Рассчитать стоимость", async () => quote.Text = "Итого: " + (await W.Service.QuoteAsync(Request())).ToString("N2") + " ₽"), Action("SaveButton", "Сохранить бронирование", async () =>
        {
            var id = await W.Service.CreateAsync(W.Session ?? throw new AppException("Войдите в систему."), Request());
            dirty = false;
            await W.Navigate(new OrdersView(W), false, true);
            W.Notice($"Заказ № {id} создан.");
        }, true)));
        Panel.Children.Add(Text("Отмена возможна не позднее чем за 24 часа. Подтверждение выполняет администратор после внесения предоплаты.", 14));
        dirty = false;
    }

    private BookingRequest Request() => new(hall.Id, (photographer.SelectedItem as User)?.Id is> 0 ? ((User)photographer.SelectedItem!).Id : null, At(day, start), At(day, end), quantities.Where(x => x.Value.Value > 0).ToDictionary(x => x.Key, x => (int)x.Value.Value!.Value));
}
