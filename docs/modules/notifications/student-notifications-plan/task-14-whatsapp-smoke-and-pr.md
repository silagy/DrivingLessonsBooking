# Task 14 of 18: Milestone B smoke with a local fake Cloud API, then the PR

> Part of [Student Notifications (stage 1)](README.md), milestone B. Requires tasks 5–13 committed on `notifications-whatsapp`.

**Files:**
- Nothing, unless a step finds a defect (its own fix commit, failing test first).
- Scratch only: `<scratchpad>/notifications-b/` (the fake, payloads, SQL).

**Interfaces:**
- Consumes: everything of milestone B, plus the task 4 recipe.
- Produces: PR B.

- [ ] **Step 1: Run every suite**

The task 4 Step 1 commands, plus `cd client && npx ng test --watch=false && npx ng build`.

- [ ] **Step 2: The fake Cloud API**

`<scratchpad>/notifications-b/fake-whatsapp.js` (Node, no dependencies) listens on 9099, logs each request's `to`, template name and parameters to `requests.log`, and answers by the last digit of `to`:

| Last digit | Response |
|------------|----------|
| `1` | 400 `{"error":{"code":131026,"message":"Message undeliverable"}}` |
| `2` | 429 for the first 2 requests to that number, then 200 |
| `9` | 200 after a 3-second delay (for the kill in Step 5) |
| other | 200 `{"messages":[{"id":"wamid.fake-<n>"}]}` |

- [ ] **Step 3: Start on a throwaway database and seed**

Database `drivinglessons_notifications_b`. Start the API with `WhatsApp__Enabled=true`, `WhatsApp__ApiBaseUrl=http://localhost:9099`, `WhatsApp__AccessToken=dev`, `WhatsApp__PhoneNumberId=dev`, `WhatsApp__ApiVersion=v0`, quiet hours **moved away from now** (`Notifications__QuietHours__NightFrom`/`NightTo` set so the current time is outside them), and the Outbox Debug logging.

Seed 2 Teachers and 8 Students: 5 with consent and numbers ending 0, 3, 4, 1 and 2; one with consent and `02-6251234`; one without consent; one Inactive. Add a Week Schedule for each Teacher. Publish next week with a window starting in 1 minute.

- [ ] **Step 4: The opening (Review Focus 6, 7)**

After the open, expected in `student_notifications` (`kind = 10`): 7 rows (the Inactive Student has none). 3 are `Sent` (0, 3, 4); the `…1` one is `Failed`, `131026 Message undeliverable`; the `…2` one is `Pending`, then `Sent` after two retries (30 s, 1 min); the landline is `Skipped InvalidNumber`; the one without consent is `Skipped NoConsent`. `requests.log` shows exactly one request for each number except `…2`, which has three. The parameters are in Hebrew, with the right week and end. `GET …/notifications` shows Opening `4 sent · 0 waiting · 1 failed · 2 skipped` and the three problems.

- [ ] **Step 5: A restart in mid-send (Review Focus 2)**

Extend the window. Give 6 more Students numbers ending in `9` (3 seconds each) beforehand. While the window change sends, `kill -9` the API after the second `…9` request is logged, then restart. Expected: every window change row ends `Sent`, and `requests.log` holds at most **one** duplicate (the in-flight one). Record the count.

- [ ] **Step 6: Supersede and quiet hours (Review Focus 8)**

Restart with quiet hours that **contain** now. Extend the window twice, 20 seconds apart. Expected: change 1's rows go `Pending` then `Superseded`; change 2's are `Pending`; both deliveries are held (`next_attempt_at_utc` = the end of quiet hours, `attempts` unchanged). Restart with quiet hours outside now again, and set the held deliveries' `next_attempt_at_utc` to now in SQL to stand in for the morning. Expected: one message per Student, showing the **second** end time.

- [ ] **Step 7: The summary's counting rules**

Compare `GET …/notifications` with hand SQL counts. Window change counts only change 2; problems list the `…1` Student once; the Teachers' emails are `—` (nothing closed yet). Close the window (extend it to 1 minute from now, wait), then expect the Teachers' rows with `גרסה 1` and a time, given Mailpit is running.

- [ ] **Step 8: Disabled is a dry run**

Restart with `WhatsApp__Enabled=false`, and publish another week. Expected: no request in `requests.log`; every consented, valid row `Skipped WhatsAppDisabled`; the log line `WhatsApp disabled. Skipped Opening message.`, with no number in it.

- [ ] **Step 9: The panel in the browser**

Hebrew first, then English, then 768px: the panel under the grid, the chips, the Teachers' list, the problems table and its RTL alignment. A Teacher-role User sees no panel and its request is never made (Network tab).

- [ ] **Step 10: Clean up and open the PR**

Drop the database, stop the fake. PR B, `feat(notifications): WhatsApp opening and window change notifications, with consent`, with the evidence of Steps 4–8 and the screenshots of Step 9. Name the onboarding prerequisites (Open Item 2) as **not** blocking the merge: production runs with `WHATSAPP_ENABLED=false` until the templates are approved.
