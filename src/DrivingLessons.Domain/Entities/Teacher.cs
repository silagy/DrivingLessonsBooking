using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Teacher : AggregateRoot<TeacherId>
{
    public TeacherName Name { get; private set; }
    public Email ContactEmail { get; private set; }
    public bool IsDeleted { get; private set; }

    private Teacher()
    {
    }

    private Teacher(TeacherId id, TeacherName name, Email contactEmail, bool isDeleted)
        : base(id)
    {
        Name = name;
        ContactEmail = contactEmail;
        IsDeleted = isDeleted;

        var createdEvent = new TeacherCreated(id, name, contactEmail);
        AddEvent(createdEvent);
    }

    public static Teacher Create(TeacherName name, Email contactEmail)
    {
        const bool isDeleted = false;
        var id = TeacherId.New();

        return new Teacher(id, name, contactEmail, isDeleted);
    }

    public void ChangeDetails(TeacherName name, Email contactEmail)
    {
        Name = name;
        ContactEmail = contactEmail;

        AddEvent(new TeacherDetailsChanged(Id, name, contactEmail));
    }

    public void Delete()
    {
        MustNotBeDeleted();

        IsDeleted = true;

        AddEvent(new TeacherDeleted(Id));
    }

    private void MustNotBeDeleted()
    {
        if (IsDeleted)
        {
            throw new TeacherAlreadyDeletedException(Id);
        }
    }
}
