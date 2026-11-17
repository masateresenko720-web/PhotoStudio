using Avalonia.Controls;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class ReferenceView : BaseView
{
    public override string Title => "Управление справочниками";
    public override string Hint => "Залы, услуги и роли пользователей • только для администратора";

    private readonly ComboBox kind = Combo(new object[] { "Залы", "Услуги", "Пользователи" }), role = Combo(new object[] { Names.Client, Names.Photographer, Names.Admin });
    private readonly ListBox list = new()
    {
        Height = 215
    };
    private readonly TextBox name = new(), description = new();
    private readonly NumericUpDown price = new()
    {
        Minimum = 0,
        Maximum = 1000000,
        Value = 1000
    };
    private readonly CheckBox confirmDelete = new()
    {
        Content = "Подтверждаю удаление выбранной записи"
    };
    private int id;
    public ReferenceView(MainWindow w)
    {
        InitializeComponent();
        Setup(w);
        Track("ReferenceList", list);
        Panel.Children.Add(Row(Field("Kind", "Раздел", kind), Action("LoadButton", "Открыть", LoadAsync), Action("NewButton", "Новая запись", () =>
        {
            id = 0;
            name.Text = "";
            description.Text = "";
            price.Value = 1000;
            list.SelectedIndex = -1;
            return Task.CompletedTask;
        })));
        list.SelectionChanged += (_, _) =>
        {
            switch (list.SelectedItem)
            {
                case Hall h:
                    id = h.Id;
                    name.Text = h.Name;
                    price.Value = h.PricePerHour;
                    description.Text = h.Description;
                    break;
                case ExtraService e:
                    id = e.Id;
                    name.Text = e.Name;
                    price.Value = e.Price;
                    description.Text = "";
                    break;
                case User u:
                    id = u.Id;
                    name.Text = u.FullName;
                    role.SelectedItem = u.Role.Name;
                    break;
            }
        };
        Panel.Children.Add(Card(list));
        Panel.Children.Add(Card(new StackPanel { Spacing = 6, Children = { Row(Field("Name", "Название / пользователь", name), Field("Price", "Ставка / цена, ₽", price)), Field("Description", "Описание зала", description, 650), Row(Field("Role", "Роль для выбранного пользователя", role), Action("SaveButton", "Сохранить", SaveAsync, true)) } }));
        Panel.Children.Add(confirmDelete);
        Panel.Children.Add(Action("DeleteButton", "Удалить выбранное", async () =>
        {
            if (!confirmDelete.IsChecked.GetValueOrDefault() || id == 0)
                throw new AppException("Выберите запись и подтвердите удаление флажком.");
            await W.Service.DeleteAsync(W.Session!, kind.SelectedItem!.ToString()!, id);
            await LoadAsync();
            W.Notice("Запись удалена.");
        }));
    }

    public override async Task LoadAsync()
    {
        var s = W.Session ?? throw new AppException("Войдите.");
        if (s.Role != Names.Admin)
            throw new AppException("Нет доступа.");
        id = 0;
        list.ItemsSource = kind.SelectedIndex == 0 ? (IEnumerable<object>)await W.Service.CatalogAsync() : kind.SelectedIndex == 1 ? await W.Service.ExtrasAsync() : await W.Service.UsersAsync(s);
        list.SelectedIndex = 0;
    }

    private async Task SaveAsync()
    {
        if (kind.SelectedIndex == 0)
            await W.Service.SaveHallAsync(W.Session!, id, name.Text ?? "", price.Value ?? 0, description.Text ?? "");
        else if (kind.SelectedIndex == 1)
            await W.Service.SaveExtraAsync(W.Session!, id, name.Text ?? "", price.Value ?? 0);
        else
        {
            if (id == 0)
                throw new AppException("Выберите пользователя.");
            await W.Service.SetRoleAsync(W.Session!, id, role.SelectedItem!.ToString()!);
        }

        await LoadAsync();
        W.Notice("Изменения сохранены. При изменении роли пользователю нужно войти заново.");
    }
}
