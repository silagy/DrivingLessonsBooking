# Task 7 of 18 (optional): The Roster's WhatsApp Consent column

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Requires task 5 committed. **Drop this task if review decision 12 is declined.** Nothing later depends on it.

**Files:**
- Modify: `src\DrivingLessons.Infrastructure\Csv\RosterCsvHeaders.cs` (`WhatsAppConsent = "הסכמה לוואטסאפ"`, optional, not in `Required`)
- Modify: `src\DrivingLessons.Application\Abstractions\RosterCsvRow.cs` (`string? WhatsAppConsent`)
- Modify: `src\DrivingLessons.Infrastructure\Csv\RosterCsvParser.cs` (reads the column when present)
- Create: `src\DrivingLessons.Domain\Values\RosterConsent.cs` (enum `Unchanged = 10, Given = 20, Withdrawn = 30`)
- Modify: `src\DrivingLessons.Domain\Values\RosterRowFailureReason.cs` (`InvalidWhatsAppConsent`)
- Modify: `src\DrivingLessons.Application\Commands\ImportRoster\ImportRosterInteractor.cs`
- Modify: `client\public\i18n\he.json`, `en.json` (the new failure reason; the column in the Roster help text)
- Test: `tests\DrivingLessons.Application.Test\Csv\RosterCsvParserTest.cs`, `tests\DrivingLessons.Application.Test\Commands\ImportRosterInteractorTest.cs`
- Modify: `docs\requirements.md` §5.5.1

**Interfaces:**
- Consumes (task 5): `Student.HasWhatsAppConsent`, `GiveWhatsAppConsent(DateTimeOffset)`, `WithdrawWhatsAppConsent()`; on `main`: the Roster import pipeline (parse → resolve Teacher and Car → create, update or reactivate → report).
- Produces: a Roster file can carry consent for many Students at once.

**Rules:**
- Header `הסכמה לוואטסאפ` (no Berosh alias: the Berosh export has no such column).
- `כן` / `yes` / `1` / `true` give consent. `לא` / `no` / `0` / `false` withdraw it. A blank cell, or no column at all, changes nothing. Matching ignores case and surrounding spaces; anything else rejects the row with `InvalidWhatsAppConsent` ("WhatsApp consent must be yes or no"), the rest of the file still imports, as with every other row failure.
- The interactor applies it **only when the state differs**: it calls `GiveWhatsAppConsent` on a Student without consent, `WithdrawWhatsAppConsent` on one with it, and nothing otherwise. That keeps the domain non-idempotent (rule 1) while a re-upload of the same file changes nothing.
- A new Student created from a row with `כן` gets consent in the same commit.

- [ ] **Step 1: Write the failing tests**

`RosterCsvParserTest`: `Reads_The_WhatsApp_Consent_Column`, `Leaves_WhatsApp_Consent_Null_Without_The_Column`. `ImportRosterInteractorTest`: `Gives_Consent_From_A_Yes_Cell`, `Withdraws_Consent_From_A_No_Cell`, `Leaves_Consent_Alone_For_A_Blank_Cell`, `Does_Not_Give_Consent_Twice_On_A_Re_Upload` (no domain exception, no extra event), `Rejects_A_Row_With_An_Unreadable_Consent` (`InvalidWhatsAppConsent`, other rows imported), `A_New_Student_From_A_Yes_Row_Has_Consent`.

Run: `dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj --filter "FullyQualifiedName~Roster"`. Expected: FAIL.

- [ ] **Step 2: Implement**

Parse the cell into `RosterConsent` inside the row validation, next to the other value parsing, and apply it after create, update or reactivate. Then add the i18n reason key `roster.failureReasons.invalidWhatsAppConsent` (he: `הסכמה לוואטסאפ חייבת להיות כן או לא`), and mention the optional column in the Roster screen's help text, if it lists the optional columns.

- [ ] **Step 3: Update requirements §5.5.1**

Add: "An optional column `הסכמה לוואטסאפ` (yes / no) gives or withdraws the Student's WhatsApp Consent; a blank cell or a missing column leaves it as it is."

- [ ] **Step 4: Run and commit**

```bash
dotnet test tests/DrivingLessons.Application.Test/DrivingLessons.Application.Test.csproj
cd client && npx ng test --watch=false && cd ..
git add src tests client/public/i18n docs/requirements.md
git commit -m "feat(roster): optional WhatsApp consent column"
```
