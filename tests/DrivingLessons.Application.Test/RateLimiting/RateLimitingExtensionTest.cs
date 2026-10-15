using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using DrivingLessons.Presentation.Web.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace DrivingLessons.Application.Test.RateLimiting;

[TestClass]
public class RateLimitingExtensionTest
{
    [TestMethod]
    public async Task Rejection_Is_A_Too_Many_Requests_Problem_With_A_Code()
    {
        //given
        var context = RejectedContext(new RejectedLease(null));

        //when
        await RateLimitingExtension.WriteRejectionAsync(context, CancellationToken.None);

        //then
        var response = context.HttpContext.Response;
        response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        response.ContentType.ShouldBe("application/problem+json");
        var body = await BodyOf(response);
        body.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status429TooManyRequests);
        body.GetProperty("code").GetString().ShouldBe("tooManyRequests");
    }

    [TestMethod]
    public async Task Rejection_Tells_The_Client_When_To_Retry()
    {
        //given
        var context = RejectedContext(new RejectedLease(TimeSpan.FromSeconds(41.2)));

        //when
        await RateLimitingExtension.WriteRejectionAsync(context, CancellationToken.None);

        //then
        context.HttpContext.Response.Headers.RetryAfter.ToString().ShouldBe("42");
    }

    [TestMethod]
    public async Task Rejection_Without_A_Known_Wait_Sends_No_Retry_After()
    {
        //given
        var context = RejectedContext(new RejectedLease(null));

        //when
        await RateLimitingExtension.WriteRejectionAsync(context, CancellationToken.None);

        //then
        context.HttpContext.Response.Headers.ShouldNotContainKey("Retry-After");
    }

    [TestMethod]
    public void Each_Client_Address_Has_Its_Own_Limit()
    {
        //given
        var first = ContextFrom(IPAddress.Parse("203.0.113.10"));
        var second = ContextFrom(IPAddress.Parse("203.0.113.11"));

        //when
        var firstKey = RateLimitingExtension.PartitionKeyOf(first);
        var secondKey = RateLimitingExtension.PartitionKeyOf(second);

        //then
        firstKey.ShouldBe("203.0.113.10");
        secondKey.ShouldNotBe(firstKey);
    }

    [TestMethod]
    public void Clients_Without_An_Address_Share_One_Limit()
    {
        //given
        var context = ContextFrom(null);

        //when
        var key = RateLimitingExtension.PartitionKeyOf(context);

        //then
        key.ShouldBe("unknown");
    }

    [TestMethod]
    public async Task Sending_A_Submission_Is_Not_Blocked_By_Spent_Identify_Attempts()
    {
        //given
        var address = IPAddress.Parse("203.0.113.20");
        var pipeline = RateLimitedPipeline();

        for (var attempt = 0; attempt < RateLimitingExtension.StudentIdentifyPermitsPerMinute; attempt++)
        {
            await pipeline(PolicyRequest(address, RateLimitPolicies.StudentIdentify));
        }

        var identify = PolicyRequest(address, RateLimitPolicies.StudentIdentify);
        await pipeline(identify);
        var submit = PolicyRequest(address, RateLimitPolicies.StudentSubmit);

        //when
        await pipeline(submit);

        //then
        identify.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
        submit.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task Sending_A_Submission_Is_Limited_Per_Minute()
    {
        //given
        var address = IPAddress.Parse("203.0.113.21");
        var pipeline = RateLimitedPipeline();

        for (var attempt = 0; attempt < RateLimitingExtension.StudentSubmitPermitsPerMinute; attempt++)
        {
            await pipeline(PolicyRequest(address, RateLimitPolicies.StudentSubmit));
        }

        var submit = PolicyRequest(address, RateLimitPolicies.StudentSubmit);

        //when
        await pipeline(submit);

        //then
        submit.Response.StatusCode.ShouldBe(StatusCodes.Status429TooManyRequests);
    }

    private static RequestDelegate RateLimitedPipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiRateLimiting();
        var provider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(provider);
        app.UseRateLimiter();
        app.Run(context => Task.CompletedTask);

        return app.Build();
    }

    private static DefaultHttpContext PolicyRequest(IPAddress address, string policy)
    {
        var context = ContextFrom(address);
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        context.Response.Body = new MemoryStream();
        var metadata = new EndpointMetadataCollection(new EnableRateLimitingAttribute(policy));
        var endpoint = new Endpoint(null, metadata, policy);
        context.SetEndpoint(endpoint);

        return context;
    }

    private static OnRejectedContext RejectedContext(RateLimitLease lease)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Response.Body = new MemoryStream();

        return new OnRejectedContext { HttpContext = httpContext, Lease = lease };
    }

    private static DefaultHttpContext ContextFrom(IPAddress? address)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = address;

        return context;
    }

    private static async Task<JsonElement> BodyOf(HttpResponse response)
    {
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);

        return document.RootElement.Clone();
    }

    private sealed class RejectedLease(TimeSpan? retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames =>
            retryAfter is null
                ? []
                : [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            var isRetryAfter = metadataName == MetadataName.RetryAfter.Name;
            metadata = isRetryAfter
                ? retryAfter
                : null;

            return isRetryAfter && retryAfter is not null;
        }
    }
}
