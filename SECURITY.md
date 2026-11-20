# Security Policy

## Supported Versions

ClinicBook is under active development. Security fixes are applied to the `main` branch and the latest release.

| Version | Supported |
| --- | --- |
| main / latest | Yes |
| older releases | No |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, report vulnerabilities privately to the maintainers:

- Email: `security@clinicbook.example` (replace with project security contact)
- Or use your repository's private security advisory feature if available.

Please include:
- A description of the vulnerability and its impact
- Steps to reproduce or a proof of concept
- Affected components/versions and any relevant logs (redacted of secrets/PII)
- Suggested remediation, if known

## Response Process

- We will acknowledge receipt within 3 business days.
- We will investigate, validate impact, and work on a fix or mitigation.
- We will coordinate disclosure timing with you when appropriate.
- Once resolved, we will publish details in release notes/advisories as appropriate.

## Security Practices

- Secrets must never be committed; use environment variables or a secret manager/Key Vault.
- Authentication tokens are stored only in HttpOnly, Secure, SameSite cookies (never localStorage).
- Follow the security requirements in [`docs/SPEC.md`](docs/SPEC.md) and [`docs/security.md`](docs/security.md).
- Dependencies and container images are scanned in CI (Dependabot, CodeQL, vulnerability/container scans).
