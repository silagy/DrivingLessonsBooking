using DrivingLessons.Application.IntegrationEvents;
using DrivingLessons.Infrastructure.Outbox;
using Shouldly;

namespace DrivingLessons.Application.Test.Outbox;

[TestClass]
public class OutboxSerializerTest
{
    [TestMethod]
    public void A_Payload_Round_Trips_With_Its_Nested_List()
    {
        //given
        var sample = new SampleClosed(Guid.NewGuid(), [new SampleVersion(Guid.NewGuid(), 2)]);

        //when
        var payload = OutboxSerializer.Serialize(sample);
        var restored = (SampleClosed)OutboxSerializer.Deserialize(payload, typeof(SampleClosed));

        //then
        restored.Id.ShouldBe(sample.Id);
        restored.Versions.ShouldBe(sample.Versions);
    }

    [TestMethod]
    public void Writes_Camel_Case_Property_Names()
    {
        //given
        var sample = new SampleClosed(Guid.Empty, []);

        //when
        var payload = OutboxSerializer.Serialize(sample);

        //then
        payload.ShouldBe("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"versions\":[]}");
    }

    public record SampleClosed(Guid Id, IReadOnlyList<SampleVersion> Versions) : IIntegrationEvent;

    public record SampleVersion(Guid TeacherId, int Version);
}
