# Task 2 of 4: Users become cards on a phone

> Part of [Students, Users and Publications on a phone](README.md). Requires task 1 committed. Steps 6 and 7 need the Browser pane tools.

**Files:**
- Create: `client\src\app\features\users\ui\components\user-card\user-card.component.ts` / `.html` / `.scss` / `.spec.ts`
- Modify: `client\src\app\features\users\ui\pages\users\users.page.html` / `.scss` / `.ts`

**Interfaces:**
- `UserCardComponent` (`app-user-card`): inputs `user: User` (required), `isYou: boolean` (default `false`), `disabled: boolean` (default `false`); outputs `actionsRequested: Event`, `restoreRequested: void`.
- Root `article.user-card`, modifier `user-card--deleted`. Pieces: `.user-card__top` (avatar, `.user-card__identity` with `.user-card__name`, the You tag and `bdi.user-card__email`, the status `p-tag`, and on an active User the ⋮ `p-button` with `styleClass="user-card__actions"`), `dl.user-card__details` (role tag, linked teacher or `users.noTeacher`), and on a Deleted User a `p-button` with `styleClass="user-card__restore"`.
- The page: `ul.users__cards`, `users__card--list` on the section while the list shows, `users__add--hidden-on-phone` on the header button while `store.hasOnlyOneUser()`.

**Why:** frames 07 and 07b, rules 8 to 12 and 14.

## The card, from frame 07

Same shape as task 1's card (padding, radius, top row, ⋮, details grid, muted rule). Differences:
- The identity holds the name row (name + You tag, `flex-wrap`) over the email: 0.8125rem, secondary color, `<bdi dir="ltr">`, `overflow-wrap: anywhere`, `text-align: start`.
- Details: `users.table.role` (the role tag as on the page: sky for Administrator, ocean for Teacher, muted on a Deleted User) and `users.table.teacher` (name, or `users.noTeacher` muted).
- Deleted: no ⋮. Restore is a text `p-button` at the bottom: `inline-size: calc(100% + 1.75rem); margin-inline: -0.875rem; margin-block-end: -1.25rem; block-size: 2.75rem; justify-content: flex-start; padding-inline: 0.875rem; border-block-start: 1px solid var(--app-bg-muted); border-radius: 0 0 14px 14px` (the history card's action).

- [ ] **Step 1: Write the failing spec**

```typescript
it('shows the name, email, role and linked Teacher', async () => { ... });
it('tags the signed-in User as you', async () => { ... });                   // isYou → 'users.you'
it('shows a dash for an Administrator without a Teacher', async () => { ... }); // 'users.noTeacher'
it('asks for the row menu on an active User', async () => { ... });          // click .user-card__actions → actionsRequested
it('offers Restore instead of the menu on a Deleted User', async () => { ... }); // no .user-card__actions; click .user-card__restore → restoreRequested; .user-card--deleted
it('disables Restore while a change is saving', async () => { ... });
```

Run: `& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/users/ui/components/user-card/user-card.component.spec.ts`
Expected: FAIL.

- [ ] **Step 2: Build the card**

Imports `TranslocoPipe`, `InitialsPipe`, `ButtonModule`, `TagModule`; the role tag's classes and the `Role` enum as on the page. Run step 1's command. Expected: PASS, 6 tests.

- [ ] **Step 3: Add the list to the page**

- Wrap the table in `div.users__table-wrap`; after `p-menu`, add `ul.users__cards` with one `li` per `store.users()` (`track user.id`): `<app-user-card [user]="user" [isYou]="user.id === store.currentUserId()" [disabled]="store.isMutating()" (actionsRequested)="onRowActions($event, user)" (restoreRequested)="onRestoreUser(user)" />`.
- `[class.users__card--list]="!store.isLoading() && !store.loadError()"` on the section.
- `[class.users__add--hidden-on-phone]="store.hasOnlyOneUser()"` on the header `p-button`.

- [ ] **Step 4: Style the phone**

`.users__cards` `display: none`. At the end, `@media screen and (max-width: 639px)`:
- Header column, stretch, `gap: 1rem`; the Add User `p-button` host `display: block` with its button `inline-size: 100%; block-size: 2.75rem` (rule 14), and `display: none` on the host with `users__add--hidden-on-phone`.
- Table wrap hidden; cards as task 1; `.users__card--list` without background, border, shadow, radius, overflow; the footer centered, 0.8125rem; `.users__empty` inside the list section gets the card look (background, border, 14px radius, shadow, `margin-block-start: 0.625rem`) and its Add User 100% wide, 44px.

- [ ] **Step 5: Run the client suite.** Expected: all green.

- [ ] **Step 6: Measure the page (GREEN)**

At 390 and 360, Hebrew and English, on `/users`: the README's measurement passes; the table wrap is hidden; card count equals `store.users().length` (the footer's count).

- [ ] **Step 7: Edge cases and actions**

1. The ⋮ on another User opens the row menu with today's four items.
2. Delete a scratch User from the menu (one created for the check), then tap Restore on its card. Expected: it's restored, the card un-mutes.
3. A long email (40 characters) wraps inside the card.
4. Desktop at 1280: the table as today, the header button as today.

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/users
git commit -m "feat(admin-mobile): users show cards on a phone"
```

End the commit message with the attribution trailer from the session's instructions.
