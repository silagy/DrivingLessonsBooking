# Task 3 of 8: CI workflow

> Part of [#74: Production Deployment](README.md). Requires tasks 1–2 committed. Work on branch `74-production-deployment`.

**Why:** Decision 10. Nothing runs the tests today except a developer's memory. `ci.yml` runs them on every PR and every push to `main`, and is **reusable** (`workflow_call`) so task 7's deploy runs exactly the same checks before it builds an image.

**Files:**
- Create: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: `DrivingLessons.sln` (both test projects), `client/package-lock.json`, the client `test` and `build` scripts.
- Produces: workflow `CI` with jobs `backend` and `client`; callable as `uses: ./.github/workflows/ci.yml` with input `ref` (string, optional — the commit to test; empty means the triggering commit). Task 7 calls it.

Facts checked at planning time: the .NET SDK is 10.0 (`net10.0`, local SDK 10.0.203); no test touches Postgres (no `UseNpgsql`/Testcontainers under `tests/`); the client tests use the Angular `unit-test` builder with Vitest and `vitest-base.config.ts` (pins `TZ=America/New_York`); the Dockerfile builds the client on `node:24-alpine`.

- [ ] **Step 1: Write the workflow**

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  pull_request:
  push:
    branches: [main]
  workflow_call:
    inputs:
      ref:
        description: Commit, branch or tag to test (empty = the triggering commit)
        type: string
        required: false
        default: ''

permissions:
  contents: read

jobs:
  backend:
    name: Backend tests
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ inputs.ref }}

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Test
        run: dotnet test DrivingLessons.sln --configuration Release

  client:
    name: Client tests and build
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: client
    steps:
      - uses: actions/checkout@v4
        with:
          ref: ${{ inputs.ref }}

      - uses: actions/setup-node@v4
        with:
          node-version: 24
          cache: npm
          cache-dependency-path: client/package-lock.json

      - name: Install
        run: npm ci

      - name: Test
        run: npx ng test --watch=false

      - name: Build
        run: npm run build
```

(`inputs.ref` is empty for `pull_request`/`push`, and `actions/checkout` treats an empty `ref` as "the triggering commit".)

- [ ] **Step 2: Lint the YAML locally**

```bash
docker run --rm -v "$(pwd -W 2>/dev/null || pwd)":/repo -w /repo rhysd/actionlint:latest -color .github/workflows/ci.yml
```

Expected: no output, exit code 0. (If Docker cannot mount the path on this machine, skip — Step 4 is the real check.)

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: run backend and client tests on every pull request and push to main

Reusable through workflow_call so the deploy runs the same checks.

Refs #74

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 4: See it run on GitHub**

Push the branch (first push of this slice):

```bash
git push -u origin 74-production-deployment
```

There is no PR yet (task 7 opens it), so `pull_request` does not fire. Open a **draft** PR now so CI runs on every later push, and task 7 turns it into the real PR:

```bash
gh pr create --draft --base main --head 74-production-deployment \
  --title "Deploy v1 to production on AWS Lightsail" \
  --body "Draft - work in progress on #74. Plan: docs/modules/deployment/us-74-production-deployment-plan/README.md"
```

In the Claude desktop app, bind the PR with the `ccd_pr` tools and read its CI from there (do not poll in a loop); elsewhere, run `gh pr checks` once the run has had time to finish.

Expected: `Backend tests` and `Client tests and build` both pass. If `Client tests and build` fails only in CI, the usual cause is a lockfile drift — run the v22.6.0 `npm ci` locally (task 2 header) and compare; never "fix" by switching CI to `npm install`.
