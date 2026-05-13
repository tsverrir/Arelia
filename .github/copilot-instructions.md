# Arelia — Copilot Instructions

Multi-tenant web app for managing choirs and small organizations. Built on .NET 10, Blazor Server, MudBlazor, EF Core + SQLite, MediatR (CQRS), FluentValidation, ASP.NET Core Identity.

## Commands

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~<TestClassName>"   # single test class
dotnet run --project src/Arelia.Web

# Add an EF Core migration
dotnet ef migrations add <Name> -p src/Arelia.Infrastructure -s src/Arelia.Web
```

## Architecture

Four layers — each only depends on the layers above it in this list:

1. **Domain** (`src/Arelia.Domain`) — entities, enums, `BaseEntity`, `Result`/`Result<T>`. Zero dependencies.
2. **Application** (`src/Arelia.Application`) — MediatR CQRS handlers, DTOs, FluentValidation validators, `IAreliaDbContext` interface. Depends only on Domain.
3. **Infrastructure** (`src/Arelia.Infrastructure`) — `AreliaDbContext`, EF migrations, `TenantContext`, file storage, PDF export, notifications. Implements Application interfaces.
4. **Web** (`src/Arelia.Web`) — Blazor Server pages/components, `TenantService`, localization (`ILocalizer`). Depends on Application and Infrastructure.

## CQRS Pattern

Each use case lives in a single file: a `record` request + its handler class, both in the same file. Commands go in `Commands/`, queries in `Queries/`, under the relevant module folder in `src/Arelia.Application/`.

```csharp
// Command returning Result<T>
public record CreateFooCommand(string Name, Guid OrganizationId) : IRequest<Result<Guid>>;

public class CreateFooHandler(IAreliaDbContext context) : IRequestHandler<CreateFooCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateFooCommand request, CancellationToken cancellationToken)
    {
        // ...
        return Result.Success(entity.Id);
    }
}

// Query — DTOs defined in the same file
public record GetFooQuery(Guid OrganizationId) : IRequest<List<FooDto>>;
public record FooDto(Guid Id, string Name);

public class GetFooHandler(IAreliaDbContext context) : IRequestHandler<GetFooQuery, List<FooDto>>
{ ... }
```

- Commands return `Result` or `Result<T>` (from `Arelia.Domain.Common`).
- Queries return plain DTOs (no `Result` wrapper).
- **No repository pattern** — handlers call `IAreliaDbContext` directly.

## Multi-Tenancy

Every entity inheriting `BaseEntity` has:
- `OrganizationId` — automatically set by `SaveChangesAsync` from `ITenantContext` if left empty (`Guid.Empty`).
- `IsActive` — used as a soft-delete flag (global query filter excludes `IsActive == false`).

`AreliaDbContext` applies a **global query filter** on all `BaseEntity` subtypes that silently filters by both `IsActive` and `ITenantContext.CurrentOrganizationId`.

**Always use `IgnoreQueryFilters()` when you intentionally need to cross tenant boundaries or access inactive records**, then add an explicit `OrganizationId == ...` predicate:

```csharp
// Cross-org lookup (e.g. permission checks)
var member = await context.OrganizationUsers
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(ou => ou.UserId == userId && ou.OrganizationId == orgId, ct);
```

## BaseEntity

All domain entities inherit `BaseEntity`:

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    // DomainEvents collection (not persisted)
}
```

`SaveChangesAsync` auto-populates `CreatedAt`/`UpdatedAt`, `CreatedBy`/`UpdatedBy`, and writes an `AuditLogEntry` for every Added/Modified entity — no extra code needed.

`Person` additionally has `IsDeleted` (permanent delete, not the same as `IsActive`). Queries on persons must filter `!p.IsDeleted` explicitly after calling `IgnoreQueryFilters()`.

## Soft Delete vs Active Status

- `IsActive = false` → deactivated/hidden (filtered out globally). Reversible.
- `Person.IsDeleted = true` → permanently deleted. Always filter this manually in person queries.

## Validation

FluentValidation validators are picked up automatically by `ValidationBehavior<TRequest, TResponse>` in the MediatR pipeline. Register them in the same assembly and they fire before the handler.

Additional inline guard checks inside handlers (returning `Result.Failure(...)`) are used for **domain-level business rule violations** that go beyond input validation.

Use `InputValidation` helpers for common input sanitization:

```csharp
InputValidation.IsValidEmail(email)
InputValidation.NormalizeOptional(value)   // trims and returns null if whitespace
InputValidation.HasTextContent(html)
```

## Authorization

Pages use `[Authorize]` + `[RequirePermission(Permission.ManagePeople)]`. Available permissions are defined in `Arelia.Domain.Enums.Permission`.

`IPermissionService` resolves effective permissions for a user+org based on active `RoleAssignment` → `RolePermission` → `Permission` chains.

## Blazor Pages

- Pages inject `IMediator` (`@inject IMediator Mediator`) and send commands/queries directly.
- Inherit `TenantAwarePage` (instead of `ComponentBase`) when the page must reload data if the user switches tenants mid-session. Override `OnTenantChangedAsync` and call `RegisterTenantChangeHandler()` inside `OnInitializedAsync`.

```csharp
@inherits TenantAwarePage
// ...
protected override async Task OnInitializedAsync()
{
    RegisterTenantChangeHandler();
    await LoadData();
}

protected override async Task OnTenantChangedAsync()
{
    await LoadData();
    StateHasChanged();
}
```

## Localization

Inject `ILocalizer` (aliased `L` by convention). Supported cultures: `en` (default), `da`, `is`. String resources live in `src/Arelia.Web/Resources/SharedResource.resx` (and `.da.resx`, `.is.resx`).

```razor
@inject ILocalizer L

<MudText>@L["People.Title"]</MudText>           // plain string
<MudText>@L.H("People.Title")</MudText>         // MarkupString (shows highlight in admin mode if key missing)
<MudText>@L["Common.ItemCount", count]</MudText> // formatted
```

Always add new keys to all three `.resx` files.

## Testing

Tests use xUnit + FluentAssertions. Test projects mirror `src/` structure under `tests/`.

Handlers are tested by **instantiating them directly** (not via MediatR). Use `TestDbContextFactory.Create(orgId)` to get an in-memory `AreliaDbContext` with tenant filtering configured:

```csharp
var orgId = Guid.NewGuid();
await using var context = TestDbContextFactory.Create(orgId);
var handler = new CreateFooHandler(context);

var result = await handler.Handle(new CreateFooCommand("Bar", orgId), CancellationToken.None);

result.IsSuccess.Should().BeTrue();
```

## Naming Conventions

- PascalCase: classes, interfaces, methods, properties, events, constants
- camelCase: private fields and local variables; no underscores
- Interfaces start with `I`; async methods end with `Async`
- File-scoped namespaces; one type per file; namespace matches folder path
