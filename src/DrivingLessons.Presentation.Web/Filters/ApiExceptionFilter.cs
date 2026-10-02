using System.Text.Json;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private const string CodeExtension = "code";

    public void OnException(ExceptionContext context)
    {
        var code = CodeOf(context.Exception);
        (int StatusCode, string Title, string? Type, string? Code)? mapping = context.Exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized", null, null),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", null, code),
            SubmissionWindowMustBeOpenException =>
                (StatusCodes.Status409Conflict, "Conflict", ProblemTypes.SubmissionWindowClosed, code),
            DomainException => (StatusCodes.Status409Conflict, "Conflict", null, code),
            _ => null
        };

        if (mapping is null)
        {
            return;
        }

        var (statusCode, title, type, problemCode) = mapping.Value;
        var problemDetails = new ProblemDetails
        {
            Type = type,
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        };

        if (problemCode is not null)
        {
            problemDetails.Extensions[CodeExtension] = problemCode;
        }

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }

    private static string CodeOf(Exception exception)
    {
        var typeName = exception.GetType().Name;
        var rule = typeName.EndsWith(nameof(Exception), StringComparison.Ordinal)
            ? typeName[..^nameof(Exception).Length]
            : typeName;

        return JsonNamingPolicy.CamelCase.ConvertName(rule);
    }
}
