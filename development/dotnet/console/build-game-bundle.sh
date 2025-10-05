#!/bin/bash
set -e

echo "========================================"
echo "Game Resource Bundle Builder"
echo "========================================"
echo ""

# Build the Resource plugin first
echo "Building Resource plugin..."
dotnet build src/plugins/WingedBean.Plugins.Resource/WingedBean.Plugins.Resource.csproj --nologo -v quiet

# Create bundle using C#
echo "Creating bundle..."
dotnet fsi - <<'FSHARP'
#r "src/plugins/WingedBean.Plugins.Resource/bin/Debug/net8.0/WingedBean.Plugins.Resource.dll"

open System.IO
open WingedBean.Plugins.Resource

let resourcesDir = "game-resources"
let outputDir = "src/host/ConsoleDungeon.Host/resources"
let bundlePath = Path.Combine(outputDir, "game-data.wbundle")

// Ensure output directory exists
Directory.CreateDirectory(outputDir) |> ignore

// Build the bundle
let builder = ResourceBundleBuilder("game-data", "1.0.0")
builder.WithMetadata(
    name = "ConsoleDungeon Game Data",
    description = "Core game data",
    author = "WingedBean Team"
) |> ignore

// Add all resource directories
for category in ["enemies"; "items"; "players"; "dungeons"] do
    let categoryPath = Path.Combine(resourcesDir, category)
    if Directory.Exists(categoryPath) then
        builder.AddDirectory(
            categoryPath,
            resourcePrefix = category,
            recursive = true,
            filePatterns = [|"*.json"|]
        ) |> ignore
        let fileCount = Directory.GetFiles(categoryPath, "*.json", SearchOption.AllDirectories).Length
        printfn $"  ✓ Added {fileCount} {category}"

// Build
builder.BuildAsync(bundlePath).Wait()

let bundleInfo = FileInfo(bundlePath)
printfn ""
printfn $"✅ Bundle created: {bundlePath}"
printfn $"   Size: {bundleInfo.Length:N0} bytes"
FSHARP

echo ""
echo "Done!"
