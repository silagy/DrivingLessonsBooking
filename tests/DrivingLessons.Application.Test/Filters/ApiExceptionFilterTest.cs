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
    public void Sign_In_Email_In_Use_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserSignInEmailAlreadyInUseException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userSignInEmailAlreadyInUse");
    }

    [TestMethod]
    public void Teacher_Already_Linked_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TeacherAlreadyLinkedToUserException(TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherAlreadyLinkedToUser");
    }

    [TestMethod]
    public void Blank_Temporary_Password_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TemporaryPasswordMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "temporaryPasswordMustNotBeEmpty");
    }

    [TestMethod]
    public void Undefined_Role_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserRoleMustBeDefinedException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userRoleMustBeDefined");
    }

    [TestMethod]
    public void Missing_User_Is_Not_Found_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserNotFoundException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status404NotFound);
        problem.Extensions.ShouldContainKeyAndValue("code", "userNotFound");
    }

    [TestMethod]
    public void Teacher_With_An_Active_User_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new TeacherMustNotHaveActiveUserException(TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherMustNotHaveActiveUser");
    }

    [TestMethod]
    public void Already_Deleted_User_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserAlreadyDeletedException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userAlreadyDeleted");
    }

    [TestMethod]
    public void Already_Active_User_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserAlreadyActiveException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userAlreadyActive");
    }

    [TestMethod]
    public void Deleting_Yourself_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotDeleteSelfException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotDeleteSelf");
    }

    [TestMethod]
    public void Last_Active_Administrator_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotBeLastActiveAdministratorException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotBeLastActiveAdministrator");
    }

    [TestMethod]
    public void Deleted_Linked_Teacher_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserLinkedTeacherMustNotBeDeletedException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userLinkedTeacherMustNotBeDeleted");
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
    public void Missing_Roster_Columns_Are_Named_In_Params()
    {
        //given
        var missingColumns = new[] { "טלפון", "מורה" };
        var context = ContextFor(new RosterFileMustContainRequiredColumnsException(missingColumns));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var parameters = ProblemOf(context).Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["columns"] = "טלפון, מורה" });
    }

    [TestMethod]
    public void Constraint_Length_Limit_Is_Named_In_Params()
    {
        //given
        var maxLength = 200;
        var context = ContextFor(new SlotConstraintMustNotExceedMaxLengthException(maxLength));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var parameters = ProblemOf(context).Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["maxLength"] = "200" });
    }

    [TestMethod]
    public void Rule_Without_Values_Carries_No_Params()
    {
        //given
        var context = ContextFor(new CarNameMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        ProblemOf(context).Extensions.ShouldNotContainKey("params");
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
