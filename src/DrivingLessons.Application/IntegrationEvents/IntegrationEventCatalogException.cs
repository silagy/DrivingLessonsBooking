namespace DrivingLessons.Application.IntegrationEvents;

public class IntegrationEventCatalogException : Exception
{
    public IntegrationEventCatalogException(string message)
        : base(message)
    {
    }
}
