# Task 4: The section on the dashboard

Part of [WhatsApp Reminders for Students Who Haven't Submitted](README.md). Read the README's Decisions 1, 2 and 6, and the phone rules in Global Constraints, first.

**Branch:** `whatsapp-reminders`. **Prerequisite:** Task 3 committed.
**Scope guard:**
- Create: `client\src\app\features\publications\ui\components\students-without-submission\students-without-submission.component.{ts,html,scss,spec.ts}`
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.{ts,html,scss,spec.ts}`
- Modify: `client\public\i18n\he.json`, `en.json` (the rest of `publications.reminders.*`)

**Interfaces:**
- Consumes: `ReminderRow` and the store signals from Task 3, `TeacherOption` (`domain\teacher-option.model.ts`), `store.dataAsOf()`.
- Produces: `StudentsWithoutSubmissionComponent`, selector `app-students-without-submission`. It is presentational: it takes signal inputs and emits outputs, and never injects the store (`client-state.md`).
  - Inputs: `rows: ReminderRow[]`, `isLoading: boolean`, `errorKey: string | null`, `showTeacher: boolean`, `teachers: TeacherOption[]`, `selectedTeacherId: string | null`, `dataAsOf: string`.
  - Output: `teacherFiltered: string | null`.

- [ ] **Step 4.1: Add the translation keys** under `publications.reminders`, in both files:

| Key | he | en |
|-----|----|----|
| `title` | `עוד לא שלחו ({{count}})` | `Haven't submitted yet ({{count}})` |
| `hint` | `הרשימה נכונה לזמן טעינת הדף ({{time}}). רעננו לפני שמתחילים לשלוח.` | `As of page load ({{time}}). Refresh before you start sending.` |
| `teacherFilter` | `מורה` | `Teacher` |
| `allTeachers` | `כל המורים` | `All teachers` |
| `send` | `וואטסאפ` | `WhatsApp` |
| `sendAria` | `שליחת תזכורת בוואטסאפ ל{{name}}` | `Send {{name}} a WhatsApp reminder` |
| `noWhatsApp` | `המספר לא מתאים לוואטסאפ` | `Not a WhatsApp number` |
| `empty` | `כל התלמידים שלחו.` | `Every student has submitted.` |
| `loadFailed` | `טעינת הרשימה נכשלה. רעננו את הדף.` | `Couldn't load the list. Refresh the page.` |

- [ ] **Step 4.2: Write the failing component tests** in `students-without-submission.component.spec.ts`, set up like `slot-count-cell.component.spec.ts` (`TranslocoTestingModule`, keys as text):

```ts
it('lists each student with a WhatsApp link that opens in a new tab')
// a row with whatsAppUrl: <a> with href === row.whatsAppUrl, target '_blank', rel containing 'noopener',
// aria-label 'publications.reminders.sendAria'

it('shows the phone and no link when the student has no WhatsApp number')
// whatsAppUrl null: no <a> in the row; the phone text inside a bdi[dir="ltr"]; 'publications.reminders.noWhatsApp' shown

it('shows each row\'s teacher and the filter only when asked')
// showTeacher true: teacher names and a select labelled 'publications.reminders.teacherFilter'
// with 'publications.reminders.allTeachers' first; showTeacher false: neither

it('emits the chosen teacher, and null for all teachers')

it('counts the students it lists in the title')
// two rows → the title is rendered with count 2

it('says everyone has submitted when the list is empty')

it('shows the load error instead of the list')
```

Run: `ng.js test --watch=false --include src/app/features/publications/ui/components/students-without-submission/students-without-submission.component.spec.ts`
Expected: FAIL (no component).

- [ ] **Step 4.3: Implement the component**

Build it to the design's frames **Dashboard (desktop) - Administrator, Open**, **Phone - Administrator**, **Phone - Teacher-role User** and **Section states** (README Spec). They show, in order: the title with its count, the hint, then the filter at the inline end. Each row has initials, name and teacher, the LTR phone, and the pill WhatsApp button (primary, chat icon). Below 640px the row stacks name, teacher and phone, with a 44px button at the inline end. The no-WhatsApp note is a muted pill. The design's hex values map to `--app-*` tokens:
- `#0057FF` → `--p-primary-color`;
- `#52555C` → `--app-text-secondary`;
- `#E6E8F6` → `--app-border`;
- `#C3CFE5` → `--app-border-interactive`;
- `#EAF1FF` / `#003293` → `--app-tag-sky-bg` / `--app-tag-sky-fg`;
- `#F5F6FF` → `--app-bg-muted`;
- the error box → `--app-error-*`.

- A `section` card in the dashboard's card style (`dashboard__card` look, through tokens in the component SCSS).
  - **Heading:** `title` with `count: rows().length`.
  - **Hint:** `hint` with `time: dataAsOf()`.
  - **Filter:** when `showTeacher()`, a `p-select` with an "All teachers" option (value `null`) followed by `teachers()`.
  - **List:** a `ul`, one `li` per row:
    - the name;
    - the teacher's name, when `showTeacher()`;
    - the phone in `<bdi dir="ltr">`;
    - the link from decision 6, `<a pButton [href]="row.whatsAppUrl" target="_blank" rel="noopener" icon="pi pi-whatsapp" [label]="'publications.reminders.send' | transloco" [attr.aria-label]="...sendAria, { name }">`, or the `noWhatsApp` note.
  - **States:** `p-progressSpinner` while `isLoading()`; `errorKey()` text on error; `empty` when there are no rows.
- **Layout:**
  - From 640px up, each row is name and teacher, then the phone, then the button at the inline end.
  - Below 640px, name and teacher stack above the phone. The button stays at the inline end, at least 44px tall; names wrap.
  - Logical properties only.

Run the component spec. Expected: PASS.

- [ ] **Step 4.4: Wire the section into the dashboard**

In `publications-dashboard.page.html`, in the `PublicationState.open` case only, after the grid `section`:

```html
<app-students-without-submission
    class="dashboard__reminders"
    [rows]="store.reminderRows()"
    [isLoading]="store.remindersAreLoading()"
    [errorKey]="store.remindersError()"
    [showTeacher]="store.canChooseTeacher()"
    [teachers]="store.teachers()"
    [selectedTeacherId]="store.reminderTeacherId()"
    [dataAsOf]="store.dataAsOf()"
    (teacherFiltered)="store.selectReminderTeacher($event)" />
```

Add the import to the page's `imports`. Page spec tests, in the existing page spec's style:
- `shows the students who haven't submitted only while open`: present when Open; absent in Draft, Published and Closed;
- `passes the teacher filter through to the store`.

Run the page spec, then the whole client suite and the build.
Expected: all pass, with the count at Task 3's plus 9; the build succeeds with no new budget warning.

- [ ] **Step 4.5: Commit**

```bash
git add client/src/app/features/publications/ui client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(publications): students who haven't submitted, with WhatsApp reminders, on the dashboard"
```
