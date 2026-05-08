# Arelia — Contributor Guide

Instructions for AI agents and human contributors working on this codebase.

## Quick Reference

```bash
dotnet build                                         # Build all projects
dotnet test                                          # Run all tests
dotnet run --project src/Arelia.Web                  # Run the web app
dotnet ef migrations add <Name> -p src/Arelia.Infrastructure -s src/Arelia.Web  # Add a migration
```

## Architecture

Four-layer Clean Architecture: **Domain → Application → Infrastructure → Web**.

- **Domain** — entities, enums, value objects. Zero dependencies.
- **Application** — MediatR CQRS handlers (commands + queries). Depends only on Domain.
- **Infrastructure** — EF Core `AreliaDbContext`, migrations, external services. Implements Application interfaces.
- **Web** — Blazor Server UI with MudBlazor components.

## Key Conventions

- **Multi-tenant**: every entity has `OrganizationId`. EF Core global query filters enforce isolation. Never bypass without `IgnoreQueryFilters()` + explicit org check.
- **CQRS via MediatR**: each use case is a `IRequest<T>` record + handler. Commands return `Result`/`Result<T>`. Queries return DTOs.
- **No repository pattern**: handlers use `IAreliaDbContext` directly.
- **File-scoped namespaces**: one type per file, namespace matches folder path.
- **Validation**: `FluentValidation` validators + `ValidationBehavior<TRequest, TResponse>` pipeline.
- **Testing**: xUnit + FluentAssertions. Tests go in `tests/` mirroring `src/` structure.

## Application Layer Modules

Handlers are organized by domain concept:

| Module | Contents |
|---|---|
| `Activities/` | Semesters, rehearsals, concerts, trips, events |
| `Admin/` | Audit log |
| `Attendance/` | Attendance recording |
| `Documents/` | Document management |
| `Finance/` | Charges, payments, credits, expenses, expense categories |
| `Notifications/` | In-app notifications |
| `Organizations/` | Tenant management, user membership, language preferences |
| `People/` | Person CRUD |
| `Rehearsals/` | Recurrence templates and generation |
| `Reports/` | Attendance, membership, financial reports |
| `Roles/` | Domain roles and assignments |
| `Rsvp/` | Activity RSVP |
| `VoiceGroups/` | Voice group management |

## Full Specification

See [docs/SPECIFICATION.md](docs/SPECIFICATION.md) for the complete product specification.