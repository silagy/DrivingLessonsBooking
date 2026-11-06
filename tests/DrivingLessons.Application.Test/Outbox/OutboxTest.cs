using DrivingLessons.Application.Common;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.EntityFramework;
using DrivingLessons.Infrastructure.Outbox;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxTest
{
    private const string SampleName = "sample-happened.v1";

    private static readonly DateTimeOffset NowUtc = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    private DrivingLessonsDbContext context = null!;
    private IntegrationEventCatalog catalog = null!;
    private Infrastructure.Outbox.Outbox outbox = null!;

    [TestInitialize]
    public void Init()
    {
        var options = new DbContextOptionsBuilder<DrivingLessonsDbContext>()
                      .UseNpgsql("Host=localhost;Database=never_opened")
                      .Options;
        context = new DrivingLessonsDbContext(options, A.Fake<IDomainEventDispatcher>());
        catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);
        var timeProvider = A.Fake<TimeProvider>();
        A.CallTo(() => timeProvider.GetUtcNow()).Returns(NowUtc);
        outbox = new Infrastructure.Outbox.Outbox(context, catalog, timeProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        context.Dispose();
    }

    [TestMethod]
    public void Adds_The_Message_Under_The_Events_Registered_Name()
    {
        //given
        var sample = new SampleHappened(Guid.NewGuid());

        //when
        outbox.Add(sample);

        //then
        var message = context.ChangeTracker.Entries<OutboxMessage>().ShouldHaveSingleItem().Entity;
        message.EventName.ShouldBe(SampleName);
        message.OccurredAtUtc.ShouldBe(NowUtc);
        OutboxSerializer.Deserialize(message.Payload, typeof(SampleHappened)).ShouldBe(sample);
    }

    [TestMethod]
    public void Adds_One_Pending_Delivery_Per_Consumer()
    {
        //given
        catalog.AddConsumer<SampleHappened, FirstConsumer>();
        catalog.AddConsumer<SampleHappened, SecondConsumer>();

        //when
        outbox.Add(new SampleHappened(Guid.NewGuid()));

        //then
        var message = context.ChangeTracker.Entries<OutboxMessage>().ShouldHaveSingleItem().Entity;
        var deliveries = context.ChangeTracker.Entries<OutboxDelivery>().Select(x => x.Entity).ToList();
        deliveries.Select(x => x.Consumer).ShouldBe([nameof(FirstConsumer), nameof(SecondConsumer)]);
        deliveries.ShouldAllBe(x => x.MessageId == message.Id && x.State == OutboxDeliveryState.Pending && x.NextAttemptAtUtc == NowUtc);
    }

    [TestMethod]
    public void An_Event_Without_Consumers_Is_Still_Stored_With_No_Delivery()
    {
        //given
        var sample = new SampleHappened(Guid.NewGuid());

        //when
        outbox.Add(sample);

        //then
        context.ChangeTracker.Entries<OutboxMessage>().ShouldHaveSingleItem();
        context.ChangeTracker.Entries<OutboxDelivery>().ShouldBeEmpty();
    }

    [TestMethod]
    public void An_Unregistered_Event_Is_Refused()
    {
        //given
        var unregistered = new UnregisteredHappened();

        //when
        var act = () => outbox.Add(unregistered);

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    public record SampleHappened(Guid Id) : IIntegrationEvent;

    public record UnregisteredHappened : IIntegrationEvent;

    public class FirstConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public Task<DeliveryOutcome> ConsumeAsync(SampleHappened integrationEvent)
        {
            return Task.FromResult(DeliveryOutcome.Done);
        }
    }

    public class SecondConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public Task<DeliveryOutcome> ConsumeAsync(SampleHappened integrationEvent)
        {
            return Task.FromResult(DeliveryOutcome.Done);
        }
    }
}
