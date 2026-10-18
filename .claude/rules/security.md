# Security Rules

## Secrets and Logging

- Never hardcode, print, or commit credentials, tokens, connection strings, or
  local environment files.
- Read secrets from the existing configuration/environment mechanism and fail
  safely when required values are missing.
- Log enough context to diagnose a failure without logging secret-bearing
  values or user data.

## Input and Output Boundaries

- Validate untrusted input before parsing or dispatch.
- Use parameterized database queries and existing safe serialization APIs.
- Preserve template escaping and avoid introducing raw HTML/string execution.
- Return user-safe errors; keep internal diagnostics in protected logs.

## Dependencies

- Add or upgrade dependencies only when the task requires it.
- Keep compatible version ranges in `pyproject.toml` and `package.json`; rely on
  `uv.lock` and `package-lock.json` for reproducible development and CI.
- Use ecosystem checks declared by the repository. Do not require globally
  installed scanners or silently rewrite lockfiles.

## Review Checklist

- No new secret or sensitive-data exposure.
- External input is validated at the correct trust boundary.
- Filesystem, process, network, and SQL operations use safe APIs.
- Permission changes are explicit and least-privilege.
- Failure paths do not leak details or suppress security-relevant errors.
