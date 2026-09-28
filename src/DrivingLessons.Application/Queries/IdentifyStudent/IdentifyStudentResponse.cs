using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.IdentifyStudent;

public class IdentifyStudentResponse
{
    public string StudentName { get; init; } = string.Empty;
    public string TeacherName { get; init; } = string.Empty;
    public string CarName { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
    public SubmissionForIdentifyStudentResponse? Submission { get; init; }
    public IReadOnlyCollection<SlotForIdentifyStudentResponse> Slots { get; init; } = [];
}

public class SubmissionForIdentifyStudentResponse
{
    public int TargetCount { get; init; }
    public DateTimeOffset LastSavedAtUtc { get; init; }
    public IReadOnlyCollection<SlotRequestForIdentifyStudentResponse> SlotRequests { get; init; } = [];

    public static Expression<Func<Submission, SubmissionForIdentifyStudentResponse>> Selector =>
        x => new SubmissionForIdentifyStudentResponse
        {
            TargetCount = x.TargetCount.Value,
            LastSavedAtUtc = x.RevisedAtUtc ?? x.SubmittedAtUtc,
            SlotRequests = x.SlotRequests
                            .OrderBy(request => request.Rank)
                            .Select(request => new SlotRequestForIdentifyStudentResponse
                            {
                                SlotId = request.SlotId.Value,
                                SessionType = request.SessionType,
                                Constraint = request.Constraint == null ? null : request.Constraint.Value
                            })
                            .ToList()
        };
}

public class SlotRequestForIdentifyStudentResponse
{
    public Guid SlotId { get; init; }
    public SessionType SessionType { get; init; }
    public string? Constraint { get; init; }
}

public class SlotForIdentifyStudentResponse
{
    public Guid Id { get; init; }
    public DayOfWeek Day { get; init; }
    public SlotWindowType Window { get; init; }
    public SlotState State { get; init; }
    public TimeOnly StartLocal => SlotWindowTimes.StartOf(Window);
    public TimeOnly EndLocal => SlotWindowTimes.EndOf(Window);

    public static Expression<Func<Slot, SlotForIdentifyStudentResponse>> Selector =>
        x => new SlotForIdentifyStudentResponse
        {
            Id = x.Id.Value,
            Day = x.Day,
            Window = x.Window,
            State = x.State
        };
}
