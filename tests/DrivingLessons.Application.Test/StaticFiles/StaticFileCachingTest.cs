using DrivingLessons.Presentation.Web.StaticFiles;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Shouldly;

namespace DrivingLessons.Application.Test.StaticFiles;

[TestClass]
public class StaticFileCachingTest
{
    [TestMethod]
    [DataRow("index.html")]
    [DataRow("he.json")]
    [DataRow("en.json")]
    [DataRow("favicon.svg")]
    public void A_File_Without_A_Fingerprint_Is_Revalidated(string fileName)
    {
        //given
        var context = ResponseContextFor(fileName);

        //when
        StaticFileCaching.SetCacheControl(context);

        //then
        context.Context.Response.Headers.CacheControl.ToString().ShouldBe("no-cache");
    }

    [TestMethod]
    [DataRow("main-IUEPGTXJ.js")]
    [DataRow("chunk-3LBJAVKY.js")]
    [DataRow("styles-IQPK5K7E.css")]
    [DataRow("primeicons-4GST5W3O.woff2")]
    public void A_Fingerprinted_File_Is_Cached_For_A_Year(string fileName)
    {
        //given
        var context = ResponseContextFor(fileName);

        //when
        StaticFileCaching.SetCacheControl(context);

        //then
        context.Context.Response.Headers.CacheControl.ToString().ShouldBe("public, max-age=31536000, immutable");
    }

    private static StaticFileResponseContext ResponseContextFor(string fileName)
    {
        var file = A.Fake<IFileInfo>();
        A.CallTo(() => file.Name).Returns(fileName);
        var httpContext = new DefaultHttpContext();

        return new StaticFileResponseContext(httpContext, file);
    }
}
