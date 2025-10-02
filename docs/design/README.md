# Design Documentation

**Purpose**: Free-form design documents, problem analysis, and technical exploration

---

## Overview

This directory contains design documents that don't fit the formal structure of RFCs or ADRs. These documents are exploratory, analytical, or provide detailed planning for specific features or problems.

### Key Characteristics
- **Freeform structure** - No enforced template or format
- **Exploratory nature** - May include multiple approaches or ideas
- **Living documents** - Can be updated as understanding evolves
- **No formal lifecycle** - Not bound to draft/accepted/implemented states

---

## Current Documents

### Console & Migration Planning

#### [Console MVP Migration Plan](./console-mvp-migration-plan.md)
**Focus**: Migration strategy for console implementation

**Topics**:
- MVP scope definition
- Migration phases
- Risk assessment

---

#### [Existing WingedBean Host Analysis](./existing-wingedbean-host-analysis.md)
**Focus**: Analysis of current WingedBean.Host architecture

**Topics**:
- Current architecture assessment
- Strengths and limitations
- Improvement opportunities

---

### ECS & Game Architecture

#### [Dungeon Crawler ECS Roadmap](./dungeon-crawler-ecs-roadmap.md)
**Focus**: High-level roadmap for ECS-based dungeon crawler

**Topics**:
- ECS integration strategy
- Game feature roadmap
- Technical milestones

---

### GitHub Automation

#### [GitHub Automation - Critical Issues Analysis](./github-automation-critical-issues-analysis.md)
**Focus**: Analysis of critical issue tracking requirements

**Topics**:
- Issue classification approach
- Automation requirements
- Workflow integration

---

#### [GitHub Automation - Revised Approach](./github-automation-revised-approach.md)
**Focus**: Updated strategy for GitHub workflow automation

**Topics**:
- Lessons learned from initial approach
- Revised automation strategy
- Implementation considerations

---

#### [GitHub Automation - Workflows Adoption Plan](./github-automation-workflows-adoption-plan.md)
**Focus**: Plan for adopting GitHub Actions workflows

**Topics**:
- Workflow design patterns
- Gradual adoption strategy
- Integration with existing processes

---

## When to Create Design Docs

Create design documents when:

1. **Exploring Solutions**: Evaluating multiple approaches to a problem
2. **Problem Analysis**: Deep-diving into a specific technical challenge
3. **Migration Planning**: Designing migration or refactoring strategies
4. **Feature Planning**: Detailed planning before RFC creation
5. **Technical Investigation**: Researching technologies or approaches

---

## Design Doc vs RFC vs ADR

| Aspect | Design Doc | RFC | ADR |
|--------|------------|-----|-----|
| **Purpose** | Explore & analyze | Propose & implement | Record decisions |
| **Structure** | Freeform | Structured template | Structured template |
| **Lifecycle** | Living (can evolve) | Draft → Implemented | Immutable |
| **Formality** | Informal | Semi-formal | Formal |
| **Scope** | Narrow or exploratory | Broad system changes | Specific decisions |

---

## Naming Conventions

- Use **descriptive kebab-case** names
- Be specific about the topic
- Include context in filename when helpful

**Examples**:
- ✅ `github-automation-revised-approach.md`
- ✅ `dungeon-crawler-ecs-roadmap.md`
- ✅ `console-mvp-migration-plan.md`
- ❌ `design1.md`
- ❌ `notes.md`

---

## Graduation Path

Design documents can evolve into formal proposals:

```
Design Doc (exploration)
    ↓
RFC (formal proposal)
    ↓
ADR (decision record)
    ↓
Implementation (execution)
```

**Example**: A design doc exploring ECS integration approaches might lead to RFC-0007 (Arch ECS Integration), which results in an ADR documenting the final technology choice.

---

## Related Documentation

- [RFCs](../rfcs/) - Formal design proposals
- [ADRs](../adr/) - Architecture decisions
- [Implementation](../implementation/) - Execution plans
- [Guides](../guides/) - How-to documentation

---

**Last Updated**: 2025-10-02
**Total Documents**: 7
