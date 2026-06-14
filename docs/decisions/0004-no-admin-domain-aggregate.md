# ADR 0004: No Admin Domain Aggregate

**Status:** Accepted
**Date:** 14 June 2026

## Context

US-01 introduced admin sign-in: a single school-owner administrator authenticates with
email and password (requirements §4, Decision #1) and reaches an admin area unreachable
without authentication. There is no registration, no roles, no profile management, and
no second admin in v1. Admin credentials are seeded from configuration (env vars in
Docker), not entered through the application.

This raised the question of where the admin belongs in the architecture: is the
administrator a domain aggregate like `Teacher`, `Student`, or `Publication`?

## Decision

Admin authentication is **infrastructure-level**. There is deliberately **no Admin
domain aggregate**.

- `AdminUser` (`src/DrivingLessons.Infrastructure/Auth/AdminUser.cs`) is a plain
  infrastructure record — `Id`, `Email`, `PasswordHash` — with no behavior.
- Auth mechanics live in Infrastructure: `PasswordVerifier`, `JwtTokenGenerator`,
  `AdminAccountGateway`, `AdminSeeder`.
- The Domain layer stays dependency-free (`Domain → ∅`) and contains no admin or
  identity concept.

### Rationale

1. **No invariants, no lifecycle, no behavior.** Aggregates exist to protect invariants
   and enforce guarded state transitions (contrast `Publication`'s
   `Draft → Published → Open → Closed` with `MustBe*` guards). `AdminUser` protects
   nothing. Modeling it as an aggregate would produce an anemic data bag — the
   anti-pattern DDD warns against.
2. **The admin is an actor, not a domain noun.** The ubiquitous language of this domain
   (requirements §5) is Teacher, Car, WeekSchedule, Slot, Publication, Student,
   Submission, SlotRequest — the things the rules act *on*. The administrator is the
   person *operating* the system, not an object the rules manipulate.
3. **Auth is a generic/supporting subdomain.** Password hashing, JWT issuance, and
   credential verification are security mechanics that depend on frameworks (ASP.NET
   Identity, JWT libraries, EF). Modeling them in Domain would break the cardinal rule
   that Domain depends on nothing.
4. **Credentials are config-sourced.** `AdminSeeder` re-hashes from configuration on
   every boot; configuration is the source of truth. The DB row is a derived cache that
   gives password verification something to check against — not authoritative aggregate
   state with its own lifecycle.
5. **v1 scope.** Exactly one admin and zero admin operations (Decision #1: single login,
   no teacher logins). Aggregates earn their place from operations; there are none here.

## Consequences

- The Domain layer stays clean and framework-free; no ceremony (typed `AdminId`,
  `AdminCreated` event, repository, EF configuration) is created for a behavior-less type.
- Auth is isolated in Infrastructure and can evolve independently of the demand-collection
  domain.
- If the system later grows admin behavior, `AdminUser` would become a `User`/`Account`
  aggregate in a **dedicated Identity/Access bounded context**, separate from the
  demand-collection core — not retrofitted into it.
- **Revisit if:** multiple admins, roles/permissions, admin lifecycle
  (invite/suspend/revoke), audited admin actions as a first-class concept, or multi-school
  tenancy where an Admin belongs to a `School`. Multi-school tenancy is already listed as a
  v2 candidate (requirements §12; see [ADR 0003](0003-roster-csv-and-weekly-link-model.md)).
