using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace PhotoStudio.Core;
public sealed class StudioService(DbFactory factory, IClock clock)
{
    public DbFactory Factory => factory;
    public IClock Clock => clock;

    private static void Need(bool condition, string message)
    {
        if (!condition)
            throw new AppException(message);
    }

    private static async Task<User> Identity(StudioDb db, Session session)
    {
        var u = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Id == session.UserId);
        Need(u != null, "Войдите в систему заново.");
        return u!;
    }

    private static void Admin(User u) => Need(u.Role.Name == Names.Admin, "Операция доступна администратору.");
    private static void Visible(User u, Booking o) => Need(u.Role.Name == Names.Admin || (u.Role.Name == Names.Client && o.ClientId == u.Id) || (u.Role.Name == Names.Photographer && o.PhotographerId == u.Id), "Нет доступа к этому заказу.");
    private static void Version(Booking o, string expected)
    {
        Need(o.Version == expected, "Данные изменены. Обновите карточку.");

    }

    private static IQueryable<Booking> Full(StudioDb db) => db.Orders.Include(x => x.Hall).Include(x => x.Client).Include(x => x.Photographer).Include(x => x.Status).Include(x => x.Extras).ThenInclude(x => x.Service);
    private static async Task<IDbContextTransaction> Transaction(StudioDb db)
    {
        var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (db.Database.IsSqlServer())
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'PhotoStudio.Booking',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=15000; IF @r<0 THROW 50001,N'Ресурс занят; повторите операцию',1;");
        return tx;
    }

    public async Task<Session> LoginAsync(string login, string password)
    {
        await using var db = factory.Create();
        login = login.Trim().ToLowerInvariant();
        var u = await db.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.Login == login);
        if (u == null || !PasswordService.Verify(password, u.PasswordHash))
            throw new AppException("Неверный логин или пароль.");
        return new(u.Id, u.FullName, u.Role.Name);
    }

    public async Task RegisterAsync(string login, string password, string last, string first, string phone)
    {
        login = login.Trim().ToLowerInvariant();
        Need(login.Length is >= 3 and <= 50 && login.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'), "Логин: 3–50 латинских букв, цифр, точки, дефисы или подчёркивания.");
        Need(last.Trim().Length is> 0 and <= 60 && first.Trim().Length is> 0 and <= 60, "Введите имя и фамилию (до 60 символов).");
        Need(phone.Trim().Length <= 20, "Телефон слишком длинный.");
        string hash = PasswordService.Hash(password);
        await using var db = factory.Create();
        Need(!await db.Users.AnyAsync(x => x.Login == login), "Логин уже занят.");
        int role = await db.Roles.Where(x => x.Name == Names.Client).Select(x => x.Id).SingleAsync();
        db.Users.Add(new User { Login = login, PasswordHash = hash, LastName = last.Trim(), FirstName = first.Trim(), Phone = phone.Trim(), RoleId = role });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new AppException("Не удалось зарегистрировать: проверьте уникальность логина.");
        }
    }

    public async Task<List<Hall>> CatalogAsync(string search = "", decimal? maximum = null, string sort = "name", DateTime? start = null, DateTime? end = null)
    {
        if (start.HasValue != end.HasValue)
            throw new AppException("Укажите обе границы времени.");
        if (start.HasValue)
            PriceRules.Validate(start.Value, end!.Value);
        await using var db = factory.Create();
        var q = db.Halls.AsNoTracking();
        search = search.Trim();
        var rows = await q.ToListAsync();
        if (search != "")
            rows = rows.Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (maximum.HasValue)
            rows = rows.Where(x => x.PricePerHour <= maximum.Value).ToList();
        if (start.HasValue)
        {
            var busy = await db.Orders.Where(x => (x.StatusId == 1 || x.StatusId == 2) && x.Start < end!.Value && x.End > start.Value).Select(x => x.HallId).ToListAsync();
            rows = rows.Where(x => !busy.Contains(x.Id)).ToList();
        }

        return (sort == "price" ? rows.OrderBy(x => x.PricePerHour).ThenBy(x => x.Id) : rows.OrderBy(x => x.Name).ThenBy(x => x.Id)).ToList();
    }

    public async Task<List<ExtraService>> ExtrasAsync()
    {
        await using var db = factory.Create();
        return await db.Services.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    }

    public async Task<List<User>> PhotographersAsync()
    {
        await using var db = factory.Create();
        return await db.Users.Include(x => x.Role).AsNoTracking().Where(x => x.Role.Name == Names.Photographer).OrderBy(x => x.LastName).ToListAsync();
    }

    public async Task<decimal> QuoteAsync(BookingRequest r)
    {
        await using var db = factory.Create();
        var hall = await db.Halls.SingleAsync(x => x.Id == r.HallId);
        var ids = r.Services.Keys.ToArray();
        var extras = await db.Services.Where(x => ids.Contains(x.Id)).ToListAsync();
        Need(extras.Count == ids.Length, "Услуга не найдена.");
        return PriceRules.Calculate(hall.PricePerHour, r.Start, r.End, extras.Select(x => (x.Price, r.Services[x.Id])));
    }

    public async Task<int> CreateAsync(Session session, BookingRequest r, bool simulateFailure = false)
    {
        PriceRules.Validate(r.Start, r.End);
        Need(r.Start > clock.Now, "Нельзя создать бронь на прошлое время.");
        await using var db = factory.Create();
        await using var tx = await Transaction(db);
        var u = await Identity(db, session);
        Need(u.Role.Name == Names.Client, "Оформление доступно клиенту.");
        var hall = await db.Halls.SingleOrDefaultAsync(x => x.Id == r.HallId);
        Need(hall != null, "Зал не найден.");
        if (r.PhotographerId is int p)
            Need(await db.Users.AnyAsync(x => x.Id == p && x.Role.Name == Names.Photographer), "Выберите действующего фотографа.");
        Need(!await Conflict(db, r.HallId, r.PhotographerId, r.Start, r.End), "Зал или фотограф уже занят в это время.");
        var ids = r.Services.Keys.ToArray();
        var extras = await db.Services.Where(x => ids.Contains(x.Id)).ToListAsync();
        Need(extras.Count == ids.Length, "Услуга не найдена.");
        decimal total = PriceRules.Calculate(hall!.PricePerHour, r.Start, r.End, extras.Select(x => (x.Price, r.Services[x.Id])));
        var order = new Booking
        {
            ClientId = u.Id,
            HallId = hall.Id,
            PhotographerId = r.PhotographerId,
            Start = r.Start,
            End = r.End,
            OrderDate = clock.Now.Date,
            StatusId = Names.New
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        if (simulateFailure)
            throw new AppException("Имитированный сбой до записи услуг.");
        foreach (var s in extras)
            db.OrderServices.Add(new BookingExtra { BookingId = order.Id, ServiceId = s.Id, Quantity = r.Services[s.Id] });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return order.Id;
    }

    private static Task<bool> Conflict(StudioDb db, int hall, int? photographer, DateTime start, DateTime end, int exclude = 0) => db.Orders.AnyAsync(o => o.Id != exclude && (o.StatusId == Names.New || o.StatusId == Names.Confirmed) && o.Start < end && o.End > start && (o.HallId == hall || (photographer != null && o.PhotographerId == photographer)));
    public async Task<List<Booking>> OrdersAsync(Session session, string search = "")
    {
        await using var db = factory.Create();
        var u = await Identity(db, session);
        var q = Full(db).AsNoTracking();
        if (u.Role.Name == Names.Client)
            q = q.Where(x => x.ClientId == u.Id);
        else if (u.Role.Name == Names.Photographer)
            q = q.Where(x => x.PhotographerId == u.Id);
        else
            Admin(u);
        var rows = await q.OrderByDescending(x => x.Start).ToListAsync();
        search = search.Trim();
        if (search != "")
            rows = rows.Where(x => x.Id.ToString() == search || x.Hall.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return rows;
    }

    public async Task<Booking> DetailsAsync(Session session, int id)
    {
        await using var db = factory.Create();
        var u = await Identity(db, session);
        var o = await Full(db).AsNoTracking().SingleAsync(x => x.Id == id);
        Visible(u, o);
        return o;
    }

    public async Task ChangeStatusAsync(Session session, int id, int status, string expected, bool paymentReceived = false)
    {
        await using var db = factory.Create();
        await using var tx = await Transaction(db);
        var u = await Identity(db, session);
        var o = await db.Orders.SingleAsync(x => x.Id == id);
        Visible(u, o);
        if (status == Names.Cancelled)
        {
            Need(u.Role.Name is Names.Admin or Names.Client, "Фотограф не отменяет заказы.");
            Need(o.StatusId is 1 or 2, "Отмена недоступна.");
            Need(o.Start - clock.Now >= TimeSpan.FromHours(24), "До съёмки менее 24 часов: отмена недоступна.");
        }
        else if (status == Names.Confirmed)
        {
            Admin(u);
            Need(o.StatusId == 1 && paymentReceived, "Подтверждение: новый заказ и отметка администратора о получении оплаты.");
        }
        else if (status == Names.Completed)
        {
            Need(u.Role.Name == Names.Admin || (u.Role.Name == Names.Photographer && o.PhotographerId == u.Id), "Нет права завершить съёмку.");
            Need(o.StatusId == 2 && clock.Now >= o.End, "Завершить можно подтверждённую съёмку после её окончания.");
        }
        else
            throw new AppException("Недопустимый переход статуса.");
        Version(o, expected);
        o.StatusId = status;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task AssignAsync(Session session, int id, int? photographer, string expected)
    {
        await using var db = factory.Create();
        await using var tx = await Transaction(db);
        Admin(await Identity(db, session));
        var o = await db.Orders.SingleAsync(x => x.Id == id);
        Need(o.StatusId is 1 or 2, "Закрытый заказ не изменяется.");
        if (photographer.HasValue)
            Need(await db.Users.AnyAsync(x => x.Id == photographer && x.Role.Name == Names.Photographer), "Выберите фотографа.");
        Need(!await Conflict(db, o.HallId, photographer, o.Start, o.End, o.Id), "Фотограф уже занят.");
        Version(o, expected);
        o.PhotographerId = photographer;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<List<ReportRow>> ReportAsync(Session session, DateTime from, DateTime to)
    {
        Need(to.Date >= from.Date, "Некорректный период.");
        await using var db = factory.Create();
        Admin(await Identity(db, session));
        var end = to.Date.AddDays(1);
        var rows = await Full(db).AsNoTracking().Where(x => x.StatusId == 3 && x.Start >= from.Date && x.Start < end).ToListAsync();
        decimal capacity = (decimal)(end - from.Date).TotalDays * 13;
        return rows.GroupBy(x => x.Hall.Name).Select(g => new ReportRow(g.Key, g.Count(), g.Sum(x => (decimal)(x.End - x.Start).TotalHours), g.Sum(x => x.Total), decimal.Round(g.Sum(x => (decimal)(x.End - x.Start).TotalHours) / capacity * 100, 2))).OrderBy(x => x.Hall).ToList();
    }

    public async Task<List<User>> UsersAsync(Session s)
    {
        await using var db = factory.Create();
        Admin(await Identity(db, s));
        return await db.Users.Include(x => x.Role).AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    }

    public async Task SaveHallAsync(Session session, int id, string name, decimal price, string description)
    {
        Need(name.Trim().Length is> 0 and <= 100 && description.Length <= 1000 && price is> 0 and <= 1000000 && decimal.Round(price, 2) == price, "Проверьте название, описание и положительную цену.");
        await using var db = factory.Create();
        await using var tx = await Transaction(db);
        Admin(await Identity(db, session));
        Need(!await db.Halls.AnyAsync(x => x.Name == name.Trim() && x.Id != id), "Название уже существует.");
        var h = id == 0 ? new Hall() : await db.Halls.SingleAsync(x => x.Id == id);
        Need(id == 0 || h.PricePerHour == price || !await db.Orders.AnyAsync(x => x.HallId == id), "Цена использованного зала зафиксирована. Создайте новый зал для нового тарифа.");
        h.Name = name.Trim();
        h.PricePerHour = price;
        h.Description = description;
        if (id == 0)
            db.Halls.Add(h);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task SaveExtraAsync(Session session, int id, string name, decimal price)
    {
        Need(name.Trim().Length is> 0 and <= 100 && price is >= 0 and <= 1000000 && decimal.Round(price, 2) == price, "Проверьте наименование и цену услуги.");
        await using var db = factory.Create();
        await using var tx = await Transaction(db);
        Admin(await Identity(db, session));
        Need(!await db.Services.AnyAsync(x => x.Name == name.Trim() && x.Id != id), "Название уже существует.");
        var e = id == 0 ? new ExtraService() : await db.Services.SingleAsync(x => x.Id == id);
        Need(id == 0 || e.Price == price || !await db.OrderServices.AnyAsync(x => x.ServiceId == id), "Цена использованной услуги зафиксирована. Создайте новую услугу для нового тарифа.");
        e.Name = name.Trim();
        e.Price = price;
        if (id == 0)
            db.Services.Add(e);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task SetRoleAsync(Session session, int userId, string role)
    {
        await using var db = factory.Create();
        await using var tx = await Transaction(db);
        Admin(await Identity(db, session));
        var u = await db.Users.Include(x => x.Role).SingleAsync(x => x.Id == userId);
        var r = await db.Roles.SingleOrDefaultAsync(x => x.Name == role);
        Need(r != null, "Роль не найдена.");
        if (u.Role.Name == Names.Admin && role != Names.Admin)
            Need(await db.Users.CountAsync(x => x.Role.Name == Names.Admin) > 1, "Нельзя убрать последнего администратора.");
        if (u.Role.Name == Names.Photographer && role != Names.Photographer)
            Need(!await db.Orders.AnyAsync(x => x.PhotographerId == u.Id && (x.StatusId == 1 || x.StatusId == 2)), "У фотографа есть активные назначения.");
        u.RoleId = r!.Id;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task DeleteAsync(Session session, string kind, int id)
    {
        await using var db = factory.Create();
        Admin(await Identity(db, session));
        if (kind == "Залы")
        {
            Need(!await db.Orders.AnyAsync(x => x.HallId == id), "Зал связан с заказами и не может быть удалён.");
            db.Halls.Remove(await db.Halls.SingleAsync(x => x.Id == id));
        }
        else if (kind == "Услуги")
        {
            Need(!await db.OrderServices.AnyAsync(x => x.ServiceId == id), "Услуга связана с заказами и не может быть удалена.");
            db.Services.Remove(await db.Services.SingleAsync(x => x.Id == id));
        }
        else
            throw new AppException("Удаление пользователей не предусмотрено: измените роль.");
        await db.SaveChangesAsync();
    }
}
