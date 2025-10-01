using Microsoft.CodeAnalysis;

namespace WingedBean.SourceGenerators.Proxy;

[Generator]
public class ProxyServiceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // TODO: Register syntax receivers for [RealizeService] attributes
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // TODO: Generate proxy service implementations
    }
}
