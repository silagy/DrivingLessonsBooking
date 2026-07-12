namespace DrivingLessons.Application.Commands.CreateWeekSchedule;

public record CreateWeekScheduleRequest(Guid TeacherId, DateOnly WeekStart);
