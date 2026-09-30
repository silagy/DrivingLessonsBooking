# Task 2 of 3: The SMTP sender — a real, right-to-left email with the Excel attached

> Part of [US-44: Versioned Excel Email](README.md). Requires task 1 committed. Work on branch `44-us-44-versioned-excel-email`, commands from the repo root in bash.

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj` (+ `MailKit`)
- Modify: `src\DrivingLessons.Infrastructure\Options\EmailOptions.cs`
- Create: `src\DrivingLessons.Infrastructure\Email\EmailMimeMessage.cs`
- Rename + modify: `src\DrivingLessons.Infrastructure\Email\LoggingEmailSender.cs` → `SmtpEmailSender.cs`
- Modify: `src\DrivingLessons.Infrastructure\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Presentation.Web\appsettings.json`, `appsettings.Development.json`
- Modify: `docker-compose.yml`, `.env.example`
- Create: `docs\decisions\0005-email-over-ses-smtp.md`
- Modify: `docs\tech-stack.md`, `docs\development\running-the-project.md`
- Test: `tests\DrivingLessons.Application.Test\Emails\EmailMimeMessageTest.cs`, `tests\DrivingLessons.Application.Test\Emails\EmailOptionsTest.cs` (new)
- Never staged: `.claude\launch.json`, `docs\modules\excel\us-45-excel-summary-sheet-plan\task-0{1,2}-*.md`

**Interfaces:**
- Consumes (task 1): every `EmailMessage` has a Hebrew `Subject`, a `Body` of lines joined by `'\n'`, and an `Attachment` whose `FileName` may hold Hebrew and spaces; `PublicationClosedHandler` catches whatever `IEmailSender.SendAsync` throws. (On `main`): `IEmailSender`, `EmailMessage`, `ExcelFile` (`DrivingLessons.Application.Abstractions`); `EmailOptions` bound from section `Email` with `ValidateDataAnnotations().ValidateOnStart()` in `AddInfrastructure`.
- Produces:
  - `EmailMimeMessage.Create(EmailMessage message, EmailOptions options) : MimeMessage` (`DrivingLessons.Infrastructure.Email`, static).
  - `SmtpEmailSender : IEmailSender` (replaces `LoggingEmailSender`; constructor `(IOptions<EmailOptions>, ILogger<SmtpEmailSender>)`).
  - `EmailOptions` — `Enabled`, `Host`, `Port`, `Security` (`MailKit.Security.SecureSocketOptions`), `Username`, `Password`, `FromAddress`, `FromName`; validation messages `Email host is required when email is enabled.`, `A valid from address is required when email is enabled.`, `An email password is required with a username.` — task 3 Step 7 expects the first at boot.
  - Configuration keys `Email:*` and compose variables `EMAIL_ENABLED`, `EMAIL_HOST`, `EMAIL_PORT`, `EMAIL_USERNAME`, `EMAIL_PASSWORD`, `EMAIL_FROM_ADDRESS`, `EMAIL_FROM_NAME`.

- [ ] **Step 1: Write the failing MIME message tests**

Create `tests\DrivingLessons.Application.Test\Emails\EmailMimeMessageTest.cs`:

```csharp
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
```

`TextBody` comes back with CRLF line endings (MIME text is CRLF on the wire), hence `ReplaceLineEndings("\n")`; `HtmlBody` is compared exactly because the right-to-left wrapper is the point. The folder is `Emails`, not `Email` — a `…Test.Email` namespace would shadow the domain `Email` value object in every other test file (README decision 9).

- [ ] **Step 2: Write the failing options tests**

Create `tests\DrivingLessons.Application.Test\Emails\EmailOptionsTest.cs`:

```csharp
using DrivingLessons.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DrivingLessons.Application.Test.Emails;

[TestClass]
public class EmailOptionsTest
{
    [TestMethod]
    public void Disabled_Email_Needs_No_Smtp_Settings()
    {
        //given
        var options = new EmailOptions();

        //when
        var result = Validate(options);

        //then
        result.Succeeded.ShouldBeTrue();
    }

