using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.FindTeachers;

public class ItemForFindTeachersResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public IReadOnlyCollection<CarForFindTeachersResponse> Cars { get; init; } = [];

    public static Expression<Func<Teacher, ItemForFindTeachersResponse>> Selector =>
        x => new ItemForFindTeachersResponse
        {
            Id = x.Id.Value,
            Name = x.Name.Value,
            ContactEmail = x.ContactEmail.Value,
            Cars = x.Cars
                       .Select(car => new CarForFindTeachersResponse
                       {
                           Id = car.Id.Value,
                           Name = car.Name.Value,
                           Type = car.Type.Value,
                           Transmission = car.Transmission
                       })
                       .ToList()
        };
}

public class CarForFindTeachersResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Transmission Transmission { get; init; }
}
