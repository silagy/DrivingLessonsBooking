# Task 6 of 6: Client — mobile shell + status screens, end-to-end verification, PR

> Part of [US-23: Public Link Gateway](README.md). Requires tasks 1–5 complete. Work on branch `24-us-23-public-link-gateway`; client commands run from `client\`.

**Files:**
- Create: `client\src\app\features\student-form\ui\components\student-shell\student-shell.component.ts`, `.html`, `.scss`
- Create: `client\src\app\features\student-form\ui\components\status-message\status-message.component.ts`, `.html`, `.scss`
- Modify (full rewrite of the task-5 skeleton): `client\src\app\features\student-form\ui\pages\student-form\student-form.page.ts`, `.html`, `.scss`
- Modify: `client\src\app\shared\language-toggle\language-toggle.component.ts`, `.scss` (touch-size variant)
- Modify: `docs\modules\student-form\README.md` (link this plan from slice 1)

**Interfaces:**
- Consumes (task 5): `StudentFormStore.view / isOpen / weekParams / opensAt / closesAt / retry()`, `StudentFormView`, `studentForm.*` translation keys; existing `BrandLogoComponent` (`app-brand-logo`) and `LanguageToggleComponent` (`app-language-toggle`).
- Produces:
  - `StudentShellComponent` (`app-student-shell`) — `caption = input<string>('')`, default content projection for the body. Slices 2–5 render every student step inside it.
  - `StatusMessageComponent` (`app-status-message`) — `icon = input.required<string>()` (PrimeIcons classes), `heading = input.required<string>()`, `tone = input<StatusMessageTone>('neutral')` where `StatusMessageTone = 'neutral' | 'success' | 'danger'`; body via content projection. Reused by slice 5's "window just closed" screen (mockup `SDoneErr`).
  - `LanguageToggleComponent.touch = input(false)` — 40px touch targets when `true`.

**Mockup mapping** (`Driving Lesson Mockup\mock\shared.jsx` → `PhoneShell`, `mock\student.jsx` → `SWinClosed`), using app tokens only (roadmap decision 6):

| Mockup | Here |
|--------|------|
| `PhoneShell` header: gradient mark + school name + EN/עב toggle, bottom border | `app-student-shell` bar: `app-brand-logo` + `app-language-toggle [touch]="true"` pushed to the inline end |
| `PhoneShell` sub-bar "Week 25 · Jun 14–19 · Teacher Cohen" (`noSub` on the closed screen) | `caption` input, shown only in the `open` view (teacher name arrives in slice 2) |
| `SWinClosed`: 56px round muted icon, 21px display-font title, 13.5px secondary body, dates in bold | `app-status-message` (icon circle + `h1` + projected body) and a bordered dates card (`dl`) with bold values |
| "Come back through the same link — no need for a new one." | `studentForm.notYetOpen.hint` |
| "After closing, this page shows the same message with the closed date." (annotation) | `closed` view: same layout, lock icon, "Closed" row with the end instant |

The layout is a centered single column capped at `30rem`, so the same page reads correctly on a 375px phone and on a desktop browser.

- [x] **Step 1: Touch-size variant on the shared language toggle**

The student form needs ≥40px touch targets (client-primeng "Responsive Sizing"); the admin bar keeps the compact toggle. This file uses 2-space indentation — keep it.

`client\src\app\shared\language-toggle\language-toggle.component.ts` — full file:

```typescript
import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { AppLanguage, LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-language-toggle',
  imports: [],
  template: `
    <div class="lang" [class.lang--touch]="touch()" role="group">
      @for (option of options; track option.lang) {
        <button
          type="button"
          class="lang__opt"
          [class.lang__opt--active]="language.lang() === option.lang"
          [attr.aria-pressed]="language.lang() === option.lang"
          (click)="language.use(option.lang)"
        >
          {{ option.label }}
        </button>
      }
    </div>
  `,
  styleUrl: './language-toggle.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageToggleComponent {
  protected readonly language = inject(LanguageService);

  readonly touch = input(false);

  protected readonly options: ReadonlyArray<{ lang: AppLanguage; label: string }> = [
    { lang: 'en', label: 'EN' },
    { lang: 'he', label: 'עב' },
  ];
}
```

Append to `client\src\app\shared\language-toggle\language-toggle.component.scss`:

```scss
.lang--touch .lang__opt {
  min-width: 2.75rem;
  min-height: 2.5rem;
}
```

- [x] **Step 2: Student shell (dumb)**

`client\src\app\features\student-form\ui\components\student-shell\student-shell.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { BrandLogoComponent } from '../../../../../shared/brand-logo/brand-logo.component';
import { LanguageToggleComponent } from '../../../../../shared/language-toggle/language-toggle.component';

