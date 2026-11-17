namespace PhotoStudio.Core;
public static class PriceRules
{
    public static void Validate(DateTime start, DateTime end)
    {
        bool Grid(DateTime t) => t.Ticks % TimeSpan.TicksPerMinute == 0 && t.Minute % 30 == 0;
        if (end <= start || start.Date != end.Date || start.TimeOfDay < TimeSpan.FromHours(9) || end.TimeOfDay > TimeSpan.FromHours(22) || !Grid(start) || !Grid(end))
            throw new AppException("Запись: один день, 09:00–22:00, шаг 30 минут; окончание позже начала.");
    }

    public static decimal Calculate(decimal rate, DateTime start, DateTime end, IEnumerable<(decimal Price, int Quantity)> extras)
    {
        Validate(start, end);
        if (rate <= 0 || rate > 1000000)
            throw new AppException("Некорректная ставка зала.");
        decimal total = 0;
        for (var t = start; t < end; t = t.AddMinutes(30))
        {
            decimal day = t.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1.20m : 1m;
            decimal hour = t.Hour >= 18 ? 1.15m : 1m;
            total += rate * .5m * day * hour;
        }

        foreach (var(price, quantity)in extras)
        {
            if (price < 0 || quantity is < 1 or > 100)
                throw new AppException("Количество услуги: от 1 до 100.");
            total += price * quantity;
        }

        total = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        if (total > 99999999.99m)
            throw new AppException("Превышена допустимая сумма.");
        return total;
    }

    public static bool Overlaps(DateTime a, DateTime b, DateTime c, DateTime d) => a < d && b > c;
}
