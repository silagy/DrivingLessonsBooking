# Task 3: The opened slot in the store

Part of [Slot requests detail and the phone rules, finished](README.md). Read the README's Global Constraints and Decisions 9 and 10 first.

**Files:**
- Create: `client\src\app\features\publications\data\item-for-find-slot-requests.response.ts`
- Modify: `client\src\app\features\publications\data\publications-api.service.ts`
- Modify: `client\src\app\features\publications\state\publications.store.ts`
- Test: `client\src\app\features\publications\state\publications.store.spec.ts`

**Interfaces:**
- Consumes: `GET api/publications/{id}/slots/{slotId}/requests?teacherId=` from task 2.
- Produces:
  - `interface ItemForFindSlotRequestsResponse { studentId: string; studentName: string; rank: number; pickCount: number; }`
  - `PublicationsApiService.findSlotRequests(id: string, teacherId: string, slotId: string): Observable<ItemForFindSlotRequestsResponse[]>`
  - On `PublicationsStore`:
    - `openSlotRequests(slotId: string): void` and `closeSlotRequests(): void`.
    - `readonly openedSlot: Signal<SlotCountForGetPublicationDashboardResponse | undefined>`: the `slotCounts()` cell with that `slotId`.
    - `readonly slotRequests: Signal<ItemForFindSlotRequestsResponse[]>`: `[]` until loaded.
    - `readonly slotRequestsAreLoading: Signal<boolean>`.
    - `readonly slotRequestsError: Signal<string | null>`: `'publications.slotRequests.loadFailed'` when the load fails.

- [ ] **Step 1: Write the failing specs**

In `publications.store.spec.ts`:
- Add `findSlotRequests` to the API fake, with the spec's existing pattern (`of(...)`, `NEVER`, `throwError(...)`).
- Add a `describe('slot requests', ...)` block on an Open publication whose dashboard has two cells, `slot-a` and `slot-b`, with the spec's existing helpers for loading a dashboard.

```typescript
    it('loads the students who requested the opened slot', async () => { ... });
    // openSlotRequests('slot-a') → findSlotRequests called with ('publication-1', <selected teacher>, 'slot-a');
    // openedSlot()?.slotId === 'slot-a'; slotRequests() equals the faked list

    it('forgets the opened slot when the list closes', async () => { ... });
    // open then closeSlotRequests() → openedSlot() undefined, slotRequests() []

    it('shows the requests of the last slot opened', async () => { ... });
    // slot-a's call returns a Subject that emits after slot-b's of(...) → slotRequests() is slot-b's list

    it('says the list could not load', async () => { ... });
    // findSlotRequests → throwError(500) → slotRequestsError() === 'publications.slotRequests.loadFailed'

    it('closes the opened slot when another Teacher is chosen', async () => { ... });
    // open, then selectTeacher('teacher-cohen') → openedSlot() undefined
```

- [ ] **Step 2: Run them to verify they fail**

Run: `ng.js test --watch=false --include src/app/features/publications/state/publications.store.spec.ts`
Expected: FAIL. `openSlotRequests` is not a function.

- [ ] **Step 3: Implement**

- The response file and the API method go next to `getDashboard`, using `HttpParams().set('teacherId', ...)`.
- In the store:
  - `openedSlotIdState` is a `linkedSignal`. Its source is `{ publicationId: this.publication()?.id, teacherId: this.selectedTeacherIdState() }` and its computation returns `null`, so a new week or Teacher closes it.
  - `slotRequestsResource` is a `resource` with `params` `{ publicationId, teacherId, slotId }`, or `undefined` while any of them is missing. Its loader calls `firstValueFrom(this.api.findSlotRequests(...))`.
  - Expose the signals from the Interfaces block in the same style as `history` and `historyError`.

- [ ] **Step 4: Run the spec**

Run the spec. Expected: PASS, and the file's existing tests still pass.

- [ ] **Step 5: Commit**

```bash
git add client/src/app/features/publications/data client/src/app/features/publications/state
git commit -m "feat(publications): the dashboard store loads who requested a slot"
```
