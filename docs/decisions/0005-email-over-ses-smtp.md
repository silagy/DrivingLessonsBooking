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
