namespace DrivingLessons.Application.Queries.FindTeachers;

public class FindTeachersInteractor
{
    private readonly ITeacherQueries queries;

    public FindTeachersInteractor(ITeacherQueries queries)
    {
        this.queries = queries;
    }

    public async Task<IReadOnlyCollection<ItemForFindTeachersResponse>> ExecuteAsync()
    {
        return await queries.FindAsync();
    }
}
