using DrivingLessons.Application.Abstractions;
using DrivingLessons.Application.Common;
using DrivingLessons.Domain.Common;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ImportRoster;

public class ImportRosterInteractor
{
    private const string PrimaryStartDateFormat = "dd/MM/yyyy";
    private const string ShortStartDateFormat = "d/M/yyyy";

    private readonly IRosterCsvParser parser;
    private readonly IStudentRepository studentRepository;
    private readonly IRosterImportRepository rosterImportRepository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;

    public ImportRosterInteractor(
        IRosterCsvParser parser,
        IStudentRepository studentRepository,
        IRosterImportRepository rosterImportRepository,
        ITeacherRepository teacherRepository,
        ICarRepository carRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        this.parser = parser;
        this.studentRepository = studentRepository;
        this.rosterImportRepository = rosterImportRepository;
        this.teacherRepository = teacherRepository;
        this.carRepository = carRepository;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
    }

    public async Task<ImportRosterResponse> ExecuteAsync(ImportRosterRequest request)
    {
        var rows = parser.Parse(request.Content);

        var activeTeachers = await teacherRepository.FindActiveAsync();
        var teachersByName = activeTeachers.ToDictionary(x => x.Name);

        var activeCars = await carRepository.FindActiveAsync();
        var carsByName = activeCars.ToDictionary(x => x.Name);

        var existingStudents = await studentRepository.FindAllAsync();
        var studentsByNationalId = existingStudents.ToDictionary(x => x.NationalId);

        var entries = new List<RosterImportEntry>();
        var failures = new List<RosterImportFailure>();
        var seenIds = new HashSet<NationalId>();

        foreach (var row in rows)
        {
            var failureReason = ProcessRow(row, teachersByName, carsByName, studentsByNationalId, seenIds, entries);

            if (failureReason is null)
            {
                continue;
            }

            var failure = RosterImportFailure.Of(row.RowNumber, row.FullName, failureReason.Value);
            failures.Add(failure);
        }

        DeactivateAbsentees(existingStudents, seenIds, entries);

        var fileName = RosterFileName.Of(request.FileName);
        var importedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var rosterImport = RosterImport.Create(fileName, importedAtUtc, entries, failures);

        rosterImportRepository.Add(rosterImport);

        await unitOfWork.CommitAsync();

        return new ImportRosterResponse(
            rosterImport.Id.Value,
            rosterImport.AddedCount,
            rosterImport.UpdatedCount,
            rosterImport.DeactivatedCount,
            rosterImport.FailedCount);
    }

    private RosterRowFailureReason? ProcessRow(
        RosterCsvRow row,
        IReadOnlyDictionary<TeacherName, Teacher> teachersByName,
        IReadOnlyDictionary<CarName, Car> carsByName,
        IReadOnlyDictionary<NationalId, Student> studentsByNationalId,
        HashSet<NationalId> seenIds,
        List<RosterImportEntry> entries)
    {
        if (row.FullName is null)
        {
            return RosterRowFailureReason.MissingName;
        }

        if (row.Phone is null)
        {
            return RosterRowFailureReason.MissingPhone;
        }

        if (row.NationalId is null)
        {
            return RosterRowFailureReason.InvalidNationalId;
        }

        if (row.TeacherName is null)
        {
            return RosterRowFailureReason.MissingTeacher;
        }

        if (row.CarName is null)
        {
            return RosterRowFailureReason.MissingCar;
        }

        NationalId nationalId;

        try
        {
            nationalId = NationalId.Of(row.NationalId);
        }
        catch (DomainException)
        {
            return RosterRowFailureReason.InvalidNationalId;
        }

        var isFirstOccurrence = seenIds.Add(nationalId);

        if (!isFirstOccurrence)
        {
            return RosterRowFailureReason.DuplicateNationalId;
        }

        var teacherName = TeacherName.Of(row.TeacherName);

        if (!teachersByName.TryGetValue(teacherName, out var teacher))
        {
            return RosterRowFailureReason.UnknownTeacher;
        }

        var carName = CarName.Of(row.CarName);

        if (!carsByName.TryGetValue(carName, out var car))
        {
            return RosterRowFailureReason.UnknownCar;
        }

        LessonsStartDate? startDate = null;

        if (row.StartDate is not null)
        {
            if (!TryParseStartDate(row.StartDate, out var parsedStartDate))
            {
                return RosterRowFailureReason.InvalidStartDate;
            }

            startDate = LessonsStartDate.Of(parsedStartDate);
        }

        var name = StudentName.Of(row.FullName);
        var phone = PhoneNumber.Of(row.Phone);

        var address = row.Address is null
            ? null
            : Address.Of(row.Address);

        var licenseType = row.LicenseType is null
            ? null
            : LicenseType.Of(row.LicenseType);

        if (studentsByNationalId.TryGetValue(nationalId, out var student))
        {
            student.UpdateFromRoster(name, phone, teacher, car, address, startDate, licenseType);

            if (!student.IsActive)
            {
                student.Reactivate();
            }

            var updatedEntry = RosterImportEntry.Of(nationalId, RosterEntryOutcome.Updated);
            entries.Add(updatedEntry);

            return null;
        }

        var createdStudent = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);
        studentRepository.Add(createdStudent);

        var addedEntry = RosterImportEntry.Of(nationalId, RosterEntryOutcome.Added);
        entries.Add(addedEntry);

        return null;
    }

    private static void DeactivateAbsentees(
        IReadOnlyCollection<Student> existingStudents,
        HashSet<NationalId> seenIds,
        List<RosterImportEntry> entries)
    {
        var absentees = existingStudents.Where(x => x.IsActive && !seenIds.Contains(x.NationalId));

        foreach (var student in absentees)
        {
            student.Deactivate();

            var entry = RosterImportEntry.Of(student.NationalId, RosterEntryOutcome.Deactivated);
            entries.Add(entry);
        }
    }

    private static bool TryParseStartDate(string value, out DateOnly result)
    {
        if (DateOnly.TryParseExact(value, PrimaryStartDateFormat, out result))
        {
            return true;
        }

        return DateOnly.TryParseExact(value, ShortStartDateFormat, out result);
    }
}
