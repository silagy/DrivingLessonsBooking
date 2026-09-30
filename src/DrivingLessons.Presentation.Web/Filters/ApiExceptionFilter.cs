using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        (int StatusCode, string Title, string? Type)? mapping = context.Exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized", null),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found", null),
            SubmissionWindowMustBeOpenException =>
                (StatusCodes.Status409Conflict, "Conflict", ProblemTypes.SubmissionWindowClosed),
            DomainException => (StatusCodes.Status409Conflict, "Conflict", null),
            _ => null
        };

        if (mapping is null)
        {
            return;
        }

        var (statusCode, title, type) = mapping.Value;
        var problemDetails = new ProblemDetails
        {
            Type = type,
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        };

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
