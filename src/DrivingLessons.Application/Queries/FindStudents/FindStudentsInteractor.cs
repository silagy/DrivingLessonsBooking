namespace DrivingLessons.Application.Queries.FindStudents;

public class FindStudentsInteractor
{
    private readonly IStudentQueries queries;

    public FindStudentsInteractor(IStudentQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyCollection<ItemForFindStudentsResponse>> ExecuteAsync(Guid? teacherId)
    {
        return await queries.FindAsync(teacherId);
    }
}
