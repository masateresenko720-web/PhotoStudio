using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class CatalogView : BaseView
{
    public override string Title => "Каталог съёмочных залов";
    public override string Hint => "Подберите пространство, проверьте дату и свободное время";

    private readonly TextBox search = new(), maximum = new(), day = new(), start = new()
    {
        Text = "10:00"
    }, end = new()
    {
        Text = "11:00"
    };
    private readonly CheckBox free = new()
    {
        Content = "Только свободные"
    };
    private readonly ComboBox sort = Combo(new object[] { "Название", "Цена по возрастанию" });
    private readonly StackPanel list = new()
    {
        Spacing = 12
    };
    private readonly TextBlock count = Text("");
    public CatalogView(MainWindow w)
    {
        InitializeComponent();
        Setup(w);
        day.Text = w.Service.Clock.Now.Date.AddDays(3).ToString("yyyy-MM-dd");
        Panel.Children.Add(Card(new StackPanel { Spacing = 6, Children = { Row(Field("Search", "Название или описание", search), Field("Maximum", "Максимальная ставка, ₽/час", maximum), Field("Sort", "Сортировать", sort)), Row(Field("Day", "Дата, ГГГГ-ММ-ДД", day, 220), Field("Start", "С", start, 130), Field("End", "По", end, 130), Track("Free", free)), Row(Action("ApplyButton", "Применить", LoadAsync, true), Action("ResetButton", "Сбросить", async () =>
        {
            search.Text = "";
            maximum.Text = "";
            free.IsChecked = false;
            sort.SelectedIndex = 0;
            await LoadAsync();
        })) } }));
        Panel.Children.Add(count);
        Panel.Children.Add(list);
    }

    public override async Task LoadAsync()
    {
        decimal? max = null;
        if (!string.IsNullOrWhiteSpace(maximum.Text))
        {
            if (!decimal.TryParse(maximum.Text, out var v) || v < 0)
                throw new AppException("Введите неотрицательную максимальную цену.");
            max = v;
        }

        var halls = await W.Service.CatalogAsync(search.Text ?? "", max, sort.SelectedIndex == 1 ? "price" : "name", free.IsChecked == true ? At(day, start) : null, free.IsChecked == true ? At(day, end) : null);
        list.Children.Clear();
        count.Text = halls.Count == 0 ? "Залы не найдены" : "Найдено залов: " + halls.Count;
        foreach (var hall in halls)
        {
            var image = Picture(hall.Id);
            image.Width = 210;
            image.Height = 120;
            image.Margin = new Thickness(0, 0, 24, 0);
            var info = new StackPanel
            {
                Width = 570,
                Spacing = 10,
                Children =
                {
                    Text(hall.Name, 22, true),
                    Text(hall.Description, 14),
                    Text($"{hall.PricePerHour:N0} ₽ / час • базовая ставка", 18, true)
                }
            };
            list.Children.Add(Card(Row(image, info, Action("Hall_" + hall.Id, "Подробнее", () => W.Navigate(new HallView(W, hall))))));
        }
    }

    public static Image Picture(int id)
    {
        var path = $"avares://PhotoStudio.Desktop/Assets/HALL-{id:00}.png";
        if (!AssetLoader.Exists(new Uri(path)))
            path = "avares://PhotoStudio.Desktop/Assets/picture.png";
        return new Image
        {
            Source = new Bitmap(AssetLoader.Open(new Uri(path))),
            Stretch = Stretch.UniformToFill
        };
    }
}
