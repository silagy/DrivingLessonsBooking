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
    private TimeProvider timeProvider = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        studentRepository = A.Fake<IStudentRepository>();
        weekScheduleRepository = A.Fake<IWeekScheduleRepository>();
        submissionRepository = A.Fake<ISubmissionRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        timeProvider = A.Fake<TimeProvider>();
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
    public async Task Window_That_Has_Ended_Is_Rejected_Before_The_Close_Job_Runs()
    {
        //given
        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(publication.Window!.EndUtc);

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, RequestWithFirstSlots(1, 1));

        //then
        await Should.ThrowAsync<SubmissionWindowMustBeOpenException>(act);
        publication.IsOpen.ShouldBeTrue();
        A.CallTo(() => submissionRepository.Add(A<Submission>._))
            .MustNotHaveHappened();
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
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
