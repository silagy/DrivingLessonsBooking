using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Domain.Entities;

public class Publication : AggregateRoot<PublicationId>
{
    private readonly List<TeacherExcelVersion> teacherVersions = [];

    public WeekStart WeekStart { get; private set; }
    public PublicationState State { get; private set; }
    public ShareableLinkToken LinkToken { get; private set; }
    public SubmissionWindow? Window { get; private set; }

    public IReadOnlyCollection<TeacherExcelVersion> TeacherVersions => teacherVersions.AsReadOnly();

    public bool IsDraft => State is PublicationState.Draft;
    public bool IsPublished => State is PublicationState.Published;
    public bool IsOpen => State is PublicationState.Open;
    public bool IsClosed => State is PublicationState.Closed;

    private Publication()
    {
    }

    private Publication(PublicationId id, WeekStart weekStart, PublicationState state, ShareableLinkToken linkToken)
        : base(id)
    {
        WeekStart = weekStart;
        State = state;
        LinkToken = linkToken;

        AddEvent(new PublicationCreated(id, weekStart, linkToken));
    }

    public static Publication Create(WeekStart weekStart)
    {
        const PublicationState state = PublicationState.Draft;
        var id = PublicationId.New();
        var linkToken = ShareableLinkToken.New();

        return new Publication(id, weekStart, state, linkToken);
    }

    public void Publish(SubmissionWindow window)
    {
        MustBeDraft();

        Window = window;
        State = PublicationState.Published;

        AddEvent(new PublicationPublished(Id, window));
    }

    public void Open()
    {
        MustBePublished();

        State = PublicationState.Open;

        AddEvent(new PublicationOpened(Id));
    }

    public void Close(IEnumerable<TeacherId> teacherIds)
    {
        MustBeOpen();

        State = PublicationState.Closed;

        foreach (var teacherId in teacherIds)
        {
            var existing = teacherVersions.FirstOrDefault(x => x.TeacherId == teacherId);

            if (existing is not null)
            {
                existing.Increment();
            }
            else
            {
                var version = TeacherExcelVersion.Create(teacherId);
                teacherVersions.Add(version);
            }
        }

        AddEvent(new PublicationClosed(Id));
    }

    public void ExtendWindow(DateTimeOffset newEndUtc)
    {
        MustBeOpen();
        WindowExtensionMustBeLater(newEndUtc);

        Window = SubmissionWindow.Of(Window!.StartUtc, newEndUtc);

        AddEvent(new PublicationWindowExtended(Id, newEndUtc));
    }

    public void Reopen(DateTimeOffset newEndUtc)
    {
        MustBeClosed();

        Window = SubmissionWindow.Of(Window!.StartUtc, newEndUtc);
        State = PublicationState.Open;

        AddEvent(new PublicationReopened(Id, newEndUtc));
    }

    private void MustBeDraft()
    {
        if (!IsDraft)
        {
            throw new PublicationMustBeDraftException(Id);
        }
    }

    private void MustBePublished()
    {
        if (!IsPublished)
        {
            throw new PublicationMustBePublishedException(Id);
        }
    }

    private void MustBeOpen()
    {
        if (!IsOpen)
        {
            throw new PublicationMustBeOpenException(Id);
        }
    }

    private void MustBeClosed()
    {
        if (!IsClosed)
        {
            throw new PublicationMustBeClosedException(Id);
        }
    }

    private void WindowExtensionMustBeLater(DateTimeOffset newEndUtc)
    {
        if (newEndUtc <= Window!.EndUtc)
        {
            throw new WindowExtensionMustBeLaterException(Id, newEndUtc);
        }
    }
}
