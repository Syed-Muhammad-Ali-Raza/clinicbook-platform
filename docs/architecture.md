# ClinicBook Architecture

## Overview

ClinicBook is built as a **modular monolith** using **Clean Architecture** and **CQRS** (MediatR). Module boundaries (Identity, Scheduling, Booking, Payments, Notifications) are explicit to allow future extraction into services if needed.

## Layers and Dependency Rule

- **Domain (`ClinicBook.Domain`)**: entities, value objects, domain events, domain rules/exceptions. No dependencies on other layers or frameworks.
- **Application (`ClinicBook.Application`)**: use cases (commands/queries/handlers), validators, DTOs, interfaces/ports, pipeline behaviors, cross-cutting abstractions. Depends on Domain.
- **Infrastructure (`ClinicBook.Infrastructure`)**: EF Core/PostgreSQL persistence, Identity integration, Redis, messaging, storage, external providers (Stripe, email/SMS), job infrastructure. Depends on Application (and implements its interfaces).
- **API (`ClinicBook.Api`)**: HTTP endpoints, middleware, authentication/authorization wiring, DI composition root, SignalR hubs. Depends on Application and Infrastructure.

**Rule:** dependencies point inward only. Domain depends on nothing.

## Request Pipeline

MediatR pipeline behaviors (in order): Logging → Validation → Authorization → Caching (queries) → Transaction (commands) → Performance monitoring.

Controllers are thin and translate HTTP requests into commands/queries; no business logic in controllers.

## Cross-Cutting Concerns

- **Error handling:** global exception middleware returns RFC 7807 ProblemDetails (no stack traces in production).
- **Validation:** FluentValidation for all requests.
- **Observability:** Serilog structured logging with correlation IDs, OpenTelemetry (traces/metrics), health checks.
- **Persistence:** EF Core with PostgreSQL, repositories/specifications, Unit of Work as commit boundary, domain event dispatch after save.
- **Security:** HttpOnly cookie-based auth (planned Phase 2), deny-by-default authorization, strict input/output handling.
- **Reliability patterns (planned):** outbox for domain events/integrations, idempotent handlers/webhooks, background jobs via Hangfire.

## Runtime Components (local/dev)

- ASP.NET Core API
- PostgreSQL (primary data store)
- Redis (cache/locks/backplane)
- Seq (log sink)
- MailHog (dev email capture)
- Nginx-served web placeholder (frontend in Phase 10)

See [`SPEC.md`](SPEC.md) and root [`AGENTS.md`](../AGENTS.md) for detailed requirements.
