using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetTeacher;

public class GetTeacherResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public IReadOnlyCollection<CarForGetTeacherResponse> Cars { get; init; } = [];

    public static Expression<Func<Teacher, GetTeacherResponse>> Selector =>
        x => new GetTeacherResponse
        {
            Id = x.Id.Value,
            Name = x.Name.Value,
            ContactEmail = x.ContactEmail.Value,
            Cars = x.Cars
                       .Where(car => !car.IsRemoved)
                       .Select(car => new CarForGetTeacherResponse
                       {
                           Id = car.Id.Value,
                           Name = car.Name.Value,
                           Type = car.Type.Value,
                           Transmission = car.Transmission
                       })
                       .ToList()
        };
}

public class CarForGetTeacherResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
}
