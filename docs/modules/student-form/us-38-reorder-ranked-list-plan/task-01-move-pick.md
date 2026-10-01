# Task 1 of 4: Client domain — `PickMove` + `movePick` (TDD)

> Part of [US-38: Reorder the Ranked List](README.md). Work on branch `38-us-38-reorder-ranked-list`.

**Files:**
- Modify: `client\src\app\features\student-form\domain\slot-pick.ts` (append `PickMove` + `movePick`)
- Test: `client\src\app\features\student-form\domain\slot-pick.spec.ts` (append five specs to the existing `describe('slot picks')`)

**Interfaces:**
- Consumes: the existing `SlotPick { slotId: string; sessionType: SessionType; constraint: string | null }` and `rankOf(picks, slotId): number | null` in the same file.
- Produces (tasks 2 and 3 rely on these exact names):
  ```ts
  export interface PickMove {
      slotId: string;
      toIndex: number;
  }

  export function movePick(picks: readonly SlotPick[], move: PickMove): SlotPick[];
  ```
  `toIndex` is the zero-based position the pick should end up at (rank − 1). Out-of-range indexes clamp to the first or last position. An unknown `slotId` returns an unchanged copy. The input array is never mutated (README decision 10).

**Why here:** `slot-pick.ts` already holds the pure list operations on picks (`upsertPick`, `removePick`, `rankOf`). Reordering is the same kind of operation and has no Angular dependency, so it lives with them and gets plain unit tests.

- [ ] **Step 1: Write the failing specs**

In `client\src\app\features\student-form\domain\slot-pick.spec.ts`, change the import line from:

```ts
import { rankOf, removePick, SlotPick, upsertPick } from './slot-pick';
```

to:

```ts
import { movePick, rankOf, removePick, SlotPick, upsertPick } from './slot-pick';
```

Then add these specs inside `describe('slot picks', () => { … })`, after the existing `it('never mutates the list it was given', …)` block and before the closing `});`:

```ts
    it('promotes a later pick and renumbers the ranks to the new order', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = movePick(picks, { slotId: 'friday-morning', toIndex: 0 });

        expect(next.map(x => x.slotId)).toEqual(['friday-morning', 'sunday-noon', 'monday-evening']);
        expect(rankOf(next, 'friday-morning')).toBe(1);
        expect(rankOf(next, 'sunday-noon')).toBe(2);
        expect(rankOf(next, 'monday-evening')).toBe(3);
    });

    it('demotes a pick below the ones after it', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = movePick(picks, { slotId: 'sunday-noon', toIndex: 1 });

        expect(next.map(x => x.slotId)).toEqual(['monday-evening', 'sunday-noon', 'friday-morning']);
    });

    it('keeps the session type and constraint with the moved pick', () => {
        const picks = [
            pick('sunday-noon', SessionType.double, 'only after 16:00'),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, 'pick me up from work'),
        ];

        const next = movePick(picks, { slotId: 'sunday-noon', toIndex: 2 });

        expect(next).toEqual([
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, 'pick me up from work'),
            pick('sunday-noon', SessionType.double, 'only after 16:00'),
        ]);
    });

    it.each([
        { toIndex: -1, order: ['monday-evening', 'sunday-noon', 'friday-morning'] },
        { toIndex: 9, order: ['sunday-noon', 'friday-morning', 'monday-evening'] },
    ])('clamps a move to $toIndex to the nearest end of the list', ({ toIndex, order }) => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
            pick('friday-morning', SessionType.single, null),
        ];

        const next = movePick(picks, { slotId: 'monday-evening', toIndex });

        expect(next.map(x => x.slotId)).toEqual(order);
    });

    it('leaves the order alone for a slot that is not picked, without mutating the list', () => {
        const picks = [
            pick('sunday-noon', SessionType.single, null),
            pick('monday-evening', SessionType.single, null),
        ];

        const unknown = movePick(picks, { slotId: 'thursday-evening', toIndex: 0 });
        const moved = movePick(picks, { slotId: 'monday-evening', toIndex: 0 });

        expect(unknown).toEqual(picks);
        expect(unknown).not.toBe(picks);
        expect(moved).not.toBe(picks);
        expect(picks.map(x => x.slotId)).toEqual(['sunday-noon', 'monday-evening']);
    });
```

- [ ] **Step 2: Run the specs to verify they fail**

Run (in `client\`): `npm test -- --watch=false`
Expected: FAIL. The build reports `Module '"./slot-pick"' has no exported member 'movePick'` (or the five new specs fail with `movePick is not a function`). Every other spec still passes.

- [ ] **Step 3: Implement `PickMove` and `movePick`**

Append to `client\src\app\features\student-form\domain\slot-pick.ts`, after `rankOf`:

```ts

export interface PickMove {
    slotId: string;
    toIndex: number;
}

export function movePick(picks: readonly SlotPick[], move: PickMove): SlotPick[] {
    const fromIndex = picks.findIndex(pick => pick.slotId === move.slotId);

    if (fromIndex < 0) {
        return [...picks];
    }

    const toIndex = Math.min(Math.max(move.toIndex, 0), picks.length - 1);
    const others = picks.filter((_, index) => index !== fromIndex);

    return [...others.slice(0, toIndex), picks[fromIndex], ...others.slice(toIndex)];
}
```

Check against the specs: `[a, b, c]` with `c → 0` gives `others = [a, b]` → `[c, a, b]`. `a → 2` gives `others = [b, c]` → `[b, c, a]`. `b → 9` clamps to 2 → `[a, c, b]`. `b → -1` clamps to 0 → `[b, a, c]`.

- [ ] **Step 4: Run the specs to verify they pass**

Run (in `client\`): `npm test -- --watch=false`
Expected: PASS, every spec including the six new cases (five `it` blocks, one of them an `it.each` with two rows).

- [ ] **Step 5: Commit**

```bash
git add client/src/app/features/student-form/domain/slot-pick.ts client/src/app/features/student-form/domain/slot-pick.spec.ts
git commit -m "feat(student-form): move a pick to a new rank in the ranked list

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
