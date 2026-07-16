using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentCreated(
    StudentId StudentId,
    NationalId NationalId,
    StudentName Name,
    TeacherId TeacherId,
    CarId CarId) : IDomainEvent;
