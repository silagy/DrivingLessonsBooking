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
    public void Student_Car_Not_Of_Teacher_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentCarMustBeAssignedToTeacherException(CarId.New(), TeacherId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentCarMustBeAssignedToTeacher");
    }

    [TestMethod]
    public void Student_Already_Deactivated_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentAlreadyDeactivatedException(StudentId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentAlreadyDeactivated");
    }

    [TestMethod]
    public void Student_Already_Active_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentAlreadyActiveException(StudentId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentAlreadyActive");
    }

    [TestMethod]
    public void Student_Not_Found_By_Id_Is_A_Not_Found_With_Its_Code()
    {
        //given
        var context = ContextFor(new StudentNotFoundException(StudentId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status404NotFound);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentNotFound");
    }

    [TestMethod]
    public void Inactive_Student_Submitting_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new SubmissionStudentMustBeActiveException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "submissionStudentMustBeActive");
    }

    [TestMethod]
    public void National_Id_In_Use_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new StudentNationalIdAlreadyInUseException(StudentName.Of("נועה מזרחי")));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "studentNationalIdAlreadyInUse");
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
    public void Blank_Password_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new PasswordMustNotBeEmptyException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "passwordMustNotBeEmpty");
    }

    [TestMethod]
    public void Wrong_Current_Password_Is_A_Conflict_Not_An_Unauthorized()
    {
        //given
        var context = ContextFor(new UserCurrentPasswordMustBeCorrectException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userCurrentPasswordMustBeCorrect");
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
    public void Same_Role_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserAlreadyHasRoleException(UserId.New()));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userAlreadyHasRole");
    }

    [TestMethod]
    public void Changing_Your_Own_Role_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotChangeOwnRoleException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotChangeOwnRole");
    }

    [TestMethod]
    public void Demoting_The_Last_Active_Administrator_Is_A_Conflict_With_Its_Rule_As_Code()
    {
        //given
        var context = ContextFor(new UserMustNotDemoteLastActiveAdministratorException());

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "userMustNotDemoteLastActiveAdministrator");
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
        var parameters = ProblemOf(context).Extensions["params"]
            .ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["columns"] = "טלפון, מורה" });
    }

    [TestMethod]
    public void Teacher_With_Active_Students_Is_A_Conflict_Naming_Them()
    {
        //given
        var names = new[] { StudentName.Of("Avi Cohen"), StudentName.Of("Noa Mizrahi") };
        var context = ContextFor(new TeacherMustNotHaveActiveStudentsException(TeacherId.New(), names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Type.ShouldBeNull();
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherMustNotHaveActiveStudents");
        var parameters = problem.Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["count"] = "2", ["names"] = "Avi Cohen, Noa Mizrahi" });
    }

    [TestMethod]
    public void Car_With_Active_Students_Is_A_Conflict_Naming_Them()
    {
        //given
        var names = new[] { StudentName.Of("נועה מזרחי") };
        var context = ContextFor(new CarMustNotHaveActiveStudentsException(CarId.New(), names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "carMustNotHaveActiveStudents");
        var parameters = problem.Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["count"] = "1", ["names"] = "נועה מזרחי" });
    }

    [TestMethod]
    public void Teacher_Assignment_With_Active_Students_Is_A_Conflict_Naming_The_Teacher_And_Them()
    {
        //given
        var names = new[] { StudentName.Of("Noa Mizrahi") };
        var context = ContextFor(new TeacherAssignmentMustNotHaveActiveStudentsException(
            CarId.New(),
            TeacherId.New(),
            TeacherName.Of("Ronit Avraham"),
            names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var problem = ProblemOf(context);
        problem.Status.ShouldBe(StatusCodes.Status409Conflict);
        problem.Extensions.ShouldContainKeyAndValue("code", "teacherAssignmentMustNotHaveActiveStudents");
        var parameters = problem.Extensions["params"].ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string>
        {
            ["count"] = "1",
            ["names"] = "Noa Mizrahi",
            ["teacher"] = "Ronit Avraham"
        });
    }

    [TestMethod]
    public void Active_Students_Beyond_Three_Are_Counted_Not_Named()
    {
        //given
        var names = new[]
        {
            StudentName.Of("Avi Cohen"),
            StudentName.Of("Dana Sasson"),
            StudentName.Of("Lia Hadad"),
            StudentName.Of("Noa Mizrahi"),
            StudentName.Of("Omer Shalev")
        };
        var context = ContextFor(new CarMustNotHaveActiveStudentsException(CarId.New(), names));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var parameters = ProblemOf(context).Extensions["params"]
            .ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string>
        {
            ["count"] = "5",
            ["names"] = "Avi Cohen, Dana Sasson, Lia Hadad"
        });
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
        var parameters = ProblemOf(context).Extensions["params"]
            .ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["maxLength"] = "200" });
    }

    [TestMethod]
    public void National_Id_In_Use_Names_The_Existing_Student_In_Params()
    {
        //given
        var context = ContextFor(new StudentNationalIdAlreadyInUseException(StudentName.Of("נועה מזרחי")));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        var parameters = ProblemOf(context).Extensions["params"]
            .ShouldBeAssignableTo<IReadOnlyDictionary<string, string>>();
        parameters.ShouldBe(new Dictionary<string, string> { ["name"] = "נועה מזרחי" });
    }

    [TestMethod]
    public void National_Id_In_Use_Keeps_The_Name_Out_Of_Its_Detail()
    {
        //given
        var context = ContextFor(new StudentNationalIdAlreadyInUseException(StudentName.Of("נועה מזרחי")));

        //when
        new ApiExceptionFilter().OnException(context);

        //then
        ProblemOf(context).Detail.ShouldBe("Another Student already has this national ID.");
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
