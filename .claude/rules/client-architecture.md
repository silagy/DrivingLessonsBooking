---
paths:
  - "client/**"
---

# Client Architecture Guide

Rules for the Angular workspace in `client\`. The client is one of two halves of a single deployable ([ADR 0001](../../docs/decisions/0001-monorepo-single-deployable.md)): the production build is copied into `Presentation.Web\wwwroot` and served by ASP.NET Core with SPA fallback. The backend is governed by its own rules (`ddd-architecture.md`, `api-guidelines.md`) — **the client consumes the API contract those rules define; it never invents endpoint shapes**.

**Stack**: latest Angular, standalone components only, signals (see `client-state.md`), PrimeNG (see `client-primeng.md`), Hebrew + English RTL-first (see `client-i18n.md`).

## Critical Rules

1. **No NgModules** — standalone components, functional guards, functional interceptors, `provide*` functions only
2. **All API calls target `/api/...`** — relative URLs, same origin by construction, no CORS, no environment-specific domains
3. **Client routes must never collide with `/api`** — the SPA fallback owns everything else
4. **Use requirements terminology** — Teacher, Car, WeekSchedule, Slot, Publication, Student, Submission, SlotRequest (`docs/requirements.md` §5). Never invent alternative names
5. **Two surfaces, two form factors** — admin features are desktop-first; the student form is mobile-first (~375px, WhatsApp-distributed links)
6. **No comments** — code must be self-documenting, same as the backend rule
7. **No `any`** — every API request/response has a typed interface mirroring the backend DTO
8. **Always search for existing code** before creating new files

## Workspace Layout

```
client\
└── src\
    ├── app\
    │   ├── app.config.ts              providers: router, http + interceptors, PrimeNG theme, i18n, initializers
    │   ├── app.routes.ts              root routes — lazy loadChildren per feature
    │   ├── app.component.ts           root shell: router-outlet + toast outlet
    │   │
    │   ├── core\                      app-wide singletons — never feature-specific
    │   │   ├── auth\                  admin auth service, authGuard, auth interceptor
    │   │   ├── http\                  problem-details.ts, apiErrorInterceptor
    │   │   ├── layout\                admin shell: topbar, navigation
    │   │   └── services\              language.service.ts, jerusalem-date.service.ts, toast.service.ts
    │   │
    │   ├── features\                  one folder per bounded context, lazy-loaded
    │   │   ├── teachers\              admin: teachers & cars setup
    │   │   ├── week-schedules\        admin: weekly grid preparation, slot toggling
    │   │   ├── publications\          admin: publish, dashboard, history, Excel download
    │   │   └── student-form\          student: anonymous submission flow (by link token)
    │   │
    │   └── shared\                    feature-agnostic only — never imports from features\
    │       ├── components\            week-grid, status-tag, empty-state, ...
    │       ├── models\                cross-feature models (problem-details, paging)
    │       ├── pipes\
    │       └── config\
    │           └── app-routes.ts      AppRoutes constants
    │
    ├── assets\
    │   └── i18n\                      en.json, he.json
    └── styles\                        theming + global SCSS (see client-primeng.md)
```

### Feature Internal Layout

Every feature uses the same four layers; dependency direction is `ui → state → data → domain`:

```
features\publications\
├── publications.routes.ts             default-exported Routes
├── domain\
│   ├── publication.model.ts           client-side model
│   └── publication-state.enum.ts      mirrors backend enum values
├── data\
│   ├── publications-api.service.ts    HTTP only — one service per backend controller pair
│   ├── get-publication.response.ts    typed DTO interfaces, named after backend DTOs
│   └── create-publication.request.ts
├── state\
│   └── publications.store.ts          signal store (see client-state.md)
└── ui\
    ├── pages\
    │   ├── publications-dashboard\    smart — routed
    │   └── publications-history\
    ├── components\                    dumb, feature-specific
    └── dialogs\
        └── publish-week-dialog\
