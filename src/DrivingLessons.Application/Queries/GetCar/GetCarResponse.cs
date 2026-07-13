using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetCar;

public class GetCarResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
    public IReadOnlyCollection<TeacherForGetCarResponse> AssignedTeachers { get; init; } = [];

    public static Expression<Func<Car, GetCarResponse>> Selector(IQueryable<Teacher> teachers)
    {
        return car => new GetCarResponse
        {
            Id = car.Id.Value,
            Name = car.Name.Value,
            Type = car.Type.Value,
            Transmission = car.Transmission,
            AssignedTeachers = teachers
                                   .Where(teacher => car.TeacherAssignments.Any(x => x.TeacherId == teacher.Id))
                                   .Select(teacher => new TeacherForGetCarResponse
                                   {
                                       Id = teacher.Id.Value,
                                       Name = teacher.Name.Value
                                   })
                                   .ToList()
        };
    }
}

public class TeacherForGetCarResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
