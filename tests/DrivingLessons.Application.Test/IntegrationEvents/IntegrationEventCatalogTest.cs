using DrivingLessons.Application.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DrivingLessons.Application.Test.IntegrationEvents;

[TestClass]
public class IntegrationEventCatalogTest
{
    private const string SampleName = "sample-happened.v1";

    [TestMethod]
    public void Names_An_Event_By_Its_Registered_Name()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var name = catalog.NameOf(typeof(SampleHappened));

        //then
        name.ShouldBe(SampleName);
    }

    [TestMethod]
    public void Finds_The_Event_Type_By_Its_Name()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var eventType = catalog.TypeOf(SampleName);

        //then
        eventType.ShouldBe(typeof(SampleHappened));
    }

    [TestMethod]
    public void Lists_Every_Consumer_Of_An_Event_In_Registration_Order()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);
        catalog.AddConsumer<SampleHappened, RecordingConsumer>();
        catalog.AddConsumer<SampleHappened, OtherConsumer>();

        //when
        var consumers = catalog.ConsumersOf(SampleName);

        //then
        consumers.ShouldBe([nameof(RecordingConsumer), nameof(OtherConsumer)]);
    }

    [TestMethod]
    public void An_Event_Without_Consumers_Has_None()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var consumers = catalog.ConsumersOf(SampleName);

        //then
        consumers.ShouldBeEmpty();
    }

    [TestMethod]
    public void Refuses_Two_Events_With_The_Same_Name()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.AddEvent<OtherHappened>(SampleName);

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void Refuses_The_Same_Event_Type_Under_Two_Names()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.AddEvent<SampleHappened>("sample-happened.v2");

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void Refuses_A_Consumer_Of_An_Unregistered_Event()
    {
        //given
        var catalog = new IntegrationEventCatalog();

        //when
        var act = () => catalog.AddConsumer<SampleHappened, RecordingConsumer>();

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void Refuses_The_Same_Consumer_Twice()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);
        catalog.AddConsumer<SampleHappened, RecordingConsumer>();

        //when
        var act = () => catalog.AddConsumer<SampleHappened, RecordingConsumer>();

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    [DataRow("unknown.v1")]
    [DataRow("")]
    public void An_Unknown_Event_Name_Is_Refused(string eventName)
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.TypeOf(eventName);

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void An_Unregistered_Event_Type_Is_Refused()
    {
        //given
        var catalog = new IntegrationEventCatalog();

        //when
        var act = () => catalog.NameOf(typeof(SampleHappened));

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public void An_Unknown_Consumer_Is_Refused()
    {
        //given
        var catalog = new IntegrationEventCatalog();
        catalog.AddEvent<SampleHappened>(SampleName);

        //when
        var act = () => catalog.ConsumerOf(SampleName, nameof(RecordingConsumer));

        //then
        act.ShouldThrow<IntegrationEventCatalogException>();
    }

    [TestMethod]
    public async Task A_Registered_Consumer_Is_Resolved_From_The_Scope_And_Receives_The_Event()
    {
        //given
        var services = new ServiceCollection();
        services.AddIntegrationEvent<SampleHappened>(SampleName);
        services.AddIntegrationEventConsumer<SampleHappened, RecordingConsumer>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var catalog = provider.GetRequiredService<IntegrationEventCatalog>();
        var registration = catalog.ConsumerOf(SampleName, nameof(RecordingConsumer));
        var sample = new SampleHappened(Guid.NewGuid());

        //when
        var outcome = await registration.InvokeAsync(scope.ServiceProvider, sample);

        //then
        scope.ServiceProvider.GetRequiredService<RecordingConsumer>().Received.ShouldBe([sample]);
        outcome.ShouldBe(DeliveryOutcome.Done);
    }

    [TestMethod]
    public void Done_Is_Not_Held()
    {
        //given
        var outcome = DeliveryOutcome.Done;

        //when
        var isHeld = outcome.IsHeld;

        //then
        isHeld.ShouldBeFalse();
        outcome.HoldUntilUtc.ShouldBeNull();
    }

    [TestMethod]
    public void Hold_Until_Carries_The_Instant()
    {
        //given
        var untilUtc = new DateTimeOffset(2026, 10, 24, 18, 0, 0, TimeSpan.Zero);

        //when
        var outcome = DeliveryOutcome.HoldUntil(untilUtc);

        //then
        outcome.IsHeld.ShouldBeTrue();
        outcome.HoldUntilUtc.ShouldBe(untilUtc);
    }

    [TestMethod]
    public void Registering_Through_Services_Keeps_One_Catalog()
    {
        //given
        var services = new ServiceCollection();

        //when
        services.AddIntegrationEvent<SampleHappened>(SampleName);
        services.AddIntegrationEvent<OtherHappened>("other-happened.v1");

        //then
        services.Count(x => x.ServiceType == typeof(IntegrationEventCatalog)).ShouldBe(1);
    }

    public record SampleHappened(Guid Id) : IIntegrationEvent;

    public record OtherHappened(Guid Id) : IIntegrationEvent;

    public class RecordingConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public List<SampleHappened> Received { get; } = [];

        public Task<DeliveryOutcome> ConsumeAsync(SampleHappened integrationEvent)
        {
            Received.Add(integrationEvent);

            return Task.FromResult(DeliveryOutcome.Done);
        }
    }

    public class OtherConsumer : IIntegrationEventConsumer<SampleHappened>
    {
        public Task<DeliveryOutcome> ConsumeAsync(SampleHappened integrationEvent)
        {
            return Task.FromResult(DeliveryOutcome.Done);
        }
    }
}
