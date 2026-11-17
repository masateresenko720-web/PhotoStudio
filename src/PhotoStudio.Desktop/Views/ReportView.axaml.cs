using Avalonia.Controls;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class ReportView : BaseView
{
    public override string Title => "Отчёт по завершённым заказам";
    public override string Hint => "Выручка, часы съёмки и ретроспективная загрузка залов";

    private readonly TextBox from = new(), to = new();
    private readonly StackPanel results = new()
    {
        Spacing = 12
    };
    public ReportView(MainWindow w)
    {
        InitializeComponent();
        Setup(w);
        from.Text = w.Service.Clock.Now.Date.AddDays(-30).ToString("yyyy-MM-dd");
        to.Text = w.Service.Clock.Now.Date.ToString("yyyy-MM-dd");
        Panel.Children.Add(Card(Row(Field("From", "Период с, ГГГГ-ММ-ДД", from), Field("To", "Период по, ГГГГ-ММ-ДД", to), Action("ReportButton", "Сформировать", LoadAsync, true))));
        Panel.Children.Add(results);
    }

    public override async Task LoadAsync()
    {
        var rows = await W.Service.ReportAsync(W.Session ?? throw new AppException("Войдите."), Day(from), Day(to));
        results.Children.Clear();
        results.Children.Add(Text($"Заказов: {rows.Sum(x => x.Orders)}  •  Часов: {rows.Sum(x => x.Hours):N1}  •  Выручка: {rows.Sum(x => x.Revenue):N2} ₽", 23, true));
        foreach (var r in rows)
            results.Children.Add(Card(new StackPanel { Spacing = 10, Children = { Text(r.Hall, 21, true), Text($"Заказов: {r.Orders}    |    Часов: {r.Hours:N1}    |    Выручка: {r.Revenue:N2} ₽    |    Загрузка: {r.Occupancy:N2} %") } }));
        if (rows.Count == 0)
            results.Children.Add(Text("Завершённых заказов за период нет."));
    }
}
