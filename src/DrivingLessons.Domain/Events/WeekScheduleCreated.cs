using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record WeekScheduleCreated(WeekScheduleId WeekScheduleId, TeacherId TeacherId, WeekStart WeekStart) : IDomainEvent;
