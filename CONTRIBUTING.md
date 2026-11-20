# Contributing to ClinicBook

Thank you for contributing to ClinicBook. This project follows the architecture and standards defined in [`AGENTS.md`](AGENTS.md) and [`docs/SPEC.md`](docs/SPEC.md).

## Getting Started

1. Read [`AGENTS.md`](AGENTS.md) and the relevant docs in `docs/`.
2. Set up the local environment (see [`README.md`](README.md)).
3. Create a short-lived branch from `main`.

## Branching and Commits

- **Branching:** trunk-based with short-lived feature branches.
- **Commits:** follow [Conventional Commits](https://www.conventionalcommits.org/).
  - Examples: `feat: add slot calculator`, `fix: handle refresh token reuse`, `docs: update API guide`, `test: add booking concurrency tests`
- Keep commits focused and logically grouped.

## Development Standards

- Follow Clean Architecture boundaries (dependencies point inward).
- Controllers must be thin: translate HTTP to MediatR commands/queries only.
- Never put tokens in `localStorage`/`sessionStorage`; use HttpOnly cookies.
- Every new feature needs: validator, handler, unit test, and API doc entry.
- C#: nullable enabled, async all the way with `CancellationToken`, constructor injection, no static mutable state.
- No magic strings; use constants/options classes.
- TypeScript (future frontend): strict mode, no `any`.

## Definition of Done

A change is done only when:
- Code compiles with **zero warnings**.
- Tests pass (and new behavior is covered).
- Validators and authorization are in place and tested where applicable.
- No secrets or personal/medical data appear in logs.
- API docs are updated (if applicable).
- `CHANGELOG.md` is updated for user-visible changes.

## Pull Requests

- Use the pull request template.
- Ensure CI checks pass (build, format, tests, scans).
- Request review; address feedback promptly.
- Keep PRs reasonably sized and scoped to a single concern.

## Reporting Bugs and Requesting Features

- Use issue templates when available.
- Provide clear reproduction steps, expected vs actual behavior, and environment details.
- For security issues, follow [`SECURITY.md`](SECURITY.md) (do not open a public issue).

## Code of Conduct

Be respectful, constructive, and collaborative. Assume good intent and focus on improving the project.
