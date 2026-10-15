using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DrivingLessons.Presentation.Web.RateLimiting;

public static class RateLimitingExtension
{
    public const int AdminSignInPermitsPerMinute = 5;
    public const int StudentIdentifyPermitsPerMinute = 30;
    public const int StudentSubmitPermitsPerMinute = 30;
    public const string TooManyRequestsCode = "tooManyRequests";

    private const string CodeExtension = "code";
    private const string ProblemContentType = "application/problem+json";
    private const string TooManyRequestsTitle = "Too Many Requests";
    private const string UnknownClient = "unknown";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            options.AddPolicy(
                RateLimitPolicies.AdminSignIn,
                context => PerClientFixedWindow(context, AdminSignInPermitsPerMinute));
            options.AddPolicy(
                RateLimitPolicies.StudentIdentify,
                context => PerClientFixedWindow(context, StudentIdentifyPermitsPerMinute));
            options.AddPolicy(
                RateLimitPolicies.StudentSubmit,
                context => PerClientFixedWindow(context, StudentSubmitPermitsPerMinute));
        });
    }

    public static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = Math.Ceiling(retryAfter.TotalSeconds);
            response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = TooManyRequestsTitle
        };
        problem.Extensions[CodeExtension] = TooManyRequestsCode;

        await response.WriteAsJsonAsync(problem, options: null, ProblemContentType, cancellationToken);
    }

    public static string PartitionKeyOf(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;

        return address?.ToString() ?? UnknownClient;
    }

    private static RateLimitPartition<string> PerClientFixedWindow(HttpContext context, int permitsPerMinute)
    {
        var clientKey = PartitionKeyOf(context);

        return RateLimitPartition.GetFixedWindowLimiter(
            clientKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = Window,
                QueueLimit = 0
            });
    }
}
