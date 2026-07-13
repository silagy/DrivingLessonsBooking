namespace DrivingLessons.Application.Commands.PublishPublication;

public record PublishPublicationRequest(DateTimeOffset StartUtc, DateTimeOffset EndUtc);
