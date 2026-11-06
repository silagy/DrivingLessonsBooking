namespace DrivingLessons.Application.IntegrationEvents.Publications;

public record PublicationClosedIntegrationEvent(Guid PublicationId, IReadOnlyList<ClosedTeacherVersion> Teachers) : IIntegrationEvent
{
    public const string Name = "publication-closed.v1";
}
