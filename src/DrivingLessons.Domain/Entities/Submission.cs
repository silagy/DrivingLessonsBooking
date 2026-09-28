using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Submission : AggregateRoot<SubmissionId>
{
    private readonly List<SlotRequest> slotRequests = [];

    public PublicationId PublicationId { get; private set; }
    public StudentId StudentId { get; private set; }
    public WeekScheduleId WeekScheduleId { get; private set; }
    public TargetSessionCount TargetCount { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }
    public DateTimeOffset? RevisedAtUtc { get; private set; }

    public IReadOnlyCollection<SlotRequest> SlotRequests => slotRequests.AsReadOnly();

    private Submission()
    {
    }

    private Submission(
        SubmissionId id,
        PublicationId publicationId,
        StudentId studentId,
        WeekScheduleId weekScheduleId,
        TargetSessionCount targetCount,
        DateTimeOffset submittedAtUtc)
        : base(id)
    {
        PublicationId = publicationId;
        StudentId = studentId;
        WeekScheduleId = weekScheduleId;
        TargetCount = targetCount;
        SubmittedAtUtc = submittedAtUtc;

        var createdEvent = new SubmissionCreated(
            id,
            publicationId,
            studentId,
            weekScheduleId,
            targetCount,
            submittedAtUtc);
        AddEvent(createdEvent);
    }

    public static Submission Create(
        Publication publication,
        Student student,
        WeekSchedule weekSchedule,
        TargetSessionCount targetCount,
        IReadOnlyList<SlotPick> picks,
        DateTimeOffset submittedAtUtc)
    {
        WindowMustBeOpen(publication);
        StudentMustBeActive(student);
        WeekScheduleMustMatchStudentWeek(publication, student, weekSchedule);
        SlotsMustBeOpenInWeekSchedule(weekSchedule, picks);
        SlotsMustBeRequestedOnce(picks);
        PicksMustCoverTarget(targetCount, picks);

        var id = SubmissionId.New();
        var submission = new Submission(
            id,
            publication.Id,
            student.Id,
            weekSchedule.Id,
            targetCount,
            submittedAtUtc);
        submission.ReplaceSlotRequests(picks);

        return submission;
    }

    public void Revise(
        Publication publication,
        Student student,
        WeekSchedule weekSchedule,
        TargetSessionCount targetCount,
        IReadOnlyList<SlotPick> picks,
        DateTimeOffset revisedAtUtc)
    {
        MustBeFor(publication, student);
        WindowMustBeOpen(publication);
        StudentMustBeActive(student);
        WeekScheduleMustMatchStudentWeek(publication, student, weekSchedule);
        SlotsMustBeOpenInWeekSchedule(weekSchedule, picks);
        SlotsMustBeRequestedOnce(picks);
        PicksMustCoverTarget(targetCount, picks);

        var weekScheduleId = weekSchedule.Id;
        WeekScheduleId = weekScheduleId;
        TargetCount = targetCount;
        RevisedAtUtc = revisedAtUtc;
        ReplaceSlotRequests(picks);

        AddEvent(new SubmissionRevised(Id, weekScheduleId, targetCount, revisedAtUtc));
    }

    private void ReplaceSlotRequests(IReadOnlyList<SlotPick> picks)
    {
        slotRequests.Clear();

        for (var index = 0; index < picks.Count; index++)
        {
            var pick = picks[index];
            var position = index + 1;
            var rank = Rank.Of(position);
            var slotRequest = SlotRequest.Create(pick, rank);
            slotRequests.Add(slotRequest);
        }
    }

    private void MustBeFor(Publication publication, Student student)
    {
        var isSamePublication = publication.Id == PublicationId;
        var isSameStudent = student.Id == StudentId;

        if (!isSamePublication || !isSameStudent)
        {
            throw new SubmissionMustBeForPublicationAndStudentException();
        }
    }

    private static void WindowMustBeOpen(Publication publication)
    {
        if (!publication.IsOpen)
        {
            throw new SubmissionWindowMustBeOpenException();
        }
    }

    private static void StudentMustBeActive(Student student)
    {
        if (!student.IsActive)
        {
            throw new SubmissionStudentMustBeActiveException();
        }
    }

    private static void WeekScheduleMustMatchStudentWeek(
        Publication publication,
        Student student,
        WeekSchedule weekSchedule)
    {
        var isStudentsTeacher = weekSchedule.TeacherId == student.TeacherId;
        var isPublicationWeek = weekSchedule.WeekStart == publication.WeekStart;

        if (!isStudentsTeacher || !isPublicationWeek)
        {
            throw new SubmissionWeekScheduleMustMatchStudentWeekException();
        }
    }

    private static void SlotsMustBeOpenInWeekSchedule(WeekSchedule weekSchedule, IReadOnlyList<SlotPick> picks)
    {
        var scheduleSlots = weekSchedule.Slots;

        foreach (var pick in picks)
        {
            var scheduleSlot = scheduleSlots.FirstOrDefault(x => x == pick.Slot);

            if (scheduleSlot is null)
            {
                throw new SubmissionSlotMustBeInWeekScheduleException();
            }

            if (!scheduleSlot.IsOpen)
            {
                throw new SubmissionSlotMustBeOpenException();
            }
        }
    }

    private static void SlotsMustBeRequestedOnce(IReadOnlyList<SlotPick> picks)
    {
        var distinctSlotCount = picks
                                    .Select(x => x.Slot)
                                    .Distinct()
                                    .Count();

        if (distinctSlotCount != picks.Count)
        {
            throw new SubmissionSlotMustBeRequestedOnceException();
        }
    }

    private static void PicksMustCoverTarget(TargetSessionCount targetCount, IReadOnlyList<SlotPick> picks)
    {
        var pickCount = picks.Count;

        if (!targetCount.IsCoveredBy(pickCount))
        {
            throw new SubmissionPicksMustCoverTargetException(targetCount, pickCount);
        }
    }
}
