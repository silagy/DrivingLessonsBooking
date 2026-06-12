# Tech Stack & Implementation Constraints

**Status:** Approved
**Date:** 12 June 2026
**Decisions:** [0001 — Monorepo, single deployable](decisions/0001-monorepo-single-deployable.md) · [0002 — Hosting on AWS Lightsail](decisions/0002-hosting-aws-lightsail.md)

## Backend

- Latest .NET, ASP.NET Core Web API
- EF Core with PostgreSQL (Npgsql)
- Architecture: modular monolith, DDD, Onion — governed by [.claude/rules/ddd-architecture.md](../.claude/rules/ddd-architecture.md)
- API style governed by [.claude/rules/api-guidelines.md](../.claude/rules/api-guidelines.md)
- All API routes live under the `/api` prefix so SPA routes never collide with endpoints

## Frontend

- Latest Angular, signal-based patterns (signals, `input()`/`output()`, no legacy NgModules)
- PrimeNG component library
- Hebrew + English with full RTL support (requirements §8.4)
- Angular workspace lives in `client\` in this repository

## Repository Layout (monorepo)

```
DrivingLessonsBooking\
├── src\          .NET solution (Domain, Application, Infrastructure, Presentation.Web)
├── client\       Angular workspace
├── tests\        .NET test projects
└── docs\         requirements, tech stack, ADRs in docs\decisions\
```

One repository, one deployable. The frontend and backend are versioned, branched, and released together.

## Single-Deployable Model

- `ng build` output is copied into `Presentation.Web\wwwroot` during the Docker image build
- ASP.NET Core serves the static files and falls back to the SPA: `MapFallbackToFile("index.html")`
- No CORS configuration — same origin by construction
- One Docker image contains the entire application

## Background Processing

Requirements §6.4 demands an automatic Excel email at every window close. This is handled by a .NET `BackgroundService` inside the app process that periodically finds open publications past their window end, closes them, and triggers the versioned email. No external scheduler, no cron, no queue.

This constraint is why hosting must be **always-on** — spin-down free tiers and static/serverless splits are ruled out.

## Hosting & Operations

- **AWS Lightsail** instance (2 GB plan, ~$10–12/mo) with a static IP
- **Docker Compose** runs three containers:
  - `app` — the single .NET image (API + Angular static files)
  - `postgres` — PostgreSQL with a named volume
  - `caddy` — reverse proxy terminating HTTPS with automatic Let's Encrypt certificates
- **Email**: AWS SES (effectively free at this volume)
- **Backups**: nightly `pg_dump` cron on the instance, uploaded to S3
- **Timezone handling** per requirements §8.3: containers run in UTC; Asia/Jerusalem is presentation-layer only

## Explicitly Rejected

| Option | Why not |
|---|---|
| Separate frontend repo | Solo developer, vertical-slice features; two PRs per feature with nothing gained |
| Static hosting (S3/CloudFront) + serverless API | The window-close email job needs an always-on process |
| ECS Fargate | Requires an ALB (~$16/mo alone) — triples the budget for zero benefit at this scale |
| RDS PostgreSQL | Starts ~$15/mo; containerized Postgres + nightly S3 dumps is enough for one school's weekly preference lists |
| Hetzner VPS | Cheaper per spec, but a second ecosystem to learn; AWS familiarity wins |
