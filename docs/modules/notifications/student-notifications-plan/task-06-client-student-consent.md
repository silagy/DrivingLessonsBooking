# Task 6 of 18: Client — WhatsApp Consent on the Students screen

> Part of [Student Notifications (stage 1)](README.md), milestone B. Work on branch `notifications-whatsapp`. Requires task 5 committed. Read `.claude/rules/client-architecture.md`, `client-state.md`, `client-primeng.md` and `client-i18n.md` first.

**Files:**
- Modify: `client\src\app\features\students\data\item-for-find-students.response.ts`, `get-student.response.ts`, `create-student.request.ts` (`hasWhatsAppConsent`)
- Modify: `client\src\app\features\students\data\students-api.service.ts` (`giveWhatsAppConsent`, `withdrawWhatsAppConsent`)
- Modify: `client\src\app\features\students\domain\student-row.model.ts` and its mapping (`hasWhatsAppConsent`)
- Modify: `client\src\app\features\students\state\students.store.ts` (`giveWhatsAppConsent(student)`, `withdrawWhatsAppConsent(student)`)
- Modify: `client\src\app\features\students\ui\pages\students\student-card-actions.ts` (+ the two commands)
- Modify: `client\src\app\features\students\ui\pages\students\students.page.ts` (wires the commands)
- Create: `client\src\app\features\students\ui\components\whatsapp-consent-flag\whatsapp-consent-flag.component.{ts,html,scss}`
- Modify: `client\src\app\features\students\ui\components\student-card\student-card.component.{ts,html}` (shows the flag)
- Modify: `client\src\app\features\students\ui\dialogs\add-student\add-student.dialog.{ts,html}` (checkbox), `domain\form-values.ts`
- Modify: `client\public\i18n\he.json`, `en.json`
- Test: `student-card-actions.spec.ts` (new or extended), `students.store.spec.ts`, `student-card.component.spec.ts`, `add-student.dialog.spec.ts` if one exists (else the store spec covers the request)

**Interfaces:**
- Consumes (task 5): `POST api/students/{id}/give-whatsapp-consent|withdraw-whatsapp-consent` (204, 404, 409); `hasWhatsAppConsent` on `GET api/students`, `GET api/students/{id}` and `POST api/students`.
- Produces: the Administrator can see and change each Student's WhatsApp Consent. Nothing else in the client depends on it.

**Behavior:**
- The card shows a small **WhatsApp Consent flag** (`pi pi-whatsapp`, tokenized colour, `aria-label` from `students.whatsappConsent.given` / `.missing`) beside the phone, in both states: missing consent is information, not an error.
- The row menu gets **one** consent item, placed after "Change Car" and before the separator: "Give WhatsApp consent" or "Withdraw WhatsApp consent", depending on the state, for active and Inactive Students alike (task 5 Step 3). No confirm dialog. A success toast names the Student (`isolateDirection(name)`, as deactivate does).
- A 409 (already given, or already withdrawn) is the stale-toggle case of `deactivate`: show `toast.apiError` and reload the list.
- The add dialog gets a `p-checkbox` "The student agreed to receive WhatsApp messages about their weekly lesson requests", unticked by default, sent as `hasWhatsAppConsent`. The edit dialog is unchanged: consent goes through the menu so that its event and timestamp stay separate from a details edit.

- [ ] **Step 1: Write the failing specs**

- `student-card-actions.spec.ts`: `offers Give WhatsApp consent to a student without it`, `offers Withdraw to a student with it`, `offers the consent item to an inactive student too`, `keeps Fix car first when the car is not the teacher's`.
- `students.store.spec.ts` (HttpTestingController, as the existing deactivate specs): `gives consent then reloads and toasts`, `withdraw on a 409 shows the API error and reloads`, `create sends hasWhatsAppConsent`.
- `student-card.component.spec.ts`: `shows the consent flag with the given label`, `shows the missing label without consent`.

Run from `client\`: `npx ng test --watch=false`. Expected: the new specs FAIL.

- [ ] **Step 2: Implement**

DTOs and the API service first (`post<void>(…/give-whatsapp-consent, {})`), then the row model mapping, then the store methods (copy `reactivate`'s shape: `mutating` on, `try`, toast, reload, `finally` off), then the menu item and the page commands, then the flag component (standalone, `OnPush`, one `input.required<boolean>()`), then the checkbox (`formControlName="hasWhatsAppConsent"`, `false` default in `form-values.ts`).

i18n keys, Hebrew first:

| Key | he | en |
|-----|----|----|
| `students.actions.giveWhatsAppConsent` | סימון הסכמה לוואטסאפ | Give WhatsApp consent |
| `students.actions.withdrawWhatsAppConsent` | ביטול הסכמה לוואטסאפ | Withdraw WhatsApp consent |
| `students.whatsappConsent.given` | הסכים/ה לקבל הודעות וואטסאפ | Agreed to WhatsApp messages |
| `students.whatsappConsent.missing` | לא הסכים/ה לקבל הודעות וואטסאפ | No WhatsApp consent |
| `students.whatsappConsentGiven` / `…Detail` | נשמרה הסכמה לוואטסאפ / {{name}} יקבל/תקבל הודעות על בקשות השיעורים | WhatsApp consent saved / {{name}} will get messages about lesson requests |
| `students.whatsappConsentWithdrawn` / `…Detail` | ההסכמה לוואטסאפ בוטלה / {{name}} לא יקבל/תקבל עוד הודעות | WhatsApp consent withdrawn / {{name}} won't get messages any more |
| `students.form.whatsappConsent` | התלמיד/ה הסכים/ה לקבל הודעות וואטסאפ על בקשות השיעורים השבועיות | The student agreed to receive WhatsApp messages about their weekly lesson requests |

(Gender-neutral slash forms follow the existing `students.*` keys. Match whatever form those use if it differs.)

- [ ] **Step 3: Run the client checks**

From `client\`: `npx ng test --watch=false` and `npx ng build`. Expected: green. Check the card in Hebrew (RTL: the flag sits on the phone's inline-end) and English, and at 768px, in the running app, as US-95 task 7 did.

- [ ] **Step 4: Commit**

```bash
git add client/src/app/features/students client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(students): give and withdraw WhatsApp consent from the Students screen"
```
