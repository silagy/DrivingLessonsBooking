namespace DrivingLessons.Application.Common;

public interface IUnitOfWork
{
    Task CommitAsync();
}
