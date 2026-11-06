using System.ComponentModel.DataAnnotations;
using DrivingLessons.Infrastructure.Options;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxOptionsTest
{
    [TestMethod]
    public void The_Defaults_Are_Valid()
    {
        //given
        var options = new OutboxOptions();

        //when
        var errors = Validate(options);

        //then
        errors.ShouldBeEmpty();
        options.PollSeconds.ShouldBe(5);
        options.BatchSize.ShouldBe(20);
        options.MaxAttempts.ShouldBe(8);
        options.LeaseSeconds.ShouldBe(300);
        options.RetentionDays.ShouldBe(30);
    }

    [TestMethod]
    [DataRow(0, 20, 8, 300, 30, nameof(OutboxOptions.PollSeconds))]
    [DataRow(5, 0, 8, 300, 30, nameof(OutboxOptions.BatchSize))]
    [DataRow(5, 20, 0, 300, 30, nameof(OutboxOptions.MaxAttempts))]
    [DataRow(5, 20, 8, 10, 30, nameof(OutboxOptions.LeaseSeconds))]
    [DataRow(5, 20, 8, 300, 0, nameof(OutboxOptions.RetentionDays))]
    public void Out_Of_Range_Settings_Are_Refused(int pollSeconds, int batchSize, int maxAttempts, int leaseSeconds, int retentionDays, string member)
    {
        //given
        var options = new OutboxOptions
        {
            PollSeconds = pollSeconds,
            BatchSize = batchSize,
            MaxAttempts = maxAttempts,
            LeaseSeconds = leaseSeconds,
            RetentionDays = retentionDays
        };

        //when
        var errors = Validate(options);

        //then
        errors.ShouldHaveSingleItem().MemberNames.ShouldBe([member]);
    }

    private static List<ValidationResult> Validate(OutboxOptions options)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(options);
        Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        return results;
    }
}
