namespace DrivingLessons.Application.Common.Exceptions;

public class RosterImportNotFoundException : NotFoundException
{
    public RosterImportNotFoundException()
        : base("No roster import was found.")
    {
    }
}
