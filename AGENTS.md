# ClinicBook: Clinic Appointment Booking Platform
Architecture and Implementation Specification (for OpenCode IDE) | Version 1.0 | November 19, 2026 | Author role: Senior Software Architect

## 0. How to use this document (instructions for OpenCode)
You are building ClinicBook, a production-grade clinic appointment booking platform. Follow this document strictly.
- Build in the phase order given in Section 18. Do not skip ahead.
- Never put business logic in controllers. Controllers only translate HTTP to a MediatR command or query.
- Never store tokens in localStorage or sessionStorage. Tokens live only in HttpOnly cookies.
- Every new feature needs: validator, handler, unit test, and an API doc entry.
- Keep the AI extension points in Section 16 intact so AI can be added later without refactoring.
- Before finishing any phase, run build, tests, and the lint/format checks. Fix all warnings.
- Save this file in the repo root as AGENTS.md (or docs/SPEC.md) so every session reads it.

## 1. Product overview
**Goal:** a multi-clinic platform where patients find a doctor, see real-time availability, book and pay for an appointment, and receive reminders. Doctors manage schedules; admins manage the platform.

**Roles:** Admin, ClinicManager, Doctor, Receptionist, Patient.

**Core features**
- Registration, login, email verification, password reset, optional MFA (TOTP)
- Doctor profiles, specialties, services with duration and price
- Weekly working schedules, breaks, time off, holidays
- Slot generation from schedule minus bookings minus time off
- Booking flow: search, select slot, temporary hold, pay, confirm
- Cancel and reschedule with policy rules and refunds
- Stripe payments with idempotent webhooks
- Email, SMS, and in-app real-time notifications (SignalR)
- Reminders 24h and 2h before the appointment
- Admin dashboard and reports (revenue, no-shows, utilization)
- Audit log of every sensitive action
- Medical notes and file attachments (encrypted, access-controlled)

**Non-functional goals:** secure by default, horizontally scalable, observable, testable, AI-ready.

## 2. Technology stack
| Layer | Choice |
| --- | --- |
| Backend | ASP.NET Core Web API, .NET 8 LTS or later, C# |
| Architecture | Clean Architecture + CQRS with MediatR |
| ORM / DB | EF Core + PostgreSQL (SQL Server is an acceptable alternative) |
| Validation | FluentValidation |
| Mapping | Mapster or manual mappers (no magic) |
| Auth | ASP.NET Core Identity + JWT in HttpOnly cookies + rotating refresh tokens |
| Cache / locks | Redis |
| Background jobs | Hangfire (Redis or PostgreSQL storage) |
| Real-time | SignalR |
| Payments | Stripe |
| Email / SMS | SMTP provider or SendGrid / Twilio behind interfaces |
| Logging | Serilog + OpenTelemetry |
| Docs | Swagger / OpenAPI |
| Tests | xUnit, Moq or NSubstitute, FluentAssertions, Testcontainers, Respawn |
| Frontend | React + TypeScript + Vite, TanStack Query, React Hook Form + Zod |
| DevOps | Docker, docker-compose, GitHub Actions, Azure (or any cloud) |

## 3. System architecture
```
[ Browser (React) ]
      | HTTPS (cookies: HttpOnly, Secure, SameSite)
[ Reverse proxy / WAF / TLS (Nginx, Azure Front Door) ]
      |
[ ASP.NET Core API ] ---- SignalR hub (real-time)
      |
  Application layer (MediatR: commands, queries, validators, behaviors)
      |
  Domain layer (entities, value objects, domain events, rules)
      |
  Infrastructure layer (EF Core, Redis, Stripe, Email, SMS, Storage, Hangfire)
      |
[ PostgreSQL ]  [ Redis ]  [ Blob storage ]  [ Stripe ]  [ Email/SMS ]
```

**Dependency rule:** Domain depends on nothing. Application depends on Domain. Infrastructure and API depend on Application. Dependencies only point inward.

**Request pipeline (MediatR behaviors, in order):** Logging, Validation, Authorization, Caching (queries only), Transaction (commands only), Performance monitoring.

**Style decisions**
- Modular monolith first. Each module (Identity, Scheduling, Booking, Payments, Notifications) has clear boundaries so it can become a microservice later.
- Domain events (for example AppointmentConfirmed) are published after commit and consumed by notification and audit handlers. This keeps modules decoupled.
- Outbox pattern for reliable events and emails (see Section 11).

## 4. Repository and folder structure
See repository scaffold under `src/`, `tests/`, `docs/`, `docker/`, `.github/`.