    [TestMethod]
    public void Enabled_Email_With_Complete_Settings_Is_Valid()
    {
        //given
        var options = new EmailOptions
        {
            Enabled = true,
            Host = "email-smtp.eu-central-1.amazonaws.com",
            Username = "smtp-user",
            Password = "smtp-password",
            FromAddress = "noreply@school.example.com"
        };

        //when
        var result = Validate(options);

        //then
        result.Succeeded.ShouldBeTrue();
    }

    [TestMethod]
    public void Enabled_Email_Without_Credentials_Is_Valid()
    {
        //given
        var options = new EmailOptions
        {
            Enabled = true,
            Host = "localhost",
            FromAddress = "noreply@local.dev"
        };

        //when
        var result = Validate(options);

        //then
        result.Succeeded.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow("", "noreply@school.example.com", "", "", "Email host is required when email is enabled.")]
    [DataRow("smtp.example.com", "", "", "", "A valid from address is required when email is enabled.")]
    [DataRow("smtp.example.com", "not-an-address", "", "", "A valid from address is required when email is enabled.")]
    [DataRow("smtp.example.com", "noreply@school.example.com", "smtp-user", "", "An email password is required with a username.")]
    public void Enabled_Email_Must_Have_Its_Smtp_Settings(
        string host,
        string fromAddress,
        string username,
        string password,
        string expectedFailure)
    {
        //given
        var options = new EmailOptions
        {
            Enabled = true,
            Host = host,
            Username = username,
            Password = password,
            FromAddress = fromAddress
        };

        //when
        var result = Validate(options);

        //then
        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain(expectedFailure);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(65536)]
    public void Port_Must_Be_A_Tcp_Port(int port)
    {
        //given
        var options = new EmailOptions
        {
            Port = port
        };

        //when
        var result = Validate(options);

        //then
        result.Failed.ShouldBeTrue();
    }

    private static ValidateOptionsResult Validate(EmailOptions options)
    {
        var validator = new DataAnnotationValidateOptions<EmailOptions>(string.Empty);

        return validator.Validate(string.Empty, options);
    }
}
```

`DataAnnotationValidateOptions<EmailOptions>` is the exact validator `ValidateDataAnnotations()` registers, so these tests prove what `ValidateOnStart()` will do at boot — attributes first (`[Range]` on `Port`), then `IValidatableObject.Validate` when they pass.

- [ ] **Step 3: Run the tests to see them fail**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~EmailMimeMessageTest|FullyQualifiedName~EmailOptionsTest"`
Expected: FAIL — the build breaks with `CS0246: The type or namespace name 'MimeKit' could not be found`, `CS0103: The name 'EmailMimeMessage' does not exist`, and `CS0117: 'EmailOptions' does not contain a definition for 'Host'`.

- [ ] **Step 4: Add MailKit**

```bash
dotnet add src/DrivingLessons.Infrastructure package MailKit --version 4.18.1
```

`src\DrivingLessons.Infrastructure\DrivingLessons.Infrastructure.csproj` now lists, after ClosedXML:

```xml
    <PackageReference Include="ClosedXML" Version="0.105.0" />
    <PackageReference Include="MailKit" Version="4.18.1" />
```

MailKit brings MimeKit. The test project sees both through its Infrastructure reference.

- [ ] **Step 5: The SMTP settings and their validation**

Replace the contents of `src\DrivingLessons.Infrastructure\Options\EmailOptions.cs` with:

```csharp
using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace DrivingLessons.Infrastructure.Options;

public sealed class EmailOptions : IValidatableObject
{
    public const string SectionName = "Email";

    private const int DefaultPort = 587;

    public bool Enabled { get; init; }

    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = DefaultPort;

    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.StartTls;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string FromName { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            yield return new ValidationResult("Email host is required when email is enabled.", [nameof(Host)]);
        }

        var fromAddress = new EmailAddressAttribute();

        if (string.IsNullOrWhiteSpace(FromAddress)
            || !fromAddress.IsValid(FromAddress))
        {
            yield return new ValidationResult(
                "A valid from address is required when email is enabled.",
                [nameof(FromAddress)]);
        }

        var hasUsername = !string.IsNullOrEmpty(Username);

        if (hasUsername
            && string.IsNullOrEmpty(Password))
        {
            yield return new ValidationResult("An email password is required with a username.", [nameof(Password)]);
        }
    }
}
```

