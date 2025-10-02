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
R-PRC-050: Use Python instead of embedded shell scripts for complex logic in workflows/hooks.
  - Shell is acceptable for simple conditionals (< 10 lines)
  - Python is required for: parsing JSON, complex string manipulation, error handling, API calls
  - Place Python scripts in `scripts/hooks/` or `scripts/workflows/`
  - Keep workflow YAML minimal - delegate to Python scripts

## Issue Management
R-ISS-010: When creating issues programmatically, include YAML metadata block (RFC-0015).
  - Required fields: `rfc`, `depends_on` (list), `priority` (critical|high|medium|low), `agent_assignable` (bool)
  - Optional fields: `retry_count`, `max_retries`, `blocks`, `phase`, `wave`, `estimate_minutes`
  - Format: YAML frontmatter (---...---) or code block (```yaml...```)
  - Pre-commit hook validates schema before allowing commits (hard block)
  - Example:
    ```yaml
    rfc: RFC-0007
    depends_on: [48, 62]
    blocks: [86, 87]
    priority: critical
    agent_assignable: true
    retry_count: 0
    max_retries: 3
    ```
R-ISS-020: When creating issues, specify the intended agent.
  - Use labels: `agent:copilot`, `agent:claude-code`, or `agent:windsurf`
  - If unsure, use `agent:unassigned` and let human decide
R-ISS-030: Before starting work on an issue, verify all dependencies are resolved.
  - Check `depends_on` field in issue metadata
  - Query each blocker's status via `gh issue view <num> --json state`
  - If any blocker is open, do not start work (enforced by workflow)
R-ISS-040: When a PR fails CI, the agent that created it must fix it (3 retry limit).
  - Read failure logs in issue comments
  - Analyze what went wrong
  - Create new PR with fixes
  - Retry count tracked in issue metadata (`retry_count` field)
  - Do not require human intervention unless max_retries exceeded
R-ISS-050: Issue titles must follow naming convention.
  - Format: `RFC-XXXX-YY: Short description` (for RFC-related work)
  - Format: `[COMPONENT] Short description` (for general work)
  - Examples: `RFC-0007-01: Create ECS contracts`, `[CI] Fix MegaLinter timeout`
R-ISS-060: Workflow scripts exceeding 50 lines OR using complex logic must use Python (RFC-0015).
  - Shell is acceptable for simple conditionals (< 10 lines)
  - Python required for: parsing JSON, complex string manipulation, error handling, API calls
  - NO Perl, awk, sed for complex operations
  - Place Python scripts in `development/python/src/workflows/` or `development/python/src/hooks/`
R-ISS-070: Before pushing new/modified workflows, test locally using nektos/act (RFC-0015).
  - Run `python development/python/src/testing/test_workflows.py --workflow <file>`
  - Or use workflow testing harness for comprehensive validation
  - Prevents runner minute waste from untested workflows

## Deprecated Rules
(None yet)
