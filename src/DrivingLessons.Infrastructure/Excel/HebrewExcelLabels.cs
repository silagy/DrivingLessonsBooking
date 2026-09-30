using DrivingLessons.Domain.Values;

namespace DrivingLessons.Infrastructure.Excel;

public static class HebrewExcelLabels
{
    public static string DayOf(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Sunday => "ראשון",
            DayOfWeek.Monday => "שני",
            DayOfWeek.Tuesday => "שלישי",
            DayOfWeek.Wednesday => "רביעי",
            DayOfWeek.Thursday => "חמישי",
            DayOfWeek.Friday => "שישי",
            _ => throw new ArgumentOutOfRangeException(nameof(day), day, null)
        };
    }

    public static string WindowOf(SlotWindowType window)
    {
        return window switch
        {
            SlotWindowType.Morning => "בוקר",
            SlotWindowType.Noon => "צהריים",
            SlotWindowType.Afternoon => "אחה״צ",
            SlotWindowType.Evening => "ערב",
            _ => throw new ArgumentOutOfRangeException(nameof(window), window, null)
        };
    }

    public static string TransmissionOf(Transmission transmission)
    {
        return transmission switch
        {
            Transmission.Automatic => "אוטומטי",
            Transmission.Manual => "ידני",
            _ => throw new ArgumentOutOfRangeException(nameof(transmission), transmission, null)
        };
    }

    public static string SessionTypeOf(SessionType sessionType)
    {
        return sessionType switch
        {
            SessionType.Single => "יחיד",
            SessionType.Double => "כפול",
            _ => throw new ArgumentOutOfRangeException(nameof(sessionType), sessionType, null)
        };
    }
}