- `Security` binds from the configuration strings `StartTls` / `None` (the binder parses enum names). `StartTls` is the default so production can never fall back to plaintext by omission; only the Development file says `None`, for Mailpit.
- Nothing is validated while `Enabled` is false — the default, and every developer machine without Mailpit.

- [ ] **Step 6: The MIME message**

Create `src\DrivingLessons.Infrastructure\Email\EmailMimeMessage.cs`:

```csharp
using System.Net;
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Options;
using MimeKit;

namespace DrivingLessons.Infrastructure.Email;

public static class EmailMimeMessage
{
    private const char LineSeparator = '\n';

    public static MimeMessage Create(EmailMessage message, EmailOptions options)
    {
        var from = new MailboxAddress(options.FromName, options.FromAddress);
        var to = MailboxAddress.Parse(message.ToEmail);

        var attachment = message.Attachment;
        var contentType = ContentType.Parse(attachment.ContentType);

        var body = new BodyBuilder
        {
            TextBody = message.Body,
            HtmlBody = RightToLeftHtml(message.Body)
        };

        body.Attachments.Add(attachment.FileName, attachment.Content, contentType);

        var mime = new MimeMessage();
        mime.From.Add(from);
        mime.To.Add(to);
        mime.Subject = message.Subject;
        mime.Body = body.ToMessageBody();

        return mime;
    }

    private static string RightToLeftHtml(string body)
    {
        var paragraphs = body
                             .Split(LineSeparator)
                             .Select(line => $"<p>{WebUtility.HtmlEncode(line)}</p>");

        return $"<div dir=\"rtl\" lang=\"he\">{string.Concat(paragraphs)}</div>";
    }
}
```

MimeKit encodes the Hebrew subject, display name and attachment file name for the wire (RFC 2047 / 2231), so they arrive intact in any mail client — the smoke in task 3 reads them back from Mailpit.

- [ ] **Step 7: The sender**

```bash
git mv src/DrivingLessons.Infrastructure/Email/LoggingEmailSender.cs src/DrivingLessons.Infrastructure/Email/SmtpEmailSender.cs
```

Replace the contents of `src\DrivingLessons.Infrastructure\Email\SmtpEmailSender.cs` with:

```csharp
using DrivingLessons.Application.Abstractions;
using DrivingLessons.Infrastructure.Options;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DrivingLessons.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private const int TimeoutMilliseconds = 30_000;

    private readonly EmailOptions options;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task SendAsync(EmailMessage message)
    {
        if (!options.Enabled)
        {
            logger.LogInformation(
                "Email disabled. Skipped send to [{Recipient}] subject [{Subject}] attachment [{AttachmentBytes}] bytes.",
                message.ToEmail,
                message.Subject,
                message.Attachment.Content.Length);

            return;
        }

        var mime = EmailMimeMessage.Create(message, options);

        using var client = new SmtpClient();
        client.Timeout = TimeoutMilliseconds;

        await client.ConnectAsync(options.Host, options.Port, options.Security);

        if (!string.IsNullOrEmpty(options.Username))
        {
            await client.AuthenticateAsync(options.Username, options.Password);
        }

        const bool quit = true;
        await client.SendAsync(mime);
        await client.DisconnectAsync(quit);
    }
}
```

- The disabled branch logs the same line as `LoggingEmailSender` did, so the disabled behavior (and any earlier smoke that searched for `Skipped send`) is unchanged.
- `SmtpClient` here is `MailKit.Net.Smtp.SmtpClient` — `System.Net.Mail` is not imported.
- Nothing is caught here: a failure propagates to `PublicationClosedHandler`, which logs it with the teacher and publication ids (task 1).
- The 30-second timeout bounds each connect / command; MailKit's default is 2 minutes, and the startup reconciliation waits on this call before the app starts listening.

- [ ] **Step 8: Register it**

In `src\DrivingLessons.Infrastructure\DependencyInjection.cs`, replace

```csharp
        services.AddScoped<IEmailSender, LoggingEmailSender>();
```

with

```csharp
        services.AddScoped<IEmailSender, SmtpEmailSender>();
```

Then `grep -rn "LoggingEmailSender" src tests --include=*.cs` → no output.

