# Task 6 of 8: The assign-teachers popover opens from its trigger in Hebrew (R4, TDD)

> Part of [US-47 + US-48: Hebrew/English Toggle With No Leftovers, Mirror-Correct RTL](README.md). Requires tasks 1–5 committed. Work on branch `47-us-47-48-language-and-rtl`.

**Files:**
- Create: `client\src\app\features\teachers\ui\components\assign-teachers-popover\rtl-popover-placement.ts`
- Create: `client\src\app\features\teachers\ui\components\assign-teachers-popover\rtl-popover-placement.spec.ts`
- Modify: `client\src\app\features\teachers\ui\components\assign-teachers-popover\assign-teachers-popover.component.ts` (whole file below), `.html` (two attributes)
- Modify: `client\src\styles\_global.scss` (append the popover-arrow rule)

**Interfaces:**
- Consumes: task 3's `LanguageService.isRtl`. PrimeNG's `Popover` (`container: HTMLDivElement | null`, `(onShow)` emitted right after PrimeNG's own `align()`), and its CSS variables `--p-popover-arrow-left`, `--p-popover-arrow-offset` (`1.25rem`) and `--p-popover-gutter` (`10px`), all read in the pane during planning.
- Produces: `rtlPopoverPlacement(triggerRight: number, panelWidth: number, viewportWidth: number): RtlPopoverPlacement` with `{ insetInlineEnd: number; arrowInset: number }`, in px. In Hebrew the panel's right edge lines up with the trigger's right edge (clamped inside the viewport), and its arrow sits `1.25rem + arrowInset` from the panel's right edge, the mirror of English. English is unchanged. Task 8 Step 6 re-checks it.

**Why:** README defect R4 and decision 9. PrimeNG's `absolutePosition` computes the trigger's physical **left** (`rect.left + scrollX`) and, when the document is RTL, writes it into `inset-inline-end`, which is the panel's **right** offset. The panel ends up anchored near the trigger's left edge and grows to the right, and the arrow CSS (`left: calc(offset + arrow-left)`, `margin-left`) is physical too. Planning measurement at 707px: trigger x 335–378, panel x 501–766, arrow-left `-6px`. The component re-anchors the panel on `(onShow)`, which PrimeNG emits right after its own `align()`. PrimeNG closes the popover on scroll and resize, so it never needs re-anchoring while open.

- [ ] **Step 1: Write the failing placement spec**

Create `client\src\app\features\teachers\ui\components\assign-teachers-popover\rtl-popover-placement.spec.ts`:

```ts
import { rtlPopoverPlacement } from './rtl-popover-placement';

const VIEWPORT_WIDTH = 707;
const PANEL_WIDTH = 266;

describe('rtlPopoverPlacement', () => {
    it('lines the panel up with the trigger\'s inline-start edge', () => {
        //given
        const triggerRight = 378;

        //when
        const placement = rtlPopoverPlacement(triggerRight, PANEL_WIDTH, VIEWPORT_WIDTH);

        //then
        expect(placement).toEqual({ insetInlineEnd: 329, arrowInset: 0 });
    });

    it('keeps the panel inside the viewport when the trigger is near the edge', () => {
        //given
        const triggerRight = 200;

        //when
        const placement = rtlPopoverPlacement(triggerRight, PANEL_WIDTH, VIEWPORT_WIDTH);

        //then
        expect(placement).toEqual({ insetInlineEnd: 441, arrowInset: 66 });
    });

    it('never pushes the panel past the inline-start edge of the viewport', () => {
        //given
        const triggerRight = 720;

        //when
        const placement = rtlPopoverPlacement(triggerRight, PANEL_WIDTH, VIEWPORT_WIDTH);

        //then
        expect(placement).toEqual({ insetInlineEnd: 0, arrowInset: 0 });
    });
});
```

The first case is the planning measurement (trigger right edge 378 in a 707px window). In the second, the panel would start at x −66, so it is pinned to x 0 and the arrow moves 66px in from the panel's right edge, back over the trigger.

- [ ] **Step 2: Run the spec to verify it fails**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. `rtl-popover-placement.spec.ts` fails to load with `Failed to resolve import "./rtl-popover-placement"`. Every other spec passes.

- [ ] **Step 3: Write the placement function**

Create `client\src\app\features\teachers\ui\components\assign-teachers-popover\rtl-popover-placement.ts`:

```ts
export interface RtlPopoverPlacement {
    insetInlineEnd: number;
    arrowInset: number;
}

export function rtlPopoverPlacement(
    triggerRight: number,
    panelWidth: number,
    viewportWidth: number,
): RtlPopoverPlacement {
    const flushWithTrigger = viewportWidth - triggerRight;
    const insideViewport = viewportWidth - panelWidth;
    const insetInlineEnd = Math.max(0, Math.min(flushWithTrigger, insideViewport));
    const panelRight = viewportWidth - insetInlineEnd;

    return { insetInlineEnd, arrowInset: Math.max(0, panelRight - triggerRight) };
}
```

- [ ] **Step 4: Run the spec**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec, including the three placement specs.

- [ ] **Step 5: Re-anchor the panel when it opens in RTL**

1. Replace the whole of `client\src\app\features\teachers\ui\components\assign-teachers-popover\assign-teachers-popover.component.ts` with:

```ts
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { Popover, PopoverModule } from 'primeng/popover';
import { LanguageService } from '../../../../../core/language.service';
import { Teacher } from '../../../domain/teacher.model';
import { rtlPopoverPlacement } from './rtl-popover-placement';

interface AssignRow {
    id: string;
    name: string;
    selected: boolean;
    assigned: boolean;
}

@Component({
    selector: 'app-assign-teachers-popover',
    imports: [FormsModule, TranslocoPipe, ButtonModule, CheckboxModule, PopoverModule],
    templateUrl: './assign-teachers-popover.component.html',
    styleUrl: './assign-teachers-popover.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignTeachersPopoverComponent {
    private readonly language = inject(LanguageService);

    readonly teachers = input.required<Teacher[]>();
    readonly assignedIds = input.required<string[]>();

    readonly applied = output<string[]>();

    private readonly popover = viewChild.required(Popover);
    private readonly trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
    private readonly selectedIds = signal<string[]>([]);

    protected readonly rows = computed<AssignRow[]>(() => {
        const selected = this.selectedIds();
        const assigned = this.assignedIds();

        return this.teachers().map((teacher) => ({
            id: teacher.id,
            name: teacher.name,
            selected: selected.includes(teacher.id),
            assigned: assigned.includes(teacher.id),
        }));
    });

    protected open(event: Event): void {
        this.selectedIds.set([...this.assignedIds()]);
        this.popover().toggle(event);
    }

    protected anchorToTrigger(): void {
        const panel = this.popover().container;

        if (!this.language.isRtl() || !panel) {
            return;
        }

        const placement = rtlPopoverPlacement(
            this.trigger().nativeElement.getBoundingClientRect().right,
            panel.offsetWidth,
            document.documentElement.clientWidth,
        );
        panel.style.insetInlineEnd = `${placement.insetInlineEnd}px`;
        panel.style.setProperty('--p-popover-arrow-left', `${placement.arrowInset}px`);
    }

    protected setSelected(teacherId: string, checked: boolean): void {
        const current = this.selectedIds();

        if (checked) {
            if (!current.includes(teacherId)) {
                this.selectedIds.set([...current, teacherId]);
            }

            return;
        }

        this.selectedIds.set(current.filter((id) => id !== teacherId));
    }

    protected apply(): void {
        this.applied.emit(this.selectedIds());
        this.popover().hide();
    }

    protected cancel(): void {
        this.popover().hide();
    }
}
```

   The panel is appended to `<body>`, which spans the layout viewport, so `document.documentElement.clientWidth` (the viewport without the scrollbar) is the width `inset-inline-end` is measured against.

2. In `client\src\app\features\teachers\ui\components\assign-teachers-popover\assign-teachers-popover.component.html`, change

```html
<button type="button" class="assign__trigger" (click)="open($event)">
  {{ 'teachers.assign' | transloco }}
</button>
<p-popover>
```

to

```html
<button #trigger type="button" class="assign__trigger" (click)="open($event)">
  {{ 'teachers.assign' | transloco }}
</button>
<p-popover (onShow)="anchorToTrigger()">
```

- [ ] **Step 6: Make the popover arrow logical**

Append to the end of `client\src\styles\_global.scss` (after task 5's rules):

```scss

.p-popover::before,
.p-popover::after {
  left: auto;
  inset-inline-start: calc(var(--p-popover-arrow-offset) + var(--p-popover-arrow-left));
}

.p-popover::before {
  margin-left: 0;
  margin-inline-start: calc(-1 * var(--p-popover-gutter));
}

.p-popover::after {
  margin-left: 0;
  margin-inline-start: calc(-1 * (var(--p-popover-gutter) - 2px));
}
```

These are PrimeNG's own arrow rules (`@primeuix/styles/popover`) with `left` / `margin-left` turned into their logical forms. In English they compute to exactly what PrimeNG does today; in Hebrew the arrow is measured from the panel's right edge. `left: auto` and `margin-left: 0` only reset PrimeNG's physical values (README Global Constraints).

- [ ] **Step 7: Check it in the browser (Hebrew, English, narrow window)**

1. With the `api` and `client` servers running and signed in as the dev admin (task 5 Step 6.1), open `/teachers` in Hebrew. Click `+ שיוך` on the first car with `computer` `left_click` (find the ref with `find` "שיוך"). Then paste into `javascript_tool`:
   ```js
   const trigger = document.querySelector('.assign__trigger').getBoundingClientRect();
   const panel = document.querySelector('.p-popover');
   const box = panel.getBoundingClientRect();
   [Math.round(box.right - trigger.right), box.left >= 0, getComputedStyle(panel, '::before').right, panel.style.getPropertyValue('--p-popover-arrow-left')]
   ```
   → `[0, true, "20px", "0px"]`: the panel's right edge is on the trigger's right edge, it fits on screen, and the arrow is 1.25rem in from the right. Before this task the first value was about `388` and the arrow-left `-6px` (planning measurement). Press Escape.
2. Press `EN`, open the same popover and run:
   ```js
   const trigger = document.querySelector('.assign__trigger').getBoundingClientRect();
   const box = document.querySelector('.p-popover').getBoundingClientRect();
   [Math.round(box.left - trigger.left), getComputedStyle(document.querySelector('.p-popover'), '::before').left]
   ```
   → `[0, "20px"]`: English is unchanged (PrimeNG's own placement). Press Escape, press `עב`.
3. **Near the edge (Review Focus 4):** `resize_window` width **480**, height **800**, reload `/teachers` in Hebrew, open `+ שיוך` on the first car and run the snippet from 1 again, adding `box.right <= document.documentElement.clientWidth` to the array. Expected: `box.left >= 0` and `box.right <= clientWidth` are both `true`. If the panel had to be pinned, the first value is no longer `0`, but the arrow (`--p-popover-arrow-left`, now non-zero) still sits over the trigger: `Math.abs((box.right - parseFloat(getComputedStyle(panel, '::before').right)) - (trigger.left + trigger.width / 2)) < trigger.width` → `true`. `resize_window` preset **desktop**.

If a check fails, stop: fix the component or the rule, re-run the suite and repeat this step.

- [ ] **Step 8: Run the suite and the build**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec.

Run (in `client\`): `npm run build`
Expected: builds clean, with no new warnings.

- [ ] **Step 9: Commit**

```bash
git add client/src/app/features/teachers/ui/components/assign-teachers-popover client/src/styles/_global.scss
git commit -m "fix(teachers): open the assign popover from its trigger in Hebrew

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
