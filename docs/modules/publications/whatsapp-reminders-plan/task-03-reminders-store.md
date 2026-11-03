# Task 3: The reminder list in the store

Part of [WhatsApp Reminders for Students Who Haven't Submitted](README.md). Read the README's Decisions 2, 4, 5 and 9 first.

**Branch:** `whatsapp-reminders`. **Prerequisite:** Task 2 committed.
**Scope guard:**
- Move: `client\src\app\features\student-form\domain\week-label.ts` and `week-label.spec.ts` → `client\src\app\shared\dates\` (`git mv`). Update the imports in `features\student-form\domain\slot-day.ts`, `features\student-form\state\student-form.store.ts` and the moved spec.
- Create: `client\src\app\features\publications\data\item-for-find-students-without-submission.response.ts`, `domain\whatsapp-reminder.ts` + `.spec.ts`, `domain\reminder-row.model.ts`
- Modify: `client\src\app\features\publications\data\publications-api.service.ts`, `state\publications.store.ts` + `.spec.ts`
- Modify: `client\public\i18n\he.json`, `en.json` (`publications.reminders.message` only)

**Interfaces:**
- Consumes: the two endpoints from Task 2, `formatWindowInstant` (`shared\dates\jerusalem-time.ts`), `PublicationsStore.shareLink()`, `publication()`, `state()`, `teachers()`, `auth.isAdministrator()`, `auth.teacherId()`.
- Produces:
  - `interface ItemForFindStudentsWithoutSubmissionResponse { studentId: string; studentName: string; teacherId: string; teacherName: string; phone: string; whatsAppNumber: string | null }`.
  - `findStudentsWithoutSubmission(id: string, teacherId: string): Observable<ItemForFindStudentsWithoutSubmissionResponse[]>` and `findSchoolStudentsWithoutSubmission(id: string): Observable<...[]>`, on `PublicationsApiService`.
  - In `domain\whatsapp-reminder.ts`:
    - `export const REMINDER_LOCALE = 'he-IL'`;
    - `export interface ReminderMessageInput { studentName: string; teacherName: string; weekNumber: number; weekStart: string; windowEndUtc: string; link: string }`;
    - `export interface ReminderMessageParams { name: string; teacherName: string; weekNumber: number; weekRange: string; closesAt: string; link: string }`;
    - `export function reminderMessageParams(input: ReminderMessageInput): ReminderMessageParams`;
    - `export function whatsAppChatUrl(whatsAppNumber: string, message: string): string`.
  - `interface ReminderRow { studentId: string; studentName: string; teacherName: string; phone: string; whatsAppUrl: string | null }` in `domain\reminder-row.model.ts`.
  - Store: `reminderRows: Signal<ReminderRow[]>`, `reminderTeacherId: Signal<string | null>`, `remindersAreLoading: Signal<boolean>`, `remindersError: Signal<string | null>`, `selectReminderTeacher(teacherId: string | null): void`.

- [ ] **Step 3.1: Move the student form's week label to `shared\dates`**

`git mv` both files and update the three imports. No code change.
Run: `ng.js test --watch=false --include src/app/shared/dates/week-label.spec.ts`, then the student-form specs (`--include "src/app/features/student-form/**/*.spec.ts"`).
Expected: they pass, with the same counts as before the move.

- [ ] **Step 3.2: Write the failing domain tests** in `whatsapp-reminder.spec.ts`:

```ts
it('builds a wa.me link with the message encoded', () => {
    // whatsAppChatUrl('972501234567', 'שלום נועה, היי & ביי?\nשורה')
    // → 'https://wa.me/972501234567?text=' + encodeURIComponent('שלום נועה, היי & ביי?\nשורה')
});

it('formats the week and the close time in Hebrew, in Jerusalem time', () => {
    // reminderMessageParams({ studentName: 'נועה לוי', teacherName: 'משה', weekNumber: 46,
    //   weekStart: '2026-11-08', windowEndUtc: '2026-11-05T18:00:00Z', link: 'https://drivecal.co.il/s/abc' })
    // name 'נועה לוי', teacherName 'משה', weekNumber 46, link unchanged;
    // weekRange === weekRangeLabel('2026-11-08', 'he-IL');
    // closesAt === formatWindowInstant('2026-11-05T18:00:00Z', 'he-IL'), and it contains '20:00' (UTC+2 in November)
});
```

- [ ] **Step 3.3: Add the message key, then implement `whatsapp-reminder.ts`**

`he.json`, `publications.reminders.message` (intent §4.4, exact; lines joined with `\n`):

```
שלום {{name}}, עוד לא קיבלנו מכם את השיעורים האפשריים לשבוע {{weekNumber}} ({{weekRange}}) אצל {{teacherName}}.\nאפשר לשלוח אותם עד {{closesAt}} בקישור הזה:\n{{link}}\nככל שתיתנו יותר שיעורים אפשריים, כך גדל הסיכוי שתקבלו את כל השיעורים שביקשתם.
```

`en.json`, same key: `Hi {{name}}, we haven't received your possible lessons for week {{weekNumber}} ({{weekRange}}) with {{teacherName}} yet.\nYou can send them until {{closesAt}} at this link:\n{{link}}\nThe more possible lessons you give, the better your chance of getting all the lessons you asked for.` (parity only; never sent, decision 4).

`whatsAppChatUrl` is `https://wa.me/${whatsAppNumber}?text=${encodeURIComponent(message)}`.
Run the spec. Expected: PASS.

- [ ] **Step 3.4: Add the response type and the two API calls**, next to `findSlotRequests`. `findStudentsWithoutSubmission` sends `teacherId` as `HttpParams`.

- [ ] **Step 3.5: Write the failing store tests** in `publications.store.spec.ts`. Extend its setup:
- the fake `PublicationsApiService` gets both calls;
- `TranslocoTestingModule.forRoot({ langs: { en: { publications: { reminders: { message: 'EN {{name}}' } } }, he: { publications: { reminders: { message: 'HE {{name}} {{link}}' } } } }, translocoConfig: { availableLangs: ['he', 'en'], defaultLang: 'en' }, preloadLangs: false })`.

Tests (Open publication unless stated; rows from the school call: Noa of Teacher A with `whatsAppNumber: '972501234567'`, Omer of Teacher B with `null`):

```ts
it('loads every teacher\'s students for an administrator while open')
// findSchoolStudentsWithoutSubmission called once with the publication id; findStudentsWithoutSubmission never;
// reminderRows() names ['Noa', 'Omer'] with teacherName

it('loads only the teacher\'s own students for a teacher')
// signed in as Teacher A: findStudentsWithoutSubmission(id, 'teacher-a') once; the school call never

it('loads no reminder list unless the publication is open')
// Draft, Published and Closed: neither call; reminderRows() is []

it('narrows the administrator\'s list to one teacher and back to all')
// selectReminderTeacher('teacher-b') → ['Omer']; selectReminderTeacher(null) → ['Noa', 'Omer']; no new API call

it('resets the teacher filter when the week changes')
// selectReminderTeacher('teacher-b'); selectWeek(<another week with an Open publication>) → reminderTeacherId() is null

it('writes the message in Hebrew when the UI is English')
// language lang 'en': Noa's whatsAppUrl starts with 'https://wa.me/972501234567?text=' and its decoded text is 'HE Noa ' + shareLink()

it('gives no link to a student without a WhatsApp number')
// Omer's whatsAppUrl is null and his phone is kept as given

it('reports a failed reminder list')
// the school call errors → remindersError() is 'publications.reminders.loadFailed'
```

Run: `ng.js test --watch=false --include src/app/features/publications/state/publications.store.spec.ts`
Expected: the new tests FAIL.

- [ ] **Step 3.6: Implement the store**

- Inject `TranslocoService`.
- **`remindersResource`:** its params are `undefined` unless `state()` is `PublicationState.open`. Otherwise they are `{ publicationId }` for an Administrator, or `{ publicationId, teacherId: auth.teacherId() }` for a Teacher. The loader calls the school or the Teacher endpoint.
- **`hebrewResource`:** params `true` while open, loader `firstValueFrom(transloco.load('he'))`.
- **`reminderTeacherIdState`:** a `linkedSignal` on the Publication id, reset to `null`, the same pattern as `openedSlotIdState`. `selectReminderTeacher` sets it, and only for an Administrator.
- **`reminderRows`:** `[]` until both resources have values; then filter by `reminderTeacherIdState`, keeping the server's order (decision 9).
  - For each row with a `whatsAppNumber`: `transloco.translate('publications.reminders.message', reminderMessageParams({...}), 'he')`. The input comes from the row, `publication().weekNumber`, `weekStart`, `windowEndUtc` and `shareLink()`. Wrap the result with `whatsAppChatUrl`.
  - Other rows get `whatsAppUrl: null`.
- **`remindersAreLoading`:** either resource loading. **`remindersError`:** `'publications.reminders.loadFailed'` when `remindersResource.error()`.

Run the store spec, then the whole client suite.
Expected: all pass; the count is Task 0's plus 2 domain and 8 store tests.

- [ ] **Step 3.7: Commit**

```bash
git add client/src/app/shared/dates client/src/app/features/student-form client/src/app/features/publications/data client/src/app/features/publications/domain client/src/app/features/publications/state client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(publications): reminder list and Hebrew WhatsApp links in the dashboard store"
```
