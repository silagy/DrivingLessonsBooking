using System.Security.Claims;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class HttpCurrentUserTest
{
    [TestMethod]
    public void Reads_The_Signed_In_User_From_The_Subject_Claim()
    {
        //given
        var id = Guid.NewGuid();
        var accessor = AccessorWith(new Claim(JwtRegisteredClaimNames.Sub, id.ToString()));

        //when
        var currentUserId = new HttpCurrentUser(accessor).Id;

        //then
        currentUserId.ShouldBe(UserId.Of(id));
    }

    [TestMethod]
    public void Request_Without_A_Signed_In_User_Fails_Loudly()
    {
        //given
        var accessor = AccessorWith();

        //when
        var act = () => new HttpCurrentUser(accessor).Id;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    public void Reads_An_Administrator_From_The_Role_Claim()
    {
        //given
        var accessor = AccessorWith(new Claim(AuthClaims.Role, AuthClaims.AdministratorRole));

        //when
        var role = new HttpCurrentUser(accessor).Role;

        //then
        role.ShouldBe(Role.Administrator);
    }

    [TestMethod]
    public void Reads_A_Teacher_And_The_Linked_Teacher()
    {
        //given
        var teacherId = Guid.NewGuid();
        var accessor = AccessorWith(
            new Claim(AuthClaims.Role, AuthClaims.TeacherRole),
            new Claim(AuthClaims.TeacherId, teacherId.ToString()));
        var currentUser = new HttpCurrentUser(accessor);

        //when
        var role = currentUser.Role;
        var linkedTeacherId = currentUser.TeacherId;

        //then
        role.ShouldBe(Role.Teacher);
        linkedTeacherId.ShouldBe(TeacherId.Of(teacherId));
    }

    [TestMethod]
    public void Reads_The_Teacher_Linked_To_An_Administrator()
    {
        //given
        var teacherId = Guid.NewGuid();
        var accessor = AccessorWith(
            new Claim(AuthClaims.Role, AuthClaims.AdministratorRole),
            new Claim(AuthClaims.TeacherId, teacherId.ToString()));

        //when
        var linkedTeacherId = new HttpCurrentUser(accessor).TeacherId;

        //then
        linkedTeacherId.ShouldBe(TeacherId.Of(teacherId));
    }

    [TestMethod]
    public void Unlinked_Administrator_Has_No_Linked_Teacher()
    {
        //given
        var accessor = AccessorWith(new Claim(AuthClaims.Role, AuthClaims.AdministratorRole));

        //when
        var linkedTeacherId = new HttpCurrentUser(accessor).TeacherId;

        //then
        linkedTeacherId.ShouldBeNull();
    }

    [TestMethod]
    [DataRow("student")]
    [DataRow("Teacher")]
    public void Unknown_Role_Fails_Loudly(string role)
    {
        //given
        var currentUser = new HttpCurrentUser(AccessorWith(new Claim(AuthClaims.Role, role)));

        //when
        var act = () =>
        {
            _ = currentUser.Role;
        };

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    public void Token_Without_A_Role_Fails_Loudly()
    {
        //given
        var currentUser = new HttpCurrentUser(AccessorWith());

        //when
        var act = () =>
        {
            _ = currentUser.Role;
        };

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    public void Teacher_Without_A_Linked_Teacher_Fails_Loudly()
    {
        //given
        var accessor = AccessorWith(new Claim(AuthClaims.Role, AuthClaims.TeacherRole));

        //when
        var act = () => new HttpCurrentUser(accessor).TeacherId;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    [TestMethod]
    [DataRow(AuthClaims.TeacherRole, "not-a-guid")]
    [DataRow(AuthClaims.TeacherRole, "00000000-0000-0000-0000-000000000000")]
    [DataRow(AuthClaims.AdministratorRole, "not-a-guid")]
    public void Malformed_Teacher_Id_Fails_Loudly(string role, string teacherId)
    {
        //given
        var accessor = AccessorWith(
            new Claim(AuthClaims.Role, role),
            new Claim(AuthClaims.TeacherId, teacherId));

        //when
        var act = () => new HttpCurrentUser(accessor).TeacherId;

        //then
        Should.Throw<InvalidOperationException>(act);
    }

    private static HttpContextAccessor AccessorWith(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        return new HttpContextAccessor { HttpContext = httpContext };
    }
}
