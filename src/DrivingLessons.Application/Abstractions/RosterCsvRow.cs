namespace DrivingLessons.Application.Abstractions;

public class RosterCsvRow
{
    public int RowNumber { get; init; }
    public string? FullName { get; init; }
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? TeacherName { get; init; }
    public string? CarName { get; init; }
    public string? Address { get; init; }
    public string? StartDate { get; init; }
    public string? LicenseType { get; init; }
}
