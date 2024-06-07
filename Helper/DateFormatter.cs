namespace PawPok.Helper;

public class DateFormatter
{
    public static string From(DateTime date)
    {
        DateTime now = DateTime.Now;
        TimeSpan diff = now - date;

        int seconds = (int)diff.TotalSeconds;
        int minutes = (int)diff.TotalMinutes;
        int hours = (int)diff.TotalHours;
        int days = (int)diff.TotalDays;
        int weeks = days / 7;
        int months = (now.Year - date.Year) * 12 + now.Month - date.Month;
        int years = now.Year - date.Year;

        if (seconds < 60)
            return $"{seconds}s ago";

        if (minutes < 60)
            return $"{minutes}m ago";

        if (hours < 24)
            return $"{hours}h ago";

        if (days < 7)
            return $"{days}d ago";

        if (weeks < 4)
            return $"{weeks}w ago";

        if (months < 12)
            return date.ToString("%d-%M");

        return date.ToString("%d-%M-%yyyy");
    }
}
