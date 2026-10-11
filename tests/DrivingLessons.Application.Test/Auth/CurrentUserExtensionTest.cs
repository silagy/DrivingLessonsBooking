using DrivingLessons.Application.Auth;
using DrivingLessons.Domain.Values;
using FakeItEasy;
using Shouldly;

namespace DrivingLessons.Application.Test.Auth;

[TestClass]
public class CurrentUserExtensionTest
{
    private ICurrentUser currentUser = null!;
    private TeacherId ownTeacherId = null!;
    private TeacherId otherTeacherId = null!;

    [TestInitialize]
    public void Init()
    {
        currentUser = A.Fake<ICurrentUser>();
        ownTeacherId = TeacherId.New();
        otherTeacherId = TeacherId.New();
    }

    [TestMethod]
    public void Administrator_Reaches_Any_Teacher()
    {
        //given
        SignedInAs(Role.Administrator, null);

        //when
        var mayReach = currentUser.MayReach(otherTeacherId);

        //then
        mayReach.ShouldBeTrue();
    }

    [TestMethod]
    public void Linked_Administrator_Reaches_Any_Teacher()
    {
        //given
        SignedInAs(Role.Administrator, ownTeacherId);

        //when
        var mayReach = currentUser.MayReach(otherTeacherId);

        //then
        mayReach.ShouldBeTrue();
    }

    [TestMethod]
    public void Teacher_Reaches_Own_Teacher()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);
        var sameTeacherId = TeacherId.Of(ownTeacherId.Value);

        //when
        var mayReach = currentUser.MayReach(sameTeacherId);

        //then
        mayReach.ShouldBeTrue();
    }

    [TestMethod]
    public void Teacher_Does_Not_Reach_Another_Teacher()
    {
        //given
        SignedInAs(Role.Teacher, ownTeacherId);

        //when
        var mayReach = currentUser.MayReach(otherTeacherId);

        //then
        mayReach.ShouldBeFalse();
    }

    [TestMethod]
    public void Teacher_Without_Linked_Teacher_Reaches_Nothing()
    {
        //given
        SignedInAs(Role.Teacher, null);

        //when
        var mayReach = currentUser.MayReach(ownTeacherId);

        //then
        mayReach.ShouldBeFalse();
    }

    private void SignedInAs(Role role, TeacherId? teacherId)
    {
        A.CallTo(() => currentUser.Role).Returns(role);
        A.CallTo(() => currentUser.TeacherId).Returns(teacherId);
    }
}
