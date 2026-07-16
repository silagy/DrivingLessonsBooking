using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentDeactivated(StudentId StudentId) : IDomainEvent;
