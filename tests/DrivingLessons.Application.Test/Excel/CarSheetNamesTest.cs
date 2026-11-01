using DrivingLessons.Infrastructure.Excel;
using Shouldly;

namespace DrivingLessons.Application.Test.Excel;

[TestClass]
public class CarSheetNamesTest
{
    [TestMethod]
    public void Keeps_A_Plain_Car_Name()
    {
        //given
        string[] carNames = ["Corolla White"];

        //when
        var sheetNames = CarSheetNames.For(carNames);

        //then
        sheetNames.ShouldBe(["Corolla White"]);
    }

    [TestMethod]
    [DataRow("A:B", "A-B")]
    [DataRow("A\\B", "A-B")]
    [DataRow("A/B", "A-B")]
    [DataRow("A?B", "A-B")]
    [DataRow("A*B", "A-B")]
    [DataRow("[A]", "-A-")]
    public void Replaces_Characters_Excel_Refuses_With_A_Hyphen(string carName, string expected)
    {
        //given

        //when
        var sheetNames = CarSheetNames.For([carName]);

        //then
        sheetNames.ShouldBe([expected]);
    }

    [TestMethod]
    [DataRow("'Mazda", "-Mazda")]
    [DataRow("Mazda'", "Mazda-")]
    [DataRow("Maz'da", "Maz'da")]
    public void Replaces_A_Leading_Or_Trailing_Apostrophe(string carName, string expected)
    {
        //given

        //when
        var sheetNames = CarSheetNames.For([carName]);

        //then
        sheetNames.ShouldBe([expected]);
    }

    [TestMethod]
    public void Cuts_A_Long_Name_To_31_Characters()
    {
        //given
        var carName = new string('א', 40);

        //when
        var sheetNames = CarSheetNames.For([carName]);

        //then
        sheetNames.ShouldBe([new string('א', 31)]);
    }

    [TestMethod]
    public void Replaces_An_Apostrophe_The_Cut_Leaves_At_The_End()
    {
        //given
        var carName = new string('א', 30) + "'בבב";

        //when
        var sheetNames = CarSheetNames.For([carName]);

        //then
        sheetNames.ShouldBe([new string('א', 30) + "-"]);
    }

    [TestMethod]
    public void Numbers_Repeated_Names_Ignoring_Case()
    {
        //given
        string[] carNames = ["Corolla", "Corolla", "corolla"];

        //when
        var sheetNames = CarSheetNames.For(carNames);

        //then
        sheetNames.ShouldBe(["Corolla", "Corolla (2)", "corolla (3)"]);
    }

    [TestMethod]
    public void Keeps_A_Numbered_Long_Name_Within_31_Characters()
    {
        //given
        var carName = new string('א', 40);

        //when
        var sheetNames = CarSheetNames.For([carName, carName]);

        //then
        sheetNames.ShouldBe([new string('א', 31), new string('א', 27) + " (2)"]);
    }

    [TestMethod]
    public void Skips_A_Number_Already_Taken()
    {
        //given
        string[] carNames = ["Corolla (2)", "Corolla", "Corolla"];

        //when
        var sheetNames = CarSheetNames.For(carNames);

        //then
        sheetNames.ShouldBe(["Corolla (2)", "Corolla", "Corolla (3)"]);
    }

    [TestMethod]
    [DataRow("סיכום")]
    [DataRow("history")]
    public void Never_Takes_A_Reserved_Name(string carName)
    {
        //given

        //when
        var sheetNames = CarSheetNames.For([carName]);

        //then
        sheetNames.ShouldBe([$"{carName} (2)"]);
    }

    [TestMethod]
    public void Returns_No_Names_For_No_Cars()
    {
        //given

        //when
        var sheetNames = CarSheetNames.For([]);

        //then
        sheetNames.ShouldBeEmpty();
    }
}
