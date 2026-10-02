using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;
using DrivingLessons.Presentation.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Shouldly;

namespace DrivingLessons.Application.Test.Filters;

[TestClass]
public class ApiExceptionFilterTest
{
    [TestMethod]
    public void Rule_Violation_Carries_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new CarNameMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "carNameMustNotBeEmpty");
    }

    [TestMethod]
    public void Missing_Entity_Carries_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TeacherNotFoundException(TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status404NotFound);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherNotFound");
    }

    [TestMethod]
    public void Closed_Window_Keeps_Its_Problem_Type_And_Carries_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new SubmissionWindowMustBeOpenException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Type.ShouldBe(ProblemTypes.SubmissionWindowClosed);
        problem.Extensions.ShouldContainKeyAndValue("code", "submissionWindowMustBeOpen");
    }

    [TestMethod]
    public void Rule_Violation_Keeps_Its_Detail_For_Developers()
    {
        //given
        var context = ContextFor(new CarNameMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        ProblemOf(context).Detail.ShouldBe("Car name must not be empty.");
    }

    [TestMethod]
    public void Failed_Sign_In_Carries_No_Code()
    {
        //given
        var context = ContextFor(new AuthenticationFailedException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status401Unauthorized);
        problem.Extensions.ShouldNotContainKey("code");
    }

    private static ExceptionContext ContextFor(Exception exception)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

        return new ExceptionContext(actionContext, []) { Exception = exception };
    }

    private static ProblemDetails ProblemOf(ExceptionContext context)
    {
        var result = context.Result.ShouldBeOfType<ObjectResult>();

        return result.Value.ShouldBeOfType<ProblemDetails>();
    }
}
