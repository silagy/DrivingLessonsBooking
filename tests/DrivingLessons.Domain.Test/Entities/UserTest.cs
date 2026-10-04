using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class UserTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var user = User.Create(name, signInEmail, passwordHash, Role.Administrator, null);

        //then
        user.Name.ShouldBe(name);
        user.SignInEmail.ShouldBe(signInEmail);
        user.PasswordHash.ShouldBe(passwordHash);
        user.Role.ShouldBe(Role.Administrator);
        user.TeacherId.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void Create__Links_The_Teacher(Role role)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //when
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .WithTeacher(teacher)
                   .Build();

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());
        var teacher = TeacherFakeBuilder.Build();

        //when
        var user = User.Create(name, signInEmail, passwordHash, Role.Teacher, teacher);

        //then
        user
            .UncommittedEvents
            .OfType<UserCreated>()
            .Where(x => x.UserId == user.Id
                        && x.Name == name
                        && x.SignInEmail == signInEmail
                        && x.Role == Role.Teacher
                        && x.TeacherId == teacher.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Create__Must_Have_Linked_Teacher_For_Teacher_Role()
    {
        //given
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var act = () => User.Create(name, signInEmail, passwordHash, Role.Teacher, null);

        //then
        Should.Throw<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(99)]
    public void Create__Must_Be_Defined_Role(int value)
    {
        //given
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());
        var role = (Role)value;

        //when
        var act = () => User.Create(name, signInEmail, passwordHash, role, null);

        //then
        Should.Throw<UserRoleMustBeDefinedException>(act);
    }

    [TestMethod]
    public void New_User_Is_Not_Deleted()
    {
        //given
        var user = new UserFakeBuilder().Build();

        //expected
        user.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void New_Users_Get_Distinct_Security_Stamps()
    {
        //given
        var first = new UserFakeBuilder().Build();
        var second = new UserFakeBuilder().Build();

        //expected
        first.SecurityStamp.ShouldNotBe(second.SecurityStamp);
    }

    [TestMethod]
    public void Delete()
    {
        //given
        var user = new UserFakeBuilder().Build();

        //when
        user.Delete();

        //then
        user.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;

        //when
        user.Delete();

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void Delete__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();

        //when
        user.Delete();

        //then
        user
            .UncommittedEvents
            .OfType<UserDeleted>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void Delete__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();

        //when
        var act = () => user.Delete();

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }
}
