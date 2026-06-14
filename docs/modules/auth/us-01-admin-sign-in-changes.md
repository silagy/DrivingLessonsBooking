# US-01: Admin Sign-In — Changes from the Plan

Companion to [`us-01-admin-sign-in-plan.md`](us-01-admin-sign-in-plan.md). Records how the implementation
on branch `2-us-01-admin-signs-in-with-email-and-password` diverged from the plan as written.

## Summary

The plan was followed faithfully for the authentication logic itself — exception filter, login interactor,
auth ports, JWT generation, admin seeding, the Angular auth service/interceptor/guard/routes, and the
Docker setup all match. The meaningful divergences are:

1. An **unplanned Scalar OpenAPI explorer** added to the API (Development-only).
2. A **richer client UI and theme layer** than the plan sketched — custom PrimeNG preset, design tokens,
   two new shared components, and a redesigned login page and admin shell.
3. One **rule-contradicting removal** worth review: `provideAnimationsAsync()` was dropped from `app.config.ts`.
4. Several **out-of-slice documentation commits** that ride along on the branch (roster/national-ID product
   direction, future-slice scaffolding).

The backend code is a near-exact match to the plan; the client diverges only in presentation, not in the
auth flow.

## Backend changes (Tasks 1–6)

| Area | Plan said | Actual | File | Why |
|------|-----------|--------|------|-----|
| OpenAPI explorer | Not mentioned | `AddOpenApi(...)` with a document transformer; Development-only `MapOpenApi()` + `MapScalarApiReference()`, both `.AllowAnonymous()` | [`Program.cs`](../../../src/DrivingLessons.Presentation.Web/Program.cs), new [`OpenApi/BearerSecuritySchemeTransformer.cs`](../../../src/DrivingLessons.Presentation.Web/OpenApi/BearerSecuritySchemeTransformer.cs) | Interactive API explorer for local dev; gated to Development and anonymous so it never ships to production behind auth |
| Packages | JwtBearer + EF Design only | Adds `Microsoft.AspNetCore.OpenApi` (10.0.7) and `Scalar.AspNetCore` (2.16.3) | [`DrivingLessons.Presentation.Web.csproj`](../../../src/DrivingLessons.Presentation.Web/DrivingLessons.Presentation.Web.csproj) | Supports the explorer above |
| Admin seeding query | `SingleOrDefaultAsync` | `FirstOrDefaultAsync` | [`Auth/AdminSeeder.cs`](../../../src/DrivingLessons.Infrastructure/Auth/AdminSeeder.cs) | Defensive: returns the first row rather than throwing if more than one admin row ever exists |
| Exception filter | `(0, string.Empty)` sentinel + `if (statusCode == 0)` guard | Nullable-tuple mapping + `if (mapping is null)` | [`Filters/ApiExceptionFilter.cs`](../../../src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs) | Functionally identical; cleaner pattern |
| JWT generation | `new JsonWebTokenHandler().CreateToken(...)` inline | Extracts `handler` to a local first | [`Auth/JwtTokenGenerator.cs`](../../../src/DrivingLessons.Infrastructure/Auth/JwtTokenGenerator.cs) | Functionally identical; readability |
| Package/tool versions | Resolved by `dotnet add` (unspecified) | Pinned to the .NET 10.0.9 ecosystem; `dotnet-ef` pinned `10.0.9` | `.csproj` files, [`.config/dotnet-tools.json`](../../../.config/dotnet-tools.json) | Reproducible builds |

Everything else in Tasks 1–6 (exception base types, `LoginInteractor` and auth ports, `AdminUser`,
`AdminAccountGateway`, `PasswordVerifier`, options, `AppDbContext`, DI wiring, `AuthController`, config files,
the `InitialCreate` migration) matches the plan.

## Client changes (Tasks 7–9)

The auth flow (`auth.service.ts`, `auth.interceptor.ts`, `auth.guard.ts`, `app.routes.ts`) matches the plan.
The presentation layer was built out well beyond the plan's minimal sketch, in line with the repo's client
rules ([`client-primeng.md`](../../../.claude/rules/client-primeng.md), [`client-i18n.md`](../../../.claude/rules/client-i18n.md)).

