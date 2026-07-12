using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetTeacher;

public class GetTeacherInteractor
{
    private readonly ITeacherQueries queries;

    public GetTeacherInteractor(ITeacherQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetTeacherResponse> ExecuteAsync(Guid id)
    {
        var teacher = await queries.GetAsync(id);

        if (teacher is null)
        {
            var teacherId = TeacherId.Of(id);
            throw new TeacherNotFoundException(teacherId);
        }

        return teacher;
    }
}
