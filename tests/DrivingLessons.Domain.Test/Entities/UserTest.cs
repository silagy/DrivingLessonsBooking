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

    [TestMethod]
    public void Restore()
    {
        //given
        var user = new UserFakeBuilder().BuildDeleted();

        //when
        user.Restore();

        //then
        user.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Restore__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().BuildDeleted();
        var stampBefore = user.SecurityStamp;

        //when
        user.Restore();

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void Restore__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().BuildDeleted();

        //when
        user.Restore();

        //then
        user
            .UncommittedEvents
            .OfType<UserRestored>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void Restore__Must_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .Build();

        //when
        var act = () => user.Restore();

        //then
        Should.Throw<UserAlreadyActiveException>(act);
    }

    [TestMethod]
    public void ChangeDetails()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user.Name.ShouldBe(name);
        user.SignInEmail.ShouldBe(signInEmail);
    }

    [TestMethod]
    public void ChangeDetails__Keeps_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user.SecurityStamp.ShouldBe(stampBefore);
    }

    [TestMethod]
    public void ChangeDetails__Keeps_The_Linked_Teacher()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var user = new UserFakeBuilder()
                   .WithRole(Role.Teacher)
                   .WithTeacher(teacher)
                   .Build();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void ChangeDetails__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        user.ChangeDetails(name, signInEmail);

        //then
        user
            .UncommittedEvents
            .OfType<UserDetailsChanged>()
            .Where(x => x.UserId == user.Id
                        && x.Name == name
                        && x.SignInEmail == signInEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangeDetails__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());

        //when
        var act = () => user.ChangeDetails(name, signInEmail);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }

    [TestMethod]
    [DataRow(Role.Administrator, Role.Teacher)]
    [DataRow(Role.Teacher, Role.Administrator)]
    public void ChangeRole(Role from, Role to)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(from)
                   .WithTeacher(TeacherFakeBuilder.Build())
                   .Build();

        //when
        user.ChangeRole(to);

        //then
        user.Role.ShouldBe(to);
    }

    [TestMethod]
    [DataRow(Role.Administrator, Role.Teacher)]
    [DataRow(Role.Teacher, Role.Administrator)]
    public void ChangeRole__Keeps_The_Linked_Teacher(Role from, Role to)
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var user = new UserFakeBuilder()
                   .WithRole(from)
                   .WithTeacher(teacher)
                   .Build();

        //when
        user.ChangeRole(to);

        //then
        user.TeacherId.ShouldBe(teacher.Id);
    }

    [TestMethod]
    public void ChangeRole__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(Role.Teacher)
                   .Build();
        var stampBefore = user.SecurityStamp;

        //when
        user.ChangeRole(Role.Administrator);

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void ChangeRole__Add_Event()
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(Role.Teacher)
                   .Build();

        //when
        user.ChangeRole(Role.Administrator);

        //then
        user
            .UncommittedEvents
            .OfType<UserRoleChanged>()
            .Where(x => x.UserId == user.Id
                        && x.Role == Role.Administrator)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangeRole__Must_Not_Have_Role(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .Build();
        var stampBefore = user.SecurityStamp;

        //when
        var act = () => user.ChangeRole(role);

        //then
        Should.Throw<UserAlreadyHasRoleException>(act);
        user.SecurityStamp.ShouldBe(stampBefore);
    }

    [TestMethod]
    public void ChangeRole__Must_Have_Linked_Teacher_For_Teacher_Role()
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(Role.Administrator)
                   .Build();

        //when
        var act = () => user.ChangeRole(Role.Teacher);

        //then
        Should.Throw<UserWithTeacherRoleMustHaveLinkedTeacherException>(act);
        user.Role.ShouldBe(Role.Administrator);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(99)]
    public void ChangeRole__Must_Be_Defined_Role(int value)
    {
        //given
        var user = new UserFakeBuilder().Build();
        var role = (Role)value;

        //when
        var act = () => user.ChangeRole(role);

        //then
        Should.Throw<UserRoleMustBeDefinedException>(act);
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangeRole__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .WithTeacher(TeacherFakeBuilder.Build())
                   .BuildDeleted();
        var otherRole = role is Role.Administrator
            ? Role.Teacher
            : Role.Administrator;

        //when
        var act = () => user.ChangeRole(otherRole);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }

    [TestMethod]
    public void SetTemporaryPassword()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.SetTemporaryPassword(passwordHash);

        //then
        user.PasswordHash.ShouldBe(passwordHash);
    }

    [TestMethod]
    public void SetTemporaryPassword__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.SetTemporaryPassword(passwordHash);

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    public void SetTemporaryPassword__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.SetTemporaryPassword(passwordHash);

        //then
        user
            .UncommittedEvents
            .OfType<UserTemporaryPasswordSet>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void SetTemporaryPassword__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var act = () => user.SetTemporaryPassword(passwordHash);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }

    [TestMethod]
    public void ChangePassword()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user.PasswordHash.ShouldBe(passwordHash);
    }

    [TestMethod]
    public void ChangePassword__Changes_The_Security_Stamp()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var stampBefore = user.SecurityStamp;
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user.SecurityStamp.ShouldNotBe(stampBefore);
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangePassword__Keeps_The_Role_And_The_Linked_Teacher(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .Build();
        var teacherBefore = user.TeacherId;
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user.Role.ShouldBe(role);
        user.TeacherId.ShouldBe(teacherBefore);
    }

    [TestMethod]
    public void ChangePassword__Add_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user
            .UncommittedEvents
            .OfType<UserPasswordChanged>()
            .Where(x => x.UserId == user.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void ChangePassword__Does_Not_Raise_The_Temporary_Password_Event()
    {
        //given
        var user = new UserFakeBuilder().Build();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        user.ChangePassword(passwordHash);

        //then
        user
            .UncommittedEvents
            .OfType<UserTemporaryPasswordSet>()
            .ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow(Role.Administrator)]
    [DataRow(Role.Teacher)]
    public void ChangePassword__Must_Not_Be_Deleted(Role role)
    {
        //given
        var user = new UserFakeBuilder()
                   .WithRole(role)
                   .BuildDeleted();
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        //when
        var act = () => user.ChangePassword(passwordHash);

        //then
        Should.Throw<UserAlreadyDeletedException>(act);
    }
}
