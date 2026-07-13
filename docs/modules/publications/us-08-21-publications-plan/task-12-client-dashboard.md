# Task 12 of 14: Client — dashboard page, dumb components, dialogs

> Part of [US-08–21: Publications Module](README.md) ([parent plan](../us-08-21-publications-plan.md)). Requires tasks 1–11 complete (the `PublicationsStore`, `publications.routes.ts`, nav links, the `publications` i18n namespace, `shared/models/publication-state.enum.ts`, the `core/services/file-download.service.ts` + `clipboard.service.ts`, and the `features/publications/domain/jerusalem-time.ts` helper — `jerusalemWallTimeToUtcIso` / `formatInstantInJerusalem` — all land in tasks 10–11). Work on branch `9-us-08-21-publications-module`, commands from `client\` unless noted.

This task builds the admin dashboard UI on top of the store. The committed mockups (`Driving Lesson Mockup\mock\admin.jsx` → `AdminPublish`, `AdminDashboard`, `AdminClosed`, `DeskGrid mode="counts"`, `StatChip`, `LifeStep`) are the UX source of truth. The dashboard reuses the shared `WeekGridComponent` with a projected count cell; state variants render via `@switch`; window instants are converted to Asia/Jerusalem for display and to UTC for the API. This commit also carries task 11's store/routing/i18n (a store with no page consumer is dead code — see the plan's commit column).

**Files:**
- Create: `client\src\app\shared\components\publication-state-tag\publication-state-tag.component.ts`
- Create: `client\src\app\features\publications\ui\components\slot-count-cell\slot-count-cell.component.ts`, `.html`, `.scss`
- Create: `client\src\app\features\publications\ui\components\publication-stat-chip\publication-stat-chip.component.ts`, `.html`, `.scss`
- Create: `client\src\app\features\publications\ui\components\share-link-box\share-link-box.component.ts`, `.html`, `.scss`
- Create: `client\src\app\features\publications\ui\dialogs\dialog-form.scss`
- Create: `client\src\app\features\publications\ui\dialogs\publish-week\publish-week.dialog.ts`, `.html`
- Create: `client\src\app\features\publications\ui\dialogs\extend-window\extend-window.dialog.ts`, `.html`
- Create: `client\src\app\features\publications\ui\dialogs\reopen-window\reopen-window.dialog.ts`, `.html`
- Create: `client\src\app\features\publications\ui\pages\publications-dashboard\publications-dashboard.page.ts`, `.html`, `.scss`
- Modify: `client\public\i18n\en.json`, `client\public\i18n\he.json` (add the dashboard/dialog keys to the `publications` namespace)

---

- [ ] **Step 1: Shared `publication-state-tag` component**

A tiny shared dumb component wrapping `p-tag`, keyed on `PublicationState`. Severity map: `draft → secondary`, `published → info`, `open → success`, `closed → danger`. Value is the translated `publications.state.{state}` key. Inline template — it is a one-liner.

`shared\components\publication-state-tag\publication-state-tag.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TagModule } from 'primeng/tag';
import { PublicationState } from '../../models/publication-state.enum';

type TagSeverity = 'secondary' | 'info' | 'success' | 'danger';

const STATE_SEVERITY: Record<PublicationState, TagSeverity> = {
    [PublicationState.draft]: 'secondary',
    [PublicationState.published]: 'info',
    [PublicationState.open]: 'success',
    [PublicationState.closed]: 'danger',
};

