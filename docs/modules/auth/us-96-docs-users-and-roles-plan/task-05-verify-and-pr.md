# Task 5 of 5: Sweep, link check and the PR

> Part of [#96: Docs for the User aggregate, the Roster amendment, requirements and glossary](README.md). Work on branch `96-docs-users-and-roles`. Requires tasks 1 to 4 committed.

**Files:**
- No planned changes. A step that finds a defect fixes it in the file it belongs to, as its own commit `docs: <what> (#96)`, before the PR.

**Interfaces:**
- Consumes: every file changed by tasks 1 to 4.
- Produces: the PR into `82-users-and-roles` that closes #96.

**Why:** Review Focus items 1, 4 and 5 (README): a stale sentence left in an untouched section, a glossary _Avoid_ word in new prose, and a broken relative link. Nothing else catches these, because no test reads the docs.

- [ ] **Step 1: Stale-phrase sweep across every doc**

```bash
git grep -n -i -E "no teacher logins|logins for teachers|no login in v1|teacher logins|single login|identified by email|pre-loaded from|exist in the uploaded roster|re-uploading the roster|in the roster\)|present in the roster|roster member|roster is authoritative|single source of truth|pre-aggregate|AdminUser|admin_users|AdminSeeder" -- README.md CLAUDE.md CONTEXT.md docs/requirements.md docs/tech-stack.md docs/development docs/decisions .claude/rules
```

Every hit must be one of these, all of them historical and kept on purpose (plan decision 6):
- `docs/requirements.md`: decision rows #1 and #19 (struck-through text), the v1.4 changelog ("no longer the single source of truth"), §5.5 "The roster is **not** the single source of truth", decision #19's rationale "Roster is authoritative".
- `docs/decisions/0003-...`: the original Decision and Alternatives text, the amendment's title and "no longer because the Roster is authoritative".
- `docs/decisions/0004-...`: its whole body (superseded, unchanged).
- `docs/decisions/0006-...`: "replacing the `AdminUser` record and the `admin_users` table", the "Keep the infrastructure `AdminUser` record" alternative, and the Consequences bullet quoting decision #1 ("single login, no teacher logins").

Any other hit is a stale sentence. Rewrite it to match the v1.5 model and commit the fix.

- [ ] **Step 2: No _Avoid_ word in the added lines**

```bash
git diff 82-users-and-roles...HEAD -- . ':(exclude)docs/modules' | grep "^+" | grep -n -i -E "\baccounts?\b|admin user|\blogins?\b|permission|\bgroups?\b|instructor|pupil|trainee|transfer|reassign|\bmov(e|es|ed|ing) (a|the) student|assign car|deleted student|archived|disabled|suspended"
```

Allowed hits, and only these:
- "Student logins" in the out-of-scope list, "Logins" never as a name for a User elsewhere.
- "per-permission configuration" / "Permission sets" where the text says they are out of scope or rejected.
- "moving a student's existing submissions" (it moves Submissions, not a Student) in the out-of-scope list.
- the text of decision #1 ("single login, no teacher logins"), struck through in `requirements.md` and quoted in ADR 0006's Consequences.

Anything else: reword it with the glossary term and commit the fix.

- [ ] **Step 3: Every relative link in a changed file resolves**

```bash
for f in $(git diff --name-only 82-users-and-roles...HEAD -- '*.md' ':(exclude)docs/modules'); do
  dir=$(dirname "$f")
  grep -o "\](\([^)#]*\)" "$f" | sed 's/^](//' | grep -v "^http" | grep -v "^$" | while read -r link; do
    test -e "$dir/$link" && echo "ok   $f -> $link" || echo "BROKEN $f -> $link"
  done
done
```

Expected: only `ok` lines. Fix every `BROKEN` link and commit.

- [ ] **Step 4: Acceptance criteria, line by line**

Tick each against the branch, quoting the line that satisfies it in the PR description:

- [ ] A new ADR records the User aggregate and the two Roles; it supersedes ADR 0004, and ADR 0004 is marked superseded: `docs/decisions/0006-user-aggregate-and-roles.md` status line, ADR 0004 line 3.
- [ ] ADR 0003 amended: no longer the single source of truth, never deactivates, mismatched Car / Teacher rows rejected, national-ID corrections at the source. The amendment bullets (from #92) plus task 3's paragraph.
- [ ] `requirements.md` updated: personas (§4), decision 1 reversed (#1 struck through, #24), out-of-scope and deferred lists (§3, §12), §5.5.1 and §5.5, affected flows (§6.1, §6.2, §6.5, §7).
- [ ] CLAUDE.md snapshot: Student by national ID; User present.
- [ ] `CONTEXT.md` glossary committed with User, Role, Administrator, Teacher Role, Temporary Password, Deleted User, Change Teacher, Change Car, Inactive Student, Roster:

```bash
for t in "User" "Role" "Administrator" "Teacher Role" "Temporary Password" "Deleted User" "Change Teacher" "Change Car" "Inactive Student" "Roster"; do grep -q "^\*\*$t\*\*:" CONTEXT.md && echo "ok $t" || echo "MISSING $t"; done
```

Expected: ten `ok` lines.

- [ ] **Step 5: Push and open the PR**

```bash
git push -u origin 96-docs-users-and-roles
```

Then open the PR with `gh pr create --repo silagy/DrivingLessonsBooking --base 82-users-and-roles --head 96-docs-users-and-roles`. Title: `Docs: ADR 0006 for Users and Roles, requirements v1.5 and the glossary (#96)`. The body has:
- A **Summary**: one bullet per changed document.
- **Acceptance criteria**: the step 4 list, ticked, each with its quoted line.
- **Beyond the AC** (plan decision 9): README, `api-guidelines.md`, the CLAUDE.md Publication and Teacher bullets. Say they can be dropped on review.
- **Facts checked against the code** (task 1 step 2), including any place the docs follow the code rather than the spec: 404 for out-of-reach data, any change of your own Role refused.
- `Closes #96` and `Part of #82`. Say it won't auto-close on this base, so close #96 by hand after the merge.
- The PR-description attribution line from the session's instructions, at the end.

After it opens, follow the session's PR instructions (bind the PR, read its status). There is no CI job for docs-only changes beyond the existing workflows; report whatever they show.
