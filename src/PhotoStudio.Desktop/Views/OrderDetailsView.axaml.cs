using Avalonia.Controls;
using PhotoStudio.Core;

namespace PhotoStudio.Desktop.Views;
public partial class OrderDetailsView : BaseView
{
    private readonly int id;
    public override string Title => "Детали заказа № " + id;
    public override string Hint => "История заказа, услуги и доступные действия";

    public OrderDetailsView(MainWindow w, int orderId)
    {
        id = orderId;
        InitializeComponent();
        Setup(w);
    }

    public override async Task LoadAsync()
    {
        Panel.Children.Clear();
        var s = W.Session ?? throw new AppException("Войдите.");
        var o = await W.Service.DetailsAsync(s, id);
        Panel.Children.Add(Card(new StackPanel { Spacing = 14, Children = { Text(o.Hall.Name + " • " + o.Status.Name, 24, true), Text($"{o.Start:dd.MM.yyyy HH:mm} — {o.End:HH:mm}\nКлиент: {o.Client.FullName}\nФотограф: {o.Photographer?.FullName ?? "не назначен"}"), Text("Услуги: " + (o.Extras.Count == 0 ? "нет" : string.Join(", ", o.Extras.Select(x => $"{x.Service.Name} × {x.Quantity} ({x.UnitPrice:N2} ₽)")))), Text($"Стоимость: {o.Total:N2} ₽ • Оплата: {(o.StatusId is 2 or 3 ? "подтверждена администратором" : "не подтверждена")}", 21, true) } }));
        bool active = o.StatusId is 1 or 2;
        if (s.Role == Names.Admin && active)
        {
            var people = await W.Service.PhotographersAsync();
            var combo = Combo(new[] { new User { Id = 0, LastName = "Без фотографа" } }.Concat(people));
            combo.SelectedIndex = o.PhotographerId.HasValue ? people.FindIndex(x => x.Id == o.PhotographerId) + 1 : 0;
            Panel.Children.Add(Card(Row(Field("Photographer", "Назначенный фотограф", combo), Action("AssignButton", "Сохранить назначение", async () =>
            {
                int p = ((User)combo.SelectedItem!).Id;
                await W.Service.AssignAsync(s, id, p == 0 ? null : p, o.Version);
                await LoadAsync();
                W.Notice("Назначение сохранено.");
            }))));
            if (o.StatusId == 1)
            {
                var receipt = Track("PaymentReceived", new CheckBox { Content = "Оплата получена — подтверждаю как администратор" });
                Panel.Children.Add(receipt);
                Panel.Children.Add(Action("ConfirmButton", "Подтвердить бронь", async () =>
                {
                    await W.Service.ChangeStatusAsync(s, id, Names.Confirmed, o.Version, receipt.IsChecked == true);
                    await LoadAsync();
                    W.Notice("Бронирование подтверждено.");
                }, true));
            }
        }

        if (active && (s.Role == Names.Admin || s.Role == Names.Client))
        {
            var check = Track("ConfirmCancel", new CheckBox { Content = "Подтверждаю отмену бронирования" });
            Panel.Children.Add(check);
            Panel.Children.Add(Action("CancelButton", "Отменить бронь", async () =>
            {
                if (check.IsChecked != true)
                    throw new AppException("Подтвердите отмену флажком.");
                await W.Service.ChangeStatusAsync(s, id, Names.Cancelled, o.Version);
                await LoadAsync();
                W.Notice("Заказ отменён.");
            }));
        }

        if (o.StatusId == 2 && (s.Role == Names.Admin || s.Role == Names.Photographer))
            Panel.Children.Add(Action("CompleteButton", "Завершить съёмку", async () =>
            {
                await W.Service.ChangeStatusAsync(s, id, Names.Completed, o.Version);
                await LoadAsync();
                W.Notice("Съёмка завершена.");
            }, true));
    }
}