@Component({
    selector: 'app-publication-state-tag',
    imports: [TagModule, TranslocoPipe],
    template: `<p-tag [severity]="severity()" [value]="'publications.state.' + state() | transloco" />`,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationStateTagComponent {
    readonly state = input.required<PublicationState>();

    protected readonly severity = computed<TagSeverity>(() => STATE_SEVERITY[this.state()]);
}
```

> Confirm the enum values against `shared\models\publication-state.enum.ts` (task 11): `draft`/`published`/`open`/`closed`. If `TagModule` is exported as `Tag` in the installed PrimeNG version, import `{ Tag } from 'primeng/tag'` — check an existing usage first; the repo currently imports the `*Module` barrels (e.g. `ButtonModule`, `SelectModule`), so `TagModule` matches convention.

- [ ] **Step 2: `slot-count-cell` dumb component**

Renders one dashboard grid cell: the request count with a "requests" caption (mockup `DeskGrid mode="counts"`), or a blocked/striped cell for an `Unavailable` slot (`--app-slot-unavailable`). Narrow inputs only — never the whole DTO blob.

`features\publications\ui\components\slot-count-cell\slot-count-cell.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { SlotState } from '../../../../../shared/models/slot-state.enum';

@Component({
    selector: 'app-slot-count-cell',
    imports: [TranslocoPipe],
    templateUrl: './slot-count-cell.component.html',
    styleUrl: './slot-count-cell.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotCountCellComponent {
    readonly requestCount = input.required<number>();
    readonly slotState = input.required<SlotState>();

    protected readonly isUnavailable = computed(() => this.slotState() === SlotState.unavailable);
    protected readonly isEmpty = computed(() => this.requestCount() === 0);
}
```

> `SlotState` moves to `shared\models\slot-state.enum.ts` in task 10 (decision #12). Import it from there, not from `features\week-schedules` (cross-feature import is forbidden).

`features\publications\ui\components\slot-count-cell\slot-count-cell.component.html`:

```html
@if (isUnavailable()) {
    <div class="slot-count slot-count--blocked">
        <span class="slot-count__blocked-label">{{ 'weekGrid.legend.unavailable' | transloco }}</span>
    </div>
} @else {
    <div class="slot-count">
        <span class="slot-count__value" [class.slot-count__value--empty]="isEmpty()">
            <bdi>{{ requestCount() }}</bdi>
        </span>
        <span class="slot-count__caption">{{ 'publications.dashboard.requests' | transloco }}</span>
    </div>
}
```

`features\publications\ui\components\slot-count-cell\slot-count-cell.component.scss` (logical properties, tokens only):

```scss
.slot-count {
    flex: 1;
    min-height: 62px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 1px;
    border-radius: 6px;
    border: 1px solid var(--app-border);
    background: var(--app-bg-card);
    box-shadow: var(--app-shadow-card);
}

.slot-count--blocked {
    background: var(--app-slot-unavailable);
    box-shadow: none;
}

.slot-count__value {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 1.3rem;
    color: var(--app-ink);
    font-variant-numeric: tabular-nums;
}

.slot-count__value--empty {
    color: var(--app-text-muted);
}

.slot-count__caption {
    font-size: 0.6rem;
    letter-spacing: 0.04em;
    text-transform: uppercase;
    color: var(--app-text-muted);
}

.slot-count__blocked-label {
    font-size: 0.7rem;
    font-weight: 500;
    color: var(--app-text-secondary);
}
```

- [ ] **Step 3: `publication-stat-chip` dumb component**

The mockup `StatChip` — a value + label pill for the dashboard stat row (students submitted / total picks / last submission).

`features\publications\ui\components\publication-stat-chip\publication-stat-chip.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'app-publication-stat-chip',
    templateUrl: './publication-stat-chip.component.html',
    styleUrl: './publication-stat-chip.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationStatChipComponent {
    readonly value = input.required<string>();
    readonly label = input.required<string>();
}
```

`features\publications\ui\components\publication-stat-chip\publication-stat-chip.component.html`:

```html
<div class="stat-chip">
    <span class="stat-chip__value"><bdi>{{ value() }}</bdi></span>
    <span class="stat-chip__label">{{ label() }}</span>
</div>
```

> The parent translates the label and formats the value (numbers via `<bdi>`, the last-submission instant via `formatInstantInJerusalem`) before passing it in — the chip stays a pure display component.

`features\publications\ui\components\publication-stat-chip\publication-stat-chip.component.scss`:

```scss
.stat-chip {
    display: flex;
    align-items: baseline;
    gap: 8px;
    padding: 10px 18px;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: 10px;
    box-shadow: var(--app-shadow-card);
}

.stat-chip__value {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 1.5rem;
    color: var(--app-ink);
    font-variant-numeric: tabular-nums;
}

.stat-chip__label {
    font-size: 0.78rem;
    color: var(--app-text-secondary);
}
```

- [ ] **Step 4: `share-link-box` dumb component**

The shareable-link row from `AdminPublish`: the link rendered LTR inside `<bdi>` (it must not reorder under RTL) plus a copy `p-button`. It emits `copyClicked` — the parent decides how to copy (dashboard → store; publish dialog → clipboard service). It never injects a store or service.

`features\publications\ui\components\share-link-box\share-link-box.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';

@Component({
    selector: 'app-share-link-box',
    imports: [ButtonModule, TranslocoPipe],
    templateUrl: './share-link-box.component.html',
    styleUrl: './share-link-box.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShareLinkBoxComponent {
    readonly link = input.required<string>();

    readonly copyClicked = output<void>();
}
```

`features\publications\ui\components\share-link-box\share-link-box.component.html`:

```html
<div class="share-link">
    <div class="share-link__title">{{ 'publications.shareLink.title' | transloco }}</div>
    <div class="share-link__row">
        <div class="share-link__value" dir="ltr"><bdi>{{ link() }}</bdi></div>
        <p-button
            [label]="'publications.shareLink.copy' | transloco"
            size="small"
            (onClick)="copyClicked.emit()" />
    </div>
    <div class="share-link__hint">{{ 'publications.shareLink.hint' | transloco }}</div>
</div>
```

`features\publications\ui\components\share-link-box\share-link-box.component.scss`:

```scss
.share-link {
    border: 1px solid var(--app-border);
    border-radius: 10px;
    padding: 14px 16px;
    background: var(--app-bg-muted);
}

.share-link__title {
    font-family: var(--app-font-display);
    font-weight: 700;
    font-size: 0.78rem;
    color: var(--app-ink);
}

.share-link__row {
    display: flex;
    gap: 10px;
    align-items: center;
    margin-block-start: 8px;
}

.share-link__value {
    flex: 1;
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: 6px;
    padding: 9px 12px;
    font-size: 0.82rem;
    color: var(--app-ink);
    font-variant-numeric: tabular-nums;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.share-link__hint {
    font-size: 0.72rem;
    color: var(--app-text-muted);
    margin-block-start: 8px;
}
```

- [ ] **Step 5: Shared dialog form styles**

Mirror the teachers-feature `dialog-form.scss` so the three dialogs share one stylesheet.

`features\publications\ui\dialogs\dialog-form.scss`:

```scss
.dialog-form {
    display: flex;
    flex-direction: column;
    gap: 1.125rem;
    padding-block-start: 0.25rem;
}

.dialog-form__grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
}

.field {
    display: flex;
    flex-direction: column;
    gap: 0.4rem;

    label {
        font-family: var(--app-font-display);
        font-weight: 700;
        font-size: 0.8rem;
        color: var(--app-ink);
    }
}

.dialog-form__hint {
    color: var(--app-text-muted);
    font-size: 0.72rem;
}

.dialog-form__actions {
    display: flex;
    justify-content: flex-end;
    gap: 0.625rem;
    margin-block-start: 0.5rem;
}
```

- [ ] **Step 6: `publish-week` dialog**

Two `p-datepicker` fields (`showTime`) in a reactive form, a read-only `share-link-box`, and a static lifecycle stepper (Draft → Published → Open → Closed). Closes with `{ startUtc, endUtc }` as UTC ISO strings via `jerusalemWallTimeToUtcIso`. The dialog is structurally dumb (data in via `DynamicDialogConfig`, result out via `ref.close`); it injects `ClipboardService`/`ToastService` only for the copy affordance (neither is a store, API service, or router).

`features\publications\ui\dialogs\publish-week\publish-week.dialog.ts`:

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ClipboardService } from '../../../../../core/services/clipboard.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { jerusalemWallTimeToUtcIso } from '../../../domain/jerusalem-time';
import { ShareLinkBoxComponent } from '../../components/share-link-box/share-link-box.component';

export interface PublishWeekDialogData {
    weekLabel: string;
    link: string;
}

export interface PublishWeekResult {
    startUtc: string;
    endUtc: string;
}

const LIFECYCLE_STEPS = ['draft', 'published', 'open', 'closed'] as const;

@Component({
    selector: 'app-publish-week-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, DatePickerModule, ShareLinkBoxComponent],
    templateUrl: './publish-week.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublishWeekDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);
    private readonly config = inject(DynamicDialogConfig<PublishWeekDialogData>);
    private readonly clipboard = inject(ClipboardService);
    private readonly toast = inject(ToastService);

    protected readonly weekLabel = this.config.data?.weekLabel ?? '';
    protected readonly link = this.config.data?.link ?? '';
    protected readonly steps = LIFECYCLE_STEPS;

    protected readonly form = this.fb.nonNullable.group({
        start: this.fb.nonNullable.control<Date | null>(null, Validators.required),
        end: this.fb.nonNullable.control<Date | null>(null, Validators.required),
    });

    protected async copyLink(): Promise<void> {
        const copied = await this.clipboard.copy(this.link);
        this.toast.success(copied ? 'publications.shareLink.copied' : 'publications.shareLink.copyFailed');
    }

    protected submit(): void {
        const { start, end } = this.form.getRawValue();

        if (this.form.invalid || !start || !end) {
            return;
        }

        const result: PublishWeekResult = {
            startUtc: jerusalemWallTimeToUtcIso(start),
            endUtc: jerusalemWallTimeToUtcIso(end),
        };

        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

`features\publications\ui\dialogs\publish-week\publish-week.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
    <p class="dialog-form__hint">
        {{ 'publications.publishDialog.subtitle' | transloco: { week: weekLabel } }}
    </p>

    <div class="dialog-form__grid">
        <div class="field">
            <label for="publish-start">{{ 'publications.publishDialog.opensAt' | transloco }}</label>
            <p-datepicker
                inputId="publish-start"
                formControlName="start"
                [showTime]="true"
                [showIcon]="true"
                hourFormat="24"
                dateFormat="D, M d" />
        </div>
        <div class="field">
            <label for="publish-end">{{ 'publications.publishDialog.closesAt' | transloco }}</label>
            <p-datepicker
                inputId="publish-end"
                formControlName="end"
                [showTime]="true"
                [showIcon]="true"
                hourFormat="24"
                dateFormat="D, M d" />
        </div>
    </div>
    <small class="dialog-form__hint">{{ 'publications.timezoneNote' | transloco }}</small>

    <app-share-link-box [link]="link" (copyClicked)="copyLink()" />

    <ol class="lifecycle">
        @for (step of steps; track step; let first = $first) {
            <li class="lifecycle__step" [class.lifecycle__step--current]="step === 'published'">
                <span class="lifecycle__dot"></span>
                <span class="lifecycle__label">{{ 'publications.state.' + step | transloco }}</span>
            </li>
        }
    </ol>

    <div class="dialog-form__actions">
        <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
        <p-button [label]="'publications.publish' | transloco" type="submit" [disabled]="form.invalid" />
    </div>
</form>

<style>
    .lifecycle {
        display: flex;
        justify-content: space-between;
        list-style: none;
        margin: 0;
        padding-block-start: 6px;
        padding-inline: 0;
    }

    .lifecycle__step {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 6px;
        flex: 1;
    }

    .lifecycle__dot {
        width: 16px;
        height: 16px;
        border-radius: 999px;
        border: 3px solid var(--app-border);
    }

    .lifecycle__step--current .lifecycle__dot {
        border: none;
        background: var(--app-grad-sky);
    }

    .lifecycle__label {
        font-family: var(--app-font-display);
        font-size: 0.72rem;
        color: var(--app-text-secondary);
    }

    .lifecycle__step--current .lifecycle__label {
        color: var(--app-ink);
        font-weight: 700;
    }
</style>
```

> The scoped `<style>` block is acceptable inside a component template here for the one-off stepper; if the project prefers a dedicated `.scss`, move it to `publish-week.dialog.scss` and set `styleUrls`. Verify `p-datepicker` is the current selector for `DatePickerModule` (PrimeNG renamed Calendar → DatePicker); `hourFormat="24"` gives 24-hour time entry.

- [ ] **Step 7: `extend-window` + `reopen-window` dialogs**

Each has one `p-datepicker` for the new end and closes with `{ newEndUtc }`. Same reactive-form + `jerusalemWallTimeToUtcIso` shape.

`features\publications\ui\dialogs\extend-window\extend-window.dialog.ts`:

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { jerusalemWallTimeToUtcIso } from '../../../domain/jerusalem-time';

export interface WindowEndResult {
    newEndUtc: string;
}

@Component({
    selector: 'app-extend-window-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, DatePickerModule],
    templateUrl: './extend-window.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExtendWindowDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);

    protected readonly form = this.fb.nonNullable.group({
        end: this.fb.nonNullable.control<Date | null>(null, Validators.required),
    });

    protected submit(): void {
        const { end } = this.form.getRawValue();

        if (this.form.invalid || !end) {
            return;
        }

        const result: WindowEndResult = { newEndUtc: jerusalemWallTimeToUtcIso(end) };
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

`features\publications\ui\dialogs\extend-window\extend-window.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
    <div class="field">
        <label for="extend-end">{{ 'publications.extendDialog.newEnd' | transloco }}</label>
        <p-datepicker
            inputId="extend-end"
            formControlName="end"
            [showTime]="true"
            [showIcon]="true"
            hourFormat="24"
            dateFormat="D, M d" />
    </div>
    <small class="dialog-form__hint">{{ 'publications.timezoneNote' | transloco }}</small>

    <div class="dialog-form__actions">
        <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
        <p-button [label]="'publications.extendWindow' | transloco" type="submit" [disabled]="form.invalid" />
    </div>
</form>
```

`features\publications\ui\dialogs\reopen-window\reopen-window.dialog.ts` (identical shape; distinct selector, class, and submit label):

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DynamicDialogRef } from 'primeng/dynamicdialog';
import { jerusalemWallTimeToUtcIso } from '../../../domain/jerusalem-time';
import { WindowEndResult } from '../extend-window/extend-window.dialog';

@Component({
    selector: 'app-reopen-window-dialog',
    imports: [ReactiveFormsModule, TranslocoPipe, ButtonModule, DatePickerModule],
    templateUrl: './reopen-window.dialog.html',
    styleUrl: '../dialog-form.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReopenWindowDialog {
    private readonly fb = inject(FormBuilder);
    private readonly ref = inject(DynamicDialogRef);

    protected readonly form = this.fb.nonNullable.group({
        end: this.fb.nonNullable.control<Date | null>(null, Validators.required),
    });

    protected submit(): void {
        const { end } = this.form.getRawValue();

        if (this.form.invalid || !end) {
            return;
        }

        const result: WindowEndResult = { newEndUtc: jerusalemWallTimeToUtcIso(end) };
        this.ref.close(result);
    }

    protected cancel(): void {
        this.ref.close();
    }
}
```

`features\publications\ui\dialogs\reopen-window\reopen-window.dialog.html`:

```html
<form [formGroup]="form" (ngSubmit)="submit()" class="dialog-form">
    <p class="dialog-form__hint">{{ 'publications.reopenDialog.subtitle' | transloco }}</p>

    <div class="field">
        <label for="reopen-end">{{ 'publications.reopenDialog.newEnd' | transloco }}</label>
        <p-datepicker
            inputId="reopen-end"
            formControlName="end"
            [showTime]="true"
            [showIcon]="true"
            hourFormat="24"
            dateFormat="D, M d" />
    </div>
    <small class="dialog-form__hint">{{ 'publications.timezoneNote' | transloco }}</small>

    <div class="dialog-form__actions">
        <p-button [label]="'general.cancel' | transloco" severity="secondary" type="button" text (onClick)="cancel()" />
        <p-button [label]="'publications.reopenWindow' | transloco" type="submit" [disabled]="form.invalid" />
    </div>
</form>
```

- [ ] **Step 8: Dashboard page (smart)**

Route-bound via `withComponentInputBinding()` (enabled in task 11): query params `teacherId`, `week`, and `publish` bind to `input()`s; effects sync the store selection and auto-open the publish dialog when `publish=1` arrives (the weekly-prep entry point in task 13 routes here with that flag). The body `@switch`es over `store.state()`; the grid reuses `WeekGridComponent` with a projected `slot-count-cell`; header buttons per state open dialogs whose results call store methods. Window instants render via `formatInstantInJerusalem` wrapped in `<bdi>`.

`features\publications\ui\pages\publications-dashboard\publications-dashboard.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, effect, inject, input } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { DialogService } from 'primeng/dynamicdialog';
import { WeekGridComponent } from '../../../../../shared/components/week-grid/week-grid.component';
import { PublicationStateTagComponent } from '../../../../../shared/components/publication-state-tag/publication-state-tag.component';
import { PublicationState } from '../../../../../shared/models/publication-state.enum';
import { formatInstantInJerusalem } from '../../../domain/jerusalem-time';
import { LanguageService } from '../../../../../core/language.service';
import { PublicationsStore } from '../../../state/publications.store';
import { SlotCountCellComponent } from '../../components/slot-count-cell/slot-count-cell.component';
import { PublicationStatChipComponent } from '../../components/publication-stat-chip/publication-stat-chip.component';
import { ShareLinkBoxComponent } from '../../components/share-link-box/share-link-box.component';
import { PublishWeekDialog, PublishWeekResult } from '../../dialogs/publish-week/publish-week.dialog';
import { ExtendWindowDialog, WindowEndResult } from '../../dialogs/extend-window/extend-window.dialog';
import { ReopenWindowDialog } from '../../dialogs/reopen-window/reopen-window.dialog';

const PUBLISH_FLAG = '1';
const DIALOG_WIDTH = '35rem';
const NARROW_DIALOG_WIDTH = '28rem';

@Component({
    selector: 'app-publications-dashboard-page',
    imports: [
        FormsModule,
        TranslocoPipe,
        ButtonModule,
        SelectModule,
        ProgressSpinnerModule,
        WeekGridComponent,
        PublicationStateTagComponent,
        SlotCountCellComponent,
        PublicationStatChipComponent,
        ShareLinkBoxComponent,
    ],
    providers: [DialogService],
    templateUrl: './publications-dashboard.page.html',
    styleUrl: './publications-dashboard.page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationsDashboardPage {
    protected readonly store = inject(PublicationsStore);
    private readonly dialogs = inject(DialogService);
    private readonly transloco = inject(TranslocoService);
    private readonly language = inject(LanguageService);

    protected readonly PublicationState = PublicationState;

    readonly teacherId = input<string>();
    readonly week = input<string>();
    readonly publish = input<string>();

    protected readonly windowLabel = computed(() => {
        const dashboard = this.store.dashboard();

        return dashboard?.windowEndUtc ? this.formatInstant(dashboard.windowEndUtc) : '';
    });

    constructor() {
        effect(() => {
            const teacherId = this.teacherId();

            if (teacherId) {
                this.store.selectTeacher(teacherId);
            }
        });

        effect(() => {
            const week = this.week();

            if (week) {
                this.store.selectWeek(week);
            }
        });

        effect(() => {
            if (this.publish() === PUBLISH_FLAG && this.store.state() === PublicationState.draft) {
                this.onPublish();
            }
        });
    }

    protected formatInstant(utcIso: string): string {
        return formatInstantInJerusalem(utcIso, this.language.lang());
    }

    protected onPublish(): void {
        const publication = this.store.publication();

        if (!publication) {
            return;
        }

        const ref = this.dialogs.open(PublishWeekDialog, {
            header: this.transloco.translate('publications.publish'),
            width: DIALOG_WIDTH,
            modal: true,
            dismissableMask: true,
            data: { weekLabel: this.store.weekLabel(), link: this.store.shareLink() },
        });

        ref?.onClose.subscribe((result?: PublishWeekResult) => {
            if (result) {
                void this.store.publish(result);
            }
        });
    }

    protected onExtend(): void {
        const ref = this.dialogs.open(ExtendWindowDialog, {
            header: this.transloco.translate('publications.extendWindow'),
            width: NARROW_DIALOG_WIDTH,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: WindowEndResult) => {
            if (result) {
                void this.store.extendWindow(result);
            }
        });
    }

    protected onReopen(): void {
        const ref = this.dialogs.open(ReopenWindowDialog, {
            header: this.transloco.translate('publications.reopenWindow'),
            width: NARROW_DIALOG_WIDTH,
            modal: true,
            dismissableMask: true,
        });

        ref?.onClose.subscribe((result?: WindowEndResult) => {
            if (result) {
                void this.store.reopen(result);
            }
        });
    }
}
```

> Confirm the exact `PublicationsStore` surface from task 11 before wiring: `selectTeacher`, `selectWeek`, `publication()`, `state()`, `dashboard()`, `isLoading()`, `loadError()`, `publish(result)`, `extendWindow(result)`, `reopen(result)`, `copyLink()`, `downloadExcel()`, `refresh()`, plus display helpers `weekLabel()`, `shareLink()`, `dataAsOf()`. If the store names differ, follow the store (it is the committed source). The three `effect()`s only push route state into the store — they never write another signal (see `client-state.md`).

`features\publications\ui\pages\publications-dashboard\publications-dashboard.page.html`:

```html
<div class="dashboard">
    <header class="dashboard__header">
        <div class="dashboard__heading">
            <div class="dashboard__eyebrow">{{ store.weekLabel() }}</div>
            <div class="dashboard__title-row">
                <p-select
                    [options]="store.teachers()"
                    optionLabel="name"
                    optionValue="id"
                    [ngModel]="store.selectedTeacherId()"
                    (ngModelChange)="store.selectTeacher($event)"
                    [placeholder]="'publications.selectTeacher' | transloco" />
                @if (store.state(); as state) {
                    <app-publication-state-tag [state]="state" />
                }
            </div>
        </div>

        <div class="dashboard__actions">
            @switch (store.state()) {
                @case (PublicationState.draft) {
                    <p-button [label]="'publications.publish' | transloco" (onClick)="onPublish()" />
                }
                @case (PublicationState.open) {
                    <p-button [label]="'publications.extendWindow' | transloco" severity="secondary" text size="small" (onClick)="onExtend()" />
                    <p-button [label]="'general.refresh' | transloco" severity="secondary" size="small" (onClick)="store.refresh()" />
                    <p-button [label]="'publications.downloadExcel' | transloco" size="small" (onClick)="store.downloadExcel()" />
                }
                @case (PublicationState.closed) {
                    <p-button [label]="'publications.reopenWindow' | transloco" severity="secondary" size="small" (onClick)="onReopen()" />
                    <p-button [label]="'publications.downloadExcel' | transloco" size="small" (onClick)="store.downloadExcel()" />
                }
            }
        </div>
    </header>

    @if (store.isLoading()) {
        <div class="dashboard__state"><p-progressSpinner /></div>
    } @else if (store.loadError(); as errorKey) {
        <div class="dashboard__state">{{ errorKey | transloco }}</div>
    } @else {
        @switch (store.state()) {
            @case (PublicationState.draft) {
                <section class="dashboard__card dashboard__notice">
                    {{ 'publications.dashboard.draftPrompt' | transloco }}
                </section>
            }
            @case (PublicationState.published) {
                <section class="dashboard__card">
                    <p class="dashboard__notice">
                        {{ 'publications.dashboard.publishedNotice' | transloco: { opensAt: formatInstant(store.dashboard()?.windowStartUtc ?? '') } }}
                    </p>
                    <app-share-link-box [link]="store.shareLink()" (copyClicked)="store.copyLink()" />
                </section>
            }
            @case (PublicationState.open) {
                <div class="dashboard__stats">
                    <app-publication-stat-chip
                        [value]="'' + (store.dashboard()?.studentsSubmitted ?? 0)"
                        [label]="'publications.dashboard.studentsSubmitted' | transloco" />
                    <app-publication-stat-chip
                        [value]="'' + (store.dashboard()?.totalPicks ?? 0)"
                        [label]="'publications.dashboard.totalPicks' | transloco" />
                    <app-publication-stat-chip
                        [value]="store.dashboard()?.lastSubmissionAtUtc ? formatInstant(store.dashboard()!.lastSubmissionAtUtc!) : '—'"
                        [label]="'publications.dashboard.lastSubmission' | transloco" />
                    <span class="dashboard__stamp">
                        {{ 'publications.dashboard.dataAsOf' | transloco }} <bdi>{{ store.dataAsOf() }}</bdi>
                    </span>
                </div>
                <section class="dashboard__card">
                    <app-week-grid [cells]="store.slotCounts()" [weekStart]="store.selectedWeekStart()">
                        <ng-template let-cell>
                            <app-slot-count-cell [requestCount]="cell.requestCount" [slotState]="cell.state" />
                        </ng-template>
                    </app-week-grid>
                </section>
            }
            @case (PublicationState.closed) {
                <section class="dashboard__card dashboard__notice">
                    {{ 'publications.dashboard.closedNotice' | transloco: { closedAt: windowLabel(), version: store.dashboard()?.latestExcelVersion ?? 1 } }}
                </section>
                <section class="dashboard__card">
                    <app-week-grid [cells]="store.slotCounts()" [weekStart]="store.selectedWeekStart()">
                        <ng-template let-cell>
                            <app-slot-count-cell [requestCount]="cell.requestCount" [slotState]="cell.state" />
                        </ng-template>
                    </app-week-grid>
                </section>
            }
        }
    }
</div>
```

> `store.slotCounts()` returns `SlotCountForGetPublicationDashboardResponse[]`; each item carries `day`/`window` (satisfying `WeekGridCell`) plus `requestCount`/`state`, so the grid projects it directly. The download buttons call `store.downloadExcel()` (the store uses `FileDownloadService` under the hood). `'' + n` coerces counts to the string input the chip expects — keep it a signal read, not a function call.

`features\publications\ui\pages\publications-dashboard\publications-dashboard.page.scss` (logical properties, tokens only):

```scss
.dashboard__header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 24px;
}

.dashboard__eyebrow {
    font-family: var(--app-font-display);
    font-weight: 800;
    font-size: 0.66rem;
    letter-spacing: 0.1em;
    text-transform: uppercase;
    color: var(--app-text-muted);
    margin-block-end: 6px;
}

.dashboard__title-row {
    display: flex;
    align-items: center;
    gap: 14px;
}

.dashboard__actions {
    display: flex;
    gap: 10px;
    align-items: center;
}

.dashboard__stats {
    display: flex;
    gap: 12px;
    align-items: center;
    margin-block-start: 18px;
}

.dashboard__stamp {
    margin-inline-start: auto;
    font-size: 0.78rem;
    color: var(--app-text-secondary);
    font-variant-numeric: tabular-nums;
}

.dashboard__card {
    background: var(--app-bg-card);
    border: 1px solid var(--app-border);
    border-radius: var(--app-radius-card);
    padding: 22px 24px;
    margin-block-start: 14px;
}

.dashboard__notice {
    color: var(--app-text-secondary);
    line-height: 1.55;
}

.dashboard__state {
    display: flex;
    justify-content: center;
    padding-block: 48px;
}
```

- [ ] **Step 9: i18n keys (both files, mirrored)**

Add the dashboard/dialog keys to the `publications` namespace created in task 11. `en.json`:

```json
"publications": {
    "state": { "draft": "Draft", "published": "Published", "open": "Open", "closed": "Closed" },
    "publish": "Publish week…",
    "extendWindow": "Extend deadline",
    "reopenWindow": "Reopen window…",
    "downloadExcel": "Download Excel",
    "selectTeacher": "Select a teacher",
    "timezoneNote": "All times Asia/Jerusalem.",
    "shareLink": {
        "title": "Shareable link",
        "copy": "Copy link",
        "copied": "Link copied.",
        "copyFailed": "Could not copy the link.",
        "hint": "One link for all teachers — generated on publish. Share it yourself, e.g. in the students' WhatsApp group."
    },
    "publishDialog": {
        "subtitle": "One window for the whole school — publish {{week}} and share a single link.",
        "opensAt": "Submissions open",
        "closesAt": "Submissions close"
    },
    "extendDialog": { "newEnd": "New close time" },
    "reopenDialog": {
        "subtitle": "Reopening sets a new close time and lets students edit again. The next close emails a fresh version.",
        "newEnd": "New close time"
    },
    "dashboard": {
        "requests": "requests",
        "studentsSubmitted": "students submitted",
        "totalPicks": "total picks",
        "lastSubmission": "last submission",
        "dataAsOf": "Data as of",
        "draftPrompt": "This week is prepared but not published. Publish it to open the submission window and generate the link.",
        "publishedNotice": "Submissions open automatically at {{opensAt}}.",
        "closedNotice": "Window closed {{closedAt}}. Excel v{{version}} was emailed to each teacher.",
        "loadFailed": "Failed to load the publication."
    }
}
```

`he.json` (mirrored):

```json
"publications": {
    "state": { "draft": "טיוטה", "published": "פורסם", "open": "פתוח", "closed": "סגור" },
    "publish": "פרסום שבוע…",
    "extendWindow": "הארכת מועד",
    "reopenWindow": "פתיחה מחדש…",
    "downloadExcel": "הורדת אקסל",
    "selectTeacher": "בחרו מורה",
    "timezoneNote": "כל השעות באזור אסיה/ירושלים.",
    "shareLink": {
        "title": "קישור לשיתוף",
        "copy": "העתקת קישור",
        "copied": "הקישור הועתק.",
        "copyFailed": "לא ניתן להעתיק את הקישור.",
        "hint": "קישור אחד לכל המורים — נוצר בעת הפרסום. שתפו אותו בעצמכם, למשל בקבוצת הוואטסאפ של התלמידים."
    },
    "publishDialog": {
        "subtitle": "חלון אחד לכל בית הספר — פרסמו את {{week}} ושתפו קישור יחיד.",
        "opensAt": "פתיחת הגשות",
        "closesAt": "סגירת הגשות"
    },
    "extendDialog": { "newEnd": "מועד סגירה חדש" },
    "reopenDialog": {
        "subtitle": "פתיחה מחדש קובעת מועד סגירה חדש ומאפשרת לתלמידים לערוך שוב. הסגירה הבאה תשלח גרסה חדשה.",
        "newEnd": "מועד סגירה חדש"
    },
    "dashboard": {
        "requests": "בקשות",
        "studentsSubmitted": "תלמידים הגישו",
        "totalPicks": "סה״כ בחירות",
        "lastSubmission": "הגשה אחרונה",
        "dataAsOf": "נתונים נכון ל־",
        "draftPrompt": "השבוע הוכן אך טרם פורסם. פרסמו אותו כדי לפתוח את חלון ההגשות וליצור את הקישור.",
        "publishedNotice": "ההגשות ייפתחו אוטומטית ב־{{opensAt}}.",
        "closedNotice": "החלון נסגר ב־{{closedAt}}. אקסל v{{version}} נשלח לכל מורה.",
        "loadFailed": "טעינת הפרסום נכשלה."
    }
}
```

> If task 11 already added a partial `publications` namespace, merge these keys into it rather than duplicating the object. Every key added to `en.json` must have its `he.json` mirror in this same commit (client-i18n rule).

- [ ] **Step 10: Build**

Run (in `client\`): `npm run build`

Expected: build succeeds, no template type errors, no unused-import warnings.

- [ ] **Step 11: Browser walk** (Browser pane / launch config; PrimeNG overlay rAF caveat noted in project memory — verify overlays via element rects, not screenshots alone)

With Postgres + the API running and signed in as admin:

1. Prepare a week for a teacher (weekly-prep) → navigate to the dashboard → **Draft** variant shows the "not published" notice + **Publish week…** button.
2. Click **Publish week…** → dialog opens with two datepickers, read-only link, lifecycle stepper. Set start +2m / end +5m → **Publish** → 204 → tag flips to **Published**, link + copy visible.
3. Wait for auto-open → **Open** variant: stat chips (zeros), count grid (blocked cells striped), **Extend / Refresh / Download Excel** buttons; **Refresh** updates the "data as of" stamp.
4. **Extend deadline** → +2m → 204. **Download Excel** → `.xlsx` downloads.
5. After close → **Closed** variant: closed notice with version, **Reopen / Download Excel** buttons.

- [ ] **Step 12: Commit** (carries task 11's store/routing/i18n)

```bash
git add client/src/app client/public/i18n
git commit -m "feat(client): publications dashboard with publish, extend, reopen and download"
```

---

**Next:** [task-13-client-history-prep.md](task-13-client-history-prep.md)
