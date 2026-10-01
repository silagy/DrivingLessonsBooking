using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace DrivingLessons.Application.EventHandlers;

public class PublicationClosedHandler : IDomainEventHandler<PublicationClosed>
{
    private const string AttachmentExtension = ".xlsx";
    private const char FileNameReplacement = '-';

    private static readonly char[] UnsafeFileNameCharacters = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    private readonly IPublicationRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IExcelGenerator excelGenerator;
    private readonly IEmailSender emailSender;
    private readonly ILogger<PublicationClosedHandler> logger;

    public PublicationClosedHandler(
        IPublicationRepository repository,
        ITeacherRepository teacherRepository,
        IExcelGenerator excelGenerator,
        IEmailSender emailSender,
        ILogger<PublicationClosedHandler> logger)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.excelGenerator = excelGenerator;
        this.emailSender = emailSender;
        this.logger = logger;
    }

    public async Task HandleAsync(PublicationClosed domainEvent)
    {
        var publication = await repository.GetAsync(domainEvent.PublicationId);

        if (publication is null)
        {
            return;
        }

        foreach (var teacherVersion in publication.TeacherVersions)
        {
            try
            {
                await SendExcelAsync(publication, teacherVersion);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Excel email for teacher [{TeacherId}] of publication [{PublicationId}] failed.",
                    teacherVersion.TeacherId.Value,
                    publication.Id.Value);
            }
        }
    }

    private async Task SendExcelAsync(Publication publication, TeacherExcelVersion teacherVersion)
    {
        var teacher = await teacherRepository.GetAsync(teacherVersion.TeacherId);

        if (teacher is null)
        {
            return;
        }

        var week = publication.WeekStart.WeekNumber;
        var teacherName = teacher.Name.Value;
        var version = teacherVersion.Version;

        var subject = Subject(week, teacherName, version);
        var body = Body(week, teacherName, version);

        var excel = await excelGenerator.GenerateAsync(publication.Id, teacherVersion.TeacherId);
        var attachment = excel with { FileName = AttachmentFileName(subject) };

        var message = new EmailMessage(teacher.ContactEmail.Value, subject, body, attachment);

        await emailSender.SendAsync(message);
    }

    private static string Subject(int week, string teacherName, int version)
    {
        return $"בקשות לשבוע {week} - {teacherName} - גרסה {version}";
    }

    private static string Body(int week, string teacherName, int version)
    {
        return string.Join(
            '\n',
            $"שלום {teacherName},",
            $"מצורף קובץ הבקשות לשבוע {week}, גרסה {version}.",
            "קובץ עם מספר גרסה גבוה יותר מחליף את כל הקבצים הקודמים של אותו שבוע.");
    }

    private static string AttachmentFileName(string subject)
    {
        var safeCharacters = subject.Select(SafeFileNameCharacter);
        var safeName = string.Concat(safeCharacters);

        return safeName + AttachmentExtension;
    }

    private static char SafeFileNameCharacter(char character)
    {
        return UnsafeFileNameCharacters.Contains(character)
            ? FileNameReplacement
            : character;
    }
}
