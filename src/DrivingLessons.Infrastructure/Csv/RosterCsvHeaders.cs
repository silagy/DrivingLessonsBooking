namespace DrivingLessons.Infrastructure.Csv;

public static class RosterCsvHeaders
{
    public const string FullName = "שם מלא";
    public const string NationalId = "תעודת זהות";
    public const string Phone = "טלפון";
    public const string Teacher = "מורה";
    public const string Car = "רכב";
    public const string Address = "כתובת";
    public const string StartDate = "תאריך התחלה";
    public const string LicenseType = "סוג רישיון";

    public static readonly string[] Required =
    [
        FullName,
        NationalId,
        Phone,
        Teacher,
        Car
    ];
}
