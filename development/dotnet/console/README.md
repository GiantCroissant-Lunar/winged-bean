# Console Project

Console implementation of WingedBean platform with Terminal.Gui v2 and xterm.js integration.

## Structure

- `src/ConsoleDungeon/` - Main Terminal.Gui v2 TUI application
- `src/ConsoleDungeon.Host/` - Bootstrap entry point
- `tests/` - Test projects

## Running

```bash
# Build
dotnet build Console.sln

# Run
dotnet run --project src/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj
```

The WebSocket server will start on port 4040. Connect from the Astro frontend's xterm.js terminal.

## Development Status

**Phase**: MVP Complete ✅
- [x] Folder structure migrated
- [x] ConsoleDungeon app preserved unchanged
- [x] ConsoleDungeon.Host wrapper created
- [x] xterm.js integration verified working

**Next Phase**: Plugin Architecture
- [ ] Add Registry + PluginLoader
- [ ] Extract WebSocket as plugin
- [ ] Extract Terminal.Gui as plugin
- [ ] Refactor ConsoleDungeon to use services

See [RFC-0004](../../../docs/rfcs/0004-project-organization-and-folder-structure.md) for full roadmap.
