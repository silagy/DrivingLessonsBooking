using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Test.Entities.Fake;

public class UserFakeBuilder
{
    private Role role = Role.Administrator;
    private Teacher? teacher;

    public UserFakeBuilder WithRole(Role role)
    {
        this.role = role;

        return this;
    }

    public UserFakeBuilder WithTeacher(Teacher teacher)
    {
        this.teacher = teacher;

        return this;
    }

    public User Build()
    {
        var resolvedTeacher = role is Role.Teacher
            ? teacher ?? TeacherFakeBuilder.Build()
            : teacher;
        var name = UserName.Of(Faker.FakeString());
        var signInEmail = Email.Of(Faker.FakeEmail());
        var passwordHash = PasswordHash.Of(Faker.FakeString());

        return User.Create(name, signInEmail, passwordHash, role, resolvedTeacher);
    }

    public User BuildDeleted()
    {
        var user = Build();
        user.Delete();

        return user;
    }
}
