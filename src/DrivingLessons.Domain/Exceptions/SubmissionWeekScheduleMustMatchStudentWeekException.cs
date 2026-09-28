using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionWeekScheduleMustMatchStudentWeekException : DomainException
{
    public SubmissionWeekScheduleMustMatchStudentWeekException()
        : base("A submission must use the week schedule of the student's teacher for the publication's week.")
    {
    }
}
