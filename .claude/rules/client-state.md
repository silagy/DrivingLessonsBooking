---
paths:
  - "client/**"
---

# Client State Management Guide

**Signals only.** No NgRx, no state held in RxJS subjects. RxJS appears in exactly one place — the `data\` API services returning `Observable` from `HttpClient` — and is consumed inside stores via `firstValueFrom` or `resource()`. Components never see an observable.

## Critical Rules

1. **All shared/feature state lives in a signal store** (`features\{feature}\state\{entities}.store.ts`) — never in components, never in `BehaviorSubject`s
2. **Stores expose `readonly` signals; mutations happen only through store methods** — no `.set()` on store state from outside
3. **Derived state is always `computed()`** — never a manually synchronized field
4. **Every component is `ChangeDetectionStrategy.OnPush`**
5. **Component I/O uses `input()` / `output()` / `model()` functions only** — never `@Input()` / `@Output()` decorators
6. **No `.subscribe()` in components** — if a component subscribes, the logic belongs in a store
7. **Queries vs commands mirror the backend CQRS split**: `resource()` for reads, `async` store methods for writes

## Signal Store Template

Private writable signals, public readonly projections, methods as the only mutation path:

```typescript
@Injectable({ providedIn: 'root' })
export class PublicationsStore {
    private readonly api = inject(PublicationsApiService);
    private readonly toast = inject(ToastService);

    private readonly mutating = signal(false);
    private readonly teacherId = signal<string | null>(null);

    private readonly publicationsResource = resource({
        params: () => this.teacherId(),
        loader: ({ params: teacherId }) =>
            teacherId
                ? firstValueFrom(this.api.findPublications(teacherId))
                : Promise.resolve([]),
    });

    readonly publications = computed(() => this.publicationsResource.value() ?? []);
    readonly isLoading = this.publicationsResource.isLoading;
    readonly loadError = computed(() => (this.publicationsResource.error() ? 'Publications.loadFailed' : null));
    readonly isMutating = this.mutating.asReadonly();

    readonly openPublications = computed(() =>
        this.publications().filter(x => x.state === PublicationState.open)
    );
    readonly isEmpty = computed(() => !this.isLoading() && !this.publications().length);

    selectTeacher(teacherId: string): void {
        this.teacherId.set(teacherId);
    }

    async publish(publicationId: string): Promise<void> {
        await this.executeCommand(() => this.api.publishPublication(publicationId), 'Publications.published');
    }

    async close(publicationId: string): Promise<void> {
        await this.executeCommand(() => this.api.closePublication(publicationId), 'Publications.closed');
    }

    async reopen(publicationId: string, request: ReopenPublicationRequest): Promise<void> {
        await this.executeCommand(() => this.api.reopenPublication(publicationId, request), 'Publications.reopened');
    }

    private async executeCommand(command: () => Observable<void>, successKey: string): Promise<void> {
        this.mutating.set(true);

        try {
            await firstValueFrom(command());
            this.toast.success(successKey);
            this.publicationsResource.reload();
        } catch (error) {
            this.toast.apiError(error);
        } finally {
            this.mutating.set(false);
        }
    }
}
```

### Store Conventions

| Concern | Convention |
|---------|-----------|
| Writable signals | `private readonly name = signal(...)` |
| Public state | `readonly name = this.name.asReadonly()` or a `computed()` |
| Derived state | `computed()` in the store — selectors live with the state, not in pages |
| Reads (queries) | `resource()` keyed on a params signal; `reload()` after a successful command |
| Writes (commands) | `async` method → `firstValueFrom(api...)` → toast → reload affected resource |
| Errors | error signals hold **i18n keys**, never raw text; 409 ProblemDetails titles surface via `toast.apiError` |
| Loading | `resource().isLoading` for reads; one `isMutating` signal for writes |
| Scope | `providedIn: 'root'` by default; provide on the feature route only when state must reset per visit |
| Naming | state = nouns (`publications`, `isLoading`), methods = verbs (`publish`, `selectTeacher`), derived = selector-like (`openPublications`, `isEmpty`) |

### Parameterized Reads — `resource()` Keyed by Route State

The student form loads everything from the link token:

```typescript
@Injectable()
export class StudentFormStore {
    private readonly api = inject(SubmissionsApiService);

    private readonly linkToken = signal<string | null>(null);

    private readonly publicationResource = resource({
        params: () => this.linkToken(),
        loader: ({ params: token }) =>
            token
                ? firstValueFrom(this.api.getPublicationByLink(token))
                : Promise.resolve(null),
    });

