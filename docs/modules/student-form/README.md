# Student-Form Module — Roadmap

Slice roadmap for the `module:student-form` backlog (18 open issues). Each slice ships as its own branch/PR with its own plan folder (`docs\modules\student-form\us-XX-...-plan\`, same format as prior modules), authored **just-in-time** when the slice starts so it absorbs what earlier slices taught. This README records the agreed decomposition and the design decisions locked during planning (16 July 2026), so slice plans don't re-litigate them.

**Prerequisite:** the [roster module](../roster/us-49-roster-plan/README.md) (US-49, [#51](https://github.com/silagy/DrivingLessonsBooking/issues/51)) must ship first — identity and routing (#52/#53) read the `Student` roster.

**UX source of truth:** the committed design mockups (`Driving Lesson Mockup` project — `mock/student.jsx` for all 12 student screens, `mock/shared.jsx` for the mobile shell primitives). The flow is a 5-step wizard at ~375px: national-ID entry → read-only details confirmation → target-count stepper → day-by-day slot-chip picking with a bottom sheet per pick → ranked review, plus window-closed / unknown-ID / validation-error / confirmation screens.

## Slices

| Slice | Content | Issues |
|-------|---------|--------|
| 1. Public link gateway | Anonymous endpoint resolves `ShareableLinkToken` (via the existing `IPublicationRepository.GetByLinkTokenAsync`) → publication state/window/week; public `/s/:token` route outside the auth guard; mobile-first student shell; outside-window read-only message | [#24](https://github.com/silagy/DrivingLessonsBooking/issues/24) |
| 2. Identity & routing | National-ID step (found / welcome-back / unknown states), roster resolves teacher + transmission shown read-only, the assigned teacher's week grid loads | [#52](https://github.com/silagy/DrivingLessonsBooking/issues/52), [#53](https://github.com/silagy/DrivingLessonsBooking/issues/53), [#28](https://github.com/silagy/DrivingLessonsBooking/issues/28) |
| 3. First submission | `Submission` aggregate + `SlotRequest` child; target stepper, slot-chip day list, bottom sheet (Single/Double + per-pick constraint), validation, submit, confirmation. Also replaces the `SubmissionQueries` stub so the admin dashboard shows real counts | [#33](https://github.com/silagy/DrivingLessonsBooking/issues/33), [#34](https://github.com/silagy/DrivingLessonsBooking/issues/34), [#35](https://github.com/silagy/DrivingLessonsBooking/issues/35), [#36](https://github.com/silagy/DrivingLessonsBooking/issues/36), [#37](https://github.com/silagy/DrivingLessonsBooking/issues/37), [#39](https://github.com/silagy/DrivingLessonsBooking/issues/39), [#40](https://github.com/silagy/DrivingLessonsBooking/issues/40), [#41](https://github.com/silagy/DrivingLessonsBooking/issues/41) |
| 4. Reorder | Drag-reorder of the ranked list on the review screen | [#38](https://github.com/silagy/DrivingLessonsBooking/issues/38) |
| 5. Edit & close race | Returning student loads their submission for editing, edits any number of times until close, clean window-closed rejection screen when the window closed between load and submit | [#26](https://github.com/silagy/DrivingLessonsBooking/issues/26), [#43](https://github.com/silagy/DrivingLessonsBooking/issues/43), [#42](https://github.com/silagy/DrivingLessonsBooking/issues/42) |
| 6. Mobile acceptance | Closes last as the ~375px end-to-end acceptance check — mobile-first is built into every slice, not a phase | [#23](https://github.com/silagy/DrivingLessonsBooking/issues/23) |

Slice 1 is the tracer bullet: it proves the two scariest unknowns (an anonymous API surface against the deny-by-default auth policy, and a public mobile route in an admin-only client) with the smallest story attached.

## Locked decisions (resolved via grilling, 16 July 2026)

| # | Decision |
|---|----------|
| 1 | **Stateless student identity**: the link token + national ID travel on every student-form API call. No session, no issued token — matches the accepted-risk posture of requirements §8.2. |
| 2 | **Anonymous API is non-idempotent create/update**: `GET` link context; `POST` identify (national ID → profile + grid + existing submission); `POST` submission (throws if one exists); `PUT` submission (throws if none — full replace). The client knows which to call from the identify response. |
| 3 | **`Submission.Revise(targetCount, picks)`** is a full-replace domain method raising one `SubmissionRevised` event (edit = replace, US-43); granular add/remove stay construction helpers. |
| 4 | **One public route `/s/:token`**, wizard step state held in the feature store (signals). No per-step child routes; refresh restarts at the ID step (the submission is loaded server-side anyway). |
| 5 | **Mobile slot picking is the mocked day-by-day chip list**, not a responsive adaptation of the desktop `WeekGridComponent`. Each pick opens the bottom sheet (session type + optional constraint). |
| 6 | **Visual style**: mockup layout/interactions faithfully, mapped onto the client's existing token system + PrimeNG where natural — no Comply365 palette/font import. |
| 7 | **Slice 3 includes the bottom sheet** (Single/Double + constraint) because it is the core add-pick interaction in the mockup; slice 4 is reorder only. |
| 8 | Extra picks beyond target (#39) fall out of the `picks ≥ target` validation — no separate mechanism. |

## Issue grooming (done at roadmap creation)

- [#25](https://github.com/silagy/DrivingLessonsBooking/issues/25) closed as duplicate of [#52](https://github.com/silagy/DrivingLessonsBooking/issues/52) (both: enter national ID, roster gates access).
- Stale pre-ADR-0003 "email" wording fixed in the titles/bodies of #25, [#41](https://github.com/silagy/DrivingLessonsBooking/issues/41), [#43](https://github.com/silagy/DrivingLessonsBooking/issues/43).
