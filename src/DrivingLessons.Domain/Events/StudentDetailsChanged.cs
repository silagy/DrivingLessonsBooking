using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentDetailsChanged(StudentId StudentId, NationalId NationalId, StudentName Name) : IDomainEvent;
