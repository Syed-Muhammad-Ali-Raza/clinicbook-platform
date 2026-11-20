# ClinicBook API Documentation

## Overview

The ClinicBook API is a RESTful HTTP API built with ASP.NET Core.

- **Base path:** `/api/v1`
- **Format:** JSON request/response bodies
- **Errors:** RFC 7807 Problem Details (`application/problem+json`) with a `traceId`
- **Docs UI (development):** `/swagger`
- **OpenAPI spec:** `/swagger/v1/swagger.json`
- **Project site (GitHub Pages placeholder):** https://syed-muhammad-ali-raza.github.io/clinicbook-platform/

## Authentication & session (Phase 2, in progress)

- **Session model:** HttpOnly cookie-based JWT. Tokens are never returned in response bodies for storage in browser JS storage.
  - Access token cookie: `cb_access` (HttpOnly, Secure in non-dev, `SameSite=Strict`, `Path=/api`, short-lived; ~15 min)
  - Refresh token cookie: `cb_refresh` (HttpOnly, Secure in non-dev, `SameSite=Strict`, `Path=/api/v1/auth`, longer-lived; ~14 days)
  - CSRF cookie: `cb_csrf` (readable by client for double-submit; `Path=/`)
- **CSRF protection:** Unsafe methods (`POST`, `PUT`, `PATCH`, `DELETE`) require a valid `Origin`/`Referer` (when present) and a matching double-submit token: cookie `cb_csrf` must equal header `X-CSRF-TOKEN` (configurable). Call `GET /api/v1/auth/csrf` first to obtain/ensure the CSRF cookie/token.
- **Refresh rotation & reuse detection:** Refresh tokens are opaque, stored server-side as SHA-256 hashes in token families. Each refresh rotates the token; presenting a revoked/rotated refresh token revokes the entire token family and forces re-login.
- **Rate limiting:** Strict limits on auth-sensitive endpoints (login/register/verify/forgot/reset), separate policy for refresh, and a global per-user/per-IP limiter. Exceeded limits return `429 Too Many Requests`.
- **Authorization:** Authenticated endpoints require a valid access cookie; unauthenticated requests receive `401`.

## Health endpoints

| Method | Path | Description | Auth |
| --- | --- | --- | --- |
| GET | `/health/live` | Liveness probe (process is running) | None |
| GET | `/health/ready` | Readiness probe (dependencies such as PostgreSQL/Redis when configured) | None |

## Auth endpoints

Base: `/api/v1/auth`

| Method | Path | Description | Auth | CSRF / Notes |
| --- | --- | --- | --- | --- |
| GET | `/csrf` | Ensures/returns a CSRF token for double-submit protection | None | Safe (no CSRF header required) |
| POST | `/register` | Registers a new user (Patient role by default) and triggers email verification flow | None | Required; rate-limited (`auth-strict`) |
| POST | `/login` | Authenticates user and sets auth cookies; may return MFA challenge | None | Required; rate-limited (`auth-strict`) |
| POST | `/refresh` | Rotates refresh token and issues a new access token (cookies) | None (uses refresh cookie) | Required; rate-limited (`auth-refresh`) |
| POST | `/logout` | Revokes current refresh token (if present) and clears auth cookies | None | Required |
| POST | `/verify-email` | Confirms a user's email address with verification token | None | Required; rate-limited (`auth-strict`) |
| POST | `/forgot-password` | Initiates password reset (generic response) | None | Required; rate-limited (`auth-strict`) |
| POST | `/reset-password` | Resets password with token and revokes active refresh tokens | None | Required; rate-limited (`auth-strict`) |
| GET | `/me` | Returns the current authenticated user | Required | Safe |
| GET | `/mfa/setup` | Returns TOTP shared key/otpauth URI for authenticator setup | Required | Safe |
| POST | `/mfa/enable` | Verifies TOTP code and enables MFA; returns recovery codes | Required | Required |
| POST | `/mfa/disable` | Disables MFA for the current user | Required | Required |

### Common request/response notes

- **`POST /register`** body: `{ "email", "password", "firstName", "lastName", "phoneNumber?" }` → `201 Created` with `{ "user", "message" }` on success.
- **`POST /login`** body: `{ "email", "password", "twoFactorCode?", "recoveryCode?" }` → `200 OK` with `{ "user", "requiresMfa": false }` and auth cookies set on success, or `{ "user", "requiresMfa": true, "reason" }` when MFA is required/not satisfied.
- **`POST /refresh`**: no body required (uses `cb_refresh` cookie) → `200 OK` with rotated cookies and `{ "user" }`; `401` if missing/invalid/expired/reused (cookies cleared on failure).
- **`GET /me`**: `200 OK` with `{ "user": { "id", "email", "firstName", "lastName", "emailConfirmed", "roles", "clinicId", "twoFactorEnabled" } }`.
- **MFA**: `POST /mfa/enable` body `{ "code" }` returns `{ "recoveryCodes": [...] }` on success.
- **Errors**: `400` validation, `401` unauthorized/invalid credentials or session, `403` CSRF/origin/forbidden, `409` conflict (e.g. email in use), `429` rate limited, `5xx` unexpected. Error responses use Problem Details and include `traceId`.

## Planned endpoint groups (upcoming phases)

- **Providers:** search/list, details, availability slots
- **Schedules:** manage provider schedules and time off
- **Appointments:** hold, confirm, cancel, reschedule, list/detail
- **Payments:** checkout, webhook, refunds
- **Admin:** reports, audit logs, user/clinic management

Detailed request/response contracts will be added/expanded alongside each feature (each feature requires validator, handler, unit test, and an API doc entry).