    readonly publication = computed(() => this.publicationResource.value() ?? null);
    readonly windowIsOpen = computed(() => this.publication()?.state === PublicationState.open);

    open(token: string): void {
        this.linkToken.set(token);
    }
}
```

### `effect()` — Outside World Only

`effect()` synchronizes signals with non-Angular targets: `localStorage`, `document` attributes, analytics. It never derives state (that is `computed()`) and never chains loads (that is `resource()` params). **An `effect()` that writes another signal is a design error.**

### `linkedSignal()` — Resettable Local Choices

For UI state that follows a source but is user-overridable:

```typescript
readonly selectedDay = linkedSignal({
    source: this.store.publication,
    computation: () => DayOfWeek.sunday,
});
```

## Smart Pages vs Dumb Components

```
Page (smart)                          Component (dumb)
──────────────────────────            ──────────────────────────
injects stores, DialogService    ──►  input() data in
calls store methods              ◄──  output() events up
composition only                      computed() display logic only
ui\pages\*.page.ts                    ui\components\ or shared\components\
```

### Decision Table

| If the component... | It is... | Allowed to... |
|---------------------|----------|---------------|
| is a route target | smart (page) | inject stores, open dialogs, navigate |
| renders data it receives | dumb | `input()`, `computed()`, `output()` |
| is used by 2+ features | dumb, in `shared\components\` | nothing stateful |
| needs data its parent lacks | **stop** — pass it down, or it is actually a page | |

### Page Template

```typescript
@Component({
    selector: 'app-publications-dashboard-page',
    imports: [WeekGridComponent, Button, TranslatePipe],
    templateUrl: './publications-dashboard.page.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicationsDashboardPage {
    protected readonly store = inject(PublicationsStore);
    private readonly dialogs = inject(PublicationDialogsService);

    readonly teacherId = input.required<string>();

    constructor() {
        effect(() => this.store.selectTeacher(this.teacherId()));
    }

    protected onExtendDeadline(publication: Publication): void {
        this.dialogs.openExtendDeadline(publication);
    }
}
```

### Dumb Component Template

Narrow inputs — the fields it renders, never a store or a state blob:

```typescript
@Component({
    selector: 'app-slot-count-cell',
    imports: [TranslatePipe],
    templateUrl: './slot-count-cell.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlotCountCellComponent {
    readonly requestCount = input.required<number>();
    readonly slotState = input.required<SlotState>();

    readonly cellSelected = output<void>();

    protected readonly isUnavailable = computed(() => this.slotState() === SlotState.unavailable);
}
```

Two-way binding in form-like dumb components uses `model()`:

```typescript
readonly targetCount = model<number>(1);
```

### Dialogs

Dialogs are structurally dumb: data in via `DynamicDialogConfig.data`, result out via `ref.close(result)`. **The page that opened the dialog calls the store with the result** — dialogs never inject stores.

## Templates

- Native control flow only: `@if` / `@for` / `@switch` / `@defer` — never `*ngIf` / `*ngFor`
- `@for` always has `track` (`track slot.id`)
- No `async` pipe — there are no observables to unwrap; call signals: `store.publications()`
- No function calls in templates except signal reads — anything heavier becomes a `computed()`
- Render the loading / error / empty / data states explicitly:

```html
@if (store.isLoading()) {
    <p-progressSpinner />
} @else if (store.loadError(); as errorKey) {
    <app-error-state [messageKey]="errorKey" />
} @else if (store.isEmpty()) {
    <app-empty-state titleKey="Publications.emptyTitle" />
} @else {
    @for (publication of store.publications(); track publication.id) {
        <app-publication-row [publication]="publication" (extend)="onExtendDeadline(publication)" />
    }
}
```

## Anti-Patterns

- **Never** hold state in a `BehaviorSubject`, `Subject`, or component field that mirrors store data
- **Never** `.subscribe()` in a component — subscription sites live inside stores only
- **Never** expose a writable signal from a store
- **Never** write one signal from an `effect()` watching another — use `computed()` or `linkedSignal()`
- **Never** use `@Input()` / `@Output()` decorators, `*ngIf` / `*ngFor`, or the `async` pipe
- **Never** inject a store, API service, or `Router` into a dumb component or dialog
- **Never** put business rules in a store — a store orchestrates and caches; the backend decides (a 409 is the backend deciding)
- **Never** add an `Unsubscribable` base class or `takeUntil` pattern — there is nothing to unsubscribe
