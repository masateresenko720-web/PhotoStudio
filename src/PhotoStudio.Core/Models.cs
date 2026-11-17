namespace PhotoStudio.Core;
public static class Names
{
    public const string Admin = "Администратор", Client = "Клиент", Photographer = "Фотограф";
    public const int New = 1, Confirmed = 2, Completed = 3, Cancelled = 4;
}

public sealed record Session(int UserId, string FullName, string Role);
public interface IClock
{
    DateTime Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}

public sealed class FixedClock(DateTime now) : IClock
{
    public DateTime Now { get; set; } = now;
}

public sealed class AppException(string message) : Exception(message);
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Status
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null !;
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

    public override string ToString() => FullName;
}

public class Hall
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal PricePerHour { get; set; }
    public string Description { get; set; } = "";

    public override string ToString() => Name;
}

public class ExtraService
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }

    public override string ToString() => $"{Name} — {Price:N2} ₽";
}

public class Booking
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public int ClientId { get; set; }
    public User Client { get; set; } = null !;
    public int HallId { get; set; }
    public Hall Hall { get; set; } = null !;
    public int? PhotographerId { get; set; }
    public User? Photographer { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    // Эти значения вычисляются и не добавляют столбцы в исходную ER-модель.
    public decimal Total => PriceRules.Calculate(Hall.PricePerHour, Start, End,
        Extras.Select(x => (x.Service.Price, x.Quantity)));
    public string Version => $"{StatusId}:{PhotographerId}:{Start.Ticks}:{End.Ticks}";
    public int StatusId { get; set; }
    public Status Status { get; set; } = null !;
    public List<BookingExtra> Extras { get; set; } = [];

    public override string ToString() => $"№ {Id}   •   {Hall?.Name}   •   {Start:dd.MM.yyyy HH:mm}–{End:HH:mm}   •   {Total:N2} ₽   •   {Status?.Name}";
}

public class BookingExtra
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null !;
    public int ServiceId { get; set; }
    public ExtraService Service { get; set; } = null !;
    public int Quantity { get; set; }
    public decimal UnitPrice => Service.Price;
}

public sealed record BookingRequest(int HallId, int? PhotographerId, DateTime Start, DateTime End, Dictionary<int, int> Services);
public sealed record ReportRow(string Hall, int Orders, decimal Hours, decimal Revenue, decimal Occupancy);
