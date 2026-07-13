using DrivingLessons.Application.EventHandlers;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using FakeItEasy;

namespace DrivingLessons.Application.Test.EventHandlers;

[TestClass]
public class WeekScheduleCreatedHandlerTest
{
    private IPublicationRepository repository = null!;
    private WeekScheduleCreatedHandler handler = null!;

    [TestInitialize]
    public void Init()
    {
        repository = A.Fake<IPublicationRepository>();
        handler = new WeekScheduleCreatedHandler(repository);
    }

    [TestMethod]
    public async Task Creates_Draft_Publication_When_None_Exists()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));
        var domainEvent = new WeekScheduleCreated(WeekScheduleId.New(), TeacherId.New(), weekStart);

        A.CallTo(() => repository.GetByWeekAsync(weekStart))
            .Returns((Publication?)null);

        //when
        await handler.HandleAsync(domainEvent);

        //then
        A.CallTo(() => repository.Add(A<Publication>.That.Matches(x => x.WeekStart == weekStart)))
            .MustHaveHappenedOnceExactly();
    }

    [TestMethod]
    public async Task Skips_Creation_When_Publication_Exists()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));
        var domainEvent = new WeekScheduleCreated(WeekScheduleId.New(), TeacherId.New(), weekStart);
        var existing = Publication.Create(weekStart);

        A.CallTo(() => repository.GetByWeekAsync(weekStart))
            .Returns(existing);

        //when
        await handler.HandleAsync(domainEvent);

        //then
        A.CallTo(() => repository.Add(A<Publication>._))
            .MustNotHaveHappened();
    }
}
