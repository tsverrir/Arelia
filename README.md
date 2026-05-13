# Arelia

Multi-tenant web application for managing choirs and small organizations.

## Features

- **Multi-tenant** — each organization is fully isolated
- **People & roles** — members, role assignments, role history
- **Activities** — semesters, rehearsals, concerts, trips, events
- **Rehearsal scheduling** — recurrence templates with manual overrides
- **Attendance tracking** — expectations, RSVP, actual attendance
- **Finance** — membership fees, payments, credits, expenses, reports
- **Documents** — file attachments per activity
- **Notifications** — in-app notification system
- **Reports** — attendance, membership, and financial summaries with CSV export

## Tech Stack

| Layer | Technology |
|---|---|
| UI | Blazor Server, MudBlazor |
| Application | MediatR (CQRS), FluentValidation |
| Persistence | EF Core, SQLite |
| Auth | ASP.NET Core Identity |
| Hosting | Docker |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (see `global.json`)
- A code editor (Visual Studio, Rider, or VS Code with C# Dev Kit)

## Getting Started

```bash
# Clone the repository
git clone https://github.com/sverrir-asmundsson/Arelia.git
cd Arelia

# Restore and build
dotnet build

# Run the web app
dotnet run --project src/Arelia.Web

# Run tests
dotnet test
```

The app creates an SQLite database automatically on first run.

## Project Structure

```
src/
  Arelia.Domain/          Domain entities, enums, value objects
  Arelia.Application/     MediatR commands, queries, DTOs (CQRS)
  Arelia.Infrastructure/  EF Core DbContext, migrations, services
  Arelia.Web/             Blazor Server UI, components, pages

tests/
  Arelia.Domain.Tests/
  Arelia.Application.Tests/
  Arelia.Infrastructure.Tests/
```

### Application Layer Modules

| Module | Responsibility |
|---|---|
| Activities | Semesters, rehearsals, concerts, trips, events |
| Admin | Audit log |
| Attendance | Attendance recording and queries |
| Documents | File/document management |
| Finance | Charges, payments, credits, expenses, expense categories |
| Notifications | In-app notifications |
| Organizations | Tenant management, user membership, language preferences |
| People | Person CRUD and queries |
| Rehearsals | Recurrence templates and rehearsal generation |
| Reports | Attendance, membership, and financial reports |
| Roles | Domain roles and role assignments |
| Rsvp | RSVP for activities |
| VoiceGroups | Voice group management |

## Architecture

The codebase follows **Clean Architecture** with four layers:

1. **Domain** — entities, enums, and domain logic (no dependencies)
2. **Application** — use cases as MediatR handlers; depends only on Domain
3. **Infrastructure** — EF Core, external services; implements Application interfaces
4. **Web** — Blazor Server UI; depends on Application and Infrastructure

All entities are scoped to an `OrganizationId` for tenant isolation, enforced by EF Core global query filters.

## Documentation

| Document | Description |
|---|---|
| [docs/docker.md](docs/docker.md) | Local development, production deployment, Watchtower, Cloudflare Tunnel |
| [docs/email.md](docs/email.md) | Transactional email setup via Resend |
| [docs/SPECIFICATION.md](docs/SPECIFICATION.md) | Full product specification |

## Docker

See [docs/docker.md](docs/docker.md) for local development, production deployment, Watchtower auto-updates, and Cloudflare Tunnel setup.

## Email

See [docs/email.md](docs/email.md) for configuring transactional email via Resend.

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make your changes and add tests
4. Run `dotnet test` to verify
5. Submit a pull request

See [AGENTS.md](AGENTS.md) for coding conventions and architecture guidance.

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
