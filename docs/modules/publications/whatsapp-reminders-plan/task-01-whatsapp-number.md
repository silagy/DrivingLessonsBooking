# Task 1: A phone's WhatsApp number

Part of [WhatsApp Reminders for Students Who Haven't Submitted](README.md). Read the README's Decision 3 first.

**Branch:** `whatsapp-reminders`. **Prerequisite:** Task 0 reported.
**Scope guard:** `src\DrivingLessons.Domain\Values\WhatsAppNumber.cs` (new), `src\DrivingLessons.Domain\Values\PhoneNumber.cs`, `tests\DrivingLessons.Domain.Test\Values\PhoneNumberTest.cs`.

**Interfaces:**
- Consumes: `PhoneNumber.Of(string)`, `PhoneNumber.Value`.
- Produces:
  - `public record WhatsAppNumber { public string Value { get; } }`, digits only, with a private constructor and an `internal static WhatsAppNumber Of(string digits)` that only `PhoneNumber` calls. It needs no validation of its own: `PhoneNumber` is the only place one is made.
  - `public WhatsAppNumber? ToWhatsAppNumber()` on `PhoneNumber`.

- [ ] **Step 1.1: Write the failing tests** in `PhoneNumberTest`, in the file's existing style (`//given //when //then`, Shouldly):

```csharp
[TestMethod]
[DataRow("050-1234567", "972501234567")]
[DataRow("0501234567", "972501234567")]
[DataRow("054 123 4567", "972541234567")]
[DataRow("(052) 123-4567", "972521234567")]
[DataRow("+972-50-1234567", "972501234567")]
[DataRow("+972 50 123 4567", "972501234567")]
[DataRow("972501234567", "972501234567")]
[DataRow("00972501234567", "972501234567")]
[DataRow("050.123.4567", "972501234567")]
[DataRow("‏050-1234567‎", "972501234567")]
public void Israeli_Mobile_Converts_To_WhatsApp_Number(string value, string expected)
// PhoneNumber.Of(value).ToWhatsAppNumber()!.Value.ShouldBe(expected);

[TestMethod]
[DataRow("02-6234567")]
[DataRow("03 1234567")]
[DataRow("050-123456")]
[DataRow("050-12345678")]
[DataRow("+44 7911 123456")]
[DataRow("+050-1234567")]
[DataRow("050-1234567 / 052-7654321")]
[DataRow("050-1234567 ext 2")]
[DataRow("1-800-123-4567")]
public void Other_Phone_Has_No_WhatsApp_Number(string value)
// PhoneNumber.Of(value).ToWhatsAppNumber().ShouldBeNull();
```

- [ ] **Step 1.2: Run them to verify they fail**

Run: `dotnet test tests\DrivingLessons.Domain.Test --filter FullyQualifiedName~PhoneNumberTest`
Expected: the build fails, because `ToWhatsAppNumber` doesn't exist.

- [ ] **Step 1.3: Implement `WhatsAppNumber` and `PhoneNumber.ToWhatsAppNumber()`**

Follow decision 3. Named constants for the prefixes (`"05"`, `"9725"`, `"009725"`, `"972"`) and lengths (10, 12, 14), with no magic values. Allowed characters are a `static readonly` set. Early returns, no nested conditions. `Value` already has its bidi marks removed (`PhoneNumber.Of`).

- [ ] **Step 1.4: Run the tests**

Run: `dotnet test tests\DrivingLessons.Domain.Test`
Expected: all pass. The Domain count is Task 0's plus 19 data rows.

- [ ] **Step 1.5: Commit**

```bash
git add src/DrivingLessons.Domain/Values/WhatsAppNumber.cs src/DrivingLessons.Domain/Values/PhoneNumber.cs tests/DrivingLessons.Domain.Test/Values/PhoneNumberTest.cs
git commit -m "feat(students): a phone's WhatsApp number, for Israeli mobiles only"
```
