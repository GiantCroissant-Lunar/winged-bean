# Domain Glossary

## Project Structure Terms
- **Framework**: The core Winged Bean framework (C#/.NET libraries)
- **Workspace**: The entire monorepo including framework, samples, and infrastructure
- **RFC**: Request for Comments - design documents for significant features or architectural changes
- **ADR**: Architecture Decision Record - records of key technical decisions
- **Execution Plan**: Implementation roadmap tied to an RFC

## .NET / C# Terms
- **Assembly**: Compiled .NET binary (.dll or .exe)
- **Solution**: Visual Studio solution file grouping related projects
- **Project**: Individual .csproj file representing a compilable unit
- **NuGet**: .NET package manager

## Unity Terms
- **Assembly Definition**: Unity's way of organizing scripts into separate compiled assemblies
- **SerializeField**: Attribute making private fields visible in Unity Inspector
- **GameObject**: Base class for entities in Unity scenes
- **MonoBehaviour**: Base class for Unity scripts attached to GameObjects
- **ScriptableObject**: Unity data container classes

## Architecture Terms
- **ECS**: Entity Component System - data-oriented architecture pattern
- **Arch**: The ECS library used by Winged Bean framework
- **Plugin System**: Dynamic loading mechanism for framework extensions
- **Target Framework**: .NET framework version (netstandard2.1, net8.0, etc.)

## Development Terms
- **Nuke**: Build automation tool used in this project
- **Hot Reload**: Runtime code changes without restart (C# Edit & Continue, Unity Hot Reload)
- **Orphaned Projects**: Projects not properly linked in solution hierarchy
