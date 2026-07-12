using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record SlotMarkedAvailable(WeekScheduleId WeekScheduleId, SlotId SlotId) : IDomainEvent;
