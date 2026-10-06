using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries.GetStudent;

public class GetStudentInteractor
{
    private readonly IStudentQueries queries;

    public GetStudentInteractor(IStudentQueries queries)
    {
        this.queries = queries;
    }

    public async Task<GetStudentResponse> ExecuteAsync(Guid id)
    {
        var student = await queries.GetAsync(id);

        if (student is null)
        {
            var studentId = StudentId.Of(id);
            throw new StudentNotFoundException(studentId);
        }

        return student;
    }
}
