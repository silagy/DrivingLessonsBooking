using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Application.IntegrationEvents.Publications;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;
using Microsoft.Extensions.Logging;

namespace DrivingLessons.Application.Consumers.TeacherExcelEmail;

public class SendTeacherExcelEmailsConsumer : IIntegrationEventConsumer<PublicationClosedIntegrationEvent>
{
    private const string AttachmentExtension = ".xlsx";
    private const char FileNameReplacement = '-';

    private static readonly char[] UnsafeFileNameCharacters = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    private readonly IPublicationRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IExcelGenerator excelGenerator;
    private readonly IEmailSender emailSender;
    private readonly ITeacherExcelEmailLog emailLog;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SendTeacherExcelEmailsConsumer> logger;

    public SendTeacherExcelEmailsConsumer(
        IPublicationRepository repository,
        ITeacherRepository teacherRepository,
        IExcelGenerator excelGenerator,
        IEmailSender emailSender,
        ITeacherExcelEmailLog emailLog,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<SendTeacherExcelEmailsConsumer> logger)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.excelGenerator = excelGenerator;
        this.emailSender = emailSender;
        this.emailLog = emailLog;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<DeliveryOutcome> ConsumeAsync(PublicationClosedIntegrationEvent integrationEvent)
    {
        var publicationId = PublicationId.Of(integrationEvent.PublicationId);
        var publication = await repository.GetAsync(publicationId);

        if (publication is null)
        {
            return DeliveryOutcome.Done;
        }

        var failedTeacherIds = new List<TeacherId>();

        foreach (var closed in integrationEvent.Teachers)
        {
            var teacherId = TeacherId.Of(closed.TeacherId);

            try
            {
                await SendOnceAsync(publication, teacherId, closed.Version);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Excel email for teacher [{TeacherId}] of publication [{PublicationId}] failed.",
                    teacherId.Value,
                    publicationId.Value);

                failedTeacherIds.Add(teacherId);
            }
        }

        if (failedTeacherIds.Any())
        {
            throw new TeacherExcelEmailFailedException(publicationId, failedTeacherIds);
        }

        return DeliveryOutcome.Done;
    }

    private async Task SendOnceAsync(Publication publication, TeacherId teacherId, int version)
    {
        var alreadySent = await emailLog.WasSentAsync(publication.Id, teacherId, version);

        if (alreadySent)
        {
            return;
        }

        var teacher = await teacherRepository.GetAsync(teacherId);

        if (teacher is null)
        {
            return;
        }

        var week = publication.WeekStart.WeekNumber;
        var teacherName = teacher.Name.Value;

        var subject = Subject(week, teacherName, version);
        var body = Body(week, teacherName, version);

        var excel = await excelGenerator.GenerateAsync(publication.Id, teacherId);
        var attachment = excel with { FileName = AttachmentFileName(subject) };

        var message = new EmailMessage(teacher.ContactEmail.Value, subject, body, attachment);

        await emailSender.SendAsync(message);

        var sentAtUtc = timeProvider.GetUtcNow();
        emailLog.RecordSent(publication.Id, teacherId, version, sentAtUtc);

        await unitOfWork.CommitAsync();
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
