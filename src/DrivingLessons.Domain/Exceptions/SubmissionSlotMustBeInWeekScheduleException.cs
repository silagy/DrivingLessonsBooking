using DrivingLessons.Domain.Common;

namespace DrivingLessons.Domain.Exceptions;

public class SubmissionSlotMustBeInWeekScheduleException : DomainException
{
    public SubmissionSlotMustBeInWeekScheduleException()
        : base("Every requested slot must belong to the submission's week schedule.")
    {
    }
}
