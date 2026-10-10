namespace DrivingLessons.Application.Commands.ChangeStudentTeacher;

public record ChangeStudentTeacherRequest(Guid TeacherId, Guid CarId);
