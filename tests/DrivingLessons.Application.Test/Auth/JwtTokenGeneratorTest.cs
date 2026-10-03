using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.Auth;
using DrivingLessons.Infrastructure.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class JwtTokenGeneratorTest
{
    private const string SignInEmail = "owner@school.example";

    private readonly JwtTokenGenerator generator = new(Microsoft.Extensions.Options.Options.Create(new JwtOptions
    {
        Issuer = "DrivingLessons",
        Audience = "DrivingLessons",
        SigningKey = "test-only-signing-key-at-least-32-characters-long!",
        ExpiryHours = 12
    }));

    [TestMethod]
    public void Token_Carries_The_User_Id_Email_Role_And_Security_Stamp()
    {
        //given
        var user = Administrator();

        //when
        var issued = generator.Generate(user);

        //then
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.AccessToken);
        token.GetClaim(JwtRegisteredClaimNames.Sub).Value.ShouldBe(user.Id.Value.ToString());
        token.GetClaim(JwtRegisteredClaimNames.Email).Value.ShouldBe(SignInEmail);
        token.GetClaim("role").Value.ShouldBe("administrator");
        token.GetClaim("security_stamp").Value.ShouldBe(user.SecurityStamp.Value);
    }

    [TestMethod]
    public void Administrator_Without_A_Teacher_Has_No_Teacher_Claim()
    {
        //given
        var user = Administrator();

        //when
        var issued = generator.Generate(user);

        //then
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.AccessToken);
        token.TryGetClaim("teacher_id", out _).ShouldBeFalse();
    }

    [TestMethod]
    public void Teacher_Token_Carries_The_Teacher_Role_And_The_Linked_Teacher_Id()
    {
        //given
        var teacher = Teacher.Create(TeacherName.Of("Teacher Cohen"), Email.Of("cohen@school.example"));
        var user = User.Create(
            UserName.Of("Teacher Cohen"),
            Email.Of("cohen.login@school.example"),
            PasswordHash.Of("stored-hash"),
            Role.Teacher,
            teacher);

        //when
        var issued = generator.Generate(user);

        //then
        var token = new JsonWebTokenHandler().ReadJsonWebToken(issued.AccessToken);
        token.GetClaim("role").Value.ShouldBe("teacher");
        token.GetClaim("teacher_id").Value.ShouldBe(teacher.Id.Value.ToString());
    }

    private static User Administrator()
    {
        return User.Create(
            UserName.Of("School Owner"),
            Email.Of(SignInEmail),
            PasswordHash.Of("stored-hash"),
            Role.Administrator,
            null);
    }
}
