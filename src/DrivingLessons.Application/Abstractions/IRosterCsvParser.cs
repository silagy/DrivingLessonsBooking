namespace DrivingLessons.Application.Abstractions;

public interface IRosterCsvParser
{
    IReadOnlyList<RosterCsvRow> Parse(Stream content);
}
