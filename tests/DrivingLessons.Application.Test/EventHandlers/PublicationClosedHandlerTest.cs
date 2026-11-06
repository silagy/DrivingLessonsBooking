using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.EventHandlers;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Application.IntegrationEvents.Publications;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.EventHandlers;

[TestClass]
public class PublicationClosedHandlerTest
{
    private IPublicationRepository publicationRepository = null!;
    private IOutbox outbox = null!;
    private PublicationClosedHandler handler = null!;
    private List<IIntegrationEvent> queued = null!;

    [TestInitialize]
    public void Init()
    {
        publicationRepository = A.Fake<IPublicationRepository>();
        outbox = A.Fake<IOutbox>();
        handler = new PublicationClosedHandler(publicationRepository, outbox);
        queued = [];

        A.CallTo(() => outbox.Add(A<IIntegrationEvent>._))
            .Invokes((IIntegrationEvent integrationEvent) => queued.Add(integrationEvent));
    }

    [TestMethod]
    public async Task Queues_The_Close_With_Each_Teachers_Excel_Version()
    {
        //given
        var cohen = TeacherId.New();
        var levi = TeacherId.New();
        var publication = Closed(cohen, levi);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        var closed = queued.ShouldHaveSingleItem().ShouldBeOfType<PublicationClosedIntegrationEvent>();
        closed.PublicationId.ShouldBe(publication.Id.Value);
        closed.Teachers.ShouldBe(
            [
                new ClosedTeacherVersion(cohen.Value, 1),
                new ClosedTeacherVersion(levi.Value, 1)
            ]);
    }

    [TestMethod]
    public async Task Queues_The_Next_Version_After_A_Reopen_And_Close()
    {
        //given
        var teacherId = TeacherId.New();
        var publication = Closed(teacherId);
        publication.Reopen(DateTimeOffset.UtcNow.AddDays(5));
        publication.Close([teacherId]);

        //when
        await handler.HandleAsync(new PublicationClosed(publication.Id));

        //then
        var closed = queued.ShouldHaveSingleItem().ShouldBeOfType<PublicationClosedIntegrationEvent>();
        closed.Teachers.ShouldBe([new ClosedTeacherVersion(teacherId.Value, 2)]);
    }

    [TestMethod]
    public async Task Queues_Nothing_When_The_Publication_Is_Gone()
    {
        //given
        var publicationId = PublicationId.New();

        A.CallTo(() => publicationRepository.GetAsync(publicationId))
            .Returns((Publication?)null);

        //when
        await handler.HandleAsync(new PublicationClosed(publicationId));

        //then
        queued.ShouldBeEmpty();
    }

    private Publication Closed(params TeacherId[] teacherIds)
    {
        var publication = Publication.Create(WeekStart.Of(new DateOnly(2026, 10, 4)));
        var startUtc = DateTimeOffset.UtcNow.AddDays(-1);
        publication.Publish(SubmissionWindow.Of(startUtc, startUtc.AddHours(1)));
        publication.Open();
        publication.Close(teacherIds);

        A.CallTo(() => publicationRepository.GetAsync(publication.Id))
            .Returns(publication);

        return publication;
    }
}
