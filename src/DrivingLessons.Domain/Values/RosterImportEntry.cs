namespace DrivingLessons.Domain.Values;

public record RosterImportEntry
{
    public NationalId NationalId { get; }
    public RosterEntryOutcome Outcome { get; }

    private RosterImportEntry(NationalId nationalId, RosterEntryOutcome outcome)
    {
        NationalId = nationalId;
        Outcome = outcome;
    }

    public static RosterImportEntry Of(NationalId nationalId, RosterEntryOutcome outcome)
    {
        return new RosterImportEntry(nationalId, outcome);
    }
}
