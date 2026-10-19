# Task 5 of 18: WhatsApp number and WhatsApp Consent on the Student

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp` (from `origin/main` after PR A merged), commands from the repo root in bash.

**Files:**
- Create: `src\DrivingLessons.Domain\Values\WhatsAppNumber.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\WhatsAppNumberMustBeIsraeliMobileException.cs`
- Modify: `src\DrivingLessons.Domain\Values\PhoneNumber.cs` (`ToWhatsAppNumber()`)
- Modify: `src\DrivingLessons.Domain\Entities\Student.cs` (`WhatsAppConsentGivenAtUtc`, `HasWhatsAppConsent`, `GiveWhatsAppConsent`, `WithdrawWhatsAppConsent`)
- Create: `src\DrivingLessons.Domain\Events\StudentWhatsAppConsentGiven.cs`, `StudentWhatsAppConsentWithdrawn.cs`
- Create: `src\DrivingLessons.Domain\Exceptions\StudentWhatsAppConsentAlreadyGivenException.cs`, `StudentMustHaveWhatsAppConsentException.cs`
- Create: `src\DrivingLessons.Application\Commands\GiveStudentWhatsAppConsent\GiveStudentWhatsAppConsentInteractor.cs`
- Create: `src\DrivingLessons.Application\Commands\WithdrawStudentWhatsAppConsent\WithdrawStudentWhatsAppConsentInteractor.cs`
- Modify: `src\DrivingLessons.Application\Commands\CreateStudent\CreateStudentRequest.cs` (`HasWhatsAppConsent`, default `false`) and `CreateStudentInteractor.cs`
- Modify: `src\DrivingLessons.Application\Queries\FindStudents\ItemForFindStudentsResponse.cs`, `Queries\GetStudent\GetStudentResponse.cs` (`HasWhatsAppConsent`), and their projections in `StudentQueries`
- Modify: `src\DrivingLessons.Application\DependencyInjection.cs`
- Modify: `src\DrivingLessons.Infrastructure\EntityFramework\EntityConfigurations\StudentConfiguration.cs` (`whatsapp_consent_given_at_utc`)
- Create: migration `{timestamp}_AddStudentWhatsAppConsent`
- Modify: `src\DrivingLessons.Presentation.Web\Controllers\Student\StudentCommandController.cs` (two actions)
- Test: `tests\DrivingLessons.Domain.Test\Values\WhatsAppNumberTest.cs` (new), `Values\PhoneNumberTest.cs`, `Entities\StudentTest.cs`
- Test: `tests\DrivingLessons.Application.Test\Commands\GiveStudentWhatsAppConsentInteractorTest.cs`, `WithdrawStudentWhatsAppConsentInteractorTest.cs` (new), `CreateStudentInteractorTest.cs`

**Interfaces:**
- Consumes (on `main`): `PhoneNumber.Value`; `Student` (with its `xmin` concurrency token, ADR 0007); `IStudentRepository.GetAsync(StudentId)`; `StudentNotFoundException`; `TimeProvider`; the controller conventions of `deactivate` / `reactivate`.
- Produces:
  - `WhatsAppNumber.Of(string value)` (validates `^9725\d{8}$`) and `.Value`.
  - `PhoneNumber.ToWhatsAppNumber() : WhatsAppNumber?`. Task 8's recipient planning calls it.
  - `Student.HasWhatsAppConsent`, `Student.GiveWhatsAppConsent(DateTimeOffset givenAtUtc)`, `Student.WithdrawWhatsAppConsent()`.
  - `POST api/students/{id}/give-whatsapp-consent` and `POST api/students/{id}/withdraw-whatsapp-consent` → 204 / 404 / 409 (Administrator).
  - `hasWhatsAppConsent` on `POST api/students` (optional) and on the list and details responses. Task 6 consumes them.

- [ ] **Step 1: Write the failing value tests**

`WhatsAppNumberTest`:
- `Of` `[DataRow("972501234567")]` → `.Value` equal.
- `Must_Be_Israeli_Mobile` `[DataRow("97221234567")] [DataRow("0501234567")] [DataRow("97250123456")] [DataRow("9725012345678")] [DataRow("")]` → `WhatsAppNumberMustBeIsraeliMobileException`.

`PhoneNumberTest`, new method `To_WhatsApp_Number`:

| Input | Output |
|-------|--------|
| `050-1234567` | `972501234567` |
| `0501234567` | `972501234567` |
| `050 123 4567` | `972501234567` |
| `(050) 123-4567` | `972501234567` |
| `+972 50-123-4567` | `972501234567` |
| `+972-0501234567` | `972501234567` |
| `00972501234567` | `972501234567` |
| `972501234567` | `972501234567` |
| `501234567` | `972501234567` |
| `‎050-1234567‏` (bidi marks, stripped by `Of`) | `972501234567` |

`To_WhatsApp_Number__Is_Null_When_Not_An_Israeli_Mobile`: `02-6251234`, `+1 212 555 0100`, `050-123456` (one digit short), `050-12345678` (one long), `1-800-123-456`.

- [ ] **Step 2: Implement them**

`PhoneNumber.ToWhatsAppNumber()`: keep the ASCII digits of `Value`. Strip a leading `00`, then a leading `972`, then a leading `0`. If what remains is 9 digits starting with `5`, return `WhatsAppNumber.Of("972" + rest)`; otherwise `null`. No exception escapes for any input `PhoneNumber.Of` accepted. Run the domain tests for the two classes: PASS.

- [ ] **Step 3: Write the failing Student tests**

In `StudentTest`:

| Test | Asserts |
|------|---------|
| `New_Student_Has_No_WhatsApp_Consent` | `//expected` `HasWhatsAppConsent` false |
| `Give_WhatsApp_Consent` | true, `WhatsAppConsentGivenAtUtc == at` |
| `Give_WhatsApp_Consent__Add_Event` | `StudentWhatsAppConsentGiven(Id, at)` |
| `Give_WhatsApp_Consent__Must_Not_Have_Given_It_Already` | → `StudentWhatsAppConsentAlreadyGivenException` |
| `Withdraw_WhatsApp_Consent` | false, `WhatsAppConsentGivenAtUtc` null |
| `Withdraw_WhatsApp_Consent__Add_Event` | `StudentWhatsAppConsentWithdrawn(Id)` |
| `Withdraw_WhatsApp_Consent__Must_Have_It` | → `StudentMustHaveWhatsAppConsentException` |
| `Consent_Survives_Deactivate_And_Reactivate` | given → deactivate → reactivate → still given (the record of consent is the Student's, not the activity's) |

