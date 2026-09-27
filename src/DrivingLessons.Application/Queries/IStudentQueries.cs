using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Queries;

public interface IStudentQueries
{
    Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(Guid? teacherId);

    Task<IdentifyStudentResponse?> GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart);
}
