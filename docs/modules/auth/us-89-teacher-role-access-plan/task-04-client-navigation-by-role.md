# Task 4 of 7: Navigation by Role and the Role tag in the user menu

> Part of [#89: Teacher-role Users Reach Only Week Schedules and Publications](README.md). Requires task 3 committed. Work on branch `89-teacher-role-navigation`. Read README decisions 8 and 9 first.

**Files:**
- Create: `client\src\app\features\admin-shell\domain\navigation.ts`
- Test: `client\src\app\features\admin-shell\domain\navigation.spec.ts` (**new**)
- Modify: `client\src\app\features\admin-shell\admin-shell.component.ts`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.html`
- Modify: `client\src\app\features\admin-shell\admin-shell.component.scss`
- Modify: `client\public\i18n\he.json`, `client\public\i18n\en.json`

**Interfaces:**
- Consumes: `AuthService.role: Signal<Role | null>` (task 2), `Role` from `client\src\app\shared\models\role.enum.ts` (task 2), `AppRoutes`.
- Produces:
  - `interface NavigationItem { labelKey: string; commands: readonly string[]; exact: boolean }`
  - `function navigationFor(role: Role | null): readonly NavigationItem[]`
  - Translation keys `shell.roles.administrator`, `shell.roles.teacher`.

**Design:**
- **2a (Administrator bar):** Dashboard, Cars & teachers, Roster, Weekly prep, Publications, History, Users. Users is last. (Same items and order as today.)
- **2b (Teacher bar):** Weekly prep, Publications, History only. The hidden items are not rendered at all.
- **2c / 2d (user menu header):** the larger avatar at the inline start; beside it, stacked: the email (LTR, secondary text), then a pill-shaped **Role tag**. Administrator tag: sky tint (light sky background, dark sky text, sky border). Teacher tag: ocean tint. These are the same tints as the Role tags on the Users screen (`users__tag--administrator` / `users__tag--teacher` in `features\users\ui\pages\users\users.page.scss`). Then the divider, "שינוי הסיסמה שלי" and "התנתקות" as today.

**Why:** AC 6 ("The admin shell renders navigation by Role (spec covers the Role → navigation mapping)").

- [ ] **Step 1: Write the failing mapping spec**

Create `client\src\app\features\admin-shell\domain\navigation.spec.ts`:

```typescript
import { Role } from '../../../shared/models/role.enum';
import { navigationFor } from './navigation';

function labelsOf(role: Role | null): string[] {
    return navigationFor(role).map((item) => item.labelKey);
}

describe('navigationFor', () => {
    it('gives an Administrator every screen, with Users last', () => {
        //expected
        expect(labelsOf(Role.administrator)).toEqual([
            'shell.nav.dashboard',
            'shell.nav.teachers',
            'shell.nav.roster',
            'shell.nav.weeklyPrep',
            'shell.nav.publications',
            'shell.nav.history',
            'shell.nav.users',
        ]);
    });

    it('gives a Teacher only weekly prep, publications and history', () => {
        //expected
        expect(labelsOf(Role.teacher)).toEqual([
            'shell.nav.weeklyPrep',
            'shell.nav.publications',
            'shell.nav.history',
        ]);
    });

    it('shows no navigation without a known Role', () => {
        //expected
        expect(navigationFor(null)).toEqual([]);
    });

    it('links each screen to its route', () => {
        //given
        const commandsByLabel = new Map(
            navigationFor(Role.administrator).map((item) => [item.labelKey, item.commands]),
        );

        //expected
        expect(commandsByLabel.get('shell.nav.dashboard')).toEqual(['/']);
        expect(commandsByLabel.get('shell.nav.teachers')).toEqual(['/', 'teachers']);
        expect(commandsByLabel.get('shell.nav.roster')).toEqual(['/', 'roster']);
        expect(commandsByLabel.get('shell.nav.weeklyPrep')).toEqual(['/', 'week-schedules']);
        expect(commandsByLabel.get('shell.nav.publications')).toEqual(['/', 'publications']);
        expect(commandsByLabel.get('shell.nav.history')).toEqual(['/', 'publications', 'history']);
        expect(commandsByLabel.get('shell.nav.users')).toEqual(['/', 'users']);
    });

    it('highlights the dashboard and publications only on their exact route', () => {
        //given
        const exactLabels = navigationFor(Role.administrator)
            .filter((item) => item.exact)
            .map((item) => item.labelKey);

        //expected
        expect(exactLabels).toEqual(['shell.nav.dashboard', 'shell.nav.publications']);
    });
});
```

(The last spec keeps today's `routerLinkActiveOptions`: Publications must not light up on History, which lives under `/publications/history`.)

- [ ] **Step 2: Run it to verify it fails**

From `client\` (PowerShell):

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false --include src/app/features/admin-shell/domain/navigation.spec.ts
```

Expected: FAIL: `Cannot find module './navigation'`.

- [ ] **Step 3: Implement the mapping**

Create `client\src\app\features\admin-shell\domain\navigation.ts`:

```typescript
import { AppRoutes } from '../../../shared/config/app-routes';
import { Role } from '../../../shared/models/role.enum';

export interface NavigationItem {
    labelKey: string;
    commands: readonly string[];
    exact: boolean;
}

const DASHBOARD: NavigationItem = { labelKey: 'shell.nav.dashboard', commands: ['/'], exact: true };
const TEACHERS: NavigationItem = { labelKey: 'shell.nav.teachers', commands: ['/', AppRoutes.teachers], exact: false };
const ROSTER: NavigationItem = { labelKey: 'shell.nav.roster', commands: ['/', AppRoutes.roster], exact: false };
const WEEKLY_PREP: NavigationItem = {
    labelKey: 'shell.nav.weeklyPrep',
    commands: ['/', AppRoutes.weekSchedules],
    exact: false,
};
const PUBLICATIONS: NavigationItem = {
    labelKey: 'shell.nav.publications',
    commands: ['/', AppRoutes.publications],
    exact: true,
};
const HISTORY: NavigationItem = {
    labelKey: 'shell.nav.history',
    commands: ['/', AppRoutes.publications, AppRoutes.publicationsHistory],
    exact: false,
};
const USERS: NavigationItem = { labelKey: 'shell.nav.users', commands: ['/', AppRoutes.users], exact: false };

const ADMINISTRATOR_NAVIGATION: readonly NavigationItem[] = [
    DASHBOARD,
    TEACHERS,
    ROSTER,
    WEEKLY_PREP,
    PUBLICATIONS,
    HISTORY,
    USERS,
];
const TEACHER_NAVIGATION: readonly NavigationItem[] = [WEEKLY_PREP, PUBLICATIONS, HISTORY];

export function navigationFor(role: Role | null): readonly NavigationItem[] {
    switch (role) {
        case Role.administrator:
            return ADMINISTRATOR_NAVIGATION;
        case Role.teacher:
            return TEACHER_NAVIGATION;
        default:
            return [];
    }
}
```

Run the spec again (same command). Expected: PASS.

- [ ] **Step 4: Render the navigation from the mapping**

In `client\src\app\features\admin-shell\admin-shell.component.ts`:

Add the imports:

```typescript
import { Role } from '../../shared/models/role.enum';
import { navigationFor } from './domain/navigation';
```

Replace `protected readonly appRoutes = AppRoutes;` with:

```typescript
    protected readonly roles = Role;
    protected readonly navigation = computed(() => navigationFor(this.auth.role()));
```

and delete the now-unused `import { AppRoutes } from '../../shared/config/app-routes';` (check: nothing else in the file uses `AppRoutes`).

In `client\src\app\features\admin-shell\admin-shell.component.html`, replace the whole `<nav class="shell__nav"> ... </nav>` element (seven hard-coded `<a>` links) with:

```html
    <nav class="shell__nav">
      @for (item of navigation(); track item.labelKey) {
        <a
          [routerLink]="item.commands"
          routerLinkActive="shell__nav-link--active"
          [routerLinkActiveOptions]="{ exact: item.exact }"
          class="shell__nav-link">
          {{ item.labelKey | transloco }}
        </a>
      }
    </nav>
```

- [ ] **Step 5: Add the Role tag to the user menu header**

In the same template, replace the `<ng-template #start> ... </ng-template>` block inside `<p-menu>` with:

```html
        <ng-template #start>
          <div class="shell-user-menu__header">
            <span class="shell__avatar shell__avatar--large" aria-hidden="true">{{ initials() }}</span>
            <div class="shell-user-menu__identity">
              @if (auth.email(); as email) {
                <bdi class="shell-user-menu__email" dir="ltr">{{ email }}</bdi>
              }
              @if (auth.role(); as role) {
                <span
                  class="shell-user-menu__role"
                  [class.shell-user-menu__role--administrator]="role === roles.administrator"
                  [class.shell-user-menu__role--teacher]="role === roles.teacher">
                  {{ 'shell.roles.' + role | transloco }}
                </span>
              }
            </div>
          </div>
        </ng-template>
```

In `client\src\app\features\admin-shell\admin-shell.component.scss`, change `align-items: center;` in `.shell-user-menu__header` to `align-items: flex-start;`, and add after the `.shell-user-menu__email` rule:

```scss
.shell-user-menu__identity {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.375rem;
  min-inline-size: 0;
}

.shell-user-menu__role {
  display: inline-flex;
  align-items: center;
  border: 1px solid transparent;
  border-radius: 999px;
  padding: 3px 10px;
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

.shell-user-menu__role--administrator {
  background: var(--p-sky-50);
  color: var(--p-sky-700);
  border-color: var(--p-sky-100);
}

.shell-user-menu__role--teacher {
  background: var(--p-ocean-50);
  color: var(--p-ocean-700);
  border-color: var(--p-ocean-100);
}
```

(The header lives in an `ng-template` declared in this component, so the component's styles reach it even though the menu is appended to `body`.)

- [ ] **Step 6: Add the translations**

Inside the `shell` object, right after `"changeMyPassword"`:

`client\public\i18n\he.json`:

```json
    "roles": {
      "administrator": "מנהל מערכת",
      "teacher": "מורה"
    },
```

`client\public\i18n\en.json`:

```json
    "roles": {
      "administrator": "Administrator",
      "teacher": "Teacher"
    },
```

- [ ] **Step 7: Run the client suite and build**

```powershell
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js test --watch=false
& "C:\Program Files\nodejs\node.exe" node_modules\@angular\cli\bin\ng.js build --project client
```

Expected: all PASS, build succeeds. The rendered shell is checked in the browser in task 7 (steps 3 and 6).

- [ ] **Step 8: Commit**

```bash
git add client/src/app/features/admin-shell client/public/i18n/he.json client/public/i18n/en.json
git commit -m "feat(client): the shell shows navigation and the Role tag by the signed-in User's Role (#89)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
