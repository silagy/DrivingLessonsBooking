using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Events;

public record TeacherDetailsChanged(TeacherId TeacherId, TeacherName Name, Email ContactEmail) : IDomainEvent;
