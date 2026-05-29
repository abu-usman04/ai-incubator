---
paths:
  - "**/*.cs"
  - "**/*.csx"
---
# C# Coding Conventions

Broad language-level rules for every `.cs` / `.csx` file in the repo.

> **See also:** Files under `src/api/` additionally follow
> [`api-conventions.md`](./api-conventions.md) (folder layout, controllers,
> repositories, HTTP error model).

## Naming

| Element                          | Case              | Example                          |
| -------------------------------- | ----------------- | -------------------------------- |
| Classes, methods, properties     | PascalCase        | `ProjectService`, `GetByIdAsync` |
| Private fields                   | `_camelCase`      | `_projectRepo`, `_logger`        |
| Local variables, parameters      | camelCase         | `userId`, `screenId`             |
| Constants                        | PascalCase        | `EarthRadius`, `MaxRetries`      |
| Interfaces                       | `I` + PascalCase  | `IProjectService`                |
| Async methods                    | PascalCase + `Async` | `CreateAsync`, `GetByIdAsync` |
| Enums                            | PascalCase, singular | `ScreenStatus`, `ListingType` |

No Hungarian notation. No abbreviations except the conventional acronyms
`Id`, `Xml`, `Uri`, `Url`, `Html` (PascalCase, not ALLCAPS).

## Code Style

- **File-scoped namespaces:** `namespace ThreadlineStudio.Api.Services.Projects;`
- **Primary constructor DI** (C# 12): `public class MyService(IDep dep) : IMyService`
- **`var` policy:** use `var` for non-primitives (types obvious from the RHS);
  use the explicit type for `int`, `string`, `bool`, `double` so numeric/string
  intent is readable at the call site.
- **Digit separators** for large numeric literals: `6_371_010`, not `6371010`.
- **`nameof()`** over string literals when referencing a member.
- **Named arguments** when the argument's meaning isn't obvious from context
  (e.g. `new Point(x: 0, y: 0)`, `DoThing(force: true)`).
- **Allman braces** — opening brace on its own line.

## Structure

### Regions

Use `#region` blocks to separate concerns inside non-trivial classes (services,
controllers, long-lived stateful classes). Skip regions for DTOs, records,
enums, and single-responsibility classes where they add noise.

Canonical top-level blocks:

```csharp
#region Private fields region
private readonly ConcurrentDictionary<string, bool> _blockIdDictionary = new();
#endregion

#region Public methods region
public void DoWork() { }
#endregion

#region Private methods region
private void InternalWork() { }
#endregion
```

Nested regions are allowed inside long method bodies when they clarify a block
(e.g. `#region Localize cities` around a self-contained loop).

## Patterns

- **Interface + implementation for all DI services** — `IFoo` + `Foo`, registered
  by DI, never instantiated directly with `new`.
- **`using var`** for disposable resources (DB connections, streams, HTTP
  clients) so lifetime is tied to the enclosing scope.
- **`ConcurrentDictionary<,>`** for any thread-shared in-memory cache. Never
  `Dictionary<,>` + `lock`.
- **No noise comments** — comment intent, not mechanics. If the code explains
  itself, omit the comment.
- **XML `/// <summary>`** on public API members when the behaviour or contract
  isn't obvious from the signature. Include `<remarks>` with a sample request
  on HTTP-facing endpoints.