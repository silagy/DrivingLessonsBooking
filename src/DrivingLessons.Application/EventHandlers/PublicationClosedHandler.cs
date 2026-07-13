using System.Globalization;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;

namespace DrivingLessons.Application.EventHandlers;

public class PublicationClosedHandler : IDomainEventHandler<PublicationClosed>
{
    private readonly IPublicationRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IExcelGenerator excelGenerator;
    private readonly IEmailSender emailSender;

    public PublicationClosedHandler(
        IPublicationRepository repository,
        ITeacherRepository teacherRepository,
        IExcelGenerator excelGenerator,
        IEmailSender emailSender)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.excelGenerator = excelGenerator;
        this.emailSender = emailSender;
    }

    public async Task HandleAsync(PublicationClosed domainEvent)
    {
        var publication = await repository.GetAsync(domainEvent.PublicationId);

        if (publication is null)
        {
            return;
        }

        var weekDate = publication.WeekStart.Value.ToDateTime(TimeOnly.MinValue);
        var week = ISOWeek.GetWeekOfYear(weekDate);

        foreach (var teacherVersion in publication.TeacherVersions)
        {
            var teacher = await teacherRepository.GetAsync(teacherVersion.TeacherId);

            if (teacher is null)
            {
                continue;
            }

            var excel = await excelGenerator.GenerateAsync(publication.Id, teacherVersion.TeacherId);
            var subject = $"Week {week} Requests - {teacher.Name.Value} - v{teacherVersion.Version}";
            var body = $"Attached are the collected requests for week {week}.";
            var message = new EmailMessage(teacher.ContactEmail.Value, subject, body, excel);

            await emailSender.SendAsync(message);
        }
    }
}