@Component({
    selector: 'app-student-shell',
    imports: [BrandLogoComponent, LanguageToggleComponent],
    templateUrl: './student-shell.component.html',
    styleUrl: './student-shell.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentShellComponent {
    readonly caption = input<string>('');
}
```

`student-shell.component.html`:

```html
<div class="student-shell">
    <header class="student-shell__bar">
        <app-brand-logo />
        <app-language-toggle class="student-shell__lang" [touch]="true" />
    </header>

    @if (caption()) {
        <div class="student-shell__caption">{{ caption() }}</div>
    }

    <main class="student-shell__main">
        <ng-content />
    </main>
</div>
```

`student-shell.component.scss`:

```scss
:host {
    display: block;
    min-height: 100dvh;
    background: var(--app-bg-page);
}

.student-shell {
    display: flex;
    flex-direction: column;
    max-width: 30rem;
    min-height: 100dvh;
    margin-inline: auto;
    background: var(--app-bg-card);
}

.student-shell__bar {
    display: flex;
    align-items: center;
    gap: 0.625rem;
    padding: 0.875rem 1rem 0.75rem;
    border-block-end: 1px solid var(--app-border);
}

.student-shell__lang {
    margin-inline-start: auto;
}

.student-shell__caption {
    padding: 0.45rem 1rem;
    background: var(--app-bg-page);
    border-block-end: 1px solid var(--app-border);
    font-size: 0.72rem;
    color: var(--app-text-secondary);
}

.student-shell__main {
    display: flex;
    flex: 1;
    flex-direction: column;
}

@media screen and (min-width: 30rem) {
    .student-shell {
        border-inline: 1px solid var(--app-border);
    }
}
```

- [x] **Step 3: Status message (dumb)**

`client\src\app\features\student-form\ui\components\status-message\status-message.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type StatusMessageTone = 'neutral' | 'success' | 'danger';

@Component({
    selector: 'app-status-message',
    templateUrl: './status-message.component.html',
    styleUrl: './status-message.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusMessageComponent {
    readonly icon = input.required<string>();
    readonly heading = input.required<string>();
    readonly tone = input<StatusMessageTone>('neutral');

    protected readonly toneClass = computed(() => `status__icon--${this.tone()}`);
}
```

The input is `heading`, not `title`, so it never collides with the native `title` tooltip attribute.

`status-message.component.html`:

```html
<section class="status">
    <span class="status__icon" [class]="toneClass()" aria-hidden="true">
        <i [class]="icon()"></i>
    </span>
    <h1 class="status__heading">{{ heading() }}</h1>
    <div class="status__body">
        <ng-content />
    </div>
</section>
```

(`[class]` merges with the static `class` attribute, so `status__icon` is kept.)

`status-message.component.scss`:

```scss
:host {
    display: block;
}

.status {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.625rem;
    padding: 4.5rem 1rem 2rem;
    text-align: center;
}

.status__icon {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 3.5rem;
    height: 3.5rem;
    border: 1px solid var(--app-border);
    border-radius: 999px;
    background: var(--app-bg-muted);
    color: var(--app-text-secondary);

    i {
        font-size: 1.4rem;
    }
}

.status__icon--success {
    border-color: transparent;
    background-image: var(--app-grad-sky);
    color: var(--app-on-accent);
}

.status__icon--danger {
    border-color: var(--p-red-200);
    background: var(--p-red-50);
    color: var(--p-red-600);
}

.status__heading {
    margin: 0.5rem 0 0;
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 1.3rem;
    line-height: 1.25;
    letter-spacing: -0.005em;
    color: var(--app-ink);
}

.status__body {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    width: 100%;
    max-width: 20rem;
    font-size: 0.85rem;
    line-height: 1.55;
    color: var(--app-text-secondary);
}
```

- [x] **Step 4: Student form page (smart) — full rewrite of the task-5 skeleton**

`student-form.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { StudentFormView } from '../../../domain/student-form-view.enum';
import { StudentFormStore } from '../../../state/student-form.store';
import { StatusMessageComponent } from '../../components/status-message/status-message.component';
import { StudentShellComponent } from '../../components/student-shell/student-shell.component';

@Component({
    selector: 'app-student-form-page',
    imports: [TranslocoPipe, ButtonModule, ProgressSpinnerModule, StatusMessageComponent, StudentShellComponent],
    templateUrl: './student-form.page.html',
    styleUrl: './student-form.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormPage {
    protected readonly store = inject(StudentFormStore);
    protected readonly views = StudentFormView;

    readonly token = input.required<string>();

    constructor() {
        effect(() => this.store.open(this.token()));
    }
}
```

`student-form.page.html` — no `<form>`, no inputs in any of these views (the acceptance criterion "no form is rendered"):

```html
<app-student-shell [caption]="store.isOpen() ? ('studentForm.weekCaption' | transloco: store.weekParams()) : ''">
    @switch (store.view()) {
        @case (views.loading) {
            <div class="student-form__loading">
                <p-progressSpinner [ariaLabel]="'studentForm.loading' | transloco" />
            </div>
        }
        @case (views.notYetOpen) {
            <app-status-message icon="pi pi-clock" [heading]="'studentForm.notYetOpen.title' | transloco">
                <p class="student-form__text">{{ 'studentForm.notYetOpen.body' | transloco: store.weekParams() }}</p>
                <dl class="student-form__window">
                    <div class="student-form__window-row">
                        <dt>{{ 'studentForm.window.opens' | transloco }}</dt>
                        <dd>{{ store.opensAt() }}</dd>
                    </div>
                    <div class="student-form__window-row">
                        <dt>{{ 'studentForm.window.closes' | transloco }}</dt>
                        <dd>{{ store.closesAt() }}</dd>
                    </div>
                </dl>
                <p class="student-form__hint">{{ 'studentForm.notYetOpen.hint' | transloco }}</p>
            </app-status-message>
        }
        @case (views.open) {
            <app-status-message icon="pi pi-check" tone="success" [heading]="'studentForm.open.title' | transloco">
                <p class="student-form__text">{{ 'studentForm.open.body' | transloco: store.weekParams() }}</p>
                <dl class="student-form__window">
                    <div class="student-form__window-row">
                        <dt>{{ 'studentForm.window.closes' | transloco }}</dt>
                        <dd>{{ store.closesAt() }}</dd>
                    </div>
                </dl>
            </app-status-message>
        }
        @case (views.closed) {
            <app-status-message icon="pi pi-lock" [heading]="'studentForm.closed.title' | transloco">
                <p class="student-form__text">{{ 'studentForm.closed.body' | transloco: store.weekParams() }}</p>
                <dl class="student-form__window">
                    <div class="student-form__window-row">
                        <dt>{{ 'studentForm.window.closed' | transloco }}</dt>
                        <dd>{{ store.closesAt() }}</dd>
                    </div>
                </dl>
                <p class="student-form__hint">{{ 'studentForm.closed.hint' | transloco }}</p>
            </app-status-message>
        }
        @case (views.invalidLink) {
            <app-status-message icon="pi pi-link" tone="danger" [heading]="'studentForm.invalidLink.title' | transloco">
                <p class="student-form__text">{{ 'studentForm.invalidLink.body' | transloco }}</p>
            </app-status-message>
        }
        @case (views.loadFailed) {
            <app-status-message
                icon="pi pi-exclamation-circle"
                tone="danger"
                [heading]="'studentForm.loadFailed.title' | transloco">
                <p class="student-form__text">{{ 'studentForm.loadFailed.body' | transloco }}</p>
                <p-button
                    [label]="'studentForm.loadFailed.retry' | transloco"
                    icon="pi pi-refresh"
                    (onClick)="store.retry()" />
            </app-status-message>
        }
    }
</app-student-shell>
```

`student-form.page.scss` (projected `dl`/`p` nodes belong to this template, so these encapsulated styles apply to them):

```scss
.student-form__loading {
    display: flex;
    justify-content: center;
    padding-block: 6rem;
}

.student-form__text,
.student-form__hint {
    margin: 0;
}

.student-form__hint {
    font-size: 0.78rem;
    color: var(--app-text-muted);
}

.student-form__window {
    width: 100%;
    margin: 0.25rem 0 0;
    padding: 0.25rem 1rem;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    box-shadow: var(--app-shadow-card);

    dt {
        margin: 0;
        font-size: 0.78rem;
        color: var(--app-text-secondary);
    }

    dd {
        margin: 0;
        font-family: var(--app-font-display);
        font-weight: 700;
        font-size: 0.875rem;
        color: var(--app-ink);
        text-align: end;
    }
}

.student-form__window-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding-block: 0.75rem;
}

.student-form__window-row + .student-form__window-row {
    border-block-start: 1px solid var(--app-bg-muted);
}
```

- [x] **Step 5: Build, test, commit**

Run (in `client\`): `npm test -- --watch=false` → all PASS; `npm run build` → success, no new warnings.

```bash
git add client/src/app/features/student-form client/src/app/shared/language-toggle
git commit -m "feat(client): mobile student shell and window status screens

Not-yet-open, open, closed, invalid-link and load-failed screens per the
SWinClosed mockup, with Asia/Jerusalem window dates and 40px touch targets."
```

- [x] **Step 6: Seed an Open → Closed publication**

Reuse the variables from task 3 Step 4 (`API`, `TOKEN`, `TEACHER_ID`; re-run its login line if the shell is new). Publish a second unused week with a window that started 5 minutes ago and ends in 3 minutes — Quartz fires the past-due open job immediately, then closes it on time. Also create a third week that stays **Draft**:

```bash
WEEK2=2026-11-22
WEEK3=2026-11-29
START=$(date -u -d '-5 minutes' +%Y-%m-%dT%H:%M:%SZ)
END=$(date -u -d '+3 minutes' +%Y-%m-%dT%H:%M:%SZ)

for W in $WEEK2 $WEEK3; do
  curl -s -o /dev/null -w "week $W schedule: %{http_code}\n" -X POST $API/api/week-schedules \
    -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
    -d "{\"teacherId\":\"$TEACHER_ID\",\"weekStart\":\"$W\"}"
done

PUB2=$(curl -s "$API/api/publications/by-week?week=$WEEK2" -H "Authorization: Bearer $TOKEN")
PUB2_ID=$(echo "$PUB2" | sed -E 's/.*"id":"([^"]+)".*/\1/')
LINK2=$(echo "$PUB2" | sed -E 's/.*"linkToken":"([^"]+)".*/\1/')
LINK3=$(curl -s "$API/api/publications/by-week?week=$WEEK3" -H "Authorization: Bearer $TOKEN" | sed -E 's/.*"linkToken":"([^"]+)".*/\1/')

curl -s -o /dev/null -w "publish week 2: %{http_code}\n" -X POST $API/api/publications/$PUB2_ID/publish \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d "{\"startUtc\":\"$START\",\"endUtc\":\"$END\"}"

sleep 5
curl -s $API/api/submissions/by-link/$LINK2; echo
echo "LINK (published)=$LINK  LINK2 (open, closes $END)=$LINK2  LINK3 (draft)=$LINK3"
```

Expected: two `201`s, `publish week 2: 204`, and the by-link JSON for `LINK2` shows `"state":"open"`. You have ~3 minutes to check the open screen before it closes.

- [x] **Step 7: Verify in the browser — every state, both languages, 375px**

With Postgres, API and client running (`http://localhost:4200`), set the Browser pane to the **mobile** preset (375×812) and reload. Verify with `get_page_text`, `read_page`, `find`, and `javascript_tool` (project memory: `screenshot` can hang on this PrimeNG app — try one at the end as proof, fall back to the text/DOM evidence if it times out). Start **logged out** (`localStorage.removeItem('auth_token')`) and in **Hebrew** (the default).

1. **Open** — `/s/{LINK2}` (do this first, while it is open): gradient check icon, `ההגשה פתוחה`, caption bar `שבוע 48 · …` (Sun 22 Nov 2026 → ISO week 48), body with the week range, one "סגירה" row showing the end time in Jerusalem time.
2. **Not yet open** — `/s/{LINK}` (task 3): clock icon, `ההגשה עוד לא נפתחה`, body `…לשבוע 47 (…)…`, dates card with **פתיחה** = Wednesday 11 Nov **18:00** and **סגירה** = Friday 13 Nov **14:00** (winter, UTC+2 — the same wall-clock times as the mockup), the come-back hint, **no caption bar**. `find` for `textbox` / `form` returns nothing — the acceptance criterion "no form is rendered".
3. **Closed** — after `END` passes, reload `/s/{LINK2}`: lock icon, `ההגשה נסגרה`, "נסגר" row with the end instant, reopen hint, no caption bar.
4. **Draft** — `/s/{LINK3}`: red link icon, `הקישור אינו תקין` — never the week or dates — **Review Focus 1**.
5. **Unknown / truncated** — `/s/NoSuchTokenAbc123` and `/s/{first 10 characters of LINK}`: invalid-link screen, URL unchanged, no toast, no redirect to `/login` — **Review Focus 2**.
6. **Load failed** — stop the API (`preview_stop` the `api` server), reload `/s/{LINK}` → `משהו השתבש` + `נסו שוב` button; start the API again, click the button → the not-yet-open screen loads without a page reload.
7. **Stale admin session** — `localStorage.setItem('auth_token', 'not-a-real-jwt')`, reload `/s/{LINK}` → still the student page — **Review Focus 4**. Remove the key.
8. **Hebrew/RTL mechanics** (`javascript_tool`) — **Review Focus 5**:
   - `document.documentElement.dir` → `"rtl"`; the brand sits at the inline-start (right) edge and the toggle at the inline-end (left) edge.
   - `document.documentElement.scrollWidth <= window.innerWidth` → `true` (no horizontal scroll at 375px).
   - `[...document.querySelectorAll('.lang__opt')].map(b => b.getBoundingClientRect().height)` → every value ≥ 40.
   - In the dates card, labels sit at the inline start and bold values at the inline end (mirrored vs English).
9. **English** — click `EN` → `dir` becomes `"ltr"`, every string switches (`Submissions aren't open yet`, `Opens` / `Closes`), and the dates re-render in English ("Wednesday, Nov 11, 18:00" or similar) **without reloading** the page. Repeat checks 1–5 in English (at least 2 and 4).
10. **Desktop** — reset the viewport (`resize_window` preset `desktop`): the shell is a centered 30rem column with side borders on the page background; nothing stretches edge to edge.
11. **Admin unaffected** — sign in, open Dashboard/Publications: the admin toggle is still the compact size, and the share-link box still shows `{origin}/s/{token}`.

Reset the viewport to `desktop` when done.

- [x] **Step 8: Link this plan from the roadmap**

In `docs\modules\student-form\README.md`, change the slice-1 table row's first cell from `1. Public link gateway` to:

```markdown
1. Public link gateway — [plan](us-23-public-link-gateway-plan/README.md)
```

- [x] **Step 9: Full check + final commit**

```bash
dotnet build
dotnet test
```

Run (in `client\`): `npm test -- --watch=false` and `npm run build`.
Expected: everything PASSES / builds clean.

```bash
git add docs/modules/student-form/README.md
git commit -m "docs(student-form): link slice 1 plan from the roadmap"
```

- [ ] **Step 10: Push and open the PR**

```bash
git push -u origin 24-us-23-public-link-gateway
gh pr create --title "US-23: Student link gateway with out-of-window message" --body "Closes #24

## Summary
- Anonymous \`GET api/submissions/by-link/{token}\` returns week, week number, state and window for a published/open/closed publication; draft and unknown links 404 without echoing the token
- Public \`/s/:token\` route outside the admin guard, with a per-visit signals store and a mobile-first student shell
- Not-yet-open / open / closed / invalid-link / load-failed screens per the SWinClosed mockup, dates in Asia/Jerusalem, verified in Hebrew (RTL) and English at 375px
- \`WeekStart.WeekNumber\` (ISO week of the Monday) now drives both the student page and the teacher's Excel email subject

## Heads-up
The email subject's week number moves forward by one for every week (it used the Sunday's ISO week). Example: the week of 14 Jun 2026 was \"Week 24\" and is now \"Week 25\", matching the requirements example and the mockup.

## Test plan
- [x] Domain: week-number cases incl. 53-week year and year boundary
- [x] Application: link interactor (found, not found, token not echoed)
- [x] Client: view mapping, Sunday–Friday range, Jerusalem time across DST (Vitest)
- [x] API smoke: draft 404, unknown 404, stale bearer 200, admin endpoints still 401
- [x] Browser: every view in HE + EN at 375px, desktop layout, admin unaffected

🤖 Generated with [Claude Code](https://claude.com/claude-code)"
```

---

## Self-Review (done at planning time)

- **Spec coverage:** US-23 AC → Published-not-open shows the dates card and no form (Step 7.2); Closed shows the closed date and no form (Step 7.3). Roadmap slice 1 scope → anonymous endpoint (task 3), public `/s/:token` route outside the guard (task 5), mobile-first student shell (this task), out-of-window message (this task). Requirements §8.3 → Jerusalem display (task 4 specs), §8.4 → both languages + RTL (Step 7.8–7.9), §10 unguessable links + mobile → token-only URL, 375px checks.
- **Type consistency:** `GetPublicationByLinkResponse` fields match between C# (task 2) and TS (task 4); `StudentFormStore` signal names in task 5 match every template reference here (`view`, `isOpen`, `weekParams`, `opensAt`, `closesAt`, `retry`); translation keys used here are exactly the ones added in task 5 Step 6.
- **Judgment calls to confirm while implementing:** `p-progressSpinner`'s `ariaLabel` input and `p-button`'s `(onClick)` are the PrimeNG 21 names used elsewhere in the app — if the installed version differs, existing usages win. If Quartz does not fire the past-due open job within a few seconds in Step 6, check `PublicationReconciliationHostedService`/scheduler logs before assuming a UI bug.