Use central package management (Directory.Packages.props), nullable reference types, and TreatWarningsAsErrors in Directory.Build.props.

## 5. Domain model and database
Tables: Users, Roles, UserRoles, RefreshTokens, Clinics, Providers, Specialties, ProviderSpecialties, Services, WorkingSchedules, ScheduleBreaks, TimeOffs, Holidays, Patients, Appointments, SlotHolds, Payments, Refunds, PaymentWebhookEvents, Notifications, NotificationPreferences, MedicalNotes, Attachments, AuditLogs, OutboxMessages.

Rules for every table
- Id is a GUID (UUIDv7 if available). All timestamps are UTC (CreatedAtUtc, UpdatedAtUtc).
- Soft delete (IsDeleted, DeletedAtUtc) with a global query filter on patient and clinical data.
- Row version column (xmin in PostgreSQL or rowversion in SQL Server) for optimistic concurrency.
- Foreign keys with explicit delete behavior. Indexes on every foreign key and every search column.
- Money stored as integer minor units plus ISO currency code.

Critical constraints
- Unique filtered index on Appointments (ProviderId, StartUtc) where Status is Pending or Confirmed. This is the last line of defense against double booking.
- Unique index on PaymentWebhookEvents (StripeEventId) for idempotency.
- Unique index on RefreshTokens (TokenHash).

Appointment status machine: Held -> PendingPayment -> Confirmed -> Completed; also Confirmed -> Cancelled, Confirmed -> NoShow, Held -> Expired. Enforce transitions inside the entity (methods like Confirm(), Cancel(reason)), never by setting the property directly.

## 6. Reusable code building blocks
Build these first. Everything else uses them.

