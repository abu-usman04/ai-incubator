---
paths:
  - "src/api/**/*.cs"
  - "src/api/**/*.csproj"
---
# C# API Conventions

Rules specific to files under `src/api/`.

> **Extends** [`csharp-conventions.md`](./csharp-conventions.md). That file
> owns all language-level rules (naming, code style, regions, DI pattern).
> This file only defines API-layer concerns: folder layout, HTTP surface,
> services, repositories, error model, and model placement.

## Folder Structure

```
Controllers/              # API controllers
Common/
  Attributes/             # Custom attributes + conventions
  Enums/                  # Enum types
  Exceptions/             # Custom exception classes
  Extensions/             # Extension methods
  Helpers/
    Configurations/       # DI registration helpers
    Converters/           # JSON converters
    ServiceMarkers/       # Marker interfaces (IScopedService, etc.)
  Middlewares/            # ASP.NET middleware
  Models/
    Entities/             # Persistence entities
    RequestModels/        # API request bodies, grouped by domain
Services/
  Caching/                # In-memory caching services
  Workers/                # BackgroundService implementations
Persistence/              # Dapper repositories, organized by DB type
```

- No BLL / Data / Presentation layer split. Organize by **feature/concern**
  inside each top-level folder.
- PascalCase folder names.

## Controllers

```csharp
[ApiController]
[Route("api/kebab-case/")]
[ControllerName(name: "kebab-case")]
public class SomethingController(
    ISomeService someService,
    ILogger<SomethingController> logger) : ControllerBase
{
    // ...
}
```

- **Route** and **controller name:** lowercase `kebab-case`.
- `[ProducesResponseType]` on every endpoint that returns data, for every
  documented status code.
- XML `<summary>` + `<remarks>` (with a sample request body) on every public
  endpoint.
- **Never catch exceptions in controllers.** Let `ExceptionMiddleware` handle
  them — see [Error Model](#error-model).

## Services

- **Throw `AppException`, never return error objects.** See [Error Model](#error-model).
- **Authorization lives in the service layer** — check workspace membership
  before performing any workspace-scoped operation.
- **IDs:** `IdGenerator.NewCuid()` for every new entity ID.
- **Timestamps:** `DateTime.UtcNow` — never `DateTime.Now`.

## Repositories

- Inject `IDbConnectionFactory`; open with `dbFactory.CreateConnection()`
  inside a `using var` so the connection is disposed on scope exit.
- Dapper only. Allowed methods: `QueryAsync<T>`,
  `QuerySingleOrDefaultAsync<T>`, `ExecuteAsync`.
- Parameters are **always named**: `new { Id = id, UserId = userId }` — no
  positional parameters, no string interpolation into SQL.
- Alias columns to the C# property name in the SELECT list:
  `"column_name" "PropertyName"`.

```csharp
public async Task<Project?> GetByIdAsync(string id)
{
    using var conn = _dbFactory.CreateConnection();
    return await conn.QuerySingleOrDefaultAsync<Project>(
        """
        SELECT  "id"         "Id",
                "workspace_id" "WorkspaceId",
                "name"       "Name",
                "created_at" "CreatedAt"
        FROM    projects
        WHERE   "id" = @Id
        """,
        new { Id = id });
}
```

## Error Model

Services throw `AppException(statusCode, message, code)`. `ExceptionMiddleware`
catches every `AppException` and formats the HTTP response — so **every**
error path flows through this single funnel.

Canonical error codes (use these; do not invent new ones without updating this
table):

| Code             | HTTP status | When to use                                   |
| ---------------- | ----------- | --------------------------------------------- |
| `NOT_FOUND`      | 404         | Requested resource does not exist / not visible |
| `UNAUTHORIZED`   | 401         | No valid authentication                       |
| `FORBIDDEN`      | 403         | Authenticated, but not allowed                |
| `BAD_REQUEST`    | 400         | Caller-supplied input is invalid              |
| `CONFLICT`       | 409         | State conflict (duplicate, stale version)     |
| `INTERNAL_ERROR` | 500         | Unexpected server-side failure                |

## Models

- **Entities** live in `Common/Models/Entities/` — one type per file,
  PascalCase properties.
- **Request DTOs** live in `Common/Models/RequestModels/<Domain>/` and are named
  `CreateXxxRequest`, `UpdateXxxRequest`, `XxxFilter`, etc. — one type per file.
- **Responses** are wrapped in `ApiResponse<T>` (from `Common/ApiResponse.cs`).
- **Do not use the `Dto` suffix** on any type. The folder already conveys the
  role (`Entities/`, `RequestModels/`), so a suffix is redundant noise.