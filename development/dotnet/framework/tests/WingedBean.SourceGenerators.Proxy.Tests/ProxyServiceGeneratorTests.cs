using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using Xunit;

namespace WingedBean.SourceGenerators.Proxy.Tests;

/// <summary>
/// Unit tests for ProxyServiceGenerator.
/// </summary>
public class ProxyServiceGeneratorTests
{
    [Fact]
    public void Generator_WithSimpleInterface_GeneratesProxyMethods()
    {
        // Arrange
        var source = @"
using WingedBean.Contracts.Core;

namespace TestNamespace
{
    public interface ITestService
    {
        void DoSomething();
        string GetSomething();
    }

    [RealizeService(typeof(ITestService))]
    [SelectionStrategy(SelectionMode.HighestPriority)]
    public partial class ProxyService : ITestService
    {
        private readonly IRegistry _registry;
        public ProxyService(IRegistry registry) { _registry = registry; }
    }
}

namespace WingedBean.Contracts.Core
{
    public interface IRegistry
    {
        TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    }

    public enum SelectionMode { One, HighestPriority, All }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class RealizeServiceAttribute : System.Attribute
    {
        public System.Type ServiceType { get; }
        public RealizeServiceAttribute(System.Type serviceType) { ServiceType = serviceType; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SelectionStrategyAttribute : System.Attribute
    {
        public SelectionMode Mode { get; }
        public SelectionStrategyAttribute(SelectionMode mode) { Mode = mode; }
    }
}
";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("public void DoSomething()");
        output.Should().Contain("public string GetSomething()");
        output.Should().Contain("service.DoSomething()");
        output.Should().Contain("return service.GetSomething()");
    }

    [Fact]
    public void Generator_WithGenericMethod_GeneratesCorrectConstraints()
    {
        // Arrange
        var source = @"
using WingedBean.Contracts.Core;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface ITestService
    {
        Task<T?> LoadAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;
    }

    [RealizeService(typeof(ITestService))]
    [SelectionStrategy(SelectionMode.HighestPriority)]
    public partial class ProxyService : ITestService
    {
        private readonly IRegistry _registry;
        public ProxyService(IRegistry registry) { _registry = registry; }
    }
}

namespace WingedBean.Contracts.Core
{
    public interface IRegistry
    {
        TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    }

    public enum SelectionMode { One, HighestPriority, All }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class RealizeServiceAttribute : System.Attribute
    {
        public System.Type ServiceType { get; }
        public RealizeServiceAttribute(System.Type serviceType) { ServiceType = serviceType; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SelectionStrategyAttribute : System.Attribute
    {
        public SelectionMode Mode { get; }
        public SelectionStrategyAttribute(SelectionMode mode) { Mode = mode; }
    }
}
";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("LoadAsync<T>");
        output.Should().Contain("where T : class");
    }

    [Fact]
    public void Generator_WithProperty_GeneratesGetterAndSetter()
    {
        // Arrange
        var source = @"
using WingedBean.Contracts.Core;

namespace TestNamespace
{
    public interface ITestService
    {
        float Volume { get; set; }
    }

    [RealizeService(typeof(ITestService))]
    [SelectionStrategy(SelectionMode.HighestPriority)]
    public partial class ProxyService : ITestService
    {
        private readonly IRegistry _registry;
        public ProxyService(IRegistry registry) { _registry = registry; }
    }
}

namespace WingedBean.Contracts.Core
{
    public interface IRegistry
    {
        TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    }

    public enum SelectionMode { One, HighestPriority, All }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class RealizeServiceAttribute : System.Attribute
    {
        public System.Type ServiceType { get; }
        public RealizeServiceAttribute(System.Type serviceType) { ServiceType = serviceType; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SelectionStrategyAttribute : System.Attribute
    {
        public SelectionMode Mode { get; }
        public SelectionStrategyAttribute(SelectionMode mode) { Mode = mode; }
    }
}
";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("public float Volume");
        output.Should().Contain("get");
        output.Should().Contain("set");
        output.Should().Contain("service.Volume");
    }

    [Fact]
    public void Generator_WithEvent_GeneratesAddAndRemove()
    {
        // Arrange
        var source = @"
using WingedBean.Contracts.Core;
using System;

namespace TestNamespace
{
    public interface ITestService
    {
        event Action<string> MessageReceived;
    }

    [RealizeService(typeof(ITestService))]
    [SelectionStrategy(SelectionMode.HighestPriority)]
    public partial class ProxyService : ITestService
    {
        private readonly IRegistry _registry;
        public ProxyService(IRegistry registry) { _registry = registry; }
    }
}

namespace WingedBean.Contracts.Core
{
    public interface IRegistry
    {
        TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    }

    public enum SelectionMode { One, HighestPriority, All }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class RealizeServiceAttribute : System.Attribute
    {
        public System.Type ServiceType { get; }
        public RealizeServiceAttribute(System.Type serviceType) { ServiceType = serviceType; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SelectionStrategyAttribute : System.Attribute
    {
        public SelectionMode Mode { get; }
        public SelectionStrategyAttribute(SelectionMode mode) { Mode = mode; }
    }
}
";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert
        diagnostics.Should().BeEmpty();
        output.Should().Contain("public event");
        output.Should().Contain("MessageReceived");
        output.Should().Contain("add");
        output.Should().Contain("remove");
        output.Should().Contain("service.MessageReceived += value");
        output.Should().Contain("service.MessageReceived -= value");
    }

    [Fact]
    public void Generator_WithoutRealizeServiceAttribute_DoesNotGenerate()
    {
        // Arrange
        var source = @"
using WingedBean.Contracts.Core;

namespace TestNamespace
{
    public interface ITestService
    {
        void DoSomething();
    }

    public partial class ProxyService : ITestService
    {
        private readonly IRegistry _registry;
        public ProxyService(IRegistry registry) { _registry = registry; }
    }
}

namespace WingedBean.Contracts.Core
{
    public interface IRegistry
    {
        TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    }

    public enum SelectionMode { One, HighestPriority, All }
}
";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert - Should not generate anything because attribute is missing
        output.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Generator_GeneratesCompilableCode()
    {
        // Arrange
        var source = @"
using WingedBean.Contracts.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IComplexService
    {
        void VoidMethod();
        string GetString();
        Task<int> AsyncMethod(string param, CancellationToken ct = default);
        T? GenericMethod<T>(string id) where T : class;
        float Volume { get; set; }
        event Action<string> DataReceived;
    }

    [RealizeService(typeof(IComplexService))]
    [SelectionStrategy(SelectionMode.HighestPriority)]
    public partial class ProxyService : IComplexService
    {
        private readonly IRegistry _registry;
        public ProxyService(IRegistry registry) { _registry = registry; }
    }
}

namespace WingedBean.Contracts.Core
{
    public interface IRegistry
    {
        TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    }

    public enum SelectionMode { One, HighestPriority, All }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public class RealizeServiceAttribute : System.Attribute
    {
        public System.Type ServiceType { get; }
        public RealizeServiceAttribute(System.Type serviceType) { ServiceType = serviceType; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SelectionStrategyAttribute : System.Attribute
    {
        public SelectionMode Mode { get; }
        public SelectionStrategyAttribute(SelectionMode mode) { Mode = mode; }
    }
}
";

        // Act
        var (diagnostics, output) = GetGeneratedOutput(source);

        // Assert - generated code should compile without errors
        diagnostics.Should().BeEmpty();
        
        // Verify all members are generated
        output.Should().Contain("public void VoidMethod()");
        output.Should().Contain("public string GetString()");
        output.Should().Contain("AsyncMethod"); // Just check method name exists
        output.Should().Contain("public T? GenericMethod<T>");
        output.Should().Contain("public float Volume");
        output.Should().Contain("public event");
        output.Should().Contain("DataReceived");
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ProxyServiceGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        var generatedSource = runResult.Results.Length > 0 && runResult.Results[0].GeneratedSources.Length > 0
            ? runResult.Results[0].GeneratedSources[0].SourceText.ToString()
            : string.Empty;

        return (diagnostics, generatedSource);
    }
}
