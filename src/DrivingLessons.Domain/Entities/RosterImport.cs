using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class RosterImport : AggregateRoot<RosterImportId>
{
    private readonly List<RosterImportEntry> entries = [];
    private readonly List<RosterImportFailure> failures = [];

    public RosterFileName FileName { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
    public int AddedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int DeactivatedCount { get; private set; }
    public int FailedCount { get; private set; }

    public IReadOnlyCollection<RosterImportEntry> Entries => entries.AsReadOnly();
    public IReadOnlyCollection<RosterImportFailure> Failures => failures.AsReadOnly();

    private RosterImport()
    {
    }

    private RosterImport(
        RosterImportId id,
        RosterFileName fileName,
        DateTime importedAtUtc,
        IReadOnlyCollection<RosterImportEntry> entries,
        IReadOnlyCollection<RosterImportFailure> failures)
        : base(id)
    {
        FileName = fileName;
        ImportedAtUtc = importedAtUtc;
        this.entries.AddRange(entries);
        this.failures.AddRange(failures);
        AddedCount = entries.Count(x => x.Outcome is RosterEntryOutcome.Added);
        UpdatedCount = entries.Count(x => x.Outcome is RosterEntryOutcome.Updated);
        DeactivatedCount = entries.Count(x => x.Outcome is RosterEntryOutcome.Deactivated);
        FailedCount = failures.Count;

        var createdEvent = new RosterImportCreated(
            id,
            AddedCount,
            UpdatedCount,
            DeactivatedCount,
            FailedCount,
            importedAtUtc);
        AddEvent(createdEvent);
    }

    public static RosterImport Create(
        RosterFileName fileName,
        DateTime importedAtUtc,
        IReadOnlyCollection<RosterImportEntry> entries,
        IReadOnlyCollection<RosterImportFailure> failures)
    {
        var id = RosterImportId.New();

        return new RosterImport(id, fileName, importedAtUtc, entries, failures);
    }
}
