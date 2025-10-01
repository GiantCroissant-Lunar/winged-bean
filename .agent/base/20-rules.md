# Normative Rules (Canon)

(Use: Adapters cite "R-CODE-010" etc.)

## Code & Architecture
R-CODE-010: Prefer editing existing files over creating new ones unless explicitly required.
R-CODE-020: Do not fabricate file contents. If unsure about implementation details, request clarification.
R-CODE-030: Follow .NET naming conventions: PascalCase for public members, camelCase for private fields with underscore prefix.
R-CODE-040: Unity-specific: Use SerializeField for inspector-visible private fields; avoid public fields.
R-CODE-050: Respect existing project structure. Do not create orphaned files outside the workspace hierarchy.

## Security
R-SEC-010: Never log, echo, or invent secrets. Use `<REDACTED>` placeholder in examples.
R-SEC-020: Do not embed credentials/tokens in code or documentation examples.
R-SEC-030: Do not log PII (Personally Identifiable Information). Redact sensitive fields by default.
R-SEC-040: External API calls must define timeout and retry/backoff policy in production code.

## Testing
R-TST-010: When adding tests, follow xUnit conventions for .NET projects.
R-TST-020: Unity tests should use Unity Test Framework (UTF) and be placed in Tests~ folders or Assembly Definitions.
R-TST-030: Flaky tests are quarantined (mark and raise issue) not deleted.

## Documentation
R-DOC-010: RFCs must follow naming convention: `docs/rfcs/XXXX-title.md` (4-digit, lowercase slug).
R-DOC-020: RFCs must include frontmatter with: `id` (4-digit), `title`, `status` (Draft/Accepted/Rejected/Postponed), `category` (one or more of: gameplay, infra, web, publishing, docs, tooling, framework).
R-DOC-030: ADRs must follow: `docs/adr/XXXX-title.md` (4-digit, lowercase slug).
R-DOC-040: Execution plans tied to RFCs: `docs/implementation/rfc-XXXX-execution-plan.md`.
R-DOC-050: Do not create documentation files (*.md) proactively. Only create when explicitly requested.

## Git
R-GIT-010: Commit bodies MUST be authored from a file and passed via `git commit -F <file>`.
  - Do not include literal backslash-escaped newlines (e.g., `\n`) in `-m` arguments.
  - Subject line ≤ 72 chars; then a blank line; then Markdown body.
  - Include co-authorship footer:
    ```
    🤖 Generated with [Claude Code](https://claude.com/claude-code)

    Co-Authored-By: Claude <noreply@anthropic.com>
    ```
R-GIT-020: Do not commit files that likely contain secrets (.env, credentials.json, appsettings.Development.json with connection strings).

## Process
R-PRC-010: When uncertain about architectural decisions, propose options rather than implementing immediately.
R-PRC-020: Use TodoWrite tool for multi-step tasks to track progress and give visibility.
R-PRC-030: Do not duplicate full rule text in adapters; adapters cite rule IDs only.
R-PRC-040: Never renumber or reuse a retired rule ID; create a new ID for semantic changes.

## Deprecated Rules
(None yet)
