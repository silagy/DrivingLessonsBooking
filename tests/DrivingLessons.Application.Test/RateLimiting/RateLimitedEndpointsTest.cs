using System.Reflection;
using DrivingLessons.Presentation.Web.Controllers;
using DrivingLessons.Presentation.Web.Controllers.Submission;
using DrivingLessons.Presentation.Web.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Shouldly;

namespace DrivingLessons.Application.Test.RateLimiting;

[TestClass]
public class RateLimitedEndpointsTest
{
    [TestMethod]
    [DataRow(typeof(AuthController), nameof(AuthController.Login), RateLimitPolicies.AdminSignIn)]
    [DataRow(
        typeof(SubmissionCommandController),
        nameof(SubmissionCommandController.IdentifyAsync),
        RateLimitPolicies.StudentIdentify)]
    [DataRow(
        typeof(SubmissionCommandController),
        nameof(SubmissionCommandController.CreateAsync),
        RateLimitPolicies.StudentIdentify)]
    [DataRow(
        typeof(SubmissionCommandController),
        nameof(SubmissionCommandController.ReviseAsync),
        RateLimitPolicies.StudentIdentify)]
    public void Every_Endpoint_That_Checks_A_Secret_Is_Rate_Limited(
        Type controller,
        string action,
        string expectedPolicy)
    {
        //given
        var method = controller.GetMethod(action)!;

        //when
        var attribute = method.GetCustomAttribute<EnableRateLimitingAttribute>();

        //then
        attribute.ShouldNotBeNull();
        attribute.PolicyName.ShouldBe(expectedPolicy);
    }
}
