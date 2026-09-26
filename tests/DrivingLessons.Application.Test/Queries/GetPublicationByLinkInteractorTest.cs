using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.GetPublicationByLink;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Queries;

[TestClass]
public class GetPublicationByLinkInteractorTest
{
    private IPublicationQueries queries = null!;
    private GetPublicationByLinkInteractor interactor = null!;
    private string linkToken = null!;

    [TestInitialize]
    public void Init()
    {
        queries = A.Fake<IPublicationQueries>();
        interactor = new GetPublicationByLinkInteractor(queries);
        linkToken = ShareableLinkToken.New().Value;
    }

    [TestMethod]
    public async Task Returns_The_Publication_For_The_Link()
    {
        //given
        var publication = new GetPublicationByLinkResponse
        {
            WeekStart = new DateOnly(2026, 6, 14),
            WeekNumber = 25,
            State = PublicationState.Published,
            WindowStartUtc = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero),
            WindowEndUtc = new DateTimeOffset(2026, 6, 12, 11, 0, 0, TimeSpan.Zero)
        };

        A.CallTo(() => queries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns(publication);

        //when
        var response = await interactor.ExecuteAsync(linkToken);

        //then
        response.ShouldBeSameAs(publication);
    }

    [TestMethod]
    public async Task Publication_Must_Exist_For_The_Link()
    {
        //given
        A.CallTo(() => queries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns((GetPublicationByLinkResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(linkToken);

        //then
        await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
    }

    [TestMethod]
    public async Task Not_Found_Message_Does_Not_Reveal_The_Link_Token()
    {
        //given
        A.CallTo(() => queries.GetByLinkTokenExcludingDraftsAsync(linkToken))
            .Returns((GetPublicationByLinkResponse?)null);

        //when
        var act = () => interactor.ExecuteAsync(linkToken);

        //then
        var exception = await Should.ThrowAsync<PublicationLinkNotFoundException>(act);
        exception.Message.ShouldNotContain(linkToken);
    }
}
