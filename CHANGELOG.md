# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Phase 1 (Foundation): initial solution structure following Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`) with test projects.
- Central package management (`Directory.Packages.props`), shared build props, `.editorconfig`, and repository hygiene files.
- Domain building blocks: `BaseEntity`, `AuditableEntity`, `ValueObject`, `IDomainEvent`, `Result`/`Error`, domain exceptions.
- Application building blocks: CQRS abstractions, repository/specification/UoW interfaces, `PagedQuery`/`PagedResult`/`ApiResponse`, validation/authorization exceptions, and MediatR pipeline behaviors (logging, validation, authorization, caching, transaction, performance).
- Infrastructure scaffolding: EF Core `AppDbContext`, generic repository, unit of work, domain event dispatcher, system clock/current user stubs, DI registration.
- API bootstrap: Serilog logging, correlation ID and global exception handling middleware (ProblemDetails), health checks (`/health/live`, `/health/ready`), Swagger (development).
- Docker Compose environment with API, PostgreSQL, Redis, Seq, and MailHog plus Dockerfiles and nginx config.
- Documentation: `AGENTS.md`, `README.md`, `SECURITY.md`, `CONTRIBUTING.md`, `.env.example`, and initial docs/spec placeholders.
- Phase 2 (Identity & security, in progress): ASP.NET Core Identity integration with cookie-based JWT access/refresh flow, refresh token rotation with family-based reuse detection, CSRF/origin protection, rate limiting policies, security headers, MFA (TOTP) endpoints/setup, and audit logging for auth actions.
- Auth API endpoints under `/api/v1/auth` (register, login, refresh, logout, verify/reset password, current user, MFA) with MediatR handlers/validators and initial EF Core identity migration.
- API integration tests for core auth flows (cookies, refresh rotation/reuse detection, CSRF enforcement, authz checks, and rate limiting) using an in-memory test host; auth API documentation updates.
