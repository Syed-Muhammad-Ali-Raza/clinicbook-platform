# ClinicBook

ClinicBook is a production-grade, multi-clinic appointment booking platform. Patients can find providers, view real-time availability, book and pay for appointments, and receive reminders. Doctors manage schedules; clinic staff and admins manage operations.

> **Status:** Phase 1 (Foundation) scaffold in progress. This repository currently provides the solution structure, core building blocks, API bootstrap (Serilog, global exception handling, health checks), and local Docker infrastructure.

## Features (planned)

- Registration, login, email verification, password reset, optional MFA (TOTP)
- Provider profiles, specialties, services with duration and price
- Weekly schedules, breaks, time off, holidays
- Real-time slot generation and booking with hold/confirm flow
- Cancel/reschedule with policy rules and refunds
- Stripe payments with idempotent webhooks
- Email/SMS/in-app notifications and reminders (24h/2h)
- Admin dashboard, reports, and audit logging
- Medical notes and encrypted file attachments
- AI-ready extension points (future)

## Architecture

- **Style:** Clean Architecture + CQRS (MediatR), modular monolith
- **Layers:** Domain (core) → Application (use cases) → Infrastructure (EF Core, Redis, integrations) → API (HTTP)
- **Key patterns:** Result pattern, MediatR pipeline behaviors (logging, validation, authorization, caching, transaction, performance), domain events, outbox (planned), HttpOnly cookie auth (planned)

See [`docs/SPEC.md`](docs/SPEC.md) and [`docs/architecture.md`](docs/architecture.md).

## Tech Stack

| Layer | Choice |
| --- | --- |
| Backend | ASP.NET Core Web API (.NET 8), C# |
| Architecture | Clean Architecture + CQRS (MediatR) |
| Database | EF Core + PostgreSQL |
| Cache/locks | Redis |
| Validation | FluentValidation |
| Auth | ASP.NET Core Identity + JWT in HttpOnly cookies (Phase 2) |
| Background jobs | Hangfire (planned) |
| Real-time | SignalR (planned) |
| Payments | Stripe (planned) |
| Logging | Serilog + OpenTelemetry |
| API docs | Swagger / OpenAPI |
| Frontend | React + TypeScript + Vite (planned) |
| DevOps | Docker, docker-compose, GitHub Actions |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) (or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local infra)
- [Node.js 20+](https://nodejs.org/) (for frontend, later phases)

## Quick Start (Docker)

1. Copy environment template:
   ```bash
   cp .env.example .env
   ```
2. Start infrastructure and API:
   ```bash
   docker compose up --build
   ```
3. Endpoints (default local ports):
   - API: `http://localhost:8080`
   - Health (live): `http://localhost:8080/health/live`
   - Health (ready): `http://localhost:8080/health/ready`
   - Swagger (dev): `http://localhost:8080/swagger`
   - Seq: `http://localhost:5341`
   - MailHog: `http://localhost:8025`
   - PostgreSQL: `localhost:5432`
   - Redis: `localhost:6379`

> Note: The web frontend container is a placeholder until Phase 10.

## Local Development (without Docker for API)

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ClinicBook.Api/ClinicBook.Api.csproj
```

Ensure PostgreSQL and Redis are running and `ConnectionStrings__Default` / `Redis__Connection` are configured (via environment variables or user-secrets).

## Configuration

Key configuration values (see `.env.example` for full list):

| Variable | Description | Example |
| --- | --- | --- |
| `ConnectionStrings__Default` | PostgreSQL connection string | `Host=postgres;Port=5432;Database=clinicbook;...` |
| `Redis__Connection` | Redis connection string | `redis:6379` |
| `Jwt__Issuer` / `Jwt__Audience` | JWT token validation settings | `https://localhost:5001` / `clinicbook-api` |
| `Jwt__KeyPath` | Path to signing key (Key Vault/secret in prod) | `/run/secrets/jwt-signing-key.pem` |
| `Stripe__SecretKey` / `Stripe__WebhookSecret` | Stripe API credentials | `sk_test_...` / `whsec_...` |
| `Smtp__Host` / `Smtp__Port` | Email provider settings | `mailhog` / `1025` |
| `Cors__AllowedOrigins` | Allowed frontend origins (comma-separated) | `http://localhost:5173` |
| `Seq__ServerUrl` | Seq log sink URL | `http://seq:5341` |

**Never commit real secrets.** Use user-secrets locally and a secret manager/Key Vault in production.

## Running Tests

```bash
dotnet test
```

Test projects:
- `tests/ClinicBook.Domain.Tests` – domain rules and value objects
- `tests/ClinicBook.Application.Tests` – handlers, validators, behaviors
- `tests/ClinicBook.Infrastructure.Tests` – persistence/integration (Testcontainers)
- `tests/ClinicBook.Api.IntegrationTests` – API and security tests

## API Docs

- Swagger UI (development): `/swagger`
- OpenAPI spec: `/swagger/v1/swagger.json`
- Health: `/health/live`, `/health/ready`

## Security Summary

- HttpOnly, Secure, SameSite cookies for tokens (no localStorage) – Phase 2
- Deny-by-default authorization, RBAC/policy/resource checks – planned
- Strict validation, parameterized queries, secure headers, rate limiting – planned/in progress
- Audit logging and secret management via env/Key Vault
- See [`SECURITY.md`](SECURITY.md) and [`docs/security.md`](docs/security.md)

## Project Structure

```
clinicbook/
  .github/                  CI/CD, templates
  docs/                     Specs, architecture, ADRs
  docker/                   Dockerfiles, nginx config
  src/
    ClinicBook.Domain/      Entities, value objects, domain events, rules
    ClinicBook.Application/ Use cases (CQRS), behaviors, interfaces
    ClinicBook.Infrastructure/ EF Core, Redis, integrations
    ClinicBook.Api/         HTTP API, middleware, composition root
    clinicbook-web/         React frontend (planned)
  tests/                    Unit, integration, API tests
  docker-compose.yml
  AGENTS.md                 Implementation spec for agents
```

## Roadmap

Implementation follows the phases in [`AGENTS.md`](AGENTS.md):
1. Foundation (current)
2. Identity & security
3. Provider & scheduling management
4. Slot engine
5. Booking flow
6. Payments
7. Jobs & notifications
8. Medical notes & files
9. Admin dashboard & reports
10. Frontend
11. Hardening
12. Production
13. AI integration

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md). Use Conventional Commits and ensure build/tests/lint pass before opening a PR.

## License

See [`LICENSE`](LICENSE).

## Contact

- Security issues: see [`SECURITY.md`](SECURITY.md)
- Project issues: use the repository issue tracker
