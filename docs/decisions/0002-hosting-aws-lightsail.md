# ADR 0002: Hosting on AWS Lightsail

**Status:** Accepted
**Date:** 12 June 2026

## Context

The system must be deployed cheaply (target under $15/month). One requirement constrains the options: at every submission-window close, the Excel file is automatically emailed to the teacher (requirements §6.4). This needs an always-on process — a .NET `BackgroundService` — which rules out spin-down free tiers and static/serverless architectures. The developer works in AWS daily, so AWS familiarity has real operational value.

## Decision

Deploy to an **AWS Lightsail** instance (2 GB plan, ~$10–12/month, static IP) running **Docker Compose**:

- `app` — the single .NET image (API + Angular static files, per [ADR 0001](0001-monorepo-single-deployable.md))
- `postgres` — containerized PostgreSQL with a named volume
- `caddy` — HTTPS termination with automatic Let's Encrypt certificates

Supporting services:

- **AWS SES** for the window-close emails (effectively free at this volume)
- **S3** for nightly `pg_dump` backups via cron on the instance

## Alternatives Considered

| Alternative | Verdict |
|---|---|
| Hetzner VPS | Best price per spec (~€4/mo for 4 GB), but a second provider ecosystem to learn and manage |
| Fly.io / Railway + Neon Postgres | Managed and cheap, but less familiar; free tiers risk sleeping the background job |
| ECS Fargate + RDS | The "proper" AWS path; ALB + RDS alone exceed $30/mo — unjustified for one school's weekly preference lists |

## Consequences

- Predictable fixed monthly cost; no surprise egress billing (Lightsail bundles transfer)
- Self-managed: OS updates, Docker upgrades, and backup verification are on us
- Postgres has no managed failover — acceptable: the blast radius is one teacher's weekly preference list, and nightly S3 dumps bound the loss
- Clear migration path if the system generalizes to multi-school tenancy: the same containers move to ECS, and Postgres data restores into RDS
