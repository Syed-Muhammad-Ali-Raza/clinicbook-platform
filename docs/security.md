# ClinicBook Security Notes

This document summarizes security design and operational practices for ClinicBook. It complements [`SECURITY.md`](../SECURITY.md) and the security requirements in [`SPEC.md`](SPEC.md) / [`AGENTS.md`](../AGENTS.md).

## Threat Model (STRIDE - initial)

| Threat | Examples | Mitigations (planned/in progress) |
| --- | --- | --- |
| Spoofing | Credential theft, token replay | HttpOnly/Secure cookies, refresh rotation + reuse detection, MFA, strong password policy, lockout |
| Tampering | Request/body tampering, CSRF, DB tampering | HTTPS, CSRF protections, input validation, parameterized queries, integrity constraints, auditing |
| Repudiation | User denies actions | Structured audit logs (who/what/when/where), immutable event records where appropriate |
| Information disclosure | PII/PHI leakage, token/log leakage | Encryption in transit/at rest, least privilege, data minimization, no secrets/PHI in logs, scoped queries (IDOR prevention) |
| Denial of service | Brute force, resource exhaustion | Rate limiting, request size/time limits, caching, resilient job processing |
| Elevation of privilege | IDOR, broken access control | Deny-by-default authZ, RBAC + policy/resource checks, scope filters on every query |

## Authentication & Session (Phase 2)

- Access and refresh tokens stored only in HttpOnly, Secure, SameSite cookies (never in JS storage).
- Short-lived access tokens; rotating opaque refresh tokens with reuse detection and revocation.
- Minimal JWT claims; no PII/medical data in tokens.
- CSRF defenses for state-changing requests and origin checks.

## Authorization

- Global `[Authorize]` by default with explicit anonymous exceptions.
- Role-based, policy-based, and resource-based checks.
- Every data access path enforces caller scope to prevent IDOR.

## Data Protection

- TLS everywhere; HSTS and strict security headers.
- Encryption at rest for database and sensitive fields/attachments; keys managed via secret manager/Key Vault.
- Stripe handles card data (SAQ-A scope); never store card numbers.

## Input/Output & Abuse Protection

- FluentValidation for all inputs; EF Core parameterized queries only.
- Upload allow-lists, size limits, safe storage, and authorized download paths.
- Rate limiting and CAPTCHA on sensitive endpoints.

## Logging, Monitoring, and Auditing

- Serilog structured logs with correlation/trace IDs; no secrets or sensitive data in logs.
- Audit log for sensitive actions (auth events, access, changes, exports).
- Alerts for repeated failures, token reuse, and anomalous access patterns.

## Supply Chain & Deployment

- Dependency/vulnerability scanning (Dependabot, `dotnet list package --vulnerable`, Trivy), secret scanning (gitleaks), CodeQL.
- Minimal, non-root containers; environment-specific secrets and configuration.
- Follow OWASP ASVS and OWASP Top 10 guidance.

This document will be expanded as features and controls are implemented.
