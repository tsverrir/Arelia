# Arelia — Assumptions & Decisions Log

_Decisions made during implementation that weren't explicitly covered by the spec or review._

---

## Phase 1 — Project Scaffolding

### A1 — .NET 9 target framework
Using .NET 9 as the target framework for all projects. The system has .NET 9 SDK installed (latest current release).

### A2 — Built-in logging (not Serilog)
Using ASP.NET Core's built-in `ILogger` for now. Serilog can be added later if needed. Keeps dependencies minimal for Phase 1.

### A3 — MediatR for CQRS
Using MediatR for the command/query pattern in the Application layer. This keeps the Application layer decoupled from infrastructure concerns and makes it testable.

### A4 — FluentValidation for input validation
Using FluentValidation for command/query validation in the Application layer. Integrates well with MediatR pipeline behaviors.

### A5 — xUnit + FluentAssertions for testing
Using xUnit as the test framework and FluentAssertions for assertion readability. NSubstitute will be added when mocking is needed.

### A6 — Interactive Server render mode
Using Blazor Server with Interactive Server render mode (not WASM). All rendering happens on the server, which is appropriate for this app's scale and the need for real-time database access.

### A7 — Global query filters for tenant isolation
EF Core global query filters on `OrganizationId` provide tenant isolation. The `Organization` entity itself is excluded from this filter (it has no parent org). Entities that are not org-scoped (like `ApplicationUser`) are also excluded.

### A8 — GUID primary keys
Using `Guid` for all entity IDs. Avoids sequential ID exposure and works well for multi-tenant systems. Generated server-side with `Guid.NewGuid()`.

### A9 — Blazor Server with Individual auth template
Creating the Blazor project with the `blazorserver` template and Individual authentication. This provides the scaffolded Identity pages out of the box.

### A10 — Project naming convention
- `Arelia.Domain` — No dependencies on other projects or external frameworks (pure C#)
- `Arelia.Application` — References Domain only. Contains MediatR handlers, DTOs, interfaces.
- `Arelia.Infrastructure` — References Application + Domain. Contains EF Core, Identity, email, file storage.
- `Arelia.Web` — References all other projects. The Blazor Server host.
