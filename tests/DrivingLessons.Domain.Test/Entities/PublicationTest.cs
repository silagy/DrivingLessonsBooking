using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class PublicationTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var publication = Publication.Create(weekStart);

        //then
        publication.WeekStart.ShouldBe(weekStart);
        publication.IsDraft.ShouldBeTrue();
        publication.LinkToken.Value.ShouldNotBeNullOrWhiteSpace();
        publication.TeacherVersions.ShouldBeEmpty();
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var weekStart = WeekStart.Of(new DateOnly(2026, 7, 19));

        //when
        var publication = Publication.Create(weekStart);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationCreated>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.WeekStart == weekStart)
                   .Where(x => x.LinkToken == publication.LinkToken)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Publish()
    {
        //given
        var publication = PublicationFakeBuilder.BuildDraft();
        var window = PublicationFakeBuilder.FakeWindow();

        //when
        publication.Publish(window);

        //then
        publication.IsPublished.ShouldBeTrue();
        publication.Window.ShouldBe(window);
    }

    [TestMethod]
    public void Publish__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildDraft();
        var window = PublicationFakeBuilder.FakeWindow();

        //when
        publication.Publish(window);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationPublished>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.Window == window)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Open)]
    [DataRow(PublicationState.Closed)]
    public void Publish__Must_Be_Draft(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var window = PublicationFakeBuilder.FakeWindow();

        //when
        var act = () => publication.Publish(window);

        //then
        Should.Throw<PublicationMustBeDraftException>(act);
    }

    [TestMethod]
    public void Open()
    {
        //given
        var publication = PublicationFakeBuilder.BuildPublished();

        //when
        publication.Open();

        //then
        publication.IsOpen.ShouldBeTrue();
    }

    [TestMethod]
    public void Open__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildPublished();

        //when
        publication.Open();

        //then
        publication.UncommittedEvents
                   .OfType<PublicationOpened>()
                   .Where(x => x.PublicationId == publication.Id)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Open)]
    [DataRow(PublicationState.Closed)]
    public void Open__Must_Be_Published(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();

        //when
        var act = () => publication.Open();

        //then
        Should.Throw<PublicationMustBePublishedException>(act);
    }

    [TestMethod]
    public void Close()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var teacherId = TeacherId.New();

        //when
        publication.Close([teacherId]);

        //then
        publication.IsClosed.ShouldBeTrue();
        publication.TeacherVersions.ShouldContain(x => x.TeacherId == teacherId && x.Version == 1);
    }

    [TestMethod]
    public void Close_Increments_Version_On_Reopen_And_Close()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var teacherId = TeacherId.New();
        publication.Close([teacherId]);
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(5);
        publication.Reopen(newEndUtc);

        //when
        publication.Close([teacherId]);

        //then
        publication.TeacherVersions.ShouldContain(x => x.TeacherId == teacherId && x.Version == 2);
    }

    [TestMethod]
    public void Close__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var teacherId = TeacherId.New();

        //when
        publication.Close([teacherId]);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationClosed>()
                   .Where(x => x.PublicationId == publication.Id)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Closed)]
    public void Close__Must_Be_Open(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var teacherId = TeacherId.New();

        //when
        var act = () => publication.Close([teacherId]);

        //then
        Should.Throw<PublicationMustBeOpenException>(act);
    }

    [TestMethod]
    public void Extend_Window()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var newEndUtc = publication.Window!.EndUtc.AddDays(2);

        //when
        publication.ExtendWindow(newEndUtc);

        //then
        publication.Window!.EndUtc.ShouldBe(newEndUtc);
    }

    [TestMethod]
    public void Extend_Window__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var newEndUtc = publication.Window!.EndUtc.AddDays(2);

        //when
        publication.ExtendWindow(newEndUtc);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationWindowExtended>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.NewEndUtc == newEndUtc)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Closed)]
    public void Extend_Window__Must_Be_Open(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(10);

        //when
        var act = () => publication.ExtendWindow(newEndUtc);

        //then
        Should.Throw<PublicationMustBeOpenException>(act);
    }

    [TestMethod]
    public void Extend_Window__Must_Be_Later()
    {
        //given
        var publication = PublicationFakeBuilder.BuildOpen();
        var sameEndUtc = publication.Window!.EndUtc;

        //when
        var act = () => publication.ExtendWindow(sameEndUtc);

        //then
        Should.Throw<WindowExtensionMustBeLaterException>(act);
    }

    [TestMethod]
    public void Reopen()
    {
        //given
        var publication = PublicationFakeBuilder.BuildClosed();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(7);

        //when
        publication.Reopen(newEndUtc);

        //then
        publication.IsOpen.ShouldBeTrue();
        publication.Window!.EndUtc.ShouldBe(newEndUtc);
    }

    [TestMethod]
    public void Reopen__Add_Event()
    {
        //given
        var publication = PublicationFakeBuilder.BuildClosed();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(7);

        //when
        publication.Reopen(newEndUtc);

        //then
        publication.UncommittedEvents
                   .OfType<PublicationReopened>()
                   .Where(x => x.PublicationId == publication.Id)
                   .Where(x => x.NewEndUtc == newEndUtc)
                   .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(PublicationState.Draft)]
    [DataRow(PublicationState.Published)]
    [DataRow(PublicationState.Open)]
    public void Reopen__Must_Be_Closed(PublicationState state)
    {
        //given
        var publication = PublicationFakeBuilder.StateBuilders[state]();
        var newEndUtc = DateTimeOffset.UtcNow.AddDays(7);

        //when
        var act = () => publication.Reopen(newEndUtc);

        //then
        Should.Throw<PublicationMustBeClosedException>(act);
    }
}
