using DrivingLessons.Application.Queries;
using DrivingLessons.Application.Queries.FindStudents;
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
                        IsActive = student.IsActive
                    };

        return await query.ToListAsync();
    }
}
