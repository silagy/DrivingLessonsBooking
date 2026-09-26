# Task 4 of 6: Client — student-form domain + data layer (TDD, Vitest)

> Part of [US-23: Public Link Gateway](README.md). Requires task 3 complete (the API contract). Work on branch `24-us-23-public-link-gateway`; client commands run from `client\`.

**Files:**
- Create: `client\src\app\features\student-form\domain\student-form-view.enum.ts`
- Create: `client\src\app\features\student-form\domain\student-form-view.ts` + `student-form-view.spec.ts`
- Create: `client\src\app\features\student-form\domain\week-label.ts` + `week-label.spec.ts`
- Create: `client\src\app\features\student-form\domain\jerusalem-time.ts` + `jerusalem-time.spec.ts`
- Create: `client\src\app\features\student-form\data\get-publication-by-link.response.ts`
- Create: `client\src\app\features\student-form\data\submissions-api.service.ts`

**Interfaces:**
- Consumes: `GET api/submissions/by-link/{token}` JSON (task 3); existing `PublicationState` (`client\src\app\shared\models\publication-state.enum.ts`).
- Produces (task 5 imports these exact names):
  - `enum StudentFormView { loading, invalidLink, loadFailed, notYetOpen, open, closed }` (string values = member names)
  - `viewForPublicationState(state: PublicationState): StudentFormView`
  - `weekDates(weekStart: string): { start: Date; end: Date }` and `weekRangeLabel(weekStart: string, locale: string): string`
  - `formatWindowInstant(utcIso: string, locale: string): string`
  - `interface GetPublicationByLinkResponse { weekStart: string; weekNumber: number; state: PublicationState; windowStartUtc: string; windowEndUtc: string; }`
  - `SubmissionsApiService.getPublicationByLink(token: string): Observable<GetPublicationByLinkResponse>`

`domain\` holds pure functions only (no Angular imports) — that is what makes them unit-testable with plain Vitest. Specs use Vitest globals (`describe`/`it`/`expect`, enabled by `tsconfig.spec.json` → `"types": ["vitest/globals"]`), same as `app.spec.ts`. The date helpers are feature-local on purpose (README decision 9 — `roster` set the precedent); **do not** import from `features\publications\`.

- [x] **Step 1: Write the failing specs**

`client\src\app\features\student-form\domain\student-form-view.spec.ts`:

```typescript
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { viewForPublicationState } from './student-form-view';
import { StudentFormView } from './student-form-view.enum';

describe('viewForPublicationState', () => {
    it.each([
        [PublicationState.published, StudentFormView.notYetOpen],
        [PublicationState.open, StudentFormView.open],
        [PublicationState.closed, StudentFormView.closed],
        [PublicationState.draft, StudentFormView.invalidLink],
    ])('maps %s to %s', (state, view) => {
        expect(viewForPublicationState(state)).toBe(view);
    });
});
```

`client\src\app\features\student-form\domain\week-label.spec.ts`:

```typescript
import { weekDates, weekRangeLabel } from './week-label';

describe('weekDates', () => {
    it('spans Sunday through Friday of the week', () => {
        const { start, end } = weekDates('2026-06-14');

        expect([start.getFullYear(), start.getMonth(), start.getDate()]).toEqual([2026, 5, 14]);
        expect([end.getFullYear(), end.getMonth(), end.getDate()]).toEqual([2026, 5, 19]);
    });

    it('crosses a month boundary', () => {
        const { end } = weekDates('2026-05-31');

        expect([end.getMonth(), end.getDate()]).toEqual([5, 5]);
    });

    it('crosses a year boundary', () => {
        const { end } = weekDates('2026-12-27');

        expect([end.getFullYear(), end.getMonth(), end.getDate()]).toEqual([2027, 0, 1]);
    });
});

describe('weekRangeLabel', () => {
    it('labels the first and last grid day', () => {
        const label = weekRangeLabel('2026-06-14', 'en');

        expect(label).toContain('14');
        expect(label).toContain('19');
    });
});
```

`client\src\app\features\student-form\domain\jerusalem-time.spec.ts` — **Review Focus 3**: the window must show Israeli wall-clock time whatever the device's timezone, on both sides of DST:

```typescript
import { formatWindowInstant } from './jerusalem-time';

