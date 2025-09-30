using FluentAssertions;
using WingedBean.Contracts.Core;
using WingedBean.PluginLoader;
using WingedBean.Providers.AssemblyContext;
using Xunit;

namespace WingedBean.PluginLoader.Tests;

/// <summary>
/// Unit tests for ActualPluginLoader.
/// </summary>
public class ActualPluginLoaderTests : IDisposable
{
    private readonly AssemblyContextProvider _contextProvider;
    private readonly ActualPluginLoader _loader;
    private readonly List<string> _createdTestFiles = new();

    public ActualPluginLoaderTests()
    {
        _contextProvider = new AssemblyContextProvider();
        _loader = new ActualPluginLoader(_contextProvider);
    }

    public void Dispose()
    {
        // Clean up test files
        foreach (var file in _createdTestFiles)
        {
            if (File.Exists(file))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        _contextProvider.Dispose();
    }

    [Fact]
    public void Constructor_WithValidContextProvider_CreatesInstance()
    {
        // Arrange & Act
        var loader = new ActualPluginLoader(_contextProvider);

        // Assert
        loader.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullContextProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ActualPluginLoader(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetLoadedPlugins_WhenNoPluginsLoaded_ReturnsEmptyCollection()
    {
        // Act
        var plugins = _loader.GetLoadedPlugins();

        // Assert
        plugins.Should().BeEmpty();
    }

    [Fact]
    public void IsLoaded_WhenPluginNotLoaded_ReturnsFalse()
    {
        // Act
        var isLoaded = _loader.IsLoaded("test-plugin");

        // Assert
        isLoaded.Should().BeFalse();
    }

    [Fact]
    public void GetPlugin_WhenPluginNotLoaded_ReturnsNull()
    {
        // Act
        var plugin = _loader.GetPlugin("test-plugin");

        // Assert
        plugin.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_WithNullPath_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = async () => await _loader.LoadAsync((string)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LoadAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = async () => await _loader.LoadAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LoadAsync_WithNonExistentPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "/tmp/non-existent-plugin.dll";

        // Act
        var act = async () => await _loader.LoadAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task LoadAsync_WithNullManifest_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = async () => await _loader.LoadAsync((PluginManifest)null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UnloadAsync_WithNullPlugin_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = async () => await _loader.UnloadAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReloadAsync_WithNullPlugin_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = async () => await _loader.ReloadAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void IsLoaded_WithNullOrEmptyId_ThrowsArgumentException()
    {
        // Arrange & Act
        var act1 = () => _loader.IsLoaded(null!);
        var act2 = () => _loader.IsLoaded(string.Empty);

        // Assert
        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPlugin_WithNullOrEmptyId_ThrowsArgumentException()
    {
        // Arrange & Act
        var act1 = () => _loader.GetPlugin(null!);
        var act2 = () => _loader.GetPlugin(string.Empty);

        // Assert
        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
    }
}
