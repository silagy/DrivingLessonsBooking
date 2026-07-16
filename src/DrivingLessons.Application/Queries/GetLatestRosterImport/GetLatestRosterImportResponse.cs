using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetLatestRosterImport;

public class GetLatestRosterImportResponse
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTime ImportedAtUtc { get; init; }
    public int Added { get; init; }
    public int Updated { get; init; }
    public int Deactivated { get; init; }
    public int Failed { get; init; }
    public IReadOnlyCollection<EntryForGetLatestRosterImportResponse> Entries { get; init; } = [];
    public IReadOnlyCollection<FailureForGetLatestRosterImportResponse> Failures { get; init; } = [];

    public static Expression<Func<RosterImport, GetLatestRosterImportResponse>> Selector =>
        x => new GetLatestRosterImportResponse
        {
            Id = x.Id.Value,
            FileName = x.FileName.Value,
            ImportedAtUtc = x.ImportedAtUtc,
            Added = x.AddedCount,
            Updated = x.UpdatedCount,
            Deactivated = x.DeactivatedCount,
            Failed = x.FailedCount,
            Entries = x.Entries
                       .Select(entry => new EntryForGetLatestRosterImportResponse
                       {
                           NationalId = entry.NationalId.Value,
                           Outcome = entry.Outcome
                       })
                       .ToList(),
            Failures = x.Failures
                        .OrderBy(failure => failure.RowNumber)
                        .Select(failure => new FailureForGetLatestRosterImportResponse
                        {
                            RowNumber = failure.RowNumber,
                            StudentName = failure.StudentName,
                            Reason = failure.Reason
                        })
                        .ToList()
        };
}

public class EntryForGetLatestRosterImportResponse
{
    public string NationalId { get; init; } = string.Empty;
    public RosterEntryOutcome Outcome { get; init; }
}

public class FailureForGetLatestRosterImportResponse
{
    public int RowNumber { get; init; }
    public string? StudentName { get; init; }
    public RosterRowFailureReason Reason { get; init; }
}
