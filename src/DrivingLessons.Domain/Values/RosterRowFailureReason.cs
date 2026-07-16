namespace DrivingLessons.Domain.Values;

public enum RosterRowFailureReason
{
    InvalidNationalId = 10,
    DuplicateNationalId = 20,
    MissingName = 30,
    MissingPhone = 40,
    MissingTeacher = 50,
    MissingCar = 60,
    UnknownTeacher = 70,
    UnknownCar = 80,
    InvalidStartDate = 90
}
