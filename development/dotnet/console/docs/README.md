# Console Application Documentation

Documentation for the ConsoleDungeon.Host Terminal.Gui application.

## Directory Structure

### `/build/`
Build system integration and NUKE documentation:
- Build system migration guides
- Build system recommendations
- NUKE integration plans
- Test reporting architecture
- Test output structure analysis

### `/checklists/`
Implementation checklists and tracking:
- `INTEGRATION-CHECKLIST.md` - Integration checklist

### `/design/`
Design documents and technical specifications:
- Plugin loading flow with resource service
- Plugin loading flowchart
- Plugin manifest NuGet dependencies

### `/handovers/`
Session handover documents tracking implementation progress:
- Namespace migration completion
- RFC-0040 implementation handovers
- Test build fixes
- Testing session handovers

### `/migration/`
Migration guides and documentation:
- `MIGRATION_GAME_CONTRACTS.md` - Game contracts migration guide

### `/phases/`
Project phase completion reports:
- Phase 1-4 completion documentation

### `/rfc/`
RFC implementation documentation:
- `RFC-0039-IMPLEMENTATION-COMPLETE.md` - RFC-0039 completion
- `RFC0040-IMPLEMENTATION-COMPLETE.md` - RFC-0040 completion
- `SUMMARY-RFC0040-IMPLEMENTATION.md` - RFC-0040 summary

### `/verification/`
Verification and testing reports:
- Render service verification
- Feature verification reports

## Root Files

- `README.md` - Console application overview
- `README-PM2.md` - PM2 process management guide
- `AGENTS.md` - Agent configuration

## Plugin Documentation

Plugin-specific documentation is located in their respective source directories:
- `src/plugins/*/README.md` - Individual plugin documentation

## Related Documentation

See also:
- `/docs/` - Project-level documentation
- `/build/docs/` - Build system documentation