describe('formatWindowInstant', () => {
    it('renders a summer instant in Israel daylight time (UTC+3)', () => {
        expect(formatWindowInstant('2026-06-12T11:00:00Z', 'en')).toContain('14:00');
    });

    it('renders a winter instant in Israel standard time (UTC+2)', () => {
        expect(formatWindowInstant('2026-01-09T12:00:00Z', 'en')).toContain('14:00');
    });

    it('uses a 24-hour clock', () => {
        expect(formatWindowInstant('2026-06-12T16:30:00Z', 'en')).toContain('19:30');
    });
});
```

Assertions check the time and day parts only — full-string equality would be brittle across ICU versions.

- [x] **Step 2: Run specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: the three new spec files FAIL to import (`./student-form-view`, `./week-label`, `./jerusalem-time` do not exist); `app.spec.ts` still passes.

- [x] **Step 3: View enum + mapping**

`client\src\app\features\student-form\domain\student-form-view.enum.ts`:

```typescript
export enum StudentFormView {
    loading = 'loading',
    invalidLink = 'invalidLink',
    loadFailed = 'loadFailed',
    notYetOpen = 'notYetOpen',
    open = 'open',
    closed = 'closed',
}
```

`client\src\app\features\student-form\domain\student-form-view.ts`:

```typescript
import { PublicationState } from '../../../shared/models/publication-state.enum';
import { StudentFormView } from './student-form-view.enum';

const VIEW_BY_STATE: Record<PublicationState, StudentFormView> = {
    [PublicationState.draft]: StudentFormView.invalidLink,
    [PublicationState.published]: StudentFormView.notYetOpen,
    [PublicationState.open]: StudentFormView.open,
    [PublicationState.closed]: StudentFormView.closed,
};

export function viewForPublicationState(state: PublicationState): StudentFormView {
    return VIEW_BY_STATE[state];
}
```

This renders state; it decides nothing (the backend already excludes drafts — the `draft` row is exhaustiveness, enforced by `Record<PublicationState, ...>`).

- [x] **Step 4: Week label helpers**

`client\src\app\features\student-form\domain\week-label.ts`:

```typescript
const GRID_LAST_DAY_OFFSET = 5;

export interface WeekDates {
    start: Date;
    end: Date;
}

export function weekDates(weekStart: string): WeekDates {
    const [year, month, day] = weekStart.split('-').map(Number);
    const start = new Date(year, month - 1, day);
    const end = new Date(year, month - 1, day + GRID_LAST_DAY_OFFSET);

    return { start, end };
}

export function weekRangeLabel(weekStart: string, locale: string): string {
    const { start, end } = weekDates(weekStart);
    const formatter = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' });

    return formatter.formatRange(start, end);
}
```

`weekStart` is a calendar date (`DateOnly` on the server), so it is built from its parts in local time — never `new Date('2026-06-14')`, which parses as UTC midnight and shows the previous day west of Greenwich. `formatRange` gives locale-correct ranges ("Jun 14 – 19", "14–19 ביוני") and is in the ES2022 lib the workspace targets.

- [x] **Step 5: Jerusalem instant formatter**

`client\src\app\features\student-form\domain\jerusalem-time.ts`:

```typescript
const JERUSALEM_TIME_ZONE = 'Asia/Jerusalem';

export function formatWindowInstant(utcIso: string, locale: string): string {
    const formatter = new Intl.DateTimeFormat(locale, {
        timeZone: JERUSALEM_TIME_ZONE,
        weekday: 'long',
        day: 'numeric',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit',
        hourCycle: 'h23',
    });

    return formatter.format(new Date(utcIso));
}
```

Mockup wording is "Wednesday, Jun 10 at 18:00" — weekday + date + 24h time, matching this format.

- [x] **Step 6: Run specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: all specs PASS (4 + 4 + 3 new cases, plus `app.spec.ts`).

- [x] **Step 7: Response DTO + API service**

Named identically to the backend DTO (client-architecture rule 7). `DateOnly` arrives as `"yyyy-MM-dd"`, instants as ISO-8601 strings, the enum as a camelCase string.

`client\src\app\features\student-form\data\get-publication-by-link.response.ts`:

```typescript
import { PublicationState } from '../../../shared/models/publication-state.enum';

export interface GetPublicationByLinkResponse {
    weekStart: string;
    weekNumber: number;
    state: PublicationState;
    windowStartUtc: string;
    windowEndUtc: string;
}
```

`client\src\app\features\student-form\data\submissions-api.service.ts` — one API service per backend controller pair (`SubmissionQueryController` now, `SubmissionCommandController` in later slices):

```typescript
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetPublicationByLinkResponse } from './get-publication-by-link.response';

@Injectable({ providedIn: 'root' })
export class SubmissionsApiService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = 'api/submissions';

    getPublicationByLink(token: string): Observable<GetPublicationByLinkResponse> {
        const encodedToken = encodeURIComponent(token);

        return this.http.get<GetPublicationByLinkResponse>(`${this.baseUrl}/by-link/${encodedToken}`);
    }
}
```

Tokens are base64url and URL-safe already; encoding guards against a hand-edited link carrying `/` or `?` into a different route.

- [x] **Step 8: Build + commit**

Run (in `client\`): `npm run build` — success (the new files compile under `strictTemplates`/strict TS even though nothing imports the service yet).

```bash
git add client/src/app/features/student-form
git commit -m "feat(client): student-form domain helpers and link api service

View mapping from publication state, Sunday-Friday week label, and
Asia/Jerusalem window formatting, each unit-tested with Vitest."
```

---

**Next:** [task-05-client-route-store.md](task-05-client-route-store.md)
