using DrivingLessons.Application.Auth;
using DrivingLessons.Application.Common;
using DrivingLessons.Application.Commands.AddCar;
using DrivingLessons.Application.Commands.ChangeCarDetails;
using DrivingLessons.Application.Commands.ChangeTeacherDetails;
using DrivingLessons.Application.Commands.ClosePublication;
using DrivingLessons.Application.Commands.CreateTeacher;
using DrivingLessons.Application.Commands.CreateWeekSchedule;
using DrivingLessons.Application.Commands.DeleteTeacher;
using DrivingLessons.Application.Commands.ExtendPublicationWindow;
using DrivingLessons.Application.Commands.MarkSlotAvailable;
using DrivingLessons.Application.Commands.MarkSlotUnavailable;
using DrivingLessons.Application.Commands.OpenPublication;
using DrivingLessons.Application.Commands.PublishPublication;
using DrivingLessons.Application.Commands.RemoveCar;
using DrivingLessons.Application.Commands.ReopenPublication;
using DrivingLessons.Application.EventHandlers;
using DrivingLessons.Application.Queries.DownloadPublicationExcel;
using DrivingLessons.Application.Queries.FindPublicationHistory;
using DrivingLessons.Application.Queries.FindTeachers;
using DrivingLessons.Application.Queries.GetPublication;
using DrivingLessons.Application.Queries.GetPublicationDashboard;
using DrivingLessons.Application.Queries.GetTeacher;
using DrivingLessons.Application.Queries.GetWeekSchedule;
using DrivingLessons.Domain.Events;
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
        services.AddScoped<DeleteTeacherInteractor>();
        services.AddScoped<RemoveCarInteractor>();
        services.AddScoped<GetTeacherInteractor>();
        services.AddScoped<FindTeachersInteractor>();
        services.AddScoped<CreateWeekScheduleInteractor>();
        services.AddScoped<MarkSlotUnavailableInteractor>();
        services.AddScoped<MarkSlotAvailableInteractor>();
        services.AddScoped<GetWeekScheduleInteractor>();
        services.AddScoped<PublishPublicationInteractor>();
        services.AddScoped<ExtendPublicationWindowInteractor>();
        services.AddScoped<ReopenPublicationInteractor>();
        services.AddScoped<OpenPublicationInteractor>();
        services.AddScoped<ClosePublicationInteractor>();
        services.AddScoped<GetPublicationInteractor>();
        services.AddScoped<GetPublicationDashboardInteractor>();
        services.AddScoped<FindPublicationHistoryInteractor>();
        services.AddScoped<DownloadPublicationExcelInteractor>();
        services.AddScoped<IDomainEventHandler<WeekScheduleCreated>, WeekScheduleCreatedHandler>();
        services.AddScoped<IDomainEventHandler<PublicationClosed>, PublicationClosedHandler>();
        return services;
    }
}
