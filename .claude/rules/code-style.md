---
paths:
  - "**/*.cs"
---

# C# Code Style Guide

Rules for all C# code in this repository. Code must be self-documenting — **no comments**, ever. If code needs a comment to be understood, restructure it instead.

## File and Namespace Organization

- File name matches the primary type name (`Publication.cs` contains `Publication`)
- File-scoped namespaces: `namespace DrivingLessons.Domain;`
- Namespace matches folder structure
- `using` directives outside the namespace declaration
- Files end with a blank line

### Class Member Ordering
1. Nested classes, enums, delegates, events
2. Static, const and readonly fields
3. Fields and properties
4. Constructors
5. Methods

Within each group, order by access: public → internal → protected → private.

### Modifier Order
Access modifier → `static` → `new`/`virtual`/`abstract`/`sealed`/`override` → `readonly` → `async`.

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Classes | PascalCase | `PublicationRepository` |
| Interfaces | PascalCase with 'I' | `IPublicationRepository` |
| Methods | PascalCase | `GetPublication()` |
| Async methods | PascalCase + 'Async' | `GetPublicationAsync()` |
| Properties | PascalCase | `SubmissionWindow` |
| Parameters / variables | camelCase | `publicationId` |
| Private fields | camelCase, **no underscore** | `private readonly List<SlotRequest> slotRequests;` |
| Constants | PascalCase | `const int DefaultTimeout = 30;` |
| Type parameters | Start with 'T' | `TEntity` |
| Extension classes | End with 'Extension' | `QueryableExtension` |

- Meaningful, descriptive names; no abbreviations except well-known ones (ID, URL)
- Two-letter acronyms keep both letters the same case (`IOStream`); three+ letters capitalize only the first (`XmlDocument`)
- Never use `this.` unless required for disambiguation (constructor assigning a same-named field)

## Formatting Fundamentals

- 4 spaces indentation, never tabs
- Maximum 120 characters per line
- One statement per line, one assignment per statement
- Space after keywords (`if`, `for`, `while`) and commas; no spaces inside parentheses
- Braces always on new lines, required for **all** control structures — never single-line `if`
- Maximum one blank line between elements; no blank lines after `{` or before `}`
- Required blank line after block statements
- Blank line between logical blocks within methods (guards, mutations, events)

### Method and Property Chains

When a chain breaks across lines, use Rider-style column alignment — each `.member` aligns at the column right after the variable name; `??` aligns with the variable name column:

```csharp
var result = collection
                 .Where(x => x.IsActive)
                 .OrderBy(x => x.Title)
                 .ToList();

var publication = await repository.GetAsync(id)
                  ?? throw new PublicationNotFoundException(id);

var window = publication.SubmissionWindow;
```

A single dot may stay on one line. Never use simple 4-space indent for chains.

### Other Wrapping Rules

- Logical operators start the new line:
  ```csharp
  if ((a && b)
      || (c && d))
  ```
- Ternary expressions always multiline
- Long parameter lists wrap with each parameter on its own line
- Each array/object initializer element on its own line

## C# Language Usage

- **Always use `var`** for all declarations
- Pattern matching: `if (obj is string text)`
- Null checks: `value is null` / `value is not null` — never `== null`
- Boolean checks: `if (!value)` — never `== false`
- Empty checks: `!collection.Any()` — never `Count == 0`
- Null-safe operators: `text?.Length ?? 0`, `OnChanged?.Invoke(args)`
- Index/range operators: `array[^1]`, `array[1..^1]`
- Switch expressions for value mapping:
  ```csharp
  var slotCount = day switch
  {
      DayOfWeek.Friday => 2,
      DayOfWeek.Saturday => throw new NotSupportedException(),
      _ => 4
  };
  ```
- Throw expressions: `Name = name ?? throw new ArgumentNullException(nameof(name));`
- Compound assignment: `count += 5`
- Expression-bodied members OK for properties and single-line local functions

### Collections

- Method parameters: most restrictive type — `IReadOnlyCollection<T>` (count/iteration) or `IReadOnlyList<T>` (indexing)
- Properties must be read-only:
  ```csharp
  private readonly List<SlotRequest> slotRequests;
  public IReadOnlyCollection<SlotRequest> SlotRequests => slotRequests.AsReadOnly();
  ```
- `List<T>` for mutable internals, arrays for fixed-size data
- Don't enumerate an `IEnumerable` multiple times
- `TryGetValue()` instead of `ContainsKey()` + indexer

### Constants

- No magic numbers or strings — extract named `const` (or `readonly` when `const` is impossible)
- `string.Empty` over `""`, `Guid.Empty` over `new Guid()`, `Array.Empty<T>()` over `new T[0]`

## Method Design

- **No nested method calls** (except in tests) — assign intermediate results to variables:
  ```csharp
  var data = GetData();
  var result = Process(data);
  ```
- **No inline instantiation + method call** — assign `new T()` to a variable first, call its method on a separate statement
- **No deep property chains in arguments** (max 1 level) — extract to a local first:
  ```csharp
  var email = student.Profile.Email;
  SendMail(email);
  ```
  Lambda parameter chains are fine: `students.Where(x => x.Profile.IsActive)`
- **Extract repeated property access** into a local variable when accessed more than once
- **All method parameters must be used** — never accept a parameter and silently ignore it
- Static methods when no instance data is used
- Avoid `ref`/`out` — return tuples instead: `(bool Success, string Value) TryGetValue(string key)`
- Configuration objects for complex parameter sets

## Control Flow

- Early returns over nested conditions:
  ```csharp
  if (item is null)
  {
      return;
  }

  if (!item.IsValid)
  {
      return;
  }

  DoProcessing(item);
  ```
- Extract complex conditions into named booleans
- Parentheses for clarity in compound conditions: `if ((a && b) || (c && d))`
- Keep switch blocks short — delegate to methods

## Type Design

- Classes over structs (almost always)
- **No public setters or public `init` properties on domain types — use factory methods** with private constructors:
  ```csharp
  public class Teacher
  {
      public TeacherName Name { get; }

      private Teacher(TeacherName name)
      {
          Name = name;
      }

      public static Teacher Create(TeacherName name)
      {
          return new Teacher(name);
      }
  }
  ```
- Tuples sparingly (2–3 elements, internal use only)

## Exception Handling

- `nameof` for parameter references: `throw new ArgumentNullException(nameof(publication));`
- Rethrow correctly: `throw;` never `throw ex;`
- Never throw `Exception`, `SystemException`, or `ApplicationException` — use specific domain exception types

## Logging

- Log errors, important operations, and critical business events — not lifecycle noise, counts, or HTTP successes
- Message parameters in PascalCase brackets, message ends with a period:
  ```csharp
  logger.LogInformation("Publication [{PublicationId}] closed.", publicationId);
  ```
- Never log sensitive information or PII (student emails and names are PII)

## Security

- Parameterized queries only — never string-concatenated SQL
- Encode user input rendered back to clients
- Shareable links use unguessable tokens, never sequential identifiers

## Code Cleanliness

- Remove unused code: members, parameters, variables, usings
- Never comment out code — delete it
- Remove unnecessary casts
- Simplify boolean expressions (`return a > b;` not `if (a > b) return true; else return false;`)