Consent is allowed on an Inactive Student: recipients exclude Inactive Students anyway (decision 10), and refusing it would only add a rule.

- [ ] **Step 4: Implement the Student change**

Guards → mutation → event, as in `Deactivate`. Messages: `$"Student {id.Value} has already given WhatsApp consent."` / `$"Student {id.Value} must have WhatsApp consent."`. Run `StudentTest`: PASS.

- [ ] **Step 5: Write the failing interactor tests**

`GiveStudentWhatsAppConsentInteractorTest`: `Gives_Consent_At_The_Current_Time_And_Commits`, `Student_Must_Exist` (→ `StudentNotFoundException`), `A_Student_Who_Already_Gave_It_Is_Refused` (the domain exception propagates, no commit). `WithdrawStudentWhatsAppConsentInteractorTest`: the mirror. `CreateStudentInteractorTest`: `Creates_With_Consent_When_Asked` (one commit, `HasWhatsAppConsent`), `Creates_Without_Consent_By_Default`.

- [ ] **Step 6: Implement the interactors, persistence and endpoints**

The interactors: resolve → domain call → `CommitAsync`, each with `(IStudentRepository, IUnitOfWork, TimeProvider)` as needed. `CreateStudentInteractor` calls `student.GiveWhatsAppConsent(timeProvider.GetUtcNow())` after `Student.Create` when `request.HasWhatsAppConsent`. Add `TimeProvider` to its constructor, and to its test's setup.

`StudentConfiguration`: `builder.Property(x => x.WhatsAppConsentGivenAtUtc).HasColumnName("whatsapp_consent_given_at_utc").HasColumnType("timestamptz");`. The projections set `HasWhatsAppConsent = x.WhatsAppConsentGivenAtUtc != null`.

```bash
dotnet ef migrations add AddStudentWhatsAppConsent --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web --output-dir EntityFramework/Migrations
```

Expected: one nullable column. Every existing Student is without consent (decision 12).

`StudentCommandController`: `GiveWhatsAppConsentAsync` (`[HttpPost("{id:guid}/give-whatsapp-consent")]`, summary "Record that the student agreed to receive WhatsApp messages about their weekly submission") and `WithdrawWhatsAppConsentAsync`, shaped exactly like `DeactivateAsync`.

Register both interactors in `Application\DependencyInjection.cs`.

- [ ] **Step 7: Run every suite and commit**

```bash
dotnet build
dotnet test tests/DrivingLessons.Domain.Test/DrivingLessons.Domain.Test.csproj
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
dotnet ef migrations has-pending-model-changes --project src/DrivingLessons.Infrastructure --startup-project src/DrivingLessons.Presentation.Web
git add src tests
git commit -m "feat(students): WhatsApp consent on the student, and Israeli mobile numbers in international format"
```
