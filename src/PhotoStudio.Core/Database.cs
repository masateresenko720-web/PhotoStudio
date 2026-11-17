using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;

namespace PhotoStudio.Core;
public sealed class StudioSettings
{
    public static string UserConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoStudio", "connection.json");
    public string Provider { get; set; } = "SqlServer";
    public string ConnectionString { get; set; } = "";
    public string DataDirectory { get; set; } = "";

    public static StudioSettings Load(string? file = null)
    {
        bool useUserConfig = file == null;
        file ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (useUserConfig && File.Exists(UserConfigPath)) file = UserConfigPath;
        var s = File.Exists(file) ? JsonSerializer.Deserialize<StudioSettings>(File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new() : new();
        string? provider = Environment.GetEnvironmentVariable("PHOTOSTUDIO_PROVIDER");
        string? connection = Environment.GetEnvironmentVariable("PHOTOSTUDIO_CONNECTION");
        if (!string.IsNullOrWhiteSpace(provider))
            s.Provider = provider;
        if (!string.IsNullOrWhiteSpace(connection))
            s.ConnectionString = connection;
        if (string.IsNullOrWhiteSpace(s.DataDirectory))
            s.DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoStudio");
        if (string.IsNullOrWhiteSpace(s.ConnectionString))
            s.ConnectionString = "Server=localhost;Database=PhotoStudio;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10";
        s.Provider = "SqlServer";

        return s;
    }

    public void SaveConnection()
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionString);
        if (!connection.IntegratedSecurity)
            throw new AppException("Для сохранения настройки выберите проверку подлинности Windows. Пароль SQL Server в файл не сохраняется.");
        Directory.CreateDirectory(Path.GetDirectoryName(UserConfigPath)!);
        string temporary = UserConfigPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, UserConfigPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

public sealed class StudioDb(DbContextOptions<StudioDb> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<ExtraService> Services => Set<ExtraService>();
    public DbSet<Booking> Orders => Set<Booking>();
    public DbSet<BookingExtra> OrderServices => Set<BookingExtra>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Role>().ToTable("roles");
        b.Entity<Status>().ToTable("statuses");
        b.Entity<User>().ToTable("users");
        b.Entity<Hall>().ToTable("halls");
        b.Entity<ExtraService>().ToTable("services");
        b.Entity<Booking>().ToTable("orders");
        b.Entity<BookingExtra>().ToTable("order_services");
        b.Entity<Role>().Property(x => x.Id).HasColumnName("role_id");
        b.Entity<Role>().Property(x => x.Name).HasColumnName("role_name").HasMaxLength(50);
        b.Entity<Status>().Property(x => x.Id).HasColumnName("status_id");
        b.Entity<Status>().Property(x => x.Name).HasColumnName("status_name").HasMaxLength(50);
        b.Entity<User>().Ignore(x => x.FullName);
        var u = b.Entity<User>();
        u.Property(x => x.Id).HasColumnName("user_id");
        u.Property(x => x.Login).HasColumnName("login").HasMaxLength(50);
        u.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        u.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(60);
        u.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(60);
        u.Property(x => x.MiddleName).HasColumnName("middle_name").HasMaxLength(60);
        u.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(20);
        u.Property(x => x.RoleId).HasColumnName("role_id");
        u.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        var h = b.Entity<Hall>();
        h.Property(x => x.Id).HasColumnName("hall_id");
        h.Property(x => x.Name).HasColumnName("hall_name").HasMaxLength(100);
        h.Property(x => x.PricePerHour).HasColumnName("price_per_hour");
        h.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        var s = b.Entity<ExtraService>();
        s.Property(x => x.Id).HasColumnName("service_id");
        s.Property(x => x.Name).HasColumnName("service_name").HasMaxLength(100);
        s.Property(x => x.Price).HasColumnName("price");
        var o = b.Entity<Booking>();
        o.Property(x => x.Id).HasColumnName("order_id");
        o.Property(x => x.OrderDate).HasColumnName("order_date");
        o.Property(x => x.Start).HasColumnName("start_time");
        o.Property(x => x.End).HasColumnName("end_time");
        o.Property(x => x.ClientId).HasColumnName("client_id");
        o.Property(x => x.HallId).HasColumnName("hall_id");
        o.Property(x => x.PhotographerId).HasColumnName("photographer_id");
        o.Property(x => x.StatusId).HasColumnName("status_id");
        o.Ignore(x => x.Total);
        o.Ignore(x => x.Version);
        o.Property(x => x.OrderDate).HasColumnType("date").HasDefaultValueSql("CONVERT(date,GETDATE())");
        o.Property(x => x.Start).HasColumnType("datetime");
        o.Property(x => x.End).HasColumnType("datetime");
        o.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.Restrict);
        o.HasOne(x => x.Photographer).WithMany().HasForeignKey(x => x.PhotographerId).OnDelete(DeleteBehavior.Restrict);
        o.HasOne(x => x.Hall).WithMany().HasForeignKey(x => x.HallId).OnDelete(DeleteBehavior.Restrict);
        o.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
        o.HasIndex(x => new { x.HallId, x.Start, x.End, x.StatusId });
        o.HasIndex(x => new { x.PhotographerId, x.Start, x.End, x.StatusId });
        var e = b.Entity<BookingExtra>();
        e.Property(x => x.Id).HasColumnName("order_service_id");
        e.Property(x => x.BookingId).HasColumnName("order_id");
        e.Property(x => x.ServiceId).HasColumnName("service_id");
        e.Property(x => x.Quantity).HasColumnName("quantity");
        e.Ignore(x => x.UnitPrice);
        e.HasOne(x => x.Booking).WithMany(x => x.Extras).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.BookingId, x.ServiceId }).IsUnique();
        foreach (var t in b.Model.GetEntityTypes())
            foreach (var prop in t.GetProperties())
            {
                if (prop.ClrType == typeof(string))
                    prop.SetIsUnicode(false);
                if (prop.ClrType == typeof(decimal))
                {
                    prop.SetPrecision(10);
                    prop.SetScale(2);
                }
            }

        b.Entity<Role>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Status>().HasIndex(x => x.Name).IsUnique();
        u.HasIndex(x => x.Login).IsUnique();
        h.HasIndex(x => x.Name).IsUnique();
        s.HasIndex(x => x.Name).IsUnique();
        h.ToTable(t => t.HasCheckConstraint("CK_HallPrice", "price_per_hour > 0"));
        s.ToTable(t => t.HasCheckConstraint("CK_ServicePrice", "price >= 0"));
        e.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ExtraQuantity", "quantity BETWEEN 1 AND 100");

        });
        o.ToTable(t =>
        {
            t.HasCheckConstraint("CK_OrderInterval", "end_time > start_time");



        });
    }
}

public sealed class DbFactory(StudioSettings settings, DbContextOptions<StudioDb>? testOptions = null)
{
    public StudioSettings Settings => settings;

    public StudioDb Create()
    {
        if (testOptions != null) return new StudioDb(testOptions);
        var b = new DbContextOptionsBuilder<StudioDb>();
        b.UseSqlServer(settings.ConnectionString);
        return new(b.Options);
    }

    public async Task InitializeAsync(IClock clock)
    {
        await using var db = Create();
        if (testOptions != null) return;
        if (!await db.Database.CanConnectAsync())
            throw new AppException("Нет подключения к SQL Server. Выполните SETUP_DATABASE.bat и проверьте appsettings.json.");
        await DatabaseSetup.ValidateAsync(db);
        // Database First: готовая база создаётся SQL-скриптами, а не при открытии форм.
        await db.Roles.AsNoTracking().ToListAsync();
        await db.Statuses.AsNoTracking().ToListAsync();
    }
}
