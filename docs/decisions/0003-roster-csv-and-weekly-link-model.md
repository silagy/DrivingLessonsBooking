# ADR 0003: Roster CSV, National-ID Identity, and Single Weekly Link

**Status:** Accepted
**Date:** 13 June 2026

## Context

The v1 design (requirements v1.0) assumed students self-identify by email and that each teacher distributes their own per-teacher link. After a conversation with the customer, two assumptions changed:

1. The school already maintains its student list as data (a CSV exported from their existing system, "Berosh"). Each row carries the student's national ID (ת.ז), full name, phone, address, **and the teacher and car already assigned to that student**. The customer wants to upload this file rather than have students register themselves.
2. Because the file already binds every student to a teacher, there is no need to hand out a different link per teacher. The customer wants **one link per week** for the whole school; the student's ID is enough to route them to the right teacher's grid.

These changes ripple through identity, the publication/link model, and the student form. No domain code for Student/Submission/Publication exists yet (only the admin-auth skeleton, US-01), so the cost is documentation and issue grooming, not code rework.

## Decision

- **National ID is the student identifier**, validated against an admin-uploaded roster. There is no self-registration; an ID not in the roster is rejected ("contact your school").
- **The roster is the single source of truth** for every student profile field — full name (single field), phone, assigned teacher, assigned car (hence transmission), address, license type. Re-upload **upserts by national ID**; students absent from a new upload are **deactivated, not deleted**, so historical submissions survive.
- **One Publication per week, school-wide**, generating a **single unguessable link**. The Publication aggregates the per-teacher Week Schedules for that week. The student's roster record selects which teacher's grid they see. Window, extend, and reopen act on the whole week.
- **The student form drops** the email step, the new-student name step, the teacher-confirmation / mismatch-warning / "this is not my teacher" flow, the conditional transmission question, and link-based default-teacher stamping. The teacher and transmission are shown read-only from the roster.
- **Excel stays per teacher** (one file per teacher per publication, emailed to each teacher's contact email, versioned per teacher on each close). The detail-sheet identifier column changes from Email to National ID (with Phone added).

This supersedes decisions #3 (link scope), #6 (email reloads submission), and #10 (conditional transmission question) in requirements §11, and adds decisions #16–#19.

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| Keep email identity; CSV only seeds profiles | Rejected: the customer's natural key is the national ID, and email is not in their data at all |
| Keep per-teacher links, add ID lookup | Rejected: redundant once the roster binds student→teacher; one link is simpler to publish and distribute |
| Allow ad-hoc (unlisted) students to self-enter | Rejected: the roster is authoritative; ad-hoc entry reintroduces the data-quality problem the upload exists to solve |
| Delete students missing from a re-upload | Rejected: would orphan or lose historical submissions; soft-deactivate instead |

## Consequences

- **Simpler student flow:** three screens of teacher/transmission confirmation collapse into one read-only confirmation, lowering friction on the primary (mobile) surface.
- **Setup gains a step:** the admin must upload the roster, and the import needs validation (resolve teacher/car names, reject malformed IDs, report bad rows) and Hebrew CSV handling (UTF-8/BOM, RTL control marks observed in phone fields).
- **Wider security blast radius:** the link is now school-wide and national IDs are semi-guessable (9 digits + check digit). The risk surface grows from one teacher's preference list to the whole school's. The mitigation (the teacher knows his students and reviews the Excel) is weaker at school scale; flagged for revisit if abuse appears. No passwords or verification are added in v1, consistent with the accepted-risk posture in requirements §8.2.
- **Publication cardinality change** is the largest model impact: "Publication" now means per-week-school-wide, aggregating per-teacher Week Schedules. Downstream artifacts (dashboard, history, versioning) become per-teacher views *within* a single weekly publication.
- **Issue backlog grooming:** several student-form user stories are closed as superseded; new stories are added for roster upload, ID identification, and ID-based routing (see requirements §11 and the issue tracker).