| Area | Plan said | Actual | File | Why |
|------|-----------|--------|------|-----|
| PrimeNG theme | Import `Aura` directly | Custom `definePreset(Aura, …)` preset (brand colors, gradients, button styling); `providePrimeNG` gains `options` (`darkModeSelector: false`, `cssLayer`) | [`theme/app-preset.ts`](../../../client/src/app/theme/app-preset.ts), [`app.config.ts`](../../../client/src/app/app.config.ts) | Matches `client-primeng.md`, which mandates a `definePreset` preset and CSS-layer cascade control |
| Design tokens | Inline 4-line `styles.scss` | `styles.scss` switched to `@layer app` imports of new `_tokens.scss` + `_global.scss` | [`styles.scss`](../../../client/src/styles.scss), [`styles/_tokens.scss`](../../../client/src/styles/_tokens.scss), [`styles/_global.scss`](../../../client/src/styles/_global.scss) | `--app-*` token aliases over `--p-*`, per `client-primeng.md` |
| Shared components | None | New `brand-logo` and `language-toggle` components | [`shared/brand-logo/`](../../../client/src/app/shared/brand-logo/), [`shared/language-toggle/`](../../../client/src/app/shared/language-toggle/) | Reused by both the login page and the admin shell |
| Login page | Bare centered card | Header (logo + language toggle), subtitle, password hint, full-width icon submit; SCSS 20→114 lines, token-driven, RTL-aware | [`features/auth/login.component.*`](../../../client/src/app/features/auth/) | Polished, brand-consistent first screen |
| Admin shell | `p-toolbar` with title + buttons | Custom `<header>` with brand logo, user-initials avatar, language toggle, logout; adds `admin-shell.component.scss` and `OnPush` | [`features/admin-shell/`](../../../client/src/app/features/admin-shell/) | Richer shell; lighter component (language handled by the toggle component) |
| Dashboard | Bare `<h2>` + `<p>` | Card-styled `<section>` with token-driven styles, `OnPush` | [`features/admin-shell/dashboard.component.ts`](../../../client/src/app/features/admin-shell/dashboard.component.ts) | Consistent with the themed shell |
| Auth service | token / isAuthenticated / login / logout | Adds an `email` computed that decodes the JWT `email` claim | [`core/auth.service.ts`](../../../client/src/app/core/auth.service.ts) | Feeds the avatar initials in the shell |
| Language service | `toggle()` | Adds `use(lang)` | [`core/language.service.ts`](../../../client/src/app/core/language.service.ts) | Explicit selection for the new language-toggle component |
| i18n keys | `auth` / `shell` / `dashboard` | Adds `brand.appName`, `auth.subtitle`, `auth.passwordHint`, `auth.footnote` (both `en` and `he`, mirrored) | [`public/i18n/en.json`](../../../client/public/i18n/en.json), [`public/i18n/he.json`](../../../client/public/i18n/he.json) | New UI copy; every string remains a translation key |

## Notable / worth review

- **`provideAnimationsAsync()` was removed from `app.config.ts`.** Both the plan (Task 7, Step 9) and
  [`client-primeng.md`](../../../.claude/rules/client-primeng.md) list it in the required PrimeNG setup.
  Without it, PrimeNG components that animate (overlays, messages) fall back to no animation. Decide whether
  this is intentional (bundle/zoneless trade-off) or should be restored.
- **Unknown `/api/*` URLs return `index.html` (200), not 404** — a consequence of the SPA fallback, already
  flagged as an accepted v1 trade-off in the plan's Risks section. No change; restated here for completeness.

## Documentation committed alongside (out of US-01 scope)

These commits sit on the branch but are not part of the sign-in slice. They are bundled because they are
already committed; they will appear in the PR diff.

- **`docs/requirements.md` (v1.0 → v1.1)** and **`docs/designer-prompt.md`** — rewritten for the
  roster-CSV / national-ID identity / single-weekly-link product model (direction for later slices).
- **[`docs/decisions/0003-roster-csv-and-weekly-link-model.md`](../../decisions/0003-roster-csv-and-weekly-link-model.md)** — new ADR capturing that product direction.
- **[`docs/decisions/0004-no-admin-domain-aggregate.md`](../../decisions/0004-no-admin-domain-aggregate.md)** — new ADR justifying US-01's infrastructure-level auth (no Admin aggregate). In-scope rationale for this slice.
- **[`.claude/rules/domain-building-blocks.md`](../../../.claude/rules/domain-building-blocks.md)** (new) plus reference updates to `ddd-architecture.md` and `CLAUDE.md` — base-type scaffolding for future aggregate slices.
- **[`docs/development/running-the-project.md`](../../development/running-the-project.md)** (new) — dev guide; documents the Scalar explorer added above.
