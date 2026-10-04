using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class User : AggregateRoot<UserId>
{
    public UserName Name { get; private set; }
    public Email SignInEmail { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public Role Role { get; private set; }
    public TeacherId? TeacherId { get; private set; }
    public SecurityStamp SecurityStamp { get; private set; }
    public bool IsDeleted { get; private set; }

    private User()
    {
    }

    private User(
        UserId id,
        UserName name,
        Email signInEmail,
        PasswordHash passwordHash,
        Role role,
        TeacherId? teacherId,
        SecurityStamp securityStamp,
        bool isDeleted)
        : base(id)
    {
        Name = name;
        SignInEmail = signInEmail;
        PasswordHash = passwordHash;
        Role = role;
        TeacherId = teacherId;
        SecurityStamp = securityStamp;
        IsDeleted = isDeleted;

        var createdEvent = new UserCreated(id, name, signInEmail, role, teacherId);
        AddEvent(createdEvent);
    }

    public static User Create(
        UserName name,
        Email signInEmail,
        PasswordHash passwordHash,
        Role role,
        Teacher? teacher)
    {
        MustHaveDefinedRole(role);
        MustHaveLinkedTeacherForTeacherRole(role, teacher);

        const bool isDeleted = false;
        var id = UserId.New();
        var teacherId = teacher?.Id;
        var securityStamp = SecurityStamp.New();

        return new User(id, name, signInEmail, passwordHash, role, teacherId, securityStamp, isDeleted);
    }

    public void Delete()
    {
        MustNotBeDeleted();

        IsDeleted = true;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserDeleted(Id));
    }

    public void Restore()
    {
        MustBeDeleted();

        IsDeleted = false;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserRestored(Id));
    }

    public void ChangeDetails(UserName name, Email signInEmail)
    {
        MustNotBeDeleted();

        Name = name;
        SignInEmail = signInEmail;

        AddEvent(new UserDetailsChanged(Id, name, signInEmail));
    }

    public void ChangeRole(Role role)
    {
        MustNotBeDeleted();
        MustHaveDefinedRole(role);
        MustNotHaveRole(role);
        MustBeLinkedToTeacherForTeacherRole(role);

        Role = role;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserRoleChanged(Id, role));
    }

    public void SetTemporaryPassword(PasswordHash passwordHash)
    {
        MustNotBeDeleted();

        PasswordHash = passwordHash;
        SecurityStamp = SecurityStamp.New();

        AddEvent(new UserTemporaryPasswordSet(Id));
    }

    private static void MustHaveDefinedRole(Role role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new UserRoleMustBeDefinedException();
        }
    }

    private static void MustHaveLinkedTeacherForTeacherRole(Role role, Teacher? teacher)
    {
        if (role is Role.Teacher
            && teacher is null)
        {
            throw new UserWithTeacherRoleMustHaveLinkedTeacherException();
        }
    }

    private void MustNotHaveRole(Role role)
    {
        if (Role == role)
        {
            throw new UserAlreadyHasRoleException(Id);
        }
    }

    private void MustBeLinkedToTeacherForTeacherRole(Role role)
    {
        if (role is Role.Teacher
            && TeacherId is null)
        {
            throw new UserWithTeacherRoleMustHaveLinkedTeacherException();
        }
    }

    private void MustNotBeDeleted()
    {
        if (IsDeleted)
        {
            throw new UserAlreadyDeletedException(Id);
        }
    }

    private void MustBeDeleted()
    {
        if (!IsDeleted)
        {
            throw new UserAlreadyActiveException(Id);
        }
    }
}
