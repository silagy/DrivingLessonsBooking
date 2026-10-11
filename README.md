# Driving Lessons Booking
 
A weekly demand collection system for a driving school. Replaces a ~3-hour manual WhatsApp-to-Excel ritual with a self-service flow: the admin publishes a weekly availability grid per teacher, students submit ranked session preferences through a shareable link, and the system produces the Excel file the teacher uses for booking.
 
## The problem
 
Every week, the teacher collects students' scheduling preferences over WhatsApp and manually assembles them into an Excel file. This takes about three hours. The Excel file is the input to his actual booking work; the collection step is pure waste.
 
## Success metric
 
Minutes from submission-window close to a usable Excel file.
**Target: under 1 minute. Baseline: ~180 minutes.**
 
## Scope boundary
 
This system ends at the Excel file. Booking, assignment, and student notification are explicitly out of scope for v1.
 
## Documentation
 
| Document | Purpose |
|---|---|
| [docs/requirements.md](docs/requirements.md) | Full v1 requirements: personas, domain model, flows, Excel spec, decisions log |
| [docs/tech-stack.md](docs/tech-stack.md) | Implementation constraints: stack (latest .NET, latest Angular, PrimeNG, signals), monorepo layout, single deployable, hosting |
| [docs/decisions/](docs/decisions/) | Architecture Decision Records: [0001 monorepo + single deployable](docs/decisions/0001-monorepo-single-deployable.md), [0002 hosting on AWS Lightsail](docs/decisions/0002-hosting-aws-lightsail.md), [0003 roster CSV and the weekly link](docs/decisions/0003-roster-csv-and-weekly-link-model.md), [0004 no admin aggregate (superseded)](docs/decisions/0004-no-admin-domain-aggregate.md), [0005 email over SES SMTP](docs/decisions/0005-email-over-ses-smtp.md), [0006 User aggregate with two Roles](docs/decisions/0006-user-aggregate-and-roles.md) |
| [CONTEXT.md](CONTEXT.md) | Glossary: the domain terms to use, and the words to avoid |
 
## Status
 
v1 is implemented, including Users and Roles and Student management from spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82). Open work is on the [issue tracker](https://github.com/silagy/DrivingLessonsBooking/issues).
