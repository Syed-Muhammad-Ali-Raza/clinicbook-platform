# ADR 0001: Record Architecture Decisions

- **Status:** Accepted
- **Date:** 2026-11-19

## Context

ClinicBook is a multi-phase, production-grade system with security, reliability, and maintainability requirements. Important technical and architectural choices need to be captured so future contributors understand the rationale and trade-offs.

## Decision

We will document significant architecture and technical decisions as Architecture Decision Records (ADRs) in `docs/adr/`.

Each ADR will include:
- Status (proposed/accepted/superseded/deprecated)
- Date
- Context
- Decision
- Consequences (positive, negative, and follow-ups)

ADRs are numbered sequentially (`0001`, `0002`, ...) and are immutable once accepted except for status updates or superseding records.

## Consequences

- **Positive:** shared understanding, traceable rationale, easier onboarding and reviews.
- **Negative:** small ongoing documentation overhead.
- **Follow-ups:** add ADRs for key choices (e.g., modular monolith boundaries, auth/session design, outbox/eventing, booking concurrency strategy, payment/webhook idempotency).
