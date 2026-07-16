using DrivingLessons.Domain.Entities;

namespace DrivingLessons.Domain.Repositories;

public interface IRosterImportRepository
{
    void Add(RosterImport rosterImport);
}