### Base entity and auditing
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    private readonly List<IDomainEvent> _events = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events;
    protected void Raise(IDomainEvent e) => _events.Add(e);
    public void ClearEvents() => _events.Clear();
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
```

### Result pattern (no exceptions for expected failures)
```csharp
public sealed record Error(string Code, string Message, ErrorType Type);
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}
```

Generic repository and Unit of Work. `IRepository<T>` with `GetByIdAsync`, `ListAsync(ISpecification<T>)`, `AddAsync`, `Remove`. `IUnitOfWork.SaveChangesAsync` is the only commit point. Use the Specification pattern for reusable query logic.

Other shared pieces
- `PagedResult<T>` and a `PagedQuery` base (page, size, sort, search). Cap page size at 100.
- `ApiResponse` and RFC 7807 `ProblemDetails` for all errors.
- `IClock` abstraction (never call `DateTime.UtcNow` directly; makes time testable).
- `ICurrentUser` (id, role, clinic id) read from the validated cookie principal.
- MediatR pipeline behaviors listed in Section 3.
- `IDomainEvent` dispatcher that runs after SaveChanges.
- Global exception middleware that maps exceptions to ProblemDetails and never leaks stack traces in production.
- Typed Options classes for all configuration, validated on startup.
- Frontend: one typed API client (axios or fetch wrapper with credentials: include), reusable form components, a useAuth hook, route guards, a shared error boundary.

## 7. Authentication and session design (HttpOnly tokens)
Principle: JavaScript must never be able to read a token.

Flow
1. POST `/api/auth/login` validates credentials, then sets two cookies:
   - Access token (JWT, 10 to 15 minutes): HttpOnly; Secure; SameSite=Strict; Path=/api
   - Refresh token (opaque random 64 bytes, 7 to 14 days): HttpOnly; Secure; SameSite=Strict; Path=/api/auth (sent only to auth endpoints)
2. API reads the access token from the cookie through `JwtBearerEvents.OnMessageReceived`.
3. When the access token expires, the client calls POST `/api/auth/refresh`. The server rotates the refresh token: the old one is revoked, a new one is issued.
4. Reuse detection: if a revoked refresh token is presented, revoke the entire token family and force re-login (it signals theft).
5. POST `/api/auth/logout` revokes the refresh token and clears both cookies.

Cookie options (centralize in CookieService)
```csharp
new CookieOptions
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    Path = path,
    Expires = expiresUtc,
    IsEssential = true
}
```

Rules
- Store only the SHA-256 hash of refresh tokens in the database, with device info, IP, created, expires, revoked, and replaced-by fields.
- JWT: asymmetric signing (RS256 or ES256) preferred; keys from Key Vault, never from the repo. Validate issuer, audience, lifetime, signing key; clock skew of 30 seconds maximum.
- Claims minimal: user id, role, clinic id, token id. No personal or medical data in the token.
- CSRF protection: SameSite=Strict plus a double-submit or antiforgery token header for all state-changing requests (POST, PUT, PATCH, DELETE), plus an Origin header check in middleware.
- Account lockout after 5 failed attempts (progressive delay), generic error messages (do not reveal whether an email exists).
- Password hashing with Identity's PBKDF2 at 600,000+ iterations or Argon2id; minimum 12 characters; check against breached-password lists.
- Email verification before first booking. Password reset tokens are single-use and expire in 15 minutes.
- Optional TOTP MFA, mandatory for Admin and Doctor roles.
- Frontend: no tokens in JS, no localStorage. The app learns the user from `GET /api/auth/me`.

## 8. Security requirements (full checklist)
Authorization
- Role-based plus policy-based and resource-based authorization. Example: a patient can read only their own appointments; a doctor only their own patients; a manager only their clinic.
- Prevent IDOR: every query filters by the current user's scope. Never trust ids from the client alone.
- Deny by default: `[Authorize]` globally, `[AllowAnonymous]` only where needed.

Input and output
- Validate every request with FluentValidation; reject unknown fields where practical.
- EF Core parameterized queries only; never concatenate SQL.
- Encode output; the React app must never use `dangerouslySetInnerHTML` with user content.
- File uploads: allow-list extensions and MIME types, check magic bytes, size limit, random file names, store outside web root, virus scan hook, serve through authorized endpoint with short-lived signed URLs.

Transport and headers (middleware)
- HTTPS only, HSTS (max-age 1 year, includeSubDomains), TLS 1.2+.
- Content-Security-Policy (strict, no inline scripts), X-Content-Type-Options: nosniff, X-Frame-Options: DENY, Referrer-Policy: strict-origin-when-cross-origin, Permissions-Policy, Cross-Origin-Opener-Policy.
- Remove the Server and X-Powered-By headers.
- CORS: explicit origin allow-list, AllowCredentials, never a wildcard.

Abuse protection
- Built-in rate limiting: strict on login, register, forgot-password, and booking endpoints; per-IP and per-user policies.
- Request body size limits and request timeouts.
- CAPTCHA (Turnstile or reCAPTCHA) on register and forgot-password.

Data protection
- Encrypt sensitive fields at rest (medical notes, phone, national id) using ASP.NET Core Data Protection or AES-GCM with keys from Key Vault. Enable database encryption at rest.
- Never log passwords, tokens, card data, or medical content. Mask personal data in logs.
- Stripe handles card data; the app never sees card numbers (stay SAQ-A).
- HIPAA/GDPR-minded design: data minimization, consent records, data export, right to deletion workflow, retention policy.
- Secrets: user-secrets in development, environment variables or Key Vault in production. Never commit secrets.

Auditing and monitoring
- AuditLogs records who, what, when, from where (IP, user agent) for login, role changes, record access, cancellations, refunds, exports.
- Alerts on repeated failures, token reuse detection, and unusual access patterns.

Supply chain and pipeline
- Dependabot, CodeQL, `dotnet list package --vulnerable`, container image scan (Trivy), secret scanning (gitleaks) in CI.
- Run containers as non-root, read-only file system where possible, minimal base images.
- Follow the OWASP ASVS and OWASP Top 10; document threat model (STRIDE) in docs/security.md.

## 9. Booking engine (the hard part)
Slot generation algorithm
1. Load provider's WorkingSchedule for the date range (stored in the clinic's time zone).
2. Subtract breaks, holidays, and TimeOffs.
3. Split remaining windows into slots of Service.DurationMinutes plus optional buffer.
4. Subtract existing active appointments and active SlotHolds.
5. Remove slots earlier than now plus minimum notice; cap at maximum advance booking days.
6. Convert to UTC for storage and to the patient's time zone for display.

Implement as a pure, deterministic domain service (SlotCalculator) with exhaustive unit tests (DST changes, midnight crossings, overlapping breaks).

Preventing double booking (defense in depth)
1. Redis distributed lock on `provider:{id}:slot:{start}` for the hold step.
2. Create a SlotHold (expires in 10 minutes) inside a transaction.
3. Database unique filtered index rejects any duplicate active appointment.
4. Optimistic concurrency token; catch DbUpdateConcurrencyException and DbUpdateException and return a friendly 409 Conflict.
5. A Hangfire job releases expired holds every minute.

Policies (configurable per clinic): cancellation window, refund percentage by notice time, reschedule limit, no-show rules, minimum notice, maximum advance days.

## 10. Payments (Stripe)
- Create the Checkout Session or PaymentIntent server-side, with the appointment id in metadata and an idempotency key.
- Never trust the redirect. Confirm only through the webhook.
- Webhook endpoint: verify the signature, store the event id in PaymentWebhookEvents, skip if already processed, then update state inside a transaction.
- Refunds go through the same service, are recorded, and are audited.
- Handle payment failure, expired sessions, and late success (payment after hold expiry triggers an automatic refund or a re-check of the slot).

## 11. Background jobs, notifications, caching
Hangfire jobs: hold expiry (every minute), reminders (24h and 2h), outbox processor, no-show marker, report snapshots, token and log cleanup. Jobs must be idempotent and retry-safe.

Outbox pattern: domain events are written to OutboxMessages in the same transaction as the data change; a job publishes them. This guarantees no lost emails or events.

Notifications: channel abstraction (INotificationChannel) for email, SMS, in-app. Templates are stored in files or the database with localization. Users control preferences.

SignalR: authenticated hub using the same cookie. Groups per user and per provider. Used for live slot updates, booking confirmations, and in-app alerts. Use the Redis backplane when scaled out.

Caching: cache availability queries in Redis for a short TTL (30 to 60 seconds) and invalidate on booking, cancellation, or schedule change. Cache reference data (specialties, services) longer.

## 12. API design
- REST, versioned (`/api/v1/...`), JSON, plural nouns, correct status codes (201, 204, 400, 401, 403, 404, 409, 422, 429).
- Consistent ProblemDetails error body with a traceId.
- Pagination, filtering, sorting on list endpoints.
- Idempotency-Key header on booking and payment creation.
- Health checks: `/health/live` and `/health/ready` (database, Redis).
- Swagger enabled in development only (or protected in production).

Main endpoints
- Auth: `POST /auth/register`, `/auth/login`, `/auth/refresh`, `/auth/logout`, `POST /auth/verify-email`, `/auth/forgot-password`, `/auth/reset-password`, `GET /auth/me`
- Providers: `GET /providers`, `GET /providers/{id}`, `GET /providers/{id}/slots?date=`
- Schedules: `PUT /providers/{id}/schedule`, `POST /providers/{id}/time-off`
- Appointments: `POST /appointments/hold`, `POST /appointments/{id}/confirm`, `POST /appointments/{id}/cancel`, `POST /appointments/{id}/reschedule`, `GET /appointments` (mine), `GET /appointments/{id}`
- Payments: `POST /payments/checkout`, `POST /payments/webhook`, `POST /payments/{id}/refund`
- Admin: `GET /admin/reports/*`, `GET /admin/audit-logs`, user and clinic management

## 13. Logging, observability, error handling
- Serilog structured logs to console and a sink (Seq, Elastic, or Application Insights). Correlation id on every request and job.
- OpenTelemetry traces and metrics (request duration, booking conflicts, payment failures).
- Global exception handler returns ProblemDetails; unexpected errors log with trace id but return a generic message.
- Slow query and slow request warnings through the performance behavior.

## 14. Testing strategy
- Unit: domain rules, slot calculator, policies, handlers (target 80%+ on Domain and Application).
- Integration: real PostgreSQL and Redis through Testcontainers; test the unique index and concurrency by firing parallel booking requests for the same slot and asserting exactly one succeeds.
- API tests: WebApplicationFactory, covering auth cookies, refresh rotation, reuse detection, authorization (IDOR) cases.
- Security tests: rate limits, CSRF rejection, header presence.
- Frontend: Vitest and Testing Library; Playwright for end-to-end booking flow.
- Coverage reported in CI; pull requests fail below the threshold.

## 15. DevOps
- Multi-stage Dockerfiles, non-root user, health checks.
- docker-compose.yml: api, web, postgres, redis, seq, mailhog (development mail catcher).
- CI (GitHub Actions): restore, build, format check, test with coverage, vulnerability scan, gitleaks, CodeQL, build and scan images.
- CD: deploy to staging on merge to main, production on a tagged release with manual approval; run EF migrations as a separate step.
- Environments: Development, Staging, Production, each with separate secrets.
- Branching: trunk-based with short-lived feature branches; Conventional Commits; protected main with required reviews and checks.

## 16. AI-ready design (for later)
Design now so AI can be added without refactoring:
- An Application/AI folder and interfaces such as `IAiAssistant`, `ISymptomTriageService`, `IDoctorRecommendationService`, `IAppointmentSummarizer`, `ISmartSchedulingAdvisor`, all with no-op default implementations registered via DI. Swapping in an LLM provider later needs only a new Infrastructure class.
- Keep clean, queryable data: specialties, symptoms tags, durations, outcomes, no-show history.
- Expose features as clear use-case handlers so they can later become LLM tools or MCP tools (for example SearchProviders, GetAvailableSlots, HoldSlot).
- Add a Feature Flags mechanism (Microsoft.FeatureManagement) to roll out AI safely.
- AI safety rules: de-identify patient data before sending it to external models, log every AI call and output in the audit log, add human-in-the-loop for anything clinical, prompt-injection filters, per-user rate limits, cost tracking, and clear user disclosure that AI is used. AI must never give a diagnosis.
- Planned AI features: symptom-based doctor recommendation, conversational booking assistant, note summarization, no-show prediction, smart overbooking and schedule optimization, multilingual support.

## 17. Repository files (include all of these)
`.gitignore` must cover: `bin/`, `obj/`, `.vs/`, `.vscode/` (except shared settings), `.idea/`, `*.user`, `*.suo`, `TestResults/`, `coverage/`, `node_modules/`, `dist/`, `build/`, `.env`, `.env.*` (except `.env.example`), `appsettings.*.local.json`, `secrets.json`, `*.pfx`, `*.key`, `*.pem`, `logs/`, `*.log`, `.DS_Store`, `Thumbs.db`, `uploads/`, and Docker volume folders. Start from the standard GitHub VisualStudio.gitignore and append the Node entries.

`.env.example`: list every variable with a dummy value, for example `ConnectionStrings__Default`, `Redis__Connection`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__KeyPath`, `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Smtp__Host`, `Cors__AllowedOrigins`. No real secrets.

`README.md` sections: project summary and screenshots, features, architecture diagram, tech stack, prerequisites, quick start (docker compose up), configuration table, running tests, API docs link, security summary, project structure, roadmap, contributing, license, contact.

Also create: `SECURITY.md` (how to report vulnerabilities), `CONTRIBUTING.md`, `LICENSE`, `CHANGELOG.md`, `.editorconfig`, `.gitattributes`, `.dockerignore`, pull request and issue templates, and `docs/adr/` for architecture decision records.

## 18. Implementation phases (build in this order)
1. **Foundation:** solution and projects, central packages, editorconfig, Serilog, Docker compose, PostgreSQL and Redis, health checks, global exception middleware, base classes, Result pattern, MediatR behaviors.
2. **Identity and security:** Identity, HttpOnly cookie JWT, refresh rotation with reuse detection, CSRF, rate limiting, security headers, MFA, audit log. Tests for all of it.
3. **Provider and scheduling management:** clinics, providers, services, schedules, breaks, time off, holidays.
4. **Slot engine:** SlotCalculator, availability endpoint, Redis caching, heavy unit tests.
5. **Booking flow:** hold, confirm, cancel, reschedule, status machine, concurrency tests.
6. **Payments:** Stripe checkout, idempotent webhooks, refunds, late-payment handling.
7. **Jobs and notifications:** Hangfire, outbox, reminders, email and SMS, SignalR.
8. **Medical notes and files:** encrypted storage, access control, audit.
9. **Admin dashboard and reports.**
10. **Frontend:** auth screens, search, booking wizard, patient and doctor dashboards, admin panel; accessibility (WCAG 2.1 AA) and responsive design.
11. **Hardening:** security review against Section 8, load test, penetration test checklist, dependency scan.
12. **Production:** CD pipeline, Azure deployment, monitoring and alerts, README with screenshots and live demo.
13. **AI integration (later)**, using the Section 16 extension points.

## 19. Coding standards and definition of done
- C#: nullable enabled, async all the way with CancellationToken, no async void, no static mutable state, constructor injection only, small focused classes, XML docs on public APIs.
- Naming: PascalCase types, camelCase locals, Async suffix, one feature per folder.
- No magic strings: use constants and options classes.
- TypeScript: strict mode, no any, ESLint and Prettier enforced.
- A task is done only when: code compiles with zero warnings, tests pass, validators exist, authorization is enforced and tested, no secrets or personal data in logs, the API doc is updated, and the change is documented in CHANGELOG.md.

## 20. Starter prompt to paste into OpenCode
Read AGENTS.md fully. Start with Phase 1 only: create the solution structure from Section 4, add the common building blocks from Section 6, configure Docker compose, Serilog, health checks, and global exception handling, and create .gitignore, .env.example, README.md, and .editorconfig as described in Section 17. Do not start Phase 2 until I confirm Phase 1 builds and its tests pass.
