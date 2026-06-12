# Task 1 of 11: Repo hygiene — .gitignore + .editorconfig

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). No prior tasks required — this is the first task. Work on branch `2-us-01-admin-signs-in-with-email-and-password`, commands run from the repo root.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication — while scaffolding the full greenfield skeleton every later slice builds on.

**Tech Stack:** .NET 10 / C# 14, EF Core 10 + Npgsql, ASP.NET Core JwtBearer, Angular 21+ (zoneless, standalone, signals only), PrimeNG + @primeuix/themes, Transloco (he/en, RTL-first), Docker Compose (postgres:17 + multi-stage app image). Root namespace: `DrivingLessons`.

**User decisions (locked):** JWT bearer · credentials seeded from config (env vars in Docker) · no tests this slice · full skeleton scaffolding.

---

**Files:**
- Create: `.gitignore` (via `dotnet new gitignore`, then append)
- Create: `.editorconfig`

- [ ] **Step 1: Generate .gitignore and append Node/Angular/env entries**

```
dotnet new gitignore
```

Append to `.gitignore`:

```gitignore

# Node / Angular
node_modules/
client/dist/
client/.angular/

# Environment
.env
```

- [ ] **Step 2: Create `.editorconfig`**

```editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
indent_size = 4
trim_trailing_whitespace = true

[*.{ts,html,scss,json,yml,yaml}]
indent_size = 2

[*.cs]
dotnet_sort_system_directives_first = true
csharp_style_namespace_declarations = file_scoped:warning
csharp_style_var_when_type_is_apparent = true:suggestion
```

- [ ] **Step 3: Commit**

```bash
git add .gitignore .editorconfig
git commit -m "chore: gitignore and editorconfig"
```

---

**Next:** [task-02-solution-skeleton.md](task-02-solution-skeleton.md)