- [ ] **Step 9: Run the tests to see them pass**

Run: `dotnet test tests\DrivingLessons.Application.Test\DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~EmailMimeMessageTest|FullyQualifiedName~EmailOptionsTest|FullyQualifiedName~PublicationClosedHandlerTest"`
Expected: PASS — 29 tests (6 MIME + 9 options + task 1's 14).

Then `dotnet build` and `dotnet test` — build clean (no new warnings), every test PASSES; the Application suite is now 126.

- [ ] **Step 10: Configuration**

In `src\DrivingLessons.Presentation.Web\appsettings.json`, replace

```json
  "Email": {
    "Enabled": false
  }
```

with

```json
  "Email": {
    "Enabled": false,
    "Host": "",
    "Port": 587,
    "Security": "StartTls",
    "Username": "",
    "Password": "",
    "FromAddress": "",
    "FromName": ""
  }
```

In `src\DrivingLessons.Presentation.Web\appsettings.Development.json`, replace

```json
  "Email": {
    "Enabled": false
  }
```

with

```json
  "Email": {
    "Enabled": false,
    "Host": "localhost",
    "Port": 1025,
    "Security": "None",
    "FromAddress": "noreply@local.dev",
    "FromName": "בית הספר לנהיגה (dev)"
  }
```

In `docker-compose.yml`, under `app.environment`, after `Jwt__ExpiryHours: "12"`, add:

```yaml
      Email__Enabled: ${EMAIL_ENABLED:-false}
      Email__Host: ${EMAIL_HOST:-}
      Email__Port: ${EMAIL_PORT:-587}
      Email__Username: ${EMAIL_USERNAME:-}
      Email__Password: ${EMAIL_PASSWORD:-}
      Email__FromAddress: ${EMAIL_FROM_ADDRESS:-}
      Email__FromName: ${EMAIL_FROM_NAME:-}
```

Append to `.env.example`:

```bash
# Teacher Excel email — AWS SES over SMTP (docs/decisions/0005-email-over-ses-smtp.md).
# Keep disabled until SES is set up: verify the sending domain, request production access
# (leave the sandbox), and create SES SMTP credentials in the SES console (not IAM access keys).
EMAIL_ENABLED=false
# Your SES region's SMTP endpoint.
EMAIL_HOST=email-smtp.eu-central-1.amazonaws.com
EMAIL_PORT=587
EMAIL_USERNAME=change-me-ses-smtp-username
EMAIL_PASSWORD=change-me-ses-smtp-password
EMAIL_FROM_ADDRESS=noreply@your-verified-domain.example
EMAIL_FROM_NAME=בית הספר לנהיגה
```

Check the compose file still renders: `docker compose config | grep Email__` → seven lines, `Email__Enabled: "false"` and `Email__Port: "587"` when your local `.env` sets no `EMAIL_*` (compose's `:-` defaults also cover a variable set to empty).

- [ ] **Step 11: Record the decision and the dev setup**

Create `docs\decisions\0005-email-over-ses-smtp.md`:

```markdown
# ADR 0005: Teacher Email over SES SMTP (MailKit)

**Status:** Accepted
**Date:** 30 September 2026

## Context

At every window close each teacher must receive their Excel file by email (requirements §6.4, US-44). [ADR 0002](0002-hosting-aws-lightsail.md) chose AWS SES as the provider. The code needs a way to talk to it, and developers need a way to see the real email without an AWS account. Lightsail instances cannot assume IAM roles, so any AWS option needs long-lived credentials on the host either way.

## Decision

Send over **SMTP with MailKit** to SES's SMTP endpoint (`email-smtp.<region>.amazonaws.com`, port 587, STARTTLS), authenticated with **SES SMTP credentials** supplied as environment variables (`EMAIL_*`, see `.env.example`).

- `SmtpEmailSender` (Infrastructure) implements `IEmailSender`; `EmailMimeMessage` builds the message (Hebrew, right-to-left HTML plus plain text, the Excel attached).
- `Email:Enabled` gates sending; disabled, the sender logs the skip. Enabled with a missing host or from address, the app fails at startup (`ValidateOnStart`).
- Locally, a **Mailpit** container (`axllent/mailpit`, SMTP on 1025, inbox on 8025) receives the mail; `appsettings.Development.json` points at it.
- A failed send is logged per teacher and never blocks the other teachers or the app (US-44 plan decision 4). There is no retry in v1; the admin download is the fallback.

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| AWS SDK (`AWSSDK.SimpleEmailV2`) | SES-native, but ties the code to one provider, and local runs need LocalStack or a real SES sandbox — the smoke could not see the delivered message |
| `System.Net.Mail.SmtpClient` | No package, but Microsoft marks it not recommended for new development; MailKit is its recommended replacement |

## Consequences

- One new package (MailKit, which brings MimeKit).
- The provider can change (any SMTP relay) with configuration only.
- SES setup is an operational step outside the code: verify the domain, leave the sandbox, create SMTP credentials, fill `EMAIL_*` on the host.
- Delivery problems are visible only in the logs until a failure record is added (US-44 plan open item 1).
```

In `docs\tech-stack.md` (Hosting & Operations), replace

```markdown
- **Email**: AWS SES (effectively free at this volume)
```

with

```markdown
- **Email**: AWS SES over SMTP with MailKit (effectively free at this volume) — see [ADR 0005](decisions/0005-email-over-ses-smtp.md); a local Mailpit container catches it in development
```

In `docs\development\running-the-project.md`, add this row at the end of the **Ports & defaults** table (after the `Dev admin login` row):

```markdown
| Mailpit inbox (optional, local email) | http://localhost:8025 (SMTP on 1025) |
```

and add this section after **API explorer (Scalar / OpenAPI)**, before **Build & test**:

````markdown
## Email locally (Mailpit)

At every window close the API emails each teacher their Excel file over SMTP ([ADR 0005](../decisions/0005-email-over-ses-smtp.md)). In Development it points at a local Mailpit on port 1025 but sends nothing until you turn it on — with `Email:Enabled` false (the default) it logs `Email disabled. Skipped send…` instead.

```bash
# (once) the local inbox — SMTP on 1025, web UI on 8025
docker run -d --name dl-mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
# (already created it before? just: docker start dl-mailpit)

# API with email on
dotnet run --project src/DrivingLessons.Presentation.Web -- --Email:Enabled=true
```

Publish a week with a short window; when it closes, each teacher's email (Hebrew subject, the `.xlsx` attached) appears at http://localhost:8025. In Docker Compose / production, set the `EMAIL_*` variables in `.env` (see `.env.example`).
````

- [ ] **Step 12: Commit**

```bash
git add src/DrivingLessons.Infrastructure/DrivingLessons.Infrastructure.csproj \
  src/DrivingLessons.Infrastructure/Options/EmailOptions.cs \
  src/DrivingLessons.Infrastructure/Email/EmailMimeMessage.cs \
  src/DrivingLessons.Infrastructure/Email/SmtpEmailSender.cs \
  src/DrivingLessons.Infrastructure/DependencyInjection.cs \
  src/DrivingLessons.Presentation.Web/appsettings.json \
  src/DrivingLessons.Presentation.Web/appsettings.Development.json \
  tests/DrivingLessons.Application.Test/Emails/EmailMimeMessageTest.cs \
  tests/DrivingLessons.Application.Test/Emails/EmailOptionsTest.cs \
  docker-compose.yml .env.example \
  docs/decisions/0005-email-over-ses-smtp.md docs/tech-stack.md docs/development/running-the-project.md
git status --short
git commit -m "feat(excel): send the close email over SMTP (MailKit, AWS SES)

LoggingEmailSender becomes SmtpEmailSender: disabled it logs the skip
as before, enabled it sends a Hebrew right-to-left email with the
Excel attached. EmailOptions carries the SMTP settings and fails the
boot when email is enabled but half-configured. Development points at
a local Mailpit; compose and .env.example carry the SES settings.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

(Step 7's `git mv` already staged the old path's removal; `git status --short` must show `R  …LoggingEmailSender.cs -> …SmtpEmailSender.cs`, the other paths staged, and only ` M .claude/launch.json` plus the two US-45 plan files unstaged.)

---

**Next:** [task-03-smoke-verification-and-pr.md](task-03-smoke-verification-and-pr.md)
