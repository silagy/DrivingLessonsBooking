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
        MustBeCarOfTeacher(car, teacher);

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
        MustBeCarOfTeacher(car, teacher);

        Name = name;
        Phone = phone;
        TeacherId = teacher.Id;
        CarId = car.Id;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;

        AddEvent(new StudentUpdatedFromRoster(Id, teacher.Id, car.Id));
    }

    public void ChangeDetails(
        NationalId nationalId,
        StudentName name,
        PhoneNumber phone,
        Address? address,
        LessonsStartDate? startDate,
        LicenseType? licenseType)
    {
        NationalId = nationalId;
        Name = name;
        Phone = phone;
        Address = address;
        StartDate = startDate;
        LicenseType = licenseType;

        AddEvent(new StudentDetailsChanged(Id, nationalId, name));
    }

    public void ChangeTeacher(Teacher teacher, Car car)
    {
        MustNotBeWithTeacher(teacher);
        MustBeCarOfTeacher(car, teacher);

        TeacherId = teacher.Id;
        CarId = car.Id;

        AddEvent(new StudentTeacherChanged(Id, teacher.Id, car.Id));
    }

    public void ChangeCar(Car car)
    {
        MustNotBeOnCar(car);
        MustBeCarOfCurrentTeacher(car);

        CarId = car.Id;

        AddEvent(new StudentCarChanged(Id, car.Id));
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

    private void MustNotBeWithTeacher(Teacher teacher)
    {
        if (teacher.Id == TeacherId)
        {
            throw new StudentAlreadyWithTeacherException(Id, teacher.Id);
        }
    }

    private void MustNotBeOnCar(Car car)
    {
        if (car.Id == CarId)
        {
            throw new StudentAlreadyOnCarException(Id, car.Id);
        }
    }

    private void MustBeCarOfCurrentTeacher(Car car)
    {
        if (!car.IsAssignedTo(TeacherId))
        {
            throw new StudentCarMustBeAssignedToTeacherException(car.Id, TeacherId);
        }
    }

    private static void MustBeCarOfTeacher(Car car, Teacher teacher)
    {
        if (!car.IsAssignedTo(teacher))
        {
            throw new StudentCarMustBeAssignedToTeacherException(car.Id, teacher.Id);
        }
    }
}
