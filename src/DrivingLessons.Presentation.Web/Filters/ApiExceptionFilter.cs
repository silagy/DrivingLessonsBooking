using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var mapping = context.Exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            DomainException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => ((int, string)?)null
        };

        if (mapping is null)
        {
            return;
        }

        var (statusCode, title) = mapping.Value;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        };

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
