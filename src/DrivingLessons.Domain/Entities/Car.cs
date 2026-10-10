using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Car : AggregateRoot<CarId>
{
    private readonly List<TeacherAssignment> teacherAssignments = [];

    public CarName Name { get; private set; }
    public CarType Type { get; private set; }
    public Transmission Transmission { get; private set; }
    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<TeacherAssignment> TeacherAssignments => teacherAssignments.AsReadOnly();

    private Car()
    {
    }

    private Car(CarId id, CarName name, CarType type, Transmission transmission, bool isDeleted)
        : base(id)
    {
        Name = name;
        Type = type;
        Transmission = transmission;
        IsDeleted = isDeleted;

        var createdEvent = new CarCreated(id, name, type, transmission);
        AddEvent(createdEvent);
    }

    public static Car Create(CarName name, CarType type, Transmission transmission)
    {
        const bool isDeleted = false;
        var id = CarId.New();

        return new Car(id, name, type, transmission, isDeleted);
    }

    public void ChangeDetails(CarName name, CarType type, Transmission transmission)
    {
        Name = name;
        Type = type;
        Transmission = transmission;

        AddEvent(new CarDetailsChanged(Id, name, type, transmission));
    }

    public void AssignTeacher(Teacher teacher)
    {
        MustNotBeAssigned(teacher);

        var assignment = TeacherAssignment.Create(teacher.Id);
        teacherAssignments.Add(assignment);

        AddEvent(new CarAssignedToTeacher(Id, teacher.Id));
    }

    public void UnassignTeacher(Teacher teacher, IReadOnlyCollection<Student> students)
    {
        var assignment = teacherAssignments.FirstOrDefault(x => x.TeacherId == teacher.Id)
                         ?? throw new TeacherNotAssignedToCarException(Id, teacher.Id);

        MustNotHaveActiveStudentsOf(teacher, students);

        teacherAssignments.Remove(assignment);

        AddEvent(new CarUnassignedFromTeacher(Id, teacher.Id));
    }

    public bool IsAssignedTo(Teacher teacher)
    {
        return teacherAssignments.Any(x => x.TeacherId == teacher.Id);
    }

    public void Delete(IReadOnlyCollection<Student> students)
    {
        MustNotBeDeleted();
        MustNotHaveActiveStudents(students);

        IsDeleted = true;

        AddEvent(new CarDeleted(Id));
    }

    private void MustNotBeAssigned(Teacher teacher)
    {
        if (IsAssignedTo(teacher))
        {
            throw new TeacherAlreadyAssignedToCarException(Id, teacher.Id);
        }
    }

    private void MustNotBeDeleted()
    {
        if (IsDeleted)
        {
            throw new CarAlreadyDeletedException(Id);
        }
    }

    private void MustNotHaveActiveStudentsOf(Teacher teacher, IReadOnlyCollection<Student> students)
    {
        var studentsOfTeacher = students.Where(x => x.TeacherId == teacher.Id);
        var activeStudentNames = ActiveStudentNamesOn(studentsOfTeacher);

        if (activeStudentNames.Count > 0)
        {
            throw new TeacherAssignmentMustNotHaveActiveStudentsException(
                Id,
                teacher.Id,
                teacher.Name,
                activeStudentNames);
        }
    }

    private void MustNotHaveActiveStudents(IReadOnlyCollection<Student> students)
    {
        var activeStudentNames = ActiveStudentNamesOn(students);

        if (activeStudentNames.Count > 0)
        {
            throw new CarMustNotHaveActiveStudentsException(Id, activeStudentNames);
        }
    }

    private List<StudentName> ActiveStudentNamesOn(IEnumerable<Student> students)
    {
        return students
               .Where(x => x.IsActive && x.CarId == Id)
               .Select(x => x.Name)
               .OrderBy(x => x.Value, StringComparer.Ordinal)
               .ToList();
    }
}
