namespace DrivingLessons.Application.Common.Exceptions;

public class PublicationLinkNotFoundException : NotFoundException
{
    public PublicationLinkNotFoundException()
        : base("No publication is available for this link.")
    {
    }
}
