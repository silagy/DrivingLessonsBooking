# Task 1 of 3: A slot is a שיעור אפשרי, and more slots mean a better chance

> Part of [More Slots, Better Chance](README.md). Read its Decisions and Global Constraints first. Branch `more-slots-better-chance`.

**Files:**
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json` (`studentForm.details`, `.target`, `.slots`, `.pick`, `.review`)
- Modify: `client\src\app\features\student-form\ui\components\slots-step\slots-step.component.html` / `.scss`
- Modify: `client\src\app\features\student-form\ui\components\review-step\review-step.component.ts` / `.html`
- Test: `client\src\app\core\translations.spec.ts`
- Docs: `CONTEXT.md` (new **Slot** entry under `### School`)

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: key `studentForm.review.preferred` (replaces `preferredOne` / `preferredMany`); key `studentForm.slots.moreSlotsHint` (replaces `slots.backups`); class `.slots__more-slots` (replaces `.slots__backups`); the new Hebrew value of `studentForm.review.addSlots`, which task 2's button reuses.

- [ ] **Step 1: Write the failing specs**

In `translations.spec.ts`, add a helper `leavesOf(tree: TranslationTree, prefix = ''): [string, string][]` (key path and value of every string leaf, the same walk as `keysWithTypographicPunctuation`), and two specs:

```typescript
it('call a slot שיעור אפשרי on the Hebrew student form, never שעה', () => {
    //given
    const clockTimeKeys = ['studentForm.pick.constraintHint'];

    //when
    const offending = leavesOf((he as TranslationTree)['studentForm'] as TranslationTree, 'studentForm.')
        .filter(([key, value]) => /שע(ה|ות)/.test(value) && !clockTimeKeys.includes(key))
        .map(([key]) => key);

    //then
    expect(offending).toEqual([]);
});

it('never call extra picks a backup on the student form', () => {
    //when
    const offending = [
        ...leavesOf((he as TranslationTree)['studentForm'] as TranslationTree, 'he: studentForm.')
            .filter(([, value]) => value.includes('גיבוי')),
        ...leavesOf((en as TranslationTree)['studentForm'] as TranslationTree, 'en: studentForm.')
            .filter(([, value]) => /backup/i.test(value)),
    ].map(([key]) => key);

    //then
    expect(offending).toEqual([]);
});
```

- [ ] **Step 2: Run them to verify they fail**

Run (from `client\`): `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/core/translations.spec.ts`
Expected: both new specs FAIL. The first lists `details.noAvailabilityTitle`, `target.body`, `target.pickSlots`, `slots.title`, `slots.body`, `pick.constraintLabel`, `review.preferredOne`, `review.preferredMany`, `review.addSlots`, `review.errors.rejected`, `review.errors.notFound`. The second lists `slots.backups` and both `preferred*` keys, in both languages.

- [ ] **Step 3: Rewrite the strings**

Exact values (he / en). Keys not listed keep their value. `en` changes only where shown.

| Key | he | en |
|-----|----|----|
| `details.noAvailabilityTitle` | עדיין אין שיעורים אפשריים לשבוע הזה. | unchanged |
| `target.body` | בשלב הבא תבחרו שיעורים אפשריים לפי סדר העדפה. מספר השיעורים האפשריים לא צריך להיות שווה למספר השיעורים שביקשתם: ככל שתיתנו יותר שיעורים אפשריים, אתם מגדילים את הסיכוי לקבל את מספר השיעורים שביקשתם. | Next you'll pick the slots you can make, ranked by preference. They don't have to match the number of lessons: the more slots you give, the better your chance of getting all the lessons you asked for. |
| `target.pickSlots` | לבחירת שיעורים אפשריים | unchanged |
| `slots.title` | בחרו שיעורים אפשריים, המועדף קודם | unchanged |
| `slots.body` | הקישו על שיעור אפשרי כדי להוסיף אותו - הסדר הוא סדר ההעדפה שלכם. הקישו על שיעור אפשרי שבחרתם כדי לשנות או להסיר אותו. מה שמקווקו לא זמין. | unchanged |
| `slots.moreSlotsHint` (new, replaces `slots.backups`) | יותר שיעורים אפשריים = יותר סיכוי לקבל את השיעורים | more slots = better chance of getting your lessons |
| `pick.constraintLabel` | אילוץ לשיעור האפשרי הזה (לא חובה) | unchanged |
| `review.preferred` (new, replaces `preferredOne` and `preferredMany`) | דירוג 1 הוא השיעור האפשרי המועדף עליכם. כל שיעור אפשרי נוסף ברשימה מגדיל את הסיכוי שתקבלו את השיעורים שביקשתם. | Rank 1 is your preferred slot. Every extra slot on the list improves your chance of getting the lessons you asked for. |
| `review.addSlots` | הוספת שיעורים אפשריים | unchanged |
| `review.unlockHint` | השליחה תיפתח כשיהיו לכם {{target}} שיעורים אפשריים. | unchanged |
| `review.errors.rejected` | לא הצלחנו לשמור את הרשימה - משהו השתנה מאז שפתחתם את הטופס, למשל שיעור אפשרי שכבר לא זמין. | unchanged |
| `review.errors.notFound` | לא מצאנו יותר את הפרטים שלכם ברשימת התלמידים או את השיעורים האפשריים של השבוע. פנו לבית הספר. | unchanged |

Delete `slots.backups`, `review.preferredOne` and `review.preferredMany` from both files.

- [ ] **Step 4: Point the templates at the new keys**

- Slots step HTML: the `<span>` becomes `<span class="slots__more-slots">{{ 'studentForm.slots.moreSlotsHint' | transloco }}</span>`. In the SCSS, rename the `.slots__backups` rule to `.slots__more-slots`, declarations unchanged.
- Review step: remove `SINGLE_PREFERRED_TARGET` and the `preferredKey` computed. In the HTML summary, `{{ preferredKey() | transloco: { target: targetCount() } }}` becomes `{{ 'studentForm.review.preferred' | transloco }}`.

- [ ] **Step 5: Run the specs**

Run Step 2's command. Expected: PASS.
Run `... ng.js test --watch=false --include src/app/features/student-form/ui/pages/student-form/student-form.page.spec.ts`. Expected: PASS (no spec reads the renamed keys or class; if one does, update its selector or key and nothing else).

- [ ] **Step 6: The glossary**

`CONTEXT.md`, under `### School`, after **Student**:

```markdown
**Slot**:
One time window on one day of a Week Schedule, Open or Unavailable. A Student picks Open Slots, ranked, as Slot Requests. On the Hebrew student form it is a **שיעור אפשרי**; **שיעור** alone means a lesson the Student asks for, never a Slot.
_Avoid_: Hour (שעה), Appointment
```

- [ ] **Step 7: Commit**

```bash
git add client/public/i18n/he.json client/public/i18n/en.json \
  client/src/app/core/translations.spec.ts \
  client/src/app/features/student-form/ui/components/slots-step \
  client/src/app/features/student-form/ui/components/review-step \
  CONTEXT.md
git commit -m "feat(student-form): call a slot שיעור אפשרי and say more slots mean a better chance, without backup framing"
```

---

**Next:** [task-02-more-room-nudge.md](task-02-more-room-nudge.md)
