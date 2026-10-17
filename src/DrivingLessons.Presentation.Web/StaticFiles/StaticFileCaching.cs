using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace DrivingLessons.Presentation.Web.StaticFiles;

public static partial class StaticFileCaching
{
    public const string Revalidate = "no-cache";
    public const string CacheForAYear = "public, max-age=31536000, immutable";

    public static StaticFileOptions Options => new()
    {
        OnPrepareResponse = SetCacheControl
    };

    public static void SetCacheControl(StaticFileResponseContext context)
    {
        var fingerprint = FingerprintedFileName();
        var isFingerprinted = fingerprint.IsMatch(context.File.Name);

        context.Context.Response.Headers.CacheControl = isFingerprinted
            ? CacheForAYear
            : Revalidate;
    }

    [GeneratedRegex("-[A-Z0-9]{8}\\.[a-z0-9]+$")]
    private static partial Regex FingerprintedFileName();
}
