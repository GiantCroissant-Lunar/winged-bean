using Xunit;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Test that also builds the game bundle as a side effect.
/// Run this to create the game-data.wbundle file.
/// </summary>
public class BuildGameBundleTest
{
    [Fact]
    public async Task BuildGameDataBundle()
    {
        // Find project root
        var current = Directory.GetCurrentDirectory();
        while (current != null && !File.Exists(Path.Combine(current, "Console.sln")))
        {
            current = Directory.GetParent(current)?.FullName;
        }

        if (current == null)
        {
            throw new Exception("Could not find Console.sln");
        }

        var resourcesDir = Path.Combine(current, "game-resources");
        var outputDir = Path.Combine(current, "src/host/ConsoleDungeon.Host/resources");
        var bundlePath = Path.Combine(outputDir, "game-data.wbundle");

        Console.WriteLine($"Building bundle from: {resourcesDir}");
        Console.WriteLine($"Output to: {bundlePath}");

        Directory.CreateDirectory(outputDir);

        var builder = new ResourceBundleBuilder("game-data", "1.0.0")
            .WithMetadata(
                name: "ConsoleDungeon Game Data",
                description: "Core game data for ConsoleDungeon",
                author: "WingedBean Team"
            );

        foreach (var category in new[] { "enemies", "items", "players", "dungeons" })
        {
            var categoryPath = Path.Combine(resourcesDir, category);
            if (Directory.Exists(categoryPath))
            {
                builder.AddDirectory(
                    categoryPath,
                    resourcePrefix: category,
                    recursive: true,
                    filePatterns: new[] { "*.json" }
                );
                
                var fileCount = Directory.GetFiles(categoryPath, "*.json", SearchOption.AllDirectories).Length;
                Console.WriteLine($"  ✓ Added {fileCount} {category}");
            }
        }

        await builder.BuildAsync(bundlePath);

        var bundleInfo = new FileInfo(bundlePath);
        Console.WriteLine($"\n✅ Bundle created: {bundlePath}");
        Console.WriteLine($"   Size: {bundleInfo.Length:N0} bytes ({bundleInfo.Length / 1024.0:F1} KB)");
        
        // Verify bundle was created
        Assert.True(File.Exists(bundlePath));
        Assert.True(bundleInfo.Length > 0);
    }
}
