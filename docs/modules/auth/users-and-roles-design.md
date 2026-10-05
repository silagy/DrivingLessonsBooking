# Users and Roles - Design Handoff

The Claude Design project with the visual design for spec [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) (Users, Roles, change own password). Read this before planning or building any client slice of #82.

| | |
|---|---|
| Design project | https://claude.ai/design/p/6a0ab892-caa4-49f7-baff-bba7ca38c862?file=Users+and+Roles.html |
| Entry file | `Users and Roles.html` |
| Spec | [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) |
| Posted on | [#82](https://github.com/silagy/DrivingLessonsBooking/issues/82) as a comment, so it travels with the spec |
| Brief the design was made from | [claude-design-prompt.md](claude-design-prompt.md): screens, states, brand tokens and hard constraints given to Claude Design |

## Which slices use it

| #82 slice | Uses the design? |
|-----------|------------------|
| (1) User aggregate + login + seeding migration, [#84](https://github.com/silagy/DrivingLessonsBooking/issues/84) | No. Backend only, no client change ([plan](us-84-user-aggregate-plan/README.md)) |
| (2) Roles, policies and Teacher scoping | Yes: navigation by Role, hidden admin controls for Teacher-role Users. Teacher-role Users reach only Week Schedules and Publications, [#89](https://github.com/silagy/DrivingLessonsBooking/issues/89): [plan](us-89-teacher-role-access-plan/README.md). Teacher data scoping, [#90](https://github.com/silagy/DrivingLessonsBooking/issues/90): [plan](us-90-teacher-data-scoping-plan/README.md) |
| (3) Users admin screen | Yes: list, create/edit dialog, role change, temporary password, delete/restore. Add Users, [#85](https://github.com/silagy/DrivingLessonsBooking/issues/85): [plan](us-85-add-users-plan/README.md). Delete and restore Users, [#86](https://github.com/silagy/DrivingLessonsBooking/issues/86): [plan](us-86-delete-restore-users-plan/README.md). Edit details, Role and Temporary Password, [#87](https://github.com/silagy/DrivingLessonsBooking/issues/87): [plan](us-87-edit-users-plan/README.md) |
| (4) Change own password | Yes: the dialog reachable from the shell. Change my own password, [#88](https://github.com/silagy/DrivingLessonsBooking/issues/88): [plan](us-88-change-my-password-plan/README.md) |
| (5)-(8) Students, Roster, docs | Check the project's other files before assuming no |

## How to import it in a new session

The design lives in Claude Design, not in this repo. Access goes through the `claude_design` MCP server (`https://api.anthropic.com/v1/design/mcp`). Authenticate with `/design-login` first. If the MCP isn't connected in the session, ask the user to connect it. Don't recreate the design from memory.

The import prompt, as handed over by the user:

```text
Use the claude_design MCP (https://api.anthropic.com/v1/design/mcp, auth via /design-login) to import this project:
https://claude.ai/design/p/6a0ab892-caa4-49f7-baff-bba7ca38c862?file=Users+and+Roles.html

Focus on these files (the whole project is readable):
- `Users and Roles.html`

Also read these files the selection imports:
- `_ds/comply365-design-system-c08fa187-8d1c-4b7b-9020-5ede0ce63bf0/_ds_bundle.js`
- `_ds/comply365-design-system-c08fa187-8d1c-4b7b-9020-5ede0ce63bf0/colors_and_type.css`
- `_ds/comply365-design-system-c08fa187-8d1c-4b7b-9020-5ede0ce63bf0/preview/_card.css`
- `_ds/comply365-design-system-c08fa187-8d1c-4b7b-9020-5ede0ce63bf0/slides/_slide.css`
- `auth/app.jsx`
- `auth/kit.jsx`
- `auth/screens1.jsx`
- `auth/screens2.jsx`
- `auth/strings.jsx`
- `design-canvas.jsx`
- `mock/admin.jsx`
- `mock/shared.jsx`

Implement: `Users and Roles.html`
```

## Translating the design into this codebase

The design is a JSX mock built on a bundled design system. It's a visual reference, not code to copy. When implementing it:

- Build with Angular + PrimeNG per `.claude\rules\client-architecture.md`, `client-state.md` and `client-primeng.md`. Map each mock control to the matching PrimeNG component. Don't port the JSX kit or `_ds_bundle.js`.
- Take colors and type from the existing app theme. Pull a value from `colors_and_type.css` only when the app has no equivalent, and say so in the PR.
- `auth/strings.jsx` holds the mock's copy. Every string becomes a Transloco key in `he.json` and `en.json`, Hebrew first, and every layout must be RTL-correct (`client-i18n.md`).
- Use the glossary from [CONTEXT.md](../../../CONTEXT.md) (User, Role, Administrator, Teacher Role, Temporary Password, Deleted User) in keys and code, even where the mock's copy words things differently.
- Business rules stay in the backend: the client shows 409 rule violations and never re-implements them (CLAUDE.md rule 12).
