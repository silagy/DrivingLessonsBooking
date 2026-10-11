using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Common;

public interface ITeacherAndCarLocks
{
    Task LockAsync(IReadOnlyCollection<TeacherId> teacherIds, IReadOnlyCollection<CarId> carIds);

    Task LockAllAsync();
}
