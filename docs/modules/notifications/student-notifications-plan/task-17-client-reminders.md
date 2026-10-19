# Task 17 of 18: Client — "Send reminder" and the scheduled reminders switch

> Part of [Student Notifications (stage 1)](README.md), milestone C. Work on branch `notifications-reminders`. Requires tasks 15 and 16 committed. Read the client rules first.

**Files:**
- Modify: `client\src\app\features\publications\data\publications-api.service.ts` (`requestReminder`, `turnOffScheduledReminders`, `turnOnScheduledReminders`)
- Modify: `client\src\app\features\publications\data\get-publication-notifications.response.ts` (`scheduledRemindersOn` now real)
- Modify: `client\src\app\features\publications\state\publication-notifications.store.ts` (+ the three commands, `canRemind`)
- Create: `client\src\app\features\publications\ui\dialogs\send-reminder\send-reminder.dialog.{ts,html}` (+ spec)
- Modify: `client\src\app\features\publications\ui\components\publication-notifications-panel\…` (button, switch, the reminders chip already from task 13)
- Modify: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.{ts,html}` (opens the dialog)
- Modify: `client\public\i18n\he.json`, `en.json`

**Interfaces:**
- Consumes: `POST api/publications/{id}/reminders` (202; 409 when not Open), `POST …/scheduled-reminders/turn-off|turn-on` (204; 409 when already in that state), `GET …/notifications` (`recipientsWithoutSubmission`, `reminders`, `scheduledRemindersOn`).
- Produces: the Administrator's controls of intent §4.4 and §4.5.

**Behavior:**
- **Send reminder** (`p-button`, `pi pi-bell`, outlined) sits in the panel header. It shows only while the Publication is `Open` (`canRemind = isAdministrator && state === open`). The client renders state; the server still decides (rule 12).
- It opens `SendReminderDialog`: "Send a WhatsApp reminder to the {{count}} students who haven't submitted yet? Students reminded recently, or 3 times already, are left out." A secondary line: "Messages wait until 08:00 during quiet hours." Confirm / Cancel. When `count` is 0 the confirm button is disabled and the text says nobody is missing.
- Confirm → `requestReminder()` → toast "Reminder queued, it goes out within a minute" → the panel reloads after 10 seconds, so the counts move (one `setTimeout` held in the store and cleared on destroy; no polling). A 409 → `toast.apiError` (the Publication closed in the meantime) and a reload.
- **Scheduled reminders**: a `p-toggleswitch` labelled "Automatic reminders at 10:00, 14:00 and 19:00". The times come from a client constant that mirrors the backend default; see Open Item. Changing it calls turn-on or turn-off. A 409 means another Administrator changed it first: reload, and toast `publications.notifications.changedElsewhere`.

- [ ] **Step 1: Failing specs**

`publication-notifications.store.spec.ts`: `requests a reminder and reloads the summary later`, `a 409 on a reminder shows the error and reloads`, `turns scheduled reminders off`, `a 409 on the switch reloads and says it changed elsewhere`, `canRemind only for an administrator on an open publication`.

`send-reminder.dialog.spec.ts`: `shows the count`, `disables confirm at zero`, `closes with true on confirm`.

Panel spec: `shows the button only when canRemind`, `the switch reflects scheduledRemindersOn`.

- [ ] **Step 2: Implement**

Follow `ExtendWindowDialog` for the dialog (DynamicDialog, `NARROW_DIALOG_WIDTH`) and `PublicationsStore.extendWindow` for the mutation shape (`mutating` signal, `try/finally`).

i18n under `publications.notifications.*`: `sendReminder`, `sendReminderTitle`, `sendReminderBody` (with `{{count}}`), `sendReminderNobody`, `sendReminderQuietHours`, `sendReminderConfirm`, `reminderQueued`, `reminderQueuedDetail`, `scheduledReminders`, `scheduledRemindersHint`, `changedElsewhere`. Hebrew first. Check the plural forms against the existing `{{count}}` keys in `he.json`.

- [ ] **Step 3: Run and commit**

```bash
cd client && npx ng test --watch=false && npx ng build && cd ..
git add client/src/app/features/publications client/public/i18n
git commit -m "feat(notifications): send a reminder and switch scheduled reminders from the dashboard"
```

**Open Item:** the check times in the switch label are a client constant. If the school changes `Notifications:Reminders:CheckTimes`, the label goes stale. The fix is to return the times in `GET …/notifications`. Do that here if review prefers it: it's one more field.
