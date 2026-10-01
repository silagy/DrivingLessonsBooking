using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Email;
using DrivingLessons.Infrastructure.Options;
using MimeKit;
using Shouldly;

namespace DrivingLessons.Application.Test.Emails;

[TestClass]
public class EmailMimeMessageTest
{
    private const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string Subject = "בקשות לשבוע 41 - משה כהן - גרסה 2";
    private const string AttachmentFileName = "בקשות לשבוע 41 - משה כהן - גרסה 2.xlsx";

    private static readonly EmailOptions Options = new()
    {
        Enabled = true,
        Host = "smtp.example.com",
        FromAddress = "noreply@school.example.com",
        FromName = "בית הספר לנהיגה"
    };

    [TestMethod]
    public void Is_From_The_School_To_The_Teacher()
    {
        //given
        var message = Message("שלום משה כהן,");

        //when
        var mime = EmailMimeMessage.Create(message, Options);

        //then
        var from = mime.From.Mailboxes.ShouldHaveSingleItem();
        from.Name.ShouldBe("בית הספר לנהיגה");
        from.Address.ShouldBe("noreply@school.example.com");
        mime.To.Mailboxes.ShouldHaveSingleItem().Address.ShouldBe("moshe.cohen@example.com");
    }

    [TestMethod]
    public void Keeps_The_Hebrew_Subject()
    {
        //given
        var message = Message("שלום משה כהן,");

        //when
        var mime = EmailMimeMessage.Create(message, Options);

        //then
        mime.Subject.ShouldBe(Subject);
    }

    [TestMethod]
    public void Carries_The_Body_As_Plain_Text()
    {
        //given
        var message = Message("שלום משה כהן,\nמצורף קובץ הבקשות לשבוע 41, גרסה 2.");

        //when
        var mime = EmailMimeMessage.Create(message, Options);

        //then
        mime.TextBody!.ReplaceLineEndings("\n").ShouldBe("שלום משה כהן,\nמצורף קובץ הבקשות לשבוע 41, גרסה 2.");
    }

    [TestMethod]
    public void Renders_The_Body_Right_To_Left_One_Paragraph_Per_Line()
    {
        //given
        var message = Message("שלום משה כהן,\nמצורף קובץ הבקשות לשבוע 41, גרסה 2.");

        //when
        var mime = EmailMimeMessage.Create(message, Options);

        //then
        mime.HtmlBody.ShouldBe(
            "<div dir=\"rtl\" lang=\"he\"><p>שלום משה כהן,</p><p>מצורף קובץ הבקשות לשבוע 41, גרסה 2.</p></div>");
    }

    [TestMethod]
    public void Encodes_Markup_In_The_Html_Body()
    {
        //given
        var message = Message("שלום <b>Cohen</b> & Levi,");

        //when
        var mime = EmailMimeMessage.Create(message, Options);

        //then
        mime.HtmlBody.ShouldBe("<div dir=\"rtl\" lang=\"he\"><p>שלום &lt;b&gt;Cohen&lt;/b&gt; &amp; Levi,</p></div>");
    }

    [TestMethod]
    public void Attaches_The_Excel_File_Under_Its_Hebrew_Name()
    {
        //given
        var message = Message("שלום משה כהן,");

        //when
        var mime = EmailMimeMessage.Create(message, Options);

        //then
        var attachment = mime.Attachments.OfType<MimePart>().ShouldHaveSingleItem();
        attachment.FileName.ShouldBe(AttachmentFileName);
        attachment.ContentType.MimeType.ShouldBe(ContentType);
        ContentOf(attachment).ShouldBe(message.Attachment.Content);
    }

    private static EmailMessage Message(string body)
    {
        byte[] content =
        [
            80,
            75,
            3,
            4
        ];
        var attachment = new ExcelFile(AttachmentFileName, content, ContentType);

        return new EmailMessage("moshe.cohen@example.com", Subject, body, attachment);
    }

    private static byte[] ContentOf(MimePart attachment)
    {
        var stream = new MemoryStream();
        attachment.Content!.DecodeTo(stream);

        return stream.ToArray();
    }
}
