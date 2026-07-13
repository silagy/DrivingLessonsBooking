using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using DrivingLessons.Infrastructure.DomainEvents;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DrivingLessons.Application.Test.DomainEvents;

[TestClass]
public class DomainEventDispatcherTest
{
    public record FakeDomainEvent(Guid Id) : IDomainEvent;

    [TestMethod]
    public async Task Dispatches_To_Registered_Handler()
    {
        //given
        var handler = A.Fake<IDomainEventHandler<FakeDomainEvent>>();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<FakeDomainEvent>>(handler);
        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);
        var domainEvent = new FakeDomainEvent(Guid.NewGuid());

        //when
        await dispatcher.DispatchAsync(domainEvent);

        //then
        A.CallTo(() => handler.HandleAsync(domainEvent))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Ignores_Events_Without_Handlers()
    {
        //given
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);
        var domainEvent = new FakeDomainEvent(Guid.NewGuid());

        //when
        var act = () => dispatcher.DispatchAsync(domainEvent);

        //then
        await Should.NotThrowAsync(act);
    }
}
