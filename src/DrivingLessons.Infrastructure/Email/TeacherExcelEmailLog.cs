using DrivingLessons.Application.Abstractions;
using DrivingLessons.Domain.Values;
using DrivingLessons.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.Email;

public class TeacherExcelEmailLog : ITeacherExcelEmailLog
{
    private readonly DrivingLessonsDbContext context;

    public TeacherExcelEmailLog(DrivingLessonsDbContext context)
    {
        this.context = context;
    }

    public async Task<bool> WasSentAsync(PublicationId publicationId, TeacherId teacherId, int version)
    {
        var publicationGuid = publicationId.Value;
        var teacherGuid = teacherId.Value;

        return await context.SentTeacherExcelEmails.AnyAsync(
            x => x.PublicationId == publicationGuid && x.TeacherId == teacherGuid && x.Version == version);
    }

    public void RecordSent(PublicationId publicationId, TeacherId teacherId, int version, DateTimeOffset sentAtUtc)
    {
        var sent = SentTeacherExcelEmail.Create(publicationId.Value, teacherId.Value, version, sentAtUtc);

        context.SentTeacherExcelEmails.Add(sent);
    }
}
