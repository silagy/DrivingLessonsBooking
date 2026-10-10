using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record StudentTeacherChanged(StudentId StudentId, TeacherId TeacherId, CarId CarId) : IDomainEvent;
