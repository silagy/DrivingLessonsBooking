using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Student : AggregateRoot<StudentId>
{
    public NationalId NationalId { get; private set; }
    public StudentName Name { get; private set; }
    public PhoneNumber Phone { get; private set; }
    public TeacherId TeacherId { get; private set; }
    public CarId CarId { get; private set; }
    public Address? Address { get; private set; }
    public LessonsStartDate? StartDate { get; private set; }
    public LicenseType? LicenseType { get; private set; }
    public bool IsActive { get; private set; }

    private Student()
    {
    }

    private Student(
        StudentId id,
        NationalId nationalId,
        StudentName name,
        PhoneNumber phone,
        TeacherId teacherId,
        CarId carId,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType,
        bool isActive)
        : base(id)
    {
        NationalId = nationalId;
        Name = name;
        Phone = phone;
        TeacherId = teacherId;
        CarId = carId;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;
        IsActive = isActive;

        var createdEvent = new StudentCreated(id, nationalId, name, teacherId, carId);
        AddEvent(createdEvent);
    }

    public static Student Create(
        NationalId nationalId,
        StudentName name,
        PhoneNumber phone,
        Teacher teacher,
        Car car,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType)
    {
        const bool isActive = true;
        var id = StudentId.New();

        return new Student(
            id,
            nationalId,
            name,
            phone,
            teacher.Id,
            car.Id,
            address,
            startDate,
            licenseType,
            isActive);
    }

    public void UpdateFromRoster(
        StudentName name,
        PhoneNumber phone,
        Teacher teacher,
        Car car,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType)
    {
        Name = name;
        Phone = phone;
        TeacherId = teacher.Id;
        CarId = car.Id;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;

        AddEvent(new StudentUpdatedFromRoster(Id, teacher.Id, car.Id));
    }

    public void Deactivate()
    {
        MustBeActive();

        IsActive = false;

        AddEvent(new StudentDeactivated(Id));
    }

    public void Reactivate()
    {
        MustBeInactive();

        IsActive = true;

        AddEvent(new StudentReactivated(Id));
    }

    private void MustBeActive()
    {
        if (!IsActive)
        {
            throw new StudentAlreadyDeactivatedException(Id);
        }
    }

    private void MustBeInactive()
    {
        if (IsActive)
        {
            throw new StudentAlreadyActiveException(Id);
        }
    }
}
