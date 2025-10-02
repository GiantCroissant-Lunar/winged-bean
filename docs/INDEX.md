# Winged Bean Documentation Index

**Project**: Winged Bean - Game Development Framework (.NET/C#, Unity)
**Architecture**: ECS (Entity Component System) using Arch library
**Last Updated**: 2025-10-02

---

## Documentation Categories

### 📋 [RFCs](./rfcs/) - Request for Comments
**Purpose**: Architectural proposals, design decisions, and major feature specifications

**What belongs here**:
- Major architectural changes and proposals
- Feature design specifications
- System-level design decisions
- Technology evaluations and selections

**Lifecycle**: Draft → Proposed → Accepted → Implemented (or Superseded)

**Naming**: `XXXX-title.md` (4-digit ID, lowercase slug)

**Status**: 13 active RFCs ([View Index →](./rfcs/README.md))

---

### 📝 [ADRs](./adr/) - Architecture Decision Records
**Purpose**: Immutable historical record of architectural decisions

**What belongs here**:
- Final architectural decisions with context and rationale
- Technology choices and trade-offs
- Structural and organizational decisions

**Lifecycle**: Immutable (never modified after creation)

**Naming**: `XXXX-title.md` (4-digit ID, lowercase slug)

**Status**: 6 decisions recorded

---

### 🎨 [Design](./design/)
**Purpose**: Free-form design documents and exploration

**What belongs here**:
- Design explorations and brainstorming
- Problem analysis documents
- Migration plans and strategies
- Technical analysis reports

**Lifecycle**: Freeform (no enforced lifecycle)

**Naming**: Descriptive kebab-case (e.g., `github-automation-workflows-adoption-plan.md`)

**Status**: 7 design documents

---

### 🛠️ [Implementation](./implementation/)
**Purpose**: Execution plans and implementation tracking for RFCs

**What belongs here**:
- Detailed execution plans for RFCs
- Status reports and progress tracking
- Implementation checklists

**Lifecycle**: Active during implementation, archived when complete

**Naming**: `rfc-XXXX-execution-plan.md`, `rfc-XXXX-status-report.md`

**Status**: 12 implementation documents

---

### 📚 [Guides](./guides/)
**Purpose**: How-to guides and best practices

**What belongs here**:
- Development guides and tutorials
- Framework usage documentation
- Best practices and conventions
- Tool usage instructions

**Lifecycle**: Maintained (updated as needed)

**Naming**: Descriptive kebab-case

---

### 🎮 [Unity](./unity/)
**Purpose**: Unity-specific documentation and plugin system

**What belongs here**:
- Unity plugin architecture
- Unity-specific implementation details
- Unity integration guides

**Lifecycle**: Maintained (updated as needed)

**Status**: Comprehensive plugin system documentation ([View →](./unity/README.md))

---

### ✅ [Test Results](./test-results/)
**Purpose**: Test execution reports and verification results

**What belongs here**:
- Test execution summaries
- Verification reports
- Quality assurance documentation

**Lifecycle**: Time-based (archive old results periodically)

---

### 📊 [Verification](./verification/)
**Purpose**: RFC and feature verification documentation

**What belongs here**:
- RFC verification reports
- Feature completion verification
- Acceptance criteria validation

**Lifecycle**: Archived after verification complete

---

### 🔄 [Development](./development/)
**Purpose**: Development process and workflow documentation

**What belongs here**:
- Development workflow guides
- Process documentation
- Development environment setup

**Lifecycle**: Maintained (updated as needed)

---

### 📦 [Handover](./handover/)
**Purpose**: Project handover and knowledge transfer

**What belongs here**:
- Context for new team members
- Project state snapshots
- Knowledge transfer documents

**Lifecycle**: Created as needed, archived when complete

---

### 💬 [Chat History](./chat-history/)
**Purpose**: Archived conversation logs and session history

**What belongs here**:
- Consolidated chat transcripts
- Development session logs
- AI assistant conversation archives

**Lifecycle**: Time-based archival (recommend: keep recent 30 days, archive older)

**Management**: Auto-archive candidates (see [Tooling Plan](#tooling-automation))

---

### 🎬 [Recordings](./recordings/)
**Purpose**: Terminal session recordings (asciinema format)

**What belongs here**:
- Terminal.Gui session recordings (.cast files)
- PTY session captures
- Development session demonstrations

**Lifecycle**: Time-based archival (recommend: keep recent 30 days, archive older)

**Management**: Auto-archive candidates (see [Tooling Plan](#tooling-automation))

---

## Quick Links

### Current Focus
- [Active RFCs](./rfcs/README.md#active-rfcs) - Current architectural work
- [Implementation Plans](./implementation/) - Execution tracking
- [Recent ADRs](./adr/) - Latest decisions

### Getting Started
- [Framework Targeting Guide](./guides/framework-targeting-guide.md)
- [Unity Plugin System](./unity/README.md)
- [Development Workflows](./development/)

---

## Tooling & Automation

### Planned Automation (Phase 2)
1. **Frontmatter Validation** - Enforce RFC/ADR metadata
2. **Orphaned File Detection** - Find unreferenced documents
3. **Auto-archival** - Move old recordings/chat-history to `.archive/`
4. **TOC Generation** - Auto-update index files

### Current Manual Processes
- RFC status updates (manual)
- Index file maintenance (manual)
- Recording cleanup (manual)

---

## Documentation Principles

### R-DOC-010: RFC Naming
RFCs use `docs/rfcs/XXXX-title.md` format (4-digit ID, lowercase slug)

### R-DOC-020: RFC Frontmatter
Required fields: `id`, `title`, `status`, `category`

### R-DOC-030: ADR Naming
ADRs use `docs/adr/XXXX-title.md` format (4-digit ID, lowercase slug)

### R-DOC-040: Execution Plans
Implementation plans use `docs/implementation/rfc-XXXX-execution-plan.md`

### R-DOC-050: Doc Creation
Only create documentation when explicitly requested (R-CODE-010)

---

## Version Information

**Documentation Structure**: v1.0
**Agent Rules**: v1.0.0
**Last Major Reorganization**: 2025-10-02

---

## Contributing

When adding new documentation:

1. **Choose the right category** based on purpose and lifecycle
2. **Follow naming conventions** for the category
3. **Add frontmatter** where required (RFCs, ADRs)
4. **Update index files** when adding significant docs
5. **Link related documents** for discoverability

For questions about documentation structure, see `.agent/base/20-rules.md` (R-DOC series).

---

**Maintained by**: Winged Bean Development Team
