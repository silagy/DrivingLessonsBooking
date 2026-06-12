# ADR 0001: Monorepo with a Single Deployable

**Status:** Accepted
**Date:** 12 June 2026

## Context

The system has a .NET backend and an Angular frontend. A decision was needed on whether the frontend lives in this repository or its own, and how the two are deployed. The project is built and maintained by a single developer; every feature is a vertical slice touching both the API and the UI; hosting cost must stay low.

## Decision

1. **Monorepo.** The Angular workspace lives in this repository under `client\`, alongside the .NET solution in `src\`.
2. **Single deployable.** The Angular production build is copied into `Presentation.Web\wwwroot` during the Docker image build. ASP.NET Core serves the static files with SPA fallback (`MapFallbackToFile("index.html")`). API routes are prefixed with `/api`.

## Consequences

- One branch, one PR, one commit history per feature — frontend and backend can never drift out of sync
- No CORS configuration; same origin by construction
- One Docker image, one container to deploy, one TLS certificate
- CI builds both stacks; a frontend-only change still produces a full image (acceptable at this scale)
- **Revisit if**: a second frontend appears, separate teams own the stacks, or frontend and backend need independent release cadences
