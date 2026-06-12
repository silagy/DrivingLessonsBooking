---
paths:
  - "client/**/*.ts"
  - "client/**/*.html"
  - "client/**/*.scss"
---

# Client PrimeNG Guide

Rules for using and styling PrimeNG. Theming is **TypeScript design tokens** (`definePreset`), not CSS files; cascade control is **CSS layers**, not `!important`.

## Critical Rules

1. **All theming flows through `providePrimeNG` + `definePreset`** — never import `theme.css` or any PrimeNG CSS in `angular.json`
2. **Every line of custom CSS lives in `@layer app`** — outside the layer it loses to PrimeNG's injected styles
3. **No hardcoded hex colors** — `var(--p-*)` tokens or `--app-*` aliases only
4. **No `!important`** — CSS layers make it unnecessary; treat any new one as a defect
5. **RTL-correct by construction** — logical properties in every override (see `client-i18n.md`)
6. **Two form factors** — student form mobile-first (~375px), admin desktop-first

## Setup

```typescript
providePrimeNG({
    theme: {
        preset: AppPreset,
        options: {
            darkModeSelector: false,
            cssLayer: { name: 'primeng', order: 'primeng, app' },
        },
    },
}),
provideAnimationsAsync(),
```

```typescript
// client\src\app\theme\app-preset.ts
export const AppPreset = definePreset(Aura, {
    primitive: {},
    semantic: {},
    components: {},
});
```

PrimeNG renders RTL when `document.documentElement.dir === 'rtl'` — `LanguageService` owns that attribute; nothing PrimeNG-specific is needed beyond logical-property discipline in overrides.

## Customization Ladder

Always try in this order; reaching for a lower rung to change a color means you skipped a token:

1. **Semantic tokens** in the preset — brand color, surfaces, focus ring, font
2. **Component tokens** in the preset — one component deviates from the system
3. **CSS in `@layer app`** — custom variant classes, structural/responsive overrides
4. **`:host ::ng-deep`** in one component's SCSS — last resort, always scoped with `:host`

## Style Organization

```
client\src\styles\
├── _global.scss          body, font, resets
├── _tokens.scss          --app-* aliases over --p-* variables
├── _utilities.scss       shared utility classes
└── components\           one file per PrimeNG component, only when needed
    ├── _dialog-overrides.scss
    └── _table-overrides.scss
```

```scss
// styles.scss
@import 'primeicons/primeicons.css';

@layer app {
    @import './styles/global';
    @import './styles/tokens';
    @import './styles/utilities';
    @import './styles/components/dialog-overrides';
}
```

Thin alias layer so a PrimeNG variable rename costs one file:

```scss
:root {
    --app-text-primary: var(--p-surface-800);
    --app-text-secondary: var(--p-surface-500);
    --app-bg-page: var(--p-surface-50);
    --app-bg-card: var(--p-surface-0);
    --app-border-default: var(--p-surface-200);
    --app-slot-unavailable: var(--p-surface-200);
    --app-slot-selected: var(--p-primary-50);
}
```

## Component Usage

### Imports — standalone, per component

```typescript
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
```

Current component names only: `Select` (not Dropdown), `DatePicker` (not Calendar), `Drawer` (not Sidebar), `Popover` (not OverlayPanel), `ToggleSwitch` (not InputSwitch), `Tabs` (not TabView).

### Buttons

- `severity` first: `primary` (default), `secondary`, `danger` (destructive actions like Close window)
- Project variants are shared classes in `@layer app`, never per-component copies
- Labels always translated

```html
<p-button [label]="'Publications.publish' | translate" (onClick)="onPublish()" />
<p-button [label]="'General.cancel' | translate" severity="secondary" />
```

### Dialogs — always DynamicDialog

All modals open through `DialogService`; dialogs are dumb (see `client-state.md`):

```typescript
openExtendDeadline(publication: Publication): void {
    const ref = this.dialogService.open(ExtendDeadlineDialog, {
        header: this.translate.instant('Publications.extendDeadline'),
        width: '32rem',
        data: { publication },
        dismissableMask: true,
    });

    ref.onClose.subscribe((newEndUtc?: string) => {
        if (newEndUtc) {
            this.store.extendDeadline(publication.id, newEndUtc);
        }
    });
}
```

- Shared variant classes in `_dialog-overrides.scss` (e.g., a danger variant for destructive confirmations) — defined once, reused
- On mobile widths the dialog goes full-screen (global override, not per-dialog)

### Toasts

- One `<p-toast />` in the root component — features never place their own
- All toasts go through `ToastService` (`core\services\`), which translates keys and applies consistent severity/life; `apiError(error)` maps ProblemDetails to a translated message

### The Week Grid

The grid (admin preparation, admin dashboard, student slot picking) is **one shared dumb component** (`shared\components\week-grid\`), not a PrimeNG table — it is a fixed Sunday–Friday × slot matrix:

- Sunday–Thursday: Morning / Noon / Afternoon / Evening; **Friday: Morning and Noon only; Saturday never renders** — the impossible cells do not exist in the DOM (requirements §5.3)
- `Unavailable` slots styled with `--app-slot-unavailable`, visibly blocked and non-interactive
- Cell content varies by surface via inputs/content projection: toggle (preparation), request count (dashboard), rank badge (student picking)
- Mirror-correct in RTL: day columns flow with the document direction, no per-cell direction hacks

### Forms

- PrimeNG inputs + typed reactive forms
- Invalid styling comes from PrimeNG's built-in `ng-dirty.ng-invalid` handling — no per-field error CSS
- Validation messages from `FormErrors.*` keys
- Datetime entry (submission window start/end) uses `DatePicker` with time; values convert to UTC ISO at the API boundary

## Responsive Sizing

- Student form: mobile-first, ~375px design width, 40px minimum touch targets, single-column flow
- Admin: desktop-first; compact 32px inputs above the desktop breakpoint
- One breakpoint convention: `992px` separates mobile/desktop; sizing overrides live in one place in `@layer app`, not per component

```scss
@layer app {
    .p-inputtext,
    .p-select {
        height: 40px;
    }

    @media screen and (min-width: 992px) {
        .p-inputtext,
        .p-select {
            height: 32px;
        }
    }
}
```

## Component-Level Overrides (last resort)

```scss
:host ::ng-deep .p-tag {
    font-size: 0.75rem;
}
```

- Always `:host ::ng-deep`; bare `::ng-deep` only for overlay content rendered outside the host (dialogs, popovers) and then always anchored to a unique class passed via the component's `styleClass`
- `ViewEncapsulation.None` only in shared wrapper components that exist to restyle a PrimeNG internal

## Anti-Patterns

- **Never** import PrimeNG theme/CSS files in `angular.json`
- **Never** write custom CSS outside `@layer app`
- **Never** hardcode hex values — tokens only
- **Never** use `!important`, unscoped `::ng-deep`, or physical CSS properties in overrides
- **Never** open a modal outside `DialogService` or place a second `<p-toast />`
- **Never** render Saturday cells or Friday Afternoon/Evening cells in any grid
- **Never** duplicate an existing variant class — check `styles\components\` first
- **Never** create a styles override file without importing it in `styles.scss`
- **Never** mix SCSS `$variables` with the CSS custom-property token system
