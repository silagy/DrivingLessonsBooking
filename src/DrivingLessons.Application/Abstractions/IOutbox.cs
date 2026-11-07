using DrivingLessons.Application.IntegrationEvents;

namespace DrivingLessons.Application.Abstractions;

public interface IOutbox
{
    void Add(IIntegrationEvent integrationEvent);
}
