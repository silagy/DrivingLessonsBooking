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
    private TimeProvider timeProvider = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        var studentRepository = A.Fake<IStudentRepository>();
        var weekScheduleRepository = A.Fake<IWeekScheduleRepository>();
        submissionRepository = A.Fake<ISubmissionRepository>();
        unitOfWork = A.Fake<IUnitOfWork>();
        timeProvider = A.Fake<TimeProvider>();
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
    public async Task Window_That_Has_Ended_Is_Rejected_And_Keeps_The_Previous_Version()
    {
        //given
        var slots = weekSchedule.Slots.ToList();
        var request = new ReviseSubmissionRequest(
            RosterNationalId,
            2,
            [
                new SlotRequestForSubmissionRequest(slots[4].Id.Value, SessionType.Single, null),
                new SlotRequestForSubmissionRequest(slots[6].Id.Value, SessionType.Double, null)
            ]);

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(publication.Window!.EndUtc.AddMinutes(1));

        //when
        var act = () => interactor.ExecuteAsync(publication.LinkToken.Value, request);

        //then
        await Should.ThrowAsync<SubmissionWindowMustBeOpenException>(act);
        existing.TargetCount.ShouldBe(TargetSessionCount.Of(1));
        existing.SlotRequests.Select(x => x.SlotId).ShouldBe([slots[0].Id]);
        existing.RevisedAtUtc.ShouldBeNull();
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustNotHaveHappened();
    }

    [TestMethod]
    public async Task Revises_Any_Number_Of_Times()
    {
        //given
        var slots = weekSchedule.Slots.ToList();
        var laterNow = Now.AddHours(3);
        var firstRevision = new ReviseSubmissionRequest(
            RosterNationalId,
            2,
            [
                new SlotRequestForSubmissionRequest(slots[4].Id.Value, SessionType.Single, null),
                new SlotRequestForSubmissionRequest(slots[6].Id.Value, SessionType.Single, null)
            ]);
        var secondRevision = new ReviseSubmissionRequest(
            RosterNationalId,
            1,
            [new SlotRequestForSubmissionRequest(slots[10].Id.Value, SessionType.Double, "pick me up from work")]);

        await interactor.ExecuteAsync(publication.LinkToken.Value, firstRevision);

        A.CallTo(() => timeProvider.GetUtcNow())
            .Returns(laterNow);

        //when
        await interactor.ExecuteAsync(publication.LinkToken.Value, secondRevision);

        //then
        existing.TargetCount.ShouldBe(TargetSessionCount.Of(1));
        existing.SlotRequests.ShouldHaveSingleItem().SlotId.ShouldBe(slots[10].Id);
        existing.RevisedAtUtc.ShouldBe(laterNow);
        existing.SubmittedAtUtc.ShouldBe(SubmittedAtUtc);
        A.CallTo(() => unitOfWork.CommitAsync())
            .MustHaveHappenedTwiceExactly();
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
