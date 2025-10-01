# WingedBean.Plugins.Config

Configuration service plugin for WingedBean platform using Microsoft.Extensions.Configuration.

## Overview

This plugin provides a Tier 3 implementation of `IConfigService` that wraps Microsoft.Extensions.Configuration functionality. It supports:

- **JSON configuration files** (appsettings.json with auto-reload)
- **Environment variables**
- **In-memory configuration sources**
- **Strongly-typed configuration binding**
- **Configuration change notifications**

## Features

- ✅ Implements `IConfigService` interface from WingedBean.Contracts.Config
- ✅ Supports hierarchical configuration with colon-separated keys
- ✅ Read configuration from appsettings.json
- ✅ Read configuration from environment variables
- ✅ In-memory configuration updates via `Set()` method
- ✅ Strongly-typed configuration access via `Get<T>()`
- ✅ Configuration sections with binding support
- ✅ Configuration change events
- ✅ Automatic reload on file changes

## Usage

### Basic Usage

```csharp
// Create service with default configuration sources
var configService = new ConfigService();

// Get string value
string? value = configService.Get("MyKey");

// Get strongly-typed value
int port = configService.Get<int>("Server:Port");

// Check if key exists
if (configService.Exists("Database:ConnectionString"))
{
    // ...
}

// Set value (in-memory only)
configService.Set("MyKey", "NewValue");
```

### Working with Sections

```csharp
var section = configService.GetSection("Database");

// Access child values
var host = section.GetSection("Host").Value;

// Bind to object
var dbOptions = new DatabaseOptions();
section.Bind(dbOptions);

// Or get as typed object
var dbOptions = section.Get<DatabaseOptions>();
```

### Configuration Change Events

```csharp
configService.ConfigChanged += (sender, e) =>
{
    Console.WriteLine($"Config changed: {e.Key}");
    Console.WriteLine($"Old: {e.OldValue}, New: {e.NewValue}");
};
```

### Custom Configuration Sources

```csharp
// Create custom configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("custom-config.json", optional: true)
    .AddEnvironmentVariables("MYAPP_")
    .Build();

var configService = new ConfigService(configuration);
```

## Configuration File Format

The plugin looks for `appsettings.json` in the current directory:

```json
{
  "Server": {
    "Port": 8080,
    "Host": "localhost"
  },
  "Database": {
    "ConnectionString": "...",
    "Timeout": 30
  },
  "Plugins": {
    "Load": ["plugin1", "plugin2"]
  }
}
```

Access nested values using colon-separated keys:
- `configService.Get("Server:Port")` → `"8080"`
- `configService.Get<int>("Server:Port")` → `8080`

## Plugin Metadata

This plugin is decorated with the `[Plugin]` attribute:

```csharp
[Plugin(
    Name = "ConfigService",
    Provides = new[] { typeof(IConfigService) },
    Priority = 100
)]
```

## Dependencies

- Microsoft.Extensions.Configuration 9.0.0
- Microsoft.Extensions.Configuration.Json 9.0.0
- Microsoft.Extensions.Configuration.EnvironmentVariables 9.0.0
- Microsoft.Extensions.Configuration.Binder 9.0.0
- WingedBean.Contracts.Core
- WingedBean.Contracts.Config

## Building

```bash
dotnet build WingedBean.Plugins.Config.csproj
```

## Testing

```bash
dotnet test ../../../tests/WingedBean.Plugins.Config.Tests/WingedBean.Plugins.Config.Tests.csproj
```

## Notes

- The `Set()` method updates values in-memory only and does not persist to configuration files
- File-based configuration sources support automatic reload when files change
- Environment variable configuration uses standard environment variable conventions
- Configuration keys are case-insensitive (inherited from Microsoft.Extensions.Configuration behavior)

## See Also

- [WingedBean.Contracts.Config](../../../../../framework/src/WingedBean.Contracts.Config/)
- [Microsoft.Extensions.Configuration Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