```

| Layer | Contains | Must NOT contain |
|-------|----------|------------------|
| `domain\` | models, enums, pure functions | Angular imports, HTTP |
| `data\` | API services, request/response interfaces | state, toasts, navigation |
| `state\` | signal stores | HTTP calls outside injected API services, DOM access |
| `ui\` | pages, components, dialogs | direct `HttpClient` or API service injection |

### File Location Quick Reference

| Code Type | Path Pattern | Name |
|-----------|--------------|------|
| Model | `features\{feature}\domain\{entity}.model.ts` | `Publication` |
| Enum | `features\{feature}\domain\{name}.enum.ts` | `PublicationState` |
| API service | `features\{feature}\data\{entities}-api.service.ts` | `PublicationsApiService` |
| Response interface | `features\{feature}\data\{op}-{entity}.response.ts` | `GetPublicationResponse` |
| Request interface | `features\{feature}\data\{op}-{entity}.request.ts` | `CreatePublicationRequest` |
| Signal store | `features\{feature}\state\{entities}.store.ts` | `PublicationsStore` |
| Page (smart) | `features\{feature}\ui\pages\{name}\{name}.page.ts` | `PublicationsDashboardPage` |
| Component (dumb) | `features\{feature}\ui\components\{name}\{name}.component.ts` | `SlotCountCellComponent` |
| Shared component | `shared\components\{name}\{name}.component.ts` | `WeekGridComponent` |
| Dialog | `features\{feature}\ui\dialogs\{name}\{name}.dialog.ts` | `PublishWeekDialog` |
| Guard | `core\auth\{name}.guard.ts` | `authGuard` |
| Interceptor | `core\http\{name}.interceptor.ts` | `apiErrorInterceptor` |
| Route constants | `shared\config\app-routes.ts` | `AppRoutes` |

### Where to Put Code?

```
Display formatting           → dumb component or pipe
Feature state + orchestration → features\{feature}\state\{entities}.store.ts
HTTP call                    → features\{feature}\data\{entities}-api.service.ts
Cross-feature UI             → shared\components\
App-wide singleton           → core\
Business rule                → the BACKEND domain — the client renders, it does not decide
```

The client never re-implements domain rules (state transitions, validation beyond UX hints). The backend throws, the client displays. Client-side validation exists only to give immediate feedback and always duplicates — never replaces — a backend rule.

## API Consumption

The contract is defined by `api-guidelines.md`. Consume it exactly:

| Backend shape | Client usage |
|---------------|--------------|
| `POST api/publications` | create — expect 201 + response body |
| `GET api/publications/{id}` | get — expect 200 or 404 |
| `GET api/publications/find?teacherId=...` | search — expect 200 + array |
| `POST api/publications/{id}/publish` | business action — expect 204, 404, or 409 |
| `GET api/submissions/by-link/{token}` | anonymous student access — token is the capability |

### API Service Template

One API service per backend controller pair (`{Entity}CommandController` + `{Entity}QueryController`). HTTP only — no state, no toasts, no mapping logic beyond typing:

```typescript
@Injectable({ providedIn: 'root' })
export class PublicationsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/publications';

    getPublication(id: string): Observable<GetPublicationResponse> {
        return this.http.get<GetPublicationResponse>(`${this.baseUrl}/${id}`);
    }

    findPublications(teacherId: string): Observable<ItemForFindPublicationsResponse[]> {
        const params = { teacherId };
        return this.http.get<ItemForFindPublicationsResponse[]>(`${this.baseUrl}/find`, { params });
    }

    createPublication(request: CreatePublicationRequest): Observable<CreatePublicationResponse> {
        return this.http.post<CreatePublicationResponse>(this.baseUrl, request);
    }

    publishPublication(id: string): Observable<void> {
        return this.http.post<void>(`${this.baseUrl}/${id}/publish`, null);
    }
}
```

### DTO Interfaces

- Named **identically to the backend DTO classes**: `GetPublicationResponse`, `CreatePublicationRequest`, `ItemForFindPublicationsResponse` — searching either codebase by name finds both halves
- Properties camelCase (System.Text.Json default)
- Enums arrive as **camelCase strings** — model them as string enums with camelCase values:

```typescript
export enum PublicationState {
    draft = 'draft',
    published = 'published',
    open = 'open',
    closed = 'closed',
}
```

- DateTimes arrive as **UTC ISO 8601 strings** — typed as `string` in DTOs, converted to Asia/Jerusalem only at display (see `client-i18n.md`)

### Error Handling — ProblemDetails

The backend's `DomainExceptionFilter` maps everything to `ProblemDetails`. One functional interceptor + per-call handling:

| Status | Meaning | Client behavior |
|--------|---------|-----------------|
| 400 | validation / model binding | form-level handling at the call site |
| 401 | not authenticated | interceptor redirects to admin login |
| 404 | `{Entity}NotFoundException` | call site decides: error state or navigation |
| 409 | `DomainException` — business rule violated | show the rule to the user (toast or inline); **never retry** |
| 500 | unexpected | interceptor shows generic error toast |

```typescript
export interface ProblemDetails {
    status: number;
    title: string;
}
```

409 means the user's view was stale (e.g., publishing an already-published Publication) — surface a translated message and refresh the affected state. There is **no ETag/If-Match handling** — the backend deliberately omits optimistic locking.

## Routing

- Route constants in `shared\config\app-routes.ts` — never inline path strings:

```typescript
export const AppRoutes = {
    login: 'login',
    teachers: 'teachers',
    weekSchedules: 'week-schedules',
    publications: 'publications',
    studentForm: 's/:token',
    unauthorized: 'unauthorized',
} as const;
```

- Features are lazy: `loadChildren: () => import('./features/publications/publications.routes')`
- Feature routes files **default-export** a `Routes` array
- Admin routes are guarded by `authGuard` (functional, `inject()`); the student form route is anonymous — the unguessable link token is the access control, mirroring the backend's `[AllowAnonymous]` posture
- The student route is short (`/s/{token}`) because it travels through WhatsApp
- Enable `withComponentInputBinding()` — route params bind to page `input()`s

## TypeScript Style

- Follows the spirit of `code-style.md`: self-documenting code, no comments, early returns, no magic values (named constants), meaningful names without abbreviations
- 4-space indent, single quotes, semicolons, max 120 chars
- `inject()` over constructor injection in all new code
- Strict mode on; `noImplicitAny`; template strict mode (`strictTemplates`)
- Prefer `readonly` on every injected dependency and signal field

## Anti-Patterns

- **Never** import `HttpClient` or an API service in a component — components talk to stores only
- **Never** put business rules in the client — render state, don't compute transitions
- **Never** create an NgModule, class-based guard, or class-based interceptor
- **Never** hardcode `/api` URLs outside `data\` services or absolute domains anywhere
- **Never** name a client DTO differently from its backend counterpart
- **Never** let `shared\` or `core\` import from `features\`, or one feature import from another
- **Never** expose sequential ids in student-facing URLs — link tokens only
- **Never** add retry logic on 409 responses — the rule violation will not go away
