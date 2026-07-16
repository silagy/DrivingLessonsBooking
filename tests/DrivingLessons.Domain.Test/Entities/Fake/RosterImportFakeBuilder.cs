using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public static class RosterImportFakeBuilder
{
    public static RosterImport Build()
    {
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entry = RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added);
        var failure = RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId);

        return RosterImport.Create(fileName, importedAtUtc, [entry], [failure]);
    }
}
