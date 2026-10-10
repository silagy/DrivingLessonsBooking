using System.Globalization;
using System.Text.Json;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    private const string CodeExtension = "code";
    private const string ParamsExtension = "params";
    private const string ColumnSeparator = ", ";
    private const int NamedStudentsLimit = 3;
    private const string MoreNamesSuffix = "...";

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

        var parameters = ParamsOf(context.Exception);

        if (parameters is not null)
        {
            problemDetails.Extensions[ParamsExtension] = parameters;
        }

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }

    private static IReadOnlyDictionary<string, string>? ParamsOf(Exception exception)
    {
        return exception switch
        {
            RosterFileMustContainRequiredColumnsException missing => new Dictionary<string, string>
            {
                ["columns"] = string.Join(ColumnSeparator, missing.MissingColumns)
            },
            SlotConstraintMustNotExceedMaxLengthException tooLong => new Dictionary<string, string>
            {
                ["maxLength"] = tooLong.MaxLength.ToString(CultureInfo.InvariantCulture)
            },
            StudentNationalIdAlreadyInUseException inUse => new Dictionary<string, string>
            {
                ["name"] = inUse.ExistingStudentName.Value
            },
            TeacherMustNotHaveActiveStudentsException teacher => ActiveStudentsParams(teacher.ActiveStudentNames),
            CarMustNotHaveActiveStudentsException car => ActiveStudentsParams(car.ActiveStudentNames),
            TeacherAssignmentMustNotHaveActiveStudentsException assignment => AssignmentParams(assignment),
            _ => null
        };
    }

    private static Dictionary<string, string> AssignmentParams(TeacherAssignmentMustNotHaveActiveStudentsException assignment)
    {
        var parameters = ActiveStudentsParams(assignment.ActiveStudentNames);
        parameters["teacher"] = assignment.TeacherName.Value;

        return parameters;
    }

    private static Dictionary<string, string> ActiveStudentsParams(IReadOnlyList<StudentName> activeStudentNames)
    {
        var namedStudents = activeStudentNames
                            .Take(NamedStudentsLimit)
                            .Select(x => x.Value);
        var joinedNames = string.Join(ColumnSeparator, namedStudents);
        var names = activeStudentNames.Count > NamedStudentsLimit
            ? joinedNames + MoreNamesSuffix
            : joinedNames;

        return new Dictionary<string, string>
        {
            ["count"] = activeStudentNames.Count.ToString(CultureInfo.InvariantCulture),
            ["names"] = names
        };
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
