using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindStudents;
using DrivingLessons.Application.Queries.GetStudent;
using DrivingLessons.Application.Queries.IdentifyStudent;
using DrivingLessons.Domain.Values;
using Microsoft.EntityFrameworkCore;

namespace DrivingLessons.Infrastructure.EntityFramework.Queries;

public class StudentQueries : IStudentQueries
{
    private readonly DrivingLessonsDbContext dbContext;

    public StudentQueries(DrivingLessonsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<ItemForFindStudentsResponse>> FindAsync(Guid? teacherId)
    {
        var students = dbContext.Students.AsQueryable();

        if (teacherId is not null)
        {
            var resolvedTeacherId = TeacherId.Of(teacherId.Value);
            students = students.Where(x => x.TeacherId == resolvedTeacherId);
        }

        var query = from student in students
                    join teacher in dbContext.Teachers
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars
                        on student.CarId equals car.Id
                    orderby teacher.Name, student.Name
                    select new ItemForFindStudentsResponse
                    {
                        Id = student.Id.Value,
                        NationalId = student.NationalId.Value,
                        Name = student.Name.Value,
                        Phone = student.Phone.Value,
                        TeacherId = teacher.Id.Value,
                        TeacherName = teacher.Name.Value,
                        CarId = car.Id.Value,
                        CarName = car.Name.Value,
                        CarTransmission = car.Transmission,
                        IsActive = student.IsActive
                    };

        return await query.ToListAsync();
    }

    public async Task<GetStudentResponse?> GetAsync(Guid id)
    {
        var studentId = StudentId.Of(id);

        var query = from student in dbContext.Students
                    join teacher in dbContext.Teachers
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars
                        on student.CarId equals car.Id
                    where student.Id == studentId
                    select new GetStudentResponse
                    {
                        Id = student.Id.Value,
                        NationalId = student.NationalId.Value,
                        Name = student.Name.Value,
                        Phone = student.Phone.Value,
                        TeacherId = teacher.Id.Value,
                        TeacherName = teacher.Name.Value,
                        CarId = car.Id.Value,
                        CarName = car.Name.Value,
                        CarTransmission = car.Transmission,
                        Address = student.Address == null
                            ? null
                            : student.Address.Value,
                        StartDate = student.StartDate == null
                            ? null
                            : (DateOnly?)student.StartDate.Value,
                        LicenseType = student.LicenseType == null
                            ? null
                            : student.LicenseType.Value,
                        IsActive = student.IsActive
                    };

        return await query.FirstOrDefaultAsync();
    }

    public async Task<IdentifyStudentResponse?> GetActiveByNationalIdAsync(NationalId nationalId, DateOnly weekStart)
    {
        var resolvedWeekStart = WeekStart.Of(weekStart);

        var query = from student in dbContext.Students
                    join teacher in dbContext.Teachers
                        on student.TeacherId equals teacher.Id
                    join car in dbContext.Cars
                        on student.CarId equals car.Id
                    where student.NationalId == nationalId && student.IsActive
                    select new
                    {
                        StudentId = student.Id,
                        TeacherId = teacher.Id,
                        StudentName = student.Name.Value,
                        TeacherName = teacher.Name.Value,
                        CarName = car.Name.Value,
                        car.Transmission
                    };

        var identified = await query.FirstOrDefaultAsync();

        if (identified is null)
        {
            return null;
        }

        var studentId = identified.StudentId;

        var submissionsThisWeek = from submission in dbContext.Submissions
                                  join publication in dbContext.Publications
                                      on submission.PublicationId equals publication.Id
                                  where submission.StudentId == studentId
                                        && publication.WeekStart == resolvedWeekStart
                                  select submission;

        var savedSubmission = await submissionsThisWeek
                                        .Select(SubmissionForIdentifyStudentResponse.Selector)
                                        .FirstOrDefaultAsync();

        var slots = await dbContext
                            .WeekSchedules
                            .Where(x => x.TeacherId == identified.TeacherId && x.WeekStart == resolvedWeekStart)
                            .SelectMany(x => x.Slots)
                            .OrderBy(slot => slot.Day)
                            .ThenBy(slot => slot.Window)
                            .Select(SlotForIdentifyStudentResponse.Selector)
                            .ToListAsync();

        return new IdentifyStudentResponse
        {
            StudentName = identified.StudentName,
            TeacherName = identified.TeacherName,
            CarName = identified.CarName,
            Transmission = identified.Transmission,
            Submission = savedSubmission,
            Slots = slots
        };
    }
}
