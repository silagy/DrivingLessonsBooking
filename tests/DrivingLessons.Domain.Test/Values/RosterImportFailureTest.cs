using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Values;

[TestClass]
public class RosterImportFailureTest
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Row_Number_Must_Be_Positive(int rowNumber)
    {
        //given
        var studentName = Faker.FakeString();

        //when
        var act = () => RosterImportFailure.Of(rowNumber, studentName, RosterRowFailureReason.InvalidNationalId);

        //then
        Should.Throw<RosterImportFailureRowNumberMustBePositiveException>(act);
    }
}
