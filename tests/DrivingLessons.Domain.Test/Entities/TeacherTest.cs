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
        teacher.Delete([]);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Add_Event()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();

        //when
        teacher.Delete([]);

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
        teacher.Delete([]);

        //when
        var act = () => teacher.Delete([]);

        //then
        Should.Throw<TeacherAlreadyDeletedException>(act);
    }

    [TestMethod]
    public void Delete__Must_Not_Have_Active_Students()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();

        //when
        var act = () => teacher.Delete([student]);

        //then
        Should.Throw<TeacherMustNotHaveActiveStudentsException>(act);
        teacher.IsDeleted.ShouldBeFalse();
    }

    [TestMethod]
    public void Delete_With_Only_Inactive_Students()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).BuildInactive();

        //when
        teacher.Delete([student]);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete_Ignores_Students_Of_Other_Teachers()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var studentOfAnotherTeacher = new StudentFakeBuilder().Build();

        //when
        teacher.Delete([studentOfAnotherTeacher]);

        //then
        teacher.IsDeleted.ShouldBeTrue();
    }

    [TestMethod]
    public void Delete__Refusal_Names_The_Active_Students_In_Order()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var first = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var second = new StudentFakeBuilder().WithTeacher(teacher).Build();
        var inactive = new StudentFakeBuilder().WithTeacher(teacher).BuildInactive();
        var expected = new[] { first.Name, second.Name }.OrderBy(x => x.Value, StringComparer.Ordinal).ToList();

        //when
        var act = () => teacher.Delete([second, inactive, first]);

        //then
        var refusal = Should.Throw<TeacherMustNotHaveActiveStudentsException>(act);
        refusal.ActiveStudentNames.ShouldBe(expected);
    }

    [TestMethod]
    public void Delete__Must_Not_Be_Deleted_Before_Active_Students_Are_Checked()
    {
        //given
        var teacher = TeacherFakeBuilder.Build();
        var student = new StudentFakeBuilder().WithTeacher(teacher).Build();
        teacher.Delete([]);

        //when
        var act = () => teacher.Delete([student]);

        //then
        Should.Throw<TeacherAlreadyDeletedException>(act);
    }
}
