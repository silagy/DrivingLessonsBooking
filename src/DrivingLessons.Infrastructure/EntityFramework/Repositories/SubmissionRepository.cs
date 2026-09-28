using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly DrivingLessonsDbContext dbContext;

    public SubmissionRepository(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Submission?> GetByPublicationAndStudentAsync(PublicationId publicationId, StudentId studentId)
    {
        return await dbContext
                         .Submissions
                         .FirstOrDefaultAsync(x => x.PublicationId == publicationId && x.StudentId == studentId);
    }

    public void Add(Submission submission)
    {
        dbContext.Submissions.Add(submission);
    }
}
