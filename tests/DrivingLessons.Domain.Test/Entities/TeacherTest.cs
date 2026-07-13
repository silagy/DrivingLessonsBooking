using DrivingLessons.Domain.Entities;
using DrivingLessons.Domain.Events;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Domain.Test.Common;
using DrivingLessons.Domain.Test.Entities.Fake;
using DrivingLessons.Domain.Values;
using Shouldly;

namespace DrivingLessons.Domain.Test.Entities;

[TestClass]
public class TeacherTest
{
    [TestMethod]
    public void Create()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        //when
        var teacher = Teacher.Create(name, contactEmail);

        //then
        teacher.Name.ShouldBe(name);
        teacher.ContactEmail.ShouldBe(contactEmail);
    }

    [TestMethod]
    public void Create__Add_Event()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());

        //when
        var teacher = Teacher.Create(name, contactEmail);

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherCreated>()
            .Where(x => x.TeacherId == teacher.Id && x.Name == name && x.ContactEmail == contactEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var newName = TeacherName.Of(Faker.FakeString());
        var newEmail = Email.Of(Faker.FakeEmail());

        //when
        teacher.ChangeDetails(newName, newEmail);

        //then
        teacher.Name.ShouldBe(newName);
        teacher.ContactEmail.ShouldBe(newEmail);
    }

    [TestMethod]
    public void Change_Details__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var newName = TeacherName.Of(Faker.FakeString());
        var newEmail = Email.Of(Faker.FakeEmail());

        //when
        teacher.ChangeDetails(newName, newEmail);

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherDetailsChanged>()
            .Where(x => x.TeacherId == teacher.Id && x.Name == newName && x.ContactEmail == newEmail)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Change_Details__Accepts_Unchanged_Values()
    {
        //given
        var name = TeacherName.Of(Faker.FakeString());
        var contactEmail = Email.Of(Faker.FakeEmail());
        var teacher = Teacher.Create(name, contactEmail);

        //when
        teacher.ChangeDetails(name, contactEmail);

        //then
        teacher.Name.ShouldBe(name);
        teacher.ContactEmail.ShouldBe(contactEmail);
    }

    [TestMethod]
    public void New_Teacher_Is_Not_Deleted()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //expected
        teacher.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Delete()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //when
        teacher.Delete();

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //when
        teacher.Delete();

        //then
        teacher
            .UncommittedEvents
            .OfType<TeacherDeleted>()
            .Where(x => x.TeacherId == teacher.Id)
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public void Delete__Must_Not_Be_Deleted()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        teacher.Delete();

        //when
        var act = () => teacher.Delete();

        //then
        Should.Throw<TeacherAlreadyDeletedException>(act);
    }
}
