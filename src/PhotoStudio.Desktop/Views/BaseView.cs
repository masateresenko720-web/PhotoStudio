using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public abstract class BaseView : UserControl
{
    protected MainWindow W = null !;
    protected StackPanel Panel = null !;
    public abstract string Title { get; }
    public virtual string Hint => "Информационная система фотостудии";
    public virtual bool IsDirty => false;
    public Dictionary<string, Control> Fields { get; } = [];

    protected void Setup(MainWindow window)
    {
        W = window;
        Panel = this.FindControl<StackPanel>("Body")!;
    }

    public virtual Task LoadAsync() => Task.CompletedTask;
    public T Get<T>(string id)
        where T : Control => (T)Fields[id];
    protected T Track<T>(string id, T c)
        where T : Control
    {
        c.Name = id;
        Fields[id] = c;
        return c;
    }

    protected StackPanel Field(string id, string label, Control c, double width = 310)
    {
        Track(id, c);
        c.HorizontalAlignment = HorizontalAlignment.Stretch;
        return new StackPanel
        {
            Width = width,
            Spacing = 7,
            Children =
            {
                new TextBlock
                {
                    Text = label,
                    FontSize = 14,
                    Foreground = Brush.Parse("#64748B")
                },
                c
            }
        };
    }

    protected Button Action(string id, string text, Func<Task> action, bool accent = false)
    {
        var b = Track(id, new Button { Content = text });
        if (accent)
            b.Classes.Add("accent");
        b.Click += (_, _) =>
        {
            if (!b.IsEnabled)
                return;
            W.Execute(async () =>
            {
                b.IsEnabled = false;
                try
                {
                    await action();
                }
                finally
                {
                    b.IsEnabled = true;
                }
            });
        };
        return b;
    }

    protected static WrapPanel Row(params Control[] c)
    {
        var r = new WrapPanel
        {
            Orientation = Orientation.Horizontal
        };
        foreach (var x in c)
        {
            x.Margin = new Thickness(0, 0, 18, 15);
            r.Children.Add(x);
        }

        return r;
    }

    protected static Border Card(Control c) => new()
    {
        Background = Brushes.White,
        CornerRadius = new CornerRadius(9),
        Padding = new Thickness(22),
        Margin = new Thickness(0, 0, 0, 12),
        BorderBrush = Brush.Parse("#DFE5EC"),
        BorderThickness = new Thickness(1),
        Child = c
    };
    protected static TextBlock Text(string s, int size = 16, bool bold = false) => new()
    {
        Text = s,
        FontSize = size,
        FontWeight = bold ? FontWeight.Bold : FontWeight.Normal
    };
    protected static DateTime Day(TextBox b)
    {
        if (!DateTime.TryParseExact(b.Text, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
            throw new AppException("Дата вводится как ГГГГ-ММ-ДД.");
        return d;
    }

    protected static DateTime At(TextBox day, TextBox time)
    {
        if (!TimeSpan.TryParseExact(time.Text, "hh\\:mm", System.Globalization.CultureInfo.InvariantCulture, out var t))
            throw new AppException("Время вводится как ЧЧ:ММ.");
        return Day(day) + t;
    }

    protected static ComboBox Combo(IEnumerable<object> items) => new()
    {
        ItemsSource = items,
        SelectedIndex = 0,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };
}
