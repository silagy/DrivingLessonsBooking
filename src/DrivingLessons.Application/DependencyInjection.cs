using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Commands.AddCar;
using DrivingLessons.Application.Commands.ChangeCarDetails;
using DrivingLessons.Application.Commands.ChangeTeacherDetails;
using DrivingLessons.Application.Commands.CreateTeacher;
using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetTeacher;
using Microsoft.Extensions.DependencyInjection;

namespace DrivingLessons.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginInteractor>();
        services.AddScoped<CreateTeacherInteractor>();
        services.AddScoped<AddCarInteractor>();
        services.AddScoped<ChangeTeacherDetailsInteractor>();
        services.AddScoped<ChangeCarDetailsInteractor>();
        services.AddScoped<GetTeacherInteractor>();
        services.AddScoped<FindTeachersInteractor>();
        return services;
    }
}
