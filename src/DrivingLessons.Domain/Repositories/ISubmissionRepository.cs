using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Repositories;

public interface ISubmissionRepository
{
    Task<Submission?> GetByPublicationAndStudentAsync(PublicationId publicationId, StudentId studentId);

    void Add(Submission submission);
}
