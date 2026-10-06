using DrivingLessons.Application.Common;
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Repositories;
using DrivingLessons.Domain.Values;

namespace DrivingLessons.Application.Commands.CreateStudent;

public class CreateStudentInteractor
{
    private readonly IStudentRepository repository;
    private readonly ITeacherRepository teacherRepository;
    private readonly ICarRepository carRepository;
    private readonly IUnitOfWork unitOfWork;

    public CreateStudentInteractor(
        IStudentRepository repository,
        ITeacherRepository teacherRepository,
        ICarRepository carRepository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.teacherRepository = teacherRepository;
        this.carRepository = carRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateStudentResponse> ExecuteAsync(CreateStudentRequest request)
    {
        var nationalId = NationalId.Of(request.NationalId);
        var name = StudentName.Of(request.Name);
        var phone = PhoneNumber.Of(request.Phone);
        var address = AddressOf(request.Address);
        var startDate = StartDateOf(request.StartDate);
        var licenseType = LicenseTypeOf(request.LicenseType);

        await NationalIdMustBeFreeAsync(nationalId);

        var teacherId = TeacherId.Of(request.TeacherId);

        var teacher = await teacherRepository.GetAsync(teacherId)
                      ?? throw new TeacherNotFoundException(teacherId);

        var carId = CarId.Of(request.CarId);

        var car = await carRepository.GetAsync(carId)
                  ?? throw new CarNotFoundException(carId);

        var student = Student.Create(nationalId, name, phone, teacher, car, address, startDate, licenseType);

        repository.Add(student);

        await unitOfWork.CommitAsync();

        return new CreateStudentResponse(student.Id.Value);
    }

    private async Task NationalIdMustBeFreeAsync(NationalId nationalId)
    {
        var existing = await repository.GetByNationalIdAsync(nationalId);

        if (existing is not null)
        {
            throw new StudentNationalIdAlreadyInUseException(existing.Name);
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
