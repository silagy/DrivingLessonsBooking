using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;

namespace DrivingLessons.Application.Queries;

public interface ITeacherQueries
{
    Task<GetTeacherResponse?> GetAsync(Guid id);

    Task<IReadOnlyCollection<ItemForFindTeachersResponse>> FindAsync();
}
