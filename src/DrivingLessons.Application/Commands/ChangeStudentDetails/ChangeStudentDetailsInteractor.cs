using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.ChangeStudentDetails;

public class ChangeStudentDetailsInteractor
{
    private readonly IStudentRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public ChangeStudentDetailsInteractor(IStudentRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, ChangeStudentDetailsRequest request)
    {
        var nationalId = NationalId.Of(request.NationalId);
        var name = StudentName.Of(request.Name);
        var phone = PhoneNumber.Of(request.Phone);
        var address = AddressOf(request.Address);
        var startDate = StartDateOf(request.StartDate);
        var licenseType = LicenseTypeOf(request.LicenseType);
        var studentId = StudentId.Of(id);

        var student = await repository.GetAsync(studentId)
                      ?? throw new StudentNotFoundException(studentId);

        await NationalIdMustBeFreeAsync(student, nationalId);

        student.ChangeDetails(nationalId, name, phone, address, startDate, licenseType);

        await unitOfWork.CommitAsync();
    }

    private async Task NationalIdMustBeFreeAsync(Student student, NationalId nationalId)
    {
        if (nationalId == student.NationalId)
        {
            return;
        }

        var other = await repository.GetByNationalIdAsync(nationalId);

        if (other is not null)
        {
            throw new StudentNationalIdAlreadyInUseException(other.Name);
        }
    }

    private static Address? AddressOf(string? value)
    {
        return value is null
            ? null
            : Address.Of(value);
    }

    private static LessonsStartDate? StartDateOf(DateOnly? value)
    {
        return value is null
            ? null
            : LessonsStartDate.Of(value.Value);
    }

    private static LicenseType? LicenseTypeOf(string? value)
    {
        return value is null
            ? null
            : LicenseType.Of(value);
    }
}
