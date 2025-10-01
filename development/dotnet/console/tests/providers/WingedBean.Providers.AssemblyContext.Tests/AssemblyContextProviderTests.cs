using System.Reflection;
using FluentAssertions;
using Xunit;

namespace WingedBean.Providers.AssemblyContext.Tests;

/// <summary>
/// Unit tests for AssemblyContextProvider.
/// </summary>
public class AssemblyContextProviderTests : IDisposable
{
    private readonly AssemblyContextProvider _provider;
    private readonly List<string> _createdContexts = new();

    public AssemblyContextProviderTests()
    {
        _provider = new AssemblyContextProvider();
    }

    public void Dispose()
    {
        _provider?.Dispose();
    }

    [Fact]
    public void CreateContext_WithValidName_CreatesContext()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);

        // Act
        var result = _provider.CreateContext(contextName);

        // Assert
        result.Should().Be(contextName);
        _provider.ContextExists(contextName).Should().BeTrue();
    }

    [Fact]
    public void CreateContext_Collectible_CreatesCollectibleContext()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);

        // Act
        _provider.CreateContext(contextName, isCollectible: true);
        var alc = _provider.GetContext(contextName);

        // Assert
        alc.Should().NotBeNull();
        alc!.IsCollectible.Should().BeTrue();
    }

    [Fact]
    public void CreateContext_NonCollectible_CreatesNonCollectibleContext()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);

        // Act
        _provider.CreateContext(contextName, isCollectible: false);
        var alc = _provider.GetContext(contextName);

        // Assert
        alc.Should().NotBeNull();
        alc!.IsCollectible.Should().BeFalse();
    }

    [Fact]
    public void CreateContext_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);
        _provider.CreateContext(contextName);

        // Act
        var act = () => _provider.CreateContext(contextName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Context '{contextName}' already exists");
    }

    [Fact]
    public void CreateContext_WithNullOrEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var act1 = () => _provider.CreateContext(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => _provider.CreateContext(string.Empty);
        act2.Should().Throw<ArgumentException>();

        var act3 = () => _provider.CreateContext("   ");
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LoadAssembly_WithValidPath_LoadsAssembly()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);
        _provider.CreateContext(contextName);

        // Use the currently executing assembly as test assembly
        var testAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act
        var assembly = _provider.LoadAssembly(contextName, testAssemblyPath);

        // Assert
        assembly.Should().NotBeNull();
        assembly.Location.Should().Be(testAssemblyPath);
    }

    [Fact]
    public void LoadAssembly_WithNonExistentContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var contextName = "NonExistentContext";
        var testAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act
        var act = () => _provider.LoadAssembly(contextName, testAssemblyPath);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Context '{contextName}' not found");
    }

    [Fact]
    public void LoadAssembly_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);
        _provider.CreateContext(contextName);
        var nonExistentPath = "/path/to/nonexistent.dll";

        // Act
        var act = () => _provider.LoadAssembly(contextName, nonExistentPath);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void LoadAssembly_WithNullOrEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);
        _provider.CreateContext(contextName);

        // Act & Assert
        var act1 = () => _provider.LoadAssembly(contextName, null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => _provider.LoadAssembly(contextName, string.Empty);
        act2.Should().Throw<ArgumentException>();

        var act3 = () => _provider.LoadAssembly(contextName, "   ");
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetContext_WithExistingContext_ReturnsContext()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);
        _provider.CreateContext(contextName);

        // Act
        var alc = _provider.GetContext(contextName);

        // Assert
        alc.Should().NotBeNull();
        alc!.Name.Should().Be(contextName);
    }

    [Fact]
    public void GetContext_WithNonExistentContext_ReturnsNull()
    {
        // Arrange
        var contextName = "NonExistentContext";

        // Act
        var alc = _provider.GetContext(contextName);

        // Assert
        alc.Should().BeNull();
    }

    [Fact]
    public async Task UnloadContextAsync_WithExistingContext_UnloadsContext()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _provider.CreateContext(contextName);

        // Act
        await _provider.UnloadContextAsync(contextName, waitForUnload: true);

        // Assert
        _provider.ContextExists(contextName).Should().BeFalse();
        _provider.GetContext(contextName).Should().BeNull();
    }

    [Fact]
    public async Task UnloadContextAsync_WithNonExistentContext_DoesNotThrow()
    {
        // Arrange
        var contextName = "NonExistentContext";

        // Act
        Func<Task> act = async () => await _provider.UnloadContextAsync(contextName);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ContextExists_WithExistingContext_ReturnsTrue()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName);
        _provider.CreateContext(contextName);

        // Act
        var exists = _provider.ContextExists(contextName);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public void ContextExists_WithNonExistentContext_ReturnsFalse()
    {
        // Arrange
        var contextName = "NonExistentContext";

        // Act
        var exists = _provider.ContextExists(contextName);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void GetLoadedContexts_WithMultipleContexts_ReturnsAllContextNames()
    {
        // Arrange
        var contextName1 = $"TestContext1_{Guid.NewGuid():N}";
        var contextName2 = $"TestContext2_{Guid.NewGuid():N}";
        _createdContexts.Add(contextName1);
        _createdContexts.Add(contextName2);

        _provider.CreateContext(contextName1);
        _provider.CreateContext(contextName2);

        // Act
        var contexts = _provider.GetLoadedContexts().ToList();

        // Assert
        contexts.Should().HaveCountGreaterOrEqualTo(2);
        contexts.Should().Contain(contextName1);
        contexts.Should().Contain(contextName2);
    }

    [Fact]
    public void GetLoadedContexts_WithNoContexts_ReturnsEmptyCollection()
    {
        // Arrange
        var provider = new AssemblyContextProvider();

        // Act
        var contexts = provider.GetLoadedContexts().ToList();

        // Assert
        contexts.Should().BeEmpty();

        provider.Dispose();
    }

    [Fact]
    public void Dispose_WithMultipleContexts_DisposesAllContexts()
    {
        // Arrange
        var provider = new AssemblyContextProvider();
        var contextName1 = $"TestContext1_{Guid.NewGuid():N}";
        var contextName2 = $"TestContext2_{Guid.NewGuid():N}";

        provider.CreateContext(contextName1);
        provider.CreateContext(contextName2);

        // Verify contexts exist before dispose
        provider.GetLoadedContexts().Should().HaveCount(2);

        // Act
        provider.Dispose();

        // Assert - after dispose, calling GetLoadedContexts should throw
        var act = () => provider.GetLoadedContexts();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void CreateContext_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var provider = new AssemblyContextProvider();
        provider.Dispose();

        // Act
        var act = () => provider.CreateContext("TestContext");

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }
}
