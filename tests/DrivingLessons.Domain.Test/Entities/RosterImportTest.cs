using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class RosterImportTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entry = RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added);
        var failure = RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId);

        //when
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, [entry], [failure]);

        //then
        rosterImport.FileName.ShouldBe(fileName);
        rosterImport.ImportedAtUtc.ShouldBe(importedAtUtc);
        rosterImport.Entries.ShouldContain(entry);
        rosterImport.Failures.ShouldContain(failure);
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entry = RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added);
        var failure = RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId);

        //when
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, [entry], [failure]);

        //then
        rosterImport
            .UncommittedEvents
            .OfType<RosterImportCreated>()
            .Where(x => x.RosterImportId == rosterImport.Id
                        && x.AddedCount == 1
                        && x.UpdatedCount == 0
                        && x.DeactivatedCount == 0
                        && x.FailedCount == 1
                        && x.ImportedAtUtc == importedAtUtc)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Counts_Are_Derived_From_Entries_And_Failures()
    {
        //given
        var fileName = RosterFileName.Of(Faker.FakeString());
        var importedAtUtc = Faker.FakeUtcDate();
        var entries = new List<RosterImportEntry>
        {
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added),
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Added),
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Updated),
            RosterImportEntry.Of(Faker.FakeNationalId(), RosterEntryOutcome.Deactivated)
        };
        var failures = new List<RosterImportFailure>
        {
            RosterImportFailure.Of(1, Faker.FakeString(), RosterRowFailureReason.InvalidNationalId),
            RosterImportFailure.Of(2, null, RosterRowFailureReason.UnknownTeacher)
        };

        //when
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, entries, failures);

        //then
        rosterImport.AddedCount.ShouldBe(2);
        rosterImport.UpdatedCount.ShouldBe(1);
        rosterImport.DeactivatedCount.ShouldBe(1);
        rosterImport.FailedCount.ShouldBe(2);
    }
}
