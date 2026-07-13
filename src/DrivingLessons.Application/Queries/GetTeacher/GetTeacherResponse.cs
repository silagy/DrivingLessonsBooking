using System.Linq.Expressions;
using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Application.Queries.GetTeacher;

public class GetTeacherResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;

    public static Expression<Func<Teacher, GetTeacherResponse>> Selector =>
        x => new GetTeacherResponse
        {
            Id = x.Id.Value,
            Name = x.Name.Value,
            ContactEmail = x.ContactEmail.Value
        };
}
