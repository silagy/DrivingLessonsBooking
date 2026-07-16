using System.Text;
using DrivingLessons.Domain.Exceptions;
using DrivingLessons.Infrastructure.Csv;
using Shouldly;

namespace DrivingLessons.Application.Test.Csv;

[TestClass]
public class RosterCsvParserTest
{
    private const string Header = "שם מלא,תעודת זהות,טלפון,מורה,רכב,כתובת,תאריך התחלה,סוג רישיון";

    [TestMethod]
    public void Parses_Rows_By_Header_Name()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,הרצל 5,01/09/2025,B";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.NationalId.ShouldBe("123456782");
        row.Phone.ShouldBe("0501234567");
        row.TeacherName.ShouldBe("משה לוי");
        row.CarName.ShouldBe("טויוטה 123");
        row.Address.ShouldBe("הרצל 5");
        row.StartDate.ShouldBe("01/09/2025");
        row.LicenseType.ShouldBe("B");
    }

    [TestMethod]
    public void Header_Order_Is_Irrelevant()
    {
        //given
        var csv = "טלפון,רכב,מורה,שם מלא,תעודת זהות\n0501234567,טויוטה 123,משה לוי,דנה כהן,123456782";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.NationalId.ShouldBe("123456782");
        row.Phone.ShouldBe("0501234567");
        row.TeacherName.ShouldBe("משה לוי");
        row.CarName.ShouldBe("טויוטה 123");
        row.Address.ShouldBeNull();
    }

    [TestMethod]
    public void Unknown_Columns_Are_Ignored()
    {
        //given
        var csv = Header + ",הערות\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,,,B,מתקדמת מהר";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.LicenseType.ShouldBe("B");
    }

    [TestMethod]
    public void Utf8_Bom_Is_Consumed()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,,,";
        var preamble = Encoding.UTF8.GetPreamble();
        var csvBytes = Encoding.UTF8.GetBytes(csv);
        using var stream = new MemoryStream();
        stream.Write(preamble);
        stream.Write(csvBytes);
        stream.Position = 0;
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
    }

    [TestMethod]
    public void Quoted_Field_Keeps_Embedded_Comma()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,\"הרצל 5, תל אביב\",,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.Address.ShouldBe("הרצל 5, תל אביב");
        row.StartDate.ShouldBeNull();
    }

    [TestMethod]
    public void Escaped_Quotes_Are_Unescaped()
    {
        //given
        var csv = Header + "\n\"דנה \"\"דני\"\" כהן\",123456782,0501234567,משה לוי,טויוטה 123,,,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה \"דני\" כהן");
    }

    [TestMethod]
    public void Quoted_Field_Keeps_Embedded_Newline()
    {
        //given
        var csv = Header + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,\"הרצל 5\nתל אביב\",,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.Address.ShouldBe("הרצל 5\nתל אביב");
    }

    [TestMethod]
    public void Directional_Marks_Are_Stripped()
    {
        //given
        var csv = Header + "\n‫דנה כהן‬,‏123456782‎,050 1234567,משה לוי,טויוטה 123,,,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        var row = rows.ShouldHaveSingleItem();
        row.FullName.ShouldBe("דנה כהן");
        row.NationalId.ShouldBe("123456782");
        row.Phone.ShouldBe("050 1234567");
    }

    [TestMethod]
    public void Missing_Required_Header_Throws()
    {
        //given
        var csv = "שם מלא,תעודת זהות,מורה,רכב\nדנה כהן,123456782,משה לוי,טויוטה 123";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var act = () => parser.Parse(stream);

        //then
        Should.Throw<RosterFileMustContainRequiredColumnsException>(act);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(Header)]
    public void Empty_File_Throws(string csv)
    {
        //given
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var act = () => parser.Parse(stream);

        //then
        Should.Throw<RosterFileMustNotBeEmptyException>(act);
    }

    [TestMethod]
    public void Row_Numbers_Count_From_The_Header()
    {
        //given
        var csv = Header
                  + "\nדנה כהן,123456782,0501234567,משה לוי,טויוטה 123,,,"
                  + "\r\nיוסי מזרחי,987654324,0521234567,משה לוי,טויוטה 123,,,";
        using var stream = StreamOf(csv);
        var parser = new RosterCsvParser();

        //when
        var rows = parser.Parse(stream);

        //then
        rows[0].RowNumber.ShouldBe(2);
        rows[1].RowNumber.ShouldBe(3);
    }

    private static MemoryStream StreamOf(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);

        return new MemoryStream(bytes);
    }
}
