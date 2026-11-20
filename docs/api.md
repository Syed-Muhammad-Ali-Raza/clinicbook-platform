# ClinicBook API Documentation

## Overview

The ClinicBook API is a RESTful HTTP API built with ASP.NET Core.

- **Base path:** `/api/v1` (versioned routes introduced as features are added)
- **Format:** JSON request/response bodies
- **Errors:** RFC 7807 Problem Details (`application/problem+json`) with a `traceId`
- **Auth:** HttpOnly cookie-based JWT session (planned Phase 2). Never send tokens in localStorage.
- **Docs UI (development):** `/swagger`
- **OpenAPI spec:** `/swagger/v1/swagger.json`

## Phase 1 Endpoints

### Health

| Method | Path | Description | Auth |
| --- | --- | --- | --- |
| GET | `/health/live` | Liveness probe (process is running) | None |
| GET | `/health/ready` | Readiness probe (dependencies such as PostgreSQL/Redis when configured) | None |

## Planned Endpoint Groups

- **Auth:** register, login, refresh, logout, verify-email, forgot/reset password, me
- **Providers:** search/list, details, availability slots
- **Schedules:** manage provider schedules and time off
- **Appointments:** hold, confirm, cancel, reschedule, list/detail
- **Payments:** checkout, webhook, refunds
- **Admin:** reports, audit logs, user/clinic management

Detailed request/response contracts will be added alongside each feature (each feature requires validator, handler, unit test, and an API doc entry).
