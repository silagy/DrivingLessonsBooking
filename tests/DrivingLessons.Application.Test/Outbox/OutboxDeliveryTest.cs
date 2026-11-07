using DrivingLessons.Infrastructure.Outbox;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxDeliveryTest
{
    private const int MaxAttempts = 8;

    private static readonly DateTimeOffset NowUtc = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void A_New_Delivery_Is_Pending_And_Due_Now()
    {
        //given
        var messageId = Guid.NewGuid();

        //when
        var delivery = OutboxDelivery.Create(messageId, "SendTeacherExcelEmailsConsumer", NowUtc);

        //then
        delivery.MessageId.ShouldBe(messageId);
        delivery.Consumer.ShouldBe("SendTeacherExcelEmailsConsumer");
        delivery.State.ShouldBe(OutboxDeliveryState.Pending);
        delivery.Attempts.ShouldBe(0);
        delivery.NextAttemptAtUtc.ShouldBe(NowUtc);
        delivery.LockedUntilUtc.ShouldBeNull();
        delivery.CompletedAtUtc.ShouldBeNull();
    }

    [TestMethod]
    public void Succeeds_And_Releases_Its_Lease()
    {
        //given
        var delivery = Pending();
        var laterUtc = NowUtc.AddSeconds(3);

        //when
        delivery.Succeed(laterUtc);

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Succeeded);
        delivery.Attempts.ShouldBe(1);
        delivery.CompletedAtUtc.ShouldBe(laterUtc);
        delivery.LockedUntilUtc.ShouldBeNull();
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(2, 2)]
    [DataRow(3, 4)]
    [DataRow(4, 8)]
    [DataRow(5, 16)]
    [DataRow(6, 32)]
    [DataRow(7, 60)]
    public void A_Failure_Schedules_The_Next_Attempt_With_A_Growing_Gap(int failures, int expectedMinutes)
    {
        //given
        var delivery = Pending();

        //when
        for (var failure = 0; failure < failures; failure++)
        {
            delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);
        }

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Pending);
        delivery.Attempts.ShouldBe(failures);
        delivery.NextAttemptAtUtc.ShouldBe(NowUtc.AddMinutes(expectedMinutes));
        delivery.LockedUntilUtc.ShouldBeNull();
    }

    [TestMethod]
    public void Keeps_The_Error_Type_And_Message()
    {
        //given
        var delivery = Pending();

        //when
        delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);

        //then
        delivery.LastError.ShouldBe("TimeoutException: SMTP timed out.");
    }

    [TestMethod]
    public void Cuts_A_Long_Error_To_Its_Maximum_Length()
    {
        //given
        var delivery = Pending();
        var longMessage = new string('x', 5000);

        //when
        delivery.Fail(new TimeoutException(longMessage), NowUtc, MaxAttempts);

        //then
        delivery.LastError!.Length.ShouldBe(OutboxDelivery.LastErrorMaxLength);
    }

    [TestMethod]
    public void Becomes_Dead_After_The_Last_Attempt()
    {
        //given
        var delivery = Pending();

        //when
        for (var failure = 0; failure < MaxAttempts; failure++)
        {
            delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);
        }

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Dead);
        delivery.Attempts.ShouldBe(MaxAttempts);
        delivery.CompletedAtUtc.ShouldBe(NowUtc);
    }

    [TestMethod]
    public void A_Hold_Reschedules_Without_Counting_An_Attempt()
    {
        //given
        var delivery = Pending();
        var untilUtc = NowUtc.AddHours(11);

        //when
        delivery.Hold(untilUtc);

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Pending);
        delivery.Attempts.ShouldBe(0);
        delivery.NextAttemptAtUtc.ShouldBe(untilUtc);
        delivery.LockedUntilUtc.ShouldBeNull();
        delivery.LastError.ShouldBeNull();
    }

    [TestMethod]
    public void A_Hold_After_Failures_Keeps_Their_Count()
    {
        //given
        var delivery = Pending();
        delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);
        var untilUtc = NowUtc.AddHours(11);

        //when
        delivery.Hold(untilUtc);

        //then
        delivery.Attempts.ShouldBe(1);
        delivery.NextAttemptAtUtc.ShouldBe(untilUtc);
    }

    [TestMethod]
    public void Is_Marked_Dead_At_Once_Without_A_Retry()
    {
        //given
        var delivery = Pending();

        //when
        delivery.MarkDead(new InvalidOperationException("Consumer [Gone] is not registered."), NowUtc);

        //then
        delivery.State.ShouldBe(OutboxDeliveryState.Dead);
        delivery.Attempts.ShouldBe(1);
        delivery.CompletedAtUtc.ShouldBe(NowUtc);
        delivery.LastError.ShouldBe("InvalidOperationException: Consumer [Gone] is not registered.");
    }

    [TestMethod]
    [DataRow(OutboxDeliveryState.Succeeded)]
    [DataRow(OutboxDeliveryState.Dead)]
    public void Delivery_Must_Be_Pending_To_Be_Held(OutboxDeliveryState state)
    {
        //given
        var delivery = InState(state);

        //when
        var act = () => delivery.Hold(NowUtc.AddHours(1));

        //then
        act.ShouldThrow<OutboxDeliveryMustBePendingException>();
    }

    [TestMethod]
    [DataRow(OutboxDeliveryState.Succeeded)]
    [DataRow(OutboxDeliveryState.Dead)]
    public void Delivery_Must_Be_Pending_To_Be_Marked_Dead(OutboxDeliveryState state)
    {
        //given
        var delivery = InState(state);

        //when
        var act = () => delivery.MarkDead(new InvalidOperationException("Unknown."), NowUtc);

        //then
        act.ShouldThrow<OutboxDeliveryMustBePendingException>();
    }

    [TestMethod]
    [DataRow(OutboxDeliveryState.Succeeded)]
    [DataRow(OutboxDeliveryState.Dead)]
    public void Delivery_Must_Be_Pending_To_Succeed(OutboxDeliveryState state)
    {
        //given
        var delivery = InState(state);

        //when
        var act = () => delivery.Succeed(NowUtc);

        //then
        act.ShouldThrow<OutboxDeliveryMustBePendingException>();
    }

    [TestMethod]
    [DataRow(OutboxDeliveryState.Succeeded)]
    [DataRow(OutboxDeliveryState.Dead)]
    public void Delivery_Must_Be_Pending_To_Fail(OutboxDeliveryState state)
    {
        //given
        var delivery = InState(state);

        //when
        var act = () => delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, MaxAttempts);

        //then
        act.ShouldThrow<OutboxDeliveryMustBePendingException>();
    }

    private static OutboxDelivery Pending()
    {
        return OutboxDelivery.Create(Guid.NewGuid(), "SendTeacherExcelEmailsConsumer", NowUtc);
    }

    private static OutboxDelivery InState(OutboxDeliveryState state)
    {
        var delivery = Pending();

        if (state == OutboxDeliveryState.Succeeded)
        {
            delivery.Succeed(NowUtc);
        }

        if (state == OutboxDeliveryState.Dead)
        {
            delivery.Fail(new TimeoutException("SMTP timed out."), NowUtc, 1);
        }

        return delivery;
    }
}
