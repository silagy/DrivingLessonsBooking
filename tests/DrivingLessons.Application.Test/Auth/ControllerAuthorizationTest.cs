using System.Reflection;
using DrivingLessons.Presentation.Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class ControllerAuthorizationTest
{
    private const string Anonymous = "Anonymous";

    private static readonly string[] TeacherOrAdministratorEndpoints =
    [
        "WeekScheduleQueryController.GetByTeacherAndWeek",
        "WeekScheduleCommandController.MarkSlotUnavailable",
        "WeekScheduleCommandController.MarkSlotAvailable",
        "PublicationQueryController.GetByWeek",
        "PublicationQueryController.GetDashboard",
        "PublicationQueryController.FindHistory",
        "PublicationQueryController.DownloadExcel",
        "MeCommandController.ChangePasswordAsync",
    ];

    private static readonly string[] AnonymousEndpoints =
    [
        "AuthController.Login",
        "SubmissionCommandController.IdentifyAsync",
        "SubmissionCommandController.CreateAsync",
        "SubmissionCommandController.ReviseAsync",
        "SubmissionQueryController.GetPublicationByLinkAsync",
    ];

    [TestMethod]
    public void Teachers_Reach_Only_Week_Schedules_Publications_And_Their_Own_Password()
    {
        //when
        var endpoints = EndpointsWithRule(AuthorizationPolicies.TeacherOrAdministrator);

        //then
        endpoints.ShouldBe(TeacherOrAdministratorEndpoints, ignoreOrder: true);
    }

    [TestMethod]
    public void Only_Sign_In_And_The_Student_Form_Are_Anonymous()
    {
        //when
        var endpoints = EndpointsWithRule(Anonymous);

        //then
        endpoints.ShouldBe(AnonymousEndpoints, ignoreOrder: true);
    }

    [TestMethod]
    public void Every_Other_Endpoint_Is_Administrator_Only()
    {
        //when
        var others = Endpoints()
            .Where(endpoint => !TeacherOrAdministratorEndpoints.Contains(endpoint.Key))
            .Where(endpoint => !AnonymousEndpoints.Contains(endpoint.Key))
            .ToList();

        //then
        others.ShouldNotBeEmpty();
        others.ShouldAllBe(endpoint => endpoint.Value == AuthorizationPolicies.Administrator);
    }

    [TestMethod]
    [DataRow("WeekScheduleCommandController.Create")]
    [DataRow("PublicationCommandController.Publish")]
    [DataRow("PublicationCommandController.ExtendWindow")]
    [DataRow("PublicationCommandController.Reopen")]
    public void Week_Schedule_Creation_And_The_Publication_Lifecycle_Stay_Administrator_Only(string endpoint)
    {
        //when
        var rule = Endpoints()[endpoint];

        //then
        rule.ShouldBe(AuthorizationPolicies.Administrator);
    }

    private static List<string> EndpointsWithRule(string rule)
    {
        return Endpoints()
            .Where(endpoint => endpoint.Value == rule)
            .Select(endpoint => endpoint.Key)
            .ToList();
    }

    private static Dictionary<string, string> Endpoints()
    {
        return typeof(AuthorizationPolicies).Assembly
            .GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(action => action.IsDefined(typeof(HttpMethodAttribute), true))
            .ToDictionary(action => $"{action.DeclaringType!.Name}.{action.Name}", RuleOf);
    }

    private static string RuleOf(MethodInfo action)
    {
        var controller = action.DeclaringType!;

        if (action.IsDefined(typeof(AllowAnonymousAttribute), true) ||
            controller.IsDefined(typeof(AllowAnonymousAttribute), true))
        {
            return Anonymous;
        }

        var policies = controller.GetCustomAttributes<AuthorizeAttribute>(true)
            .Concat(action.GetCustomAttributes<AuthorizeAttribute>(true))
            .Select(RuleOfAttribute)
            .DefaultIfEmpty(AuthorizationPolicies.Administrator)
            .Distinct()
            .Order();

        return string.Join("+", policies);
    }

    private static string RuleOfAttribute(AuthorizeAttribute attribute)
    {
        if (attribute.Policy is not null)
        {
            return attribute.Policy;
        }

        return attribute.Roles is null
            ? AuthorizationPolicies.Administrator
            : $"Roles:{attribute.Roles}";
    }
}
