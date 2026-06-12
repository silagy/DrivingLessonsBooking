# Task 3 of 11: Exception base types + global exception filter (PRD-mandated)

> Part of [US-01: Admin Signs In](README.md) ([parent plan](../us-01-admin-sign-in-plan.md), GitHub issue #2). Requires tasks 1–2 complete. Work on branch `2-us-01-admin-signs-in-with-email-and-password`, commands run from the repo root.

## Shared Context

**Goal:** Implement US-01 — the single school-owner admin signs in with email + password and reaches an admin area unreachable without authentication.

**Architecture:** Modular monolith, DDD, Onion. The PRD fixes the architecture rules: controllers with zero logic delegating to thin interactors, a global exception filter mapping domain violations to 409. Root namespace: `DrivingLessons`.

**Onion references:** `Presentation.Web → Application + Infrastructure (DI only)`, `Infrastructure → Application`, `Application → Domain`. Domain references nothing.

**User decisions (locked):** JWT bearer · credentials seeded from config · no tests this slice.

---

**Files:**
- Create: `src/DrivingLessons.Domain/Common/DomainException.cs`
- Create: `src/DrivingLessons.Application/Common/Exceptions/NotFoundException.cs`
- Create: `src/DrivingLessons.Application/Common/Exceptions/AuthenticationFailedException.cs`
- Create: `src/DrivingLessons.Presentation.Web/Filters/ApiExceptionFilter.cs`

- [ ] **Step 1: `DomainException.cs`**

```csharp
namespace DrivingLessons.Domain.Common;

/// <summary>Base type for domain rule violations. Mapped to HTTP 409 by the API exception filter.</summary>
public abstract class DomainException(string message) : Exception(message);
```

- [ ] **Step 2: `NotFoundException.cs`**

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

/// <summary>Requested resource does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);
```

- [ ] **Step 3: `AuthenticationFailedException.cs`**

```csharp
namespace DrivingLessons.Application.Common.Exceptions;

/// <summary>Login failed. Deliberately carries no detail about which credential was wrong. Mapped to HTTP 401.</summary>
public sealed class AuthenticationFailedException() : Exception("Invalid credentials.");
```

- [ ] **Step 4: `ApiExceptionFilter.cs`**

```csharp
using DrivingLessons.Application.Common.Exceptions;
using DrivingLessons.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrivingLessons.Presentation.Web.Filters;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusCode, title) = context.Exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            DomainException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
        {
            return; // unknown exception: let the default 500 pipeline handle it
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = context.Exception.Message
        })
        { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
```

- [ ] **Step 5: Build + commit**

Run: `dotnet build` — expected: success.

```bash
git add src
git commit -m "feat(api): exception base types and global exception filter"
```

---

**Next:** [task-04-application-auth.md](task-04-application-auth.md)
