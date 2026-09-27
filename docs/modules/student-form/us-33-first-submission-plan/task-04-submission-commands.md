# Task 4 of 11: Submission commands — create and revise interactors (TDD)

> Part of [US-33…US-41: First Submission](README.md). Requires task 3 complete. Work on branch `33-us-33-34-35-36-37-39-40-41-first-submission`, commands from the repo root.

**Files:**
- Create: `src\DrivingLessons.Application\Commands\Common\SlotRequestForSubmissionRequest.cs`
- Create: `src\DrivingLessons.Application\Commands\Common\SubmissionContext.cs`, `SubmissionContextResolver.cs`
- Create: `src\DrivingLessons.Application\Commands\CreateSubmission\CreateSubmissionRequest.cs`, `CreateSubmissionInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\ReviseSubmission\ReviseSubmissionRequest.cs`, `ReviseSubmissionInteractor.cs`
- Create: `src\DrivingLessons.Application\Common\Exceptions\SubmissionNotFoundException.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\SubmissionAlreadyExistsException.cs`
- Modify: `src\DrivingLessons.Application\Common\Exceptions\SlotNotFoundException.cs`, `WeekScheduleNotFoundException.cs` (payload-free overloads)
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs`, `ReviseSubmissionInteractorTest.cs`

**Interfaces:**
- Consumes (task 3): `ISubmissionRepository.GetByPublicationAndStudentAsync` / `Add`, `IStudentRepository.GetActiveByNationalIdAsync`; (task 2): `Submission.Create`, `submission.Revise`; (task 1): `TargetSessionCount.Of`, `SlotConstraint.Of`, `SlotPick.Of`, `SessionType`. (On `main`): `IPublicationRepository.GetByLinkTokenAsync(ShareableLinkToken)`, `IWeekScheduleRepository.GetByTeacherAndWeekAsync(TeacherId, WeekStart)`, `IUnitOfWork.CommitAsync()`, `PublicationLinkNotFoundException()`, `StudentNotFoundException()` (slice 2), `NationalId.Of`, `TimeProvider` (registered as `TimeProvider.System`).
- Produces (task 5 relies on these exact names):
  - `CreateSubmissionInteractor.ExecuteAsync(string linkToken, CreateSubmissionRequest request) : Task`
  - `ReviseSubmissionInteractor.ExecuteAsync(string linkToken, ReviseSubmissionRequest request) : Task`
  - `record CreateSubmissionRequest(string NationalId, int TargetCount, IReadOnlyList<SlotRequestForSubmissionRequest> SlotRequests)` and `record ReviseSubmissionRequest(...)` with the same three members.
  - `record SlotRequestForSubmissionRequest(Guid SlotId, SessionType SessionType, string? Constraint)` — list order **is** rank order.
  - Outcomes: **404** `PublicationLinkNotFoundException` (unknown or draft link), `StudentNotFoundException` (no **active** roster student with the ID — incl. one removed by a later upload), `WeekScheduleNotFoundException` (the teacher has no grid this week), `SlotNotFoundException` (a slot id outside the student's grid), `SubmissionNotFoundException` (revise, none yet) · **409** `NationalId` exceptions, `TargetSessionCountMustBePositiveException`, `SlotConstraint…`, `SessionTypeMustBeSingleOrDoubleException`, `SubmissionAlreadyExistsException` (create, one exists), and every task-2 `Submission…` rule.

**Why a shared resolver.** Both commands start identically — link token → non-draft publication, national ID → roster student, the student's teacher + the publication's week → week schedule, slot ids → slots of *that* grid — and differ only in "must not exist yet" vs "must exist". `SubmissionContextResolver` (in `Commands\Common\`) holds that loading once. It contains no business rule: it answers "does it exist?" (ddd-architecture critical rule 4) and hands resolved entities to the domain, which answers "is it allowed?". Resolving slot ids **inside the student's own week schedule** is what turns a slot id from another teacher's grid into a 404 before the domain ever sees it (**Review Focus 3**). A draft publication is "not found", the same visibility rule as slice 1's `GetByLinkTokenExcludingDraftsAsync`.

**Payload-free not-found messages.** These endpoints are anonymous; the existing `SlotNotFoundException(WeekScheduleId, SlotId)` and `WeekScheduleNotFoundException(Guid teacherId, DateOnly)` messages would hand an anonymous caller internal ids. Each gets a parameterless overload used only here; the admin paths keep theirs.

Precedents to open before coding: `Commands\CreateWeekSchedule\CreateWeekScheduleInteractor.cs` (resolve → "already exists" → create → commit, `IUnitOfWork` injected directly), `Commands\MarkSlotUnavailable\MarkSlotUnavailableInteractor.cs` (slot resolved from its week schedule), `Queries\IdentifyStudent\IdentifyStudentInteractor.cs` (link first, then `NationalId.Of`), `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs` (FakeItEasy + real domain objects, `Invokes` capture).

- [ ] **Step 1: Write the failing create-interactor tests**

`tests\DrivingLessons.Application.Test\Commands\CreateSubmissionInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.Common;
using DrivingLessons.Application.Commands.CreateSubmission;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class CreateSubmissionInteractorTest
{
    private const string RosterNationalId = "000000018";

    private static readonly DateTimeOffset Now = new(2026, 10, 1, 9, 30, 0, TimeSpan.Zero);

    private IPublicationRepository publicationRepository = null!;
    private IStudentRepository studentRepository = null!;
    private IWeekScheduleRepository weekScheduleRepository = null!;
    private ISubmissionRepository submissionRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private CreateSubmissionInteractor interactor = null!;
    private Teacher teacher = null!;
    private Student student = null!;
    private Publication publication = null!;
    private WeekSchedule weekSchedule = null!;
    private Submission? addedSubmission;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        studentRepository = A.Fake<IStudentRepository>();
        weekScheduleRepository = A.Fake<IWeekScheduleRepository>();
        submissionRepository = A.Fake<ISubmissionRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        var timeProvider = A.Fake<TimeProvider>();
        var contextResolver = new SubmissionContextResolver(
            publicationRepository,
            studentRepository,
            weekScheduleRepository);
        interactor = new CreateSubmissionInteractor(contextResolver, submissionRepository, unitOfWork, timeProvider);

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(Now);

        teacher = Teacher.Create(TeacherName.Of("Teacher Cohen"), Email.Of("cohen@school.test"));
        var car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        student = Student.Create(
            NationalId.Of(RosterNationalId),
            StudentName.Of("Test Student"),
            PhoneNumber.Of("0501234567"),
            teacher,
            car,
            null,
            null,
            null);

        var weekStart = WeekStart.Of(new DateOnly(2026, 10, 4));
        publication = Publication.Create(weekStart);
        publication.Publish(SubmissionWindow.Of(Now.AddDays(-1), Now.AddDays(2)));
        publication.Open();
        weekSchedule = WeekSchedule.Create(teacher, weekStart);
        addedSubmission = null;

        A.CallTo(() => publicationRepository.GetByLinkTokenAsync(publication.LinkToken))
            .Returns(publication);

        A.CallTo(() => studentRepository.GetActiveByNationalIdAsync(student.NationalId))
            .Returns(student);

        A.CallTo(() => weekScheduleRepository.GetByTeacherAndWeekAsync(teacher.Id, weekStart))
            .Returns(weekSchedule);

        A.CallTo(() => submissionRepository.GetByPublicationAndStudentAsync(publication.Id, student.Id))
            .Returns((Submission?)null);

        A.CallTo(() => submissionRepository.Add(A<Submission>._))
            .Invokes(call => addedSubmission = call.GetArgument<Submission>(0));
    }

    [TestMethod]
    public async Task Adds_The_Submission_Ranked_In_Request_Order()
    {
        //given
        var slots = weekSchedule.Slots.ToList();
        var request = new CreateSubmissionRequest(
            RosterNationalId,
            2,
            [
                new SlotRequestForSubmissionRequest(slots[5].Id.Value, SessionType.Double, "  only after 16:00  "),
                new SlotRequestForSubmissionRequest(slots[0].Id.Value, SessionType.Single, null),
                new SlotRequestForSubmissionRequest(slots[9].Id.Value, SessionType.Single, null)
            ]);

        //when
        await interactor.ExecuteAsync(publication.LinkToken.Value, request);

        //then
        addedSubmission.ShouldNotBeNull();
        addedSubmission.StudentId.ShouldBe(student.Id);
        addedSubmission.WeekScheduleId.ShouldBe(weekSchedule.Id);
        addedSubmission.TargetCount.ShouldBe(TargetSessionCount.Of(2));
        addedSubmission.SubmittedAtUtc.ShouldBe(Now);
        addedSubmission.SlotRequests.Select(x => x.SlotId).ShouldBe([slots[5].Id, slots[0].Id, slots[9].Id]);
        addedSubmission.SlotRequests.First().SessionType.ShouldBe(SessionType.Double);
        addedSubmission.SlotRequests.First().Constraint.ShouldBe(SlotConstraint.Of("only after 16:00"));
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Link_Must_Belong_To_A_Publication()
    {
        //given
        var unknownToken = ShareableLinkToken.New();

        A.CallTo(() => publicationRepository.GetByLinkTokenAsync(unknownToken))
            .Returns((Publication?)null);

        //when
        var act = () => interactor.ExecuteAsync(unknownToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
        A.CallTo(() => studentRepository.GetActiveByNationalIdAsync(A<NationalId>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Draft_Link_Is_Not_Found()
    {
        //given
        var draft = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 11)));

        A.CallTo(() => publicationRepository.GetByLinkTokenAsync(draft.LinkToken))
            .Returns(draft);

        //when
        var act = () => interactor.ExecuteAsync(draft.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
    }

    [TestMethod]
    [DataRow("000000019")]
    [DataRow("12a")]
    public async Task National_Id_Must_Be_Well_Formed(string malformedNationalId)
    {
        //given
        var request = new CreateSubmissionRequest(malformedNationalId, 1, []);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, request);

        //then
        await Should.ThrowAsync<DomainException>(act);
        A.CallTo(() => studentRepository.GetActiveByNationalIdAsync(A<NationalId>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Student_Must_Be_Active_On_The_Roster()
    {
        //given
        A.CallTo(() => studentRepository.GetActiveByNationalIdAsync(student.NationalId))
            .Returns((Student?)null);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<StudentNotFoundException>(act);
    }

    [TestMethod]
    public async Task Teacher_Must_Have_A_Week_Schedule_This_Week()
    {
        //given
        A.CallTo(() => weekScheduleRepository.GetByTeacherAndWeekAsync(teacher.Id, publication.WeekStart))
            .Returns((WeekSchedule?)null);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<WeekScheduleNotFoundException>(act);
    }

    [TestMethod]
    public async Task Slot_Must_Be_In_The_Students_Week_Schedule()
    {
        //given
        var otherTeacher = Teacher.Create(TeacherName.Of("Teacher Levi"), Email.Of("levi@school.test"));
        var otherTeachersWeek = WeekSchedule.Create(otherTeacher, publication.WeekStart);
        var foreignSlotId = otherTeachersWeek.Slots.First().Id.Value;
        var request = new CreateSubmissionRequest(
            RosterNationalId,
            1,
            [new SlotRequestForSubmissionRequest(foreignSlotId, SessionType.Single, null)]);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, request);

        //then
        await Should.ThrowAsync<SlotNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Submission_Must_Not_Already_Exist()
    {
        //given
        var existingPicks = new List<SlotPick> { SlotPick.Of(weekSchedule.Slots.First(), SessionType.Single, null) };
        var existing = Submission.Create(
            publication,
            student,
            weekSchedule,
            TargetSessionCount.Of(1),
            existingPicks,
            Now);

        A.CallTo(() => submissionRepository.GetByPublicationAndStudentAsync(publication.Id, student.Id))
            .Returns(existing);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<SubmissionAlreadyExistsException>(act);
        A.CallTo(() => submissionRepository.Add(A<Submission>._))
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Unavailable_Slot_Is_Rejected()
    {
        //given
        var slot = weekSchedule.Slots.First();
        weekSchedule.MarkSlotUnavailable(slot);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<SubmissionSlotMustBeOpenException>(act);
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Closed_Window_Is_Rejected()
    {
        //given
        publication.Close([teacher.Id]);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<SubmissionWindowMustBeOpenException>(act);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public async Task Target_Must_Be_Positive(int targetCount)
    {
        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(targetCount, 1));

        //then
        await Should.ThrowAsync<TargetSessionCountMustBePositiveException>(act);
    }

    [TestMethod]
    public async Task Too_Long_Constraint_Is_Rejected()
    {
        //given
        var tooLong = new string('a', SlotConstraint.MaxLength + 1);
        var slotId = weekSchedule.Slots.First().Id.Value;
        var request = new CreateSubmissionRequest(
            RosterNationalId,
            1,
            [new SlotRequestForSubmissionRequest(slotId, SessionType.Single, tooLong)]);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, request);

        //then
        await Should.ThrowAsync<SlotConstraintMustNotExceedMaxLengthException>(act);
    }

    private CreateSubmissionRequest RequestWithFirstSlots(int targetCount, int slotCount)
    {
        var slotRequests = weekSchedule
                               .Slots
                               .Take(slotCount)
                               .Select(slot => new SlotRequestForSubmissionRequest(slot.Id.Value, SessionType.Single, null))
                               .ToList();

        return new CreateSubmissionRequest(RosterNationalId, targetCount, slotRequests);
    }
}
```

What these pin: `Slot_Must_Be_In_The_Students_Week_Schedule` → **Review Focus 3**; `Submission_Must_Not_Already_Exist` → **Review Focus 5**; `Unavailable_Slot_Is_Rejected` → **Review Focus 2**; `Closed_Window_Is_Rejected` → **Review Focus 4**; `Student_Must_Be_Active_On_The_Roster` → **Review Focus 6** (the repository only returns active students, so a student deactivated between identify and submit is "not on file", exactly like identify); `Too_Long_Constraint_Is_Rejected` → **Review Focus 7**. Every "must not" configures `null` explicitly — an unconfigured FakeItEasy `Task<T?>` call returns a dummy, not null (slice-2 convention).

- [ ] **Step 2: Write the failing revise-interactor tests**

`tests\DrivingLessons.Application.Test\Commands\ReviseSubmissionInteractorTest.cs`:

```csharp
using DrivingLessons.Application.Commands.Common;
using DrivingLessons.Application.Commands.ReviseSubmission;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Commands;

[TestClass]
public class ReviseSubmissionInteractorTest
{
    private const string RosterNationalId = "000000018";

    private static readonly DateTimeOffset SubmittedAtUtc = new(2026, 10, 1, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 8, 15, 0, TimeSpan.Zero);

    private IPublicationRepository publicationRepository = null!;
    private ISubmissionRepository submissionRepository = null!;
    private IUnitOfWork unitOfWork = null!;
    private ReviseSubmissionInteractor interactor = null!;
    private Teacher teacher = null!;
    private Student student = null!;
    private Publication publication = null!;
    private WeekSchedule weekSchedule = null!;
    private Submission existing = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        var studentRepository = A.Fake<IStudentRepository>();
        var weekScheduleRepository = A.Fake<IWeekScheduleRepository>();
        submissionRepository = A.Fake<ISubmissionRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        var timeProvider = A.Fake<TimeProvider>();
        var contextResolver = new SubmissionContextResolver(
            publicationRepository,
            studentRepository,
            weekScheduleRepository);
        interactor = new ReviseSubmissionInteractor(contextResolver, submissionRepository, unitOfWork, timeProvider);

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(Now);

        teacher = Teacher.Create(TeacherName.Of("Teacher Cohen"), Email.Of("cohen@school.test"));
        var car = Car.Create(CarName.Of("Corolla White"), CarType.Of("Corolla"), Transmission.Automatic);
        student = Student.Create(
            NationalId.Of(RosterNationalId),
            StudentName.Of("Test Student"),
            PhoneNumber.Of("0501234567"),
            teacher,
            car,
            null,
            null,
            null);

        var weekStart = WeekStart.Of(new DateOnly(2026, 10, 4));
        publication = Publication.Create(weekStart);
        publication.Publish(SubmissionWindow.Of(SubmittedAtUtc.AddDays(-1), Now.AddDays(2)));
        publication.Open();
        weekSchedule = WeekSchedule.Create(teacher, weekStart);

        var firstPicks = new List<SlotPick> { SlotPick.Of(weekSchedule.Slots.First(), SessionType.Single, null) };
        existing = Submission.Create(
            publication,
            student,
            weekSchedule,
            TargetSessionCount.Of(1),
            firstPicks,
            SubmittedAtUtc);

        A.CallTo(() => publicationRepository.GetByLinkTokenAsync(publication.LinkToken))
            .Returns(publication);

        A.CallTo(() => studentRepository.GetActiveByNationalIdAsync(student.NationalId))
            .Returns(student);

        A.CallTo(() => weekScheduleRepository.GetByTeacherAndWeekAsync(teacher.Id, weekStart))
            .Returns(weekSchedule);

        A.CallTo(() => submissionRepository.GetByPublicationAndStudentAsync(publication.Id, student.Id))
            .Returns(existing);
    }

    [TestMethod]
    public async Task Replaces_The_Existing_Submission()
    {
        //given
        var slots = weekSchedule.Slots.ToList();
        var request = new ReviseSubmissionRequest(
            RosterNationalId,
            2,
            [
                new SlotRequestForSubmissionRequest(slots[12].Id.Value, SessionType.Single, null),
                new SlotRequestForSubmissionRequest(slots[3].Id.Value, SessionType.Double, "pick me up from work")
            ]);

        //when
        await interactor.ExecuteAsync(publication.LinkToken.Value, request);

        //then
        existing.TargetCount.ShouldBe(TargetSessionCount.Of(2));
        existing.SlotRequests.Select(x => x.SlotId).ShouldBe([slots[12].Id, slots[3].Id]);
        existing.RevisedAtUtc.ShouldBe(Now);
        existing.SubmittedAtUtc.ShouldBe(SubmittedAtUtc);
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Submission_Must_Exist()
    {
        //given
        A.CallTo(() => submissionRepository.GetByPublicationAndStudentAsync(publication.Id, student.Id))
            .Returns((Submission?)null);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlot());

        //then
        await Should.ThrowAsync<SubmissionNotFoundException>(act);
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Closed_Window_Is_Rejected()
    {
        //given
        publication.Close([teacher.Id]);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlot());

        //then
        await Should.ThrowAsync<SubmissionWindowMustBeOpenException>(act);
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Link_Must_Belong_To_A_Publication()
    {
        //given
        var unknownToken = ShareableLinkToken.New();

        A.CallTo(() => publicationRepository.GetByLinkTokenAsync(unknownToken))
            .Returns((Publication?)null);

        //when
        var act = () => interactor.ExecuteAsync(unknownToken.Value, RequestWithFirstSlot());

        //then
        await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
    }

    private ReviseSubmissionRequest RequestWithFirstSlot()
    {
        var slotId = weekSchedule.Slots.First().Id.Value;

        return new ReviseSubmissionRequest(
            RosterNationalId,
            1,
            [new SlotRequestForSubmissionRequest(slotId, SessionType.Single, null)]);
    }
}
```

The shared resolution paths (malformed ID, roster, grid, foreign slot) are covered once, in the create tests; these four pin only what revise does differently.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CreateSubmissionInteractorTest|FullyQualifiedName~ReviseSubmissionInteractorTest"`
Expected: build FAILS — the `Commands.Common`, `Commands.CreateSubmission`, `Commands.ReviseSubmission` namespaces, `SubmissionAlreadyExistsException` and `SubmissionNotFoundException` do not exist.

- [ ] **Step 4: Exceptions**

`src\DrivingLessons.Domain\Exceptions\SubmissionAlreadyExistsException.cs` (a domain exception → 409, same placement as `WeekScheduleAlreadyExistsException`):

```csharp
using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionAlreadyExistsException : DomainException
{
    public SubmissionAlreadyExistsException()
        : base("A submission already exists for this student and week.")
    {
    }
}
```

`src\DrivingLessons.Application\Common\Exceptions\SubmissionNotFoundException.cs`:

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

public class SubmissionNotFoundException : NotFoundException
{
    public SubmissionNotFoundException()
        : base("No submission exists for this student and week yet.")
    {
    }
}
```

In `src\DrivingLessons.Application\Common\Exceptions\SlotNotFoundException.cs`, add a second constructor before the existing one:

```csharp
    public SlotNotFoundException()
        : base("The requested slot is not in the student's week schedule.")
    {
    }

```

In `src\DrivingLessons.Application\Common\Exceptions\WeekScheduleNotFoundException.cs`, add a third constructor before the existing ones:

```csharp
    public WeekScheduleNotFoundException()
        : base("The student's teacher has no week schedule for this week.")
    {
    }

```

- [ ] **Step 5: Request DTOs**

`src\DrivingLessons.Application\Commands\Common\SlotRequestForSubmissionRequest.cs`:

```csharp
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.Common;

public record SlotRequestForSubmissionRequest(Guid SlotId, SessionType SessionType, string? Constraint);
```

`src\DrivingLessons.Application\Commands\CreateSubmission\CreateSubmissionRequest.cs`:

```csharp
using DrivingLessons.Application.Commands.Common;

namespace DrivingLessons.Application.Commands.CreateSubmission;

public record CreateSubmissionRequest(
    string NationalId,
    int TargetCount,
    IReadOnlyList<SlotRequestForSubmissionRequest> SlotRequests);
```

`src\DrivingLessons.Application\Commands\ReviseSubmission\ReviseSubmissionRequest.cs`:

```csharp
using DrivingLessons.Application.Commands.Common;

namespace DrivingLessons.Application.Commands.ReviseSubmission;

public record ReviseSubmissionRequest(
    string NationalId,
    int TargetCount,
    IReadOnlyList<SlotRequestForSubmissionRequest> SlotRequests);
```

The list item is shared by both requests (one Swagger schema, globally unique name). The request carries no rank: order is rank (US-37). A missing `nationalId` or `slotRequests` is a 400 at model binding (non-nullable members are required); a missing `targetCount` binds as `0` and is refused by `TargetSessionCount.Of` (409).

- [ ] **Step 6: `SubmissionContext` and its resolver**

`src\DrivingLessons.Application\Commands\Common\SubmissionContext.cs`:

```csharp
using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Application.Commands.Common;

public record SubmissionContext(Publication Publication, Student Student, WeekSchedule WeekSchedule);
```

`src\DrivingLessons.Application\Commands\Common\SubmissionContextResolver.cs`:

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.Common;

public class SubmissionContextResolver
{
    private readonly IPublicationRepository publicationRepository;
    private readonly IStudentRepository studentRepository;
    private readonly IWeekScheduleRepository weekScheduleRepository;

    public SubmissionContextResolver(
        IPublicationRepository publicationRepository,
        IStudentRepository studentRepository,
        IWeekScheduleRepository weekScheduleRepository)
    {
        this.publicationRepository = publicationRepository;
        this.studentRepository = studentRepository;
        this.weekScheduleRepository = weekScheduleRepository;
    }

    public async Task<SubmissionContext> ResolveAsync(string linkToken, string nationalId)
    {
        var token = ShareableLinkToken.Of(linkToken);

        var publication = await publicationRepository.GetByLinkTokenAsync(token);

        if (publication is null || publication.IsDraft)
        {
            throw new PublicationLinkNotFoundException();
        }

        var resolvedNationalId = NationalId.Of(nationalId);

        var student = await studentRepository.GetActiveByNationalIdAsync(resolvedNationalId)
                      ?? throw new StudentNotFoundException();

        var weekStart = publication.WeekStart;

        var weekSchedule = await weekScheduleRepository.GetByTeacherAndWeekAsync(student.TeacherId, weekStart)
                           ?? throw new WeekScheduleNotFoundException();

        return new SubmissionContext(publication, student, weekSchedule);
    }

    public static IReadOnlyList<SlotPick> ResolvePicks(
        WeekSchedule weekSchedule,
        IReadOnlyList<SlotRequestForSubmissionRequest> slotRequests)
    {
        var picks = new List<SlotPick>();

        foreach (var slotRequest in slotRequests)
        {
            var slotId = SlotId.Of(slotRequest.SlotId);

            var slot = weekSchedule.Slots.FirstOrDefault(x => x.Id == slotId)
                       ?? throw new SlotNotFoundException();

            var constraint = slotRequest.Constraint is null
                ? null
                : SlotConstraint.Of(slotRequest.Constraint);

            var pick = SlotPick.Of(slot, slotRequest.SessionType, constraint);
            picks.Add(pick);
        }

        return picks;
    }
}
```

Order matters: the link is resolved first, so a dead or draft link is a 404 whatever the body holds (slice-2 identify precedent); `NationalId.Of` validates before any roster read. `GetActiveByNationalIdAsync` returns active students only (task 3), so a student removed by a later roster upload is a 404 here — the same "not on file" answer identify gives (slice-2 decision 4) and the one the client can explain ("contact your school"); the aggregate still refuses an inactive student itself (task 2). `ResolvePicks` is `static` (code-style: no instance data used). A blank constraint must arrive as `null` — the client trims and sends `null` for empty text (task 7); a whitespace-only string is refused by `SlotConstraint.Of` (409), never silently dropped.

- [ ] **Step 7: Interactors**

`src\DrivingLessons.Application\Commands\CreateSubmission\CreateSubmissionInteractor.cs`:

```csharp
using DrivingLessons.Application.Commands.Common;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateSubmission;

public class CreateSubmissionInteractor
{
    private readonly SubmissionContextResolver contextResolver;
    private readonly ISubmissionRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public CreateSubmissionInteractor(
        SubmissionContextResolver contextResolver,
        ISubmissionRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.contextResolver = contextResolver;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
    }

    public async Task ExecuteAsync(string linkToken, CreateSubmissionRequest request)
    {
        var context = await contextResolver.ResolveAsync(linkToken, request.NationalId);
        var publication = context.Publication;
        var student = context.Student;
        var weekSchedule = context.WeekSchedule;

        var existingSubmission = await repository.GetByPublicationAndStudentAsync(publication.Id, student.Id);

        if (existingSubmission is not null)
        {
            throw new SubmissionAlreadyExistsException();
        }

        var targetCount = TargetSessionCount.Of(request.TargetCount);
        var picks = SubmissionContextResolver.ResolvePicks(weekSchedule, request.SlotRequests);
        var submittedAtUtc = timeProvider.GetUtcNow();

        var submission = Submission.Create(publication, student, weekSchedule, targetCount, picks, submittedAtUtc);

        repository.Add(submission);

        await unitOfWork.CommitAsync();
    }
}
```

`src\DrivingLessons.Application\Commands\ReviseSubmission\ReviseSubmissionInteractor.cs`:

```csharp
using DrivingLessons.Application.Commands.Common;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ReviseSubmission;

public class ReviseSubmissionInteractor
{
    private readonly SubmissionContextResolver contextResolver;
    private readonly ISubmissionRepository repository;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public ReviseSubmissionInteractor(
        SubmissionContextResolver contextResolver,
        ISubmissionRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.contextResolver = contextResolver;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
    }

    public async Task ExecuteAsync(string linkToken, ReviseSubmissionRequest request)
    {
        var context = await contextResolver.ResolveAsync(linkToken, request.NationalId);
        var publication = context.Publication;
        var student = context.Student;
        var weekSchedule = context.WeekSchedule;

        var submission = await repository.GetByPublicationAndStudentAsync(publication.Id, student.Id)
                         ?? throw new SubmissionNotFoundException();

        var targetCount = TargetSessionCount.Of(request.TargetCount);
        var picks = SubmissionContextResolver.ResolvePicks(weekSchedule, request.SlotRequests);
        var revisedAtUtc = timeProvider.GetUtcNow();

        submission.Revise(publication, student, weekSchedule, targetCount, picks, revisedAtUtc);

        await unitOfWork.CommitAsync();
    }
}
```

Both commands return nothing (the endpoints answer 204, README decision 1). The existence check comes before any body validation, so a repeated POST is always reported as "already exists" (**Review Focus 5**). `TimeProvider.GetUtcNow()` is a UTC `DateTimeOffset` (offset 0), which Npgsql requires for `timestamptz`.

- [ ] **Step 8: Register the resolver and interactors**

In `src\DrivingLessons.Application\DependencyInjection.cs`, add these usings after `using DrivingLessons.Application.Commands.ClosePublication;` (keep the list alphabetical):

```csharp
using DrivingLessons.Application.Commands.Common;
```

after `using DrivingLessons.Application.Commands.CreateCar;`:

```csharp
using DrivingLessons.Application.Commands.CreateSubmission;
```

and after `using DrivingLessons.Application.Commands.ReopenPublication;`:

```csharp
using DrivingLessons.Application.Commands.ReviseSubmission;
```

Register after `services.AddScoped<IdentifyStudentInteractor>();`:

```csharp
        services.AddScoped<SubmissionContextResolver>();
        services.AddScoped<CreateSubmissionInteractor>();
        services.AddScoped<ReviseSubmissionInteractor>();
```

(`ISubmissionRepository` was registered in task 3; the other repositories, `IUnitOfWork` and `TimeProvider` already are.)

- [ ] **Step 9: Run tests to verify they pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~CreateSubmissionInteractorTest|FullyQualifiedName~ReviseSubmissionInteractorTest"`
Expected: 18 tests PASS (create 14 incl. data rows, revise 4).

Then: `dotnet build` and `dotnet test` — build clean (no new warnings), every test PASS.

- [ ] **Step 10: Commit**

```bash
git add src/DrivingLessons.Application src/DrivingLessons.Domain/Exceptions/SubmissionAlreadyExistsException.cs tests/DrivingLessons.Application.Test/Commands
git commit -m "feat(app): create and revise a student's submission

Both commands resolve the link, the roster student, their teacher's
grid for the week and the requested slots inside that grid, then hand
the domain resolved entities. Create refuses when a submission exists;
revise replaces it. Not-found messages carry no ids."
```

---

**Next:** [task-05-submission-endpoints.md](task-05-submission-endpoints.md)
