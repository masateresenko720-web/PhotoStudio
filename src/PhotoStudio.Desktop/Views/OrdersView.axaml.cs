using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class OrdersView : BaseView
{
    public override string Title => W.Session?.Role == Names.Photographer ? "Мои съёмки" : W.Session?.Role == Names.Client ? "Мои заказы" : "Заказы фотостудии";
    public override string Hint => "Доступные записи и актуальные статусы бронирований";

    private readonly TextBox search = new();
    private readonly ListBox list = new()
    {
        MinHeight = 280,
        MaxHeight = 480
    };
    public OrdersView(MainWindow w)
    {
        InitializeComponent();
        Setup(w);
        Track("OrdersList", list);
        list.ItemTemplate = new FuncDataTemplate<Booking>((o, _) => new StackPanel { Spacing = 7, Margin = new Avalonia.Thickness(10), Children = { Text($"№ {o!.Id}   •   {o.Hall.Name}   •   {o.Status.Name}", 18, true), Text($"{o.Start:dd.MM.yyyy HH:mm} — {o.End:HH:mm}    |    {o.Total:N2} ₽    |    {o.Client.FullName}", 15) } });
        Panel.Children.Add(Card(Row(Field("Search", "Поиск по номеру или залу", search, 600), Action("ApplyButton", "Найти", LoadAsync))));
        Panel.Children.Add(Card(list));
        Panel.Children.Add(Action("DetailsButton", "Открыть детали", () => list.SelectedItem is Booking o ? W.Navigate(new OrderDetailsView(W, o.Id)) : throw new AppException("Выберите заказ."), true));
    }

    public override async Task LoadAsync()
    {
        var rows = await W.Service.OrdersAsync(W.Session ?? throw new AppException("Войдите."), search.Text ?? "");
        list.ItemsSource = rows;
        list.SelectedIndex = rows.Count > 0 ? 0 : -1;
        if (rows.Count == 0)
            W.Notice("Заказы не найдены.");
    }
}
