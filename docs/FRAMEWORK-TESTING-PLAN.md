# Framework Testing Plan

**Date**: 2025-10-14  
**Status**: Planning → Implementation  
**Scope**: All projects under `development/dotnet/framework/src/`

## Overview

Create comprehensive test coverage for all framework projects (Tier 3 services and Tier 4 providers).

## Current Test Coverage

### ✅ Existing Tests
```
framework/tests/
├── WingedBean.FigmaSharp.Core.Tests/
├── WingedBean.Gameplay.Goap.Smoke/
├── WingedBean.Hosting.Tests/
├── WingedBean.PluginSystem.Tests/
└── WingedBean.Registry.Tests/
```

### ❌ Missing Tests

#### Framework Libraries
1. **WingedBean.Contracts.Gameplay** - No tests
2. **WingedBean.Gameplay.AI** - No tests
3. **WingedBean.Gameplay.Goap** - Only smoke tests
4. **WingedBean.Reporting.Runtime** - No tests
5. **WingedBean.Testing.Recorder** - No tests

#### Framework Plugins (Tier 3 Services)
1. **WingedBean.Plugins.Analytics** - No tests
2. **WingedBean.Plugins.ArchECS** - No tests
3. **WingedBean.Plugins.Audio** - No tests
4. **WingedBean.Plugins.Config** - No tests
5. **WingedBean.Plugins.Diagnostics** - No tests ⚠️ **Priority**
6. **WingedBean.Plugins.Resource** - No tests
7. **WingedBean.Plugins.Resource.NuGet** - No tests
8. **WingedBean.Plugins.WebSocket** - No tests

#### Provider Plugins (Tier 4)
1. **WingedBean.Plugins.Diagnostics.Sentry** - No tests ⚠️ **Priority**
2. **WingedBean.Plugins.Diagnostics.OpenTelemetry** - No tests (not yet created)
3. **WingedBean.Plugins.Diagnostics.Firebase** - No tests (not yet created)

## Test Project Structure

### Naming Convention
```
{ProjectName}.Tests/
├── {FeatureName}Tests.cs
├── {ServiceName}Tests.cs
└── Helpers/
    └── TestHelpers.cs
```

### Standard Test Project Template
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="Moq" /> <!-- For mocking -->
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/{ProjectName}/{ProjectName}.csproj" />
  </ItemGroup>
</Project>
```

## Priority Test Projects

### Phase 1: Diagnostics (High Priority)

#### 1.1 WingedBean.Plugins.Diagnostics.Tests
**Location**: `framework/tests/WingedBean.Plugins.Diagnostics.Tests/`

**Test Classes**:
- `DiagnosticsServiceTests.cs`
  - Provider registration/unregistration
  - Backend broadcasting
  - Span lifecycle
  - Breadcrumb management
  - Error recording
  - Metrics recording
  - Health checks
  - Alert management

- `IDiagnosticsBackendTests.cs`
  - Interface contract tests
  - Mock backend implementation

- `InMemoryDiagnosticsBackendTests.cs`
  - In-memory backend functionality
  - Data retention
  - Export functionality

**Estimated Effort**: 4-6 hours

#### 1.2 WingedBean.Plugins.Diagnostics.Sentry.Tests
**Location**: `framework/tests/WingedBean.Plugins.Diagnostics.Sentry.Tests/`

**Test Classes**:
- `SentryDiagnosticsBackendTests.cs`
  - Sentry SDK initialization
  - Span tracking
  - Breadcrumb recording
  - Exception capture
  - Metrics recording (as events)
  - Health check recording
  - Flush functionality

- `SentryDiagnosticsPluginActivatorTests.cs`
  - Plugin activation
  - Configuration loading
  - Service registration
  - Backend registration

**Mocking Strategy**:
- Mock Sentry SDK calls
- Use in-memory Sentry transport for testing
- Verify SDK method calls without actual Sentry connection

**Estimated Effort**: 3-4 hours

### Phase 2: Core Plugins (Medium Priority)

#### 2.1 WingedBean.Plugins.ArchECS.Tests
**Test Classes**:
- `ArchECSServiceTests.cs`
- `EntityManagementTests.cs`
- `ComponentSystemTests.cs`
- `QueryTests.cs`

**Estimated Effort**: 4-5 hours

#### 2.2 WingedBean.Plugins.Config.Tests
**Test Classes**:
- `ConfigServiceTests.cs`
- `ConfigurationLoadingTests.cs`
- `EnvironmentVariableTests.cs`
- `JsonConfigurationTests.cs`

**Estimated Effort**: 2-3 hours

#### 2.3 WingedBean.Plugins.Resource.Tests
**Test Classes**:
- `ResourceServiceTests.cs`
- `FileSystemResourceTests.cs`
- `ResourceCachingTests.cs`

**Estimated Effort**: 3-4 hours

#### 2.4 WingedBean.Plugins.Resource.NuGet.Tests
**Test Classes**:
- `NuGetResourceProviderTests.cs`
- `PackageDownloadTests.cs`
- `PackageResolutionTests.cs`

**Estimated Effort**: 3-4 hours

### Phase 3: Specialized Plugins (Lower Priority)

#### 3.1 WingedBean.Plugins.Audio.Tests
**Test Classes**:
- `AudioServiceTests.cs`
- `LibVLCIntegrationTests.cs`
- `PlaybackTests.cs`

**Estimated Effort**: 3-4 hours

#### 3.2 WingedBean.Plugins.Analytics.Tests
**Test Classes**:
- `AnalyticsServiceTests.cs`
- `FirebaseAnalyticsTests.cs`
- `EventTrackingTests.cs`

**Estimated Effort**: 2-3 hours

#### 3.3 WingedBean.Plugins.WebSocket.Tests
**Test Classes**:
- `WebSocketServiceTests.cs`
- `ConnectionManagementTests.cs`
- `MessageHandlingTests.cs`

**Estimated Effort**: 3-4 hours

### Phase 4: Framework Libraries

#### 4.1 WingedBean.Gameplay.AI.Tests
**Test Classes**:
- `AISystemTests.cs`
- `BehaviorTreeTests.cs`
- `DecisionMakingTests.cs`

**Estimated Effort**: 4-5 hours

#### 4.2 WingedBean.Gameplay.Goap.Tests (Expand)
**Test Classes**:
- `GoapPlannerTests.cs`
- `ActionTests.cs`
- `GoalTests.cs`
- `WorldStateTests.cs`

**Estimated Effort**: 3-4 hours

## Test Patterns and Best Practices

### 1. Arrange-Act-Assert (AAA)
```csharp
[Fact]
public void RegisterBackend_ShouldAddBackendToCollection()
{
    // Arrange
    var service = new DiagnosticsService(logger, config);
    var backend = new InMemoryDiagnosticsBackend();

    // Act
    service.RegisterBackend("test", backend);

    // Assert
    service.GetRegisteredBackends().Should().Contain("test");
}
```

### 2. Mocking External Dependencies
```csharp
[Fact]
public void SentryBackend_ShouldInitializeSdk()
{
    // Arrange
    var loggerMock = new Mock<ILogger<SentryDiagnosticsBackend>>();
    var dsn = "https://test@sentry.io/1";

    // Act
    var backend = new SentryDiagnosticsBackend(loggerMock.Object, dsn);

    // Assert
    // Verify Sentry SDK was initialized
    loggerMock.Verify(
        x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("initialized")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
}
```

### 3. Integration Tests
```csharp
[Fact]
public async Task MultipleBackends_ShouldReceiveAllEvents()
{
    // Arrange
    var service = new DiagnosticsService(logger, config);
    var backend1 = new InMemoryDiagnosticsBackend();
    var backend2 = new InMemoryDiagnosticsBackend();
    
    service.RegisterBackend("backend1", backend1);
    service.RegisterBackend("backend2", backend2);

    // Act
    using (service.StartSpan("test-operation"))
    {
        // Operation
    }

    // Assert
    backend1.GetSpans().Should().HaveCount(1);
    backend2.GetSpans().Should().HaveCount(1);
}
```

### 4. Async Testing
```csharp
[Fact]
public async Task FlushAsync_ShouldBroadcastToAllBackends()
{
    // Arrange
    var service = new DiagnosticsService(logger, config);
    var backend1 = Mock.Of<IDiagnosticsBackend>();
    var backend2 = Mock.Of<IDiagnosticsBackend>();
    
    service.RegisterBackend("backend1", backend1);
    service.RegisterBackend("backend2", backend2);

    // Act
    await service.FlushAsync();

    // Assert
    Mock.Get(backend1).Verify(x => x.FlushAsync(), Times.Once);
    Mock.Get(backend2).Verify(x => x.FlushAsync(), Times.Once);
}
```

## Test Coverage Goals

| Category | Target Coverage | Priority |
|----------|----------------|----------|
| Diagnostics (Base) | 80%+ | High |
| Diagnostics Providers | 70%+ | High |
| Core Plugins | 70%+ | Medium |
| Specialized Plugins | 60%+ | Medium |
| Framework Libraries | 60%+ | Low |

## Test Execution

### Running Tests
```bash
# All framework tests
cd development/dotnet/framework/tests
dotnet test

# Specific test project
dotnet test WingedBean.Plugins.Diagnostics.Tests/

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### CI/CD Integration
- Run on every PR
- Generate coverage reports
- Fail build if coverage drops below threshold
- Run integration tests in separate pipeline

## Implementation Timeline

### Week 1: Diagnostics (Priority)
- Day 1-2: Diagnostics base plugin tests
- Day 3-4: Sentry provider tests
- Day 5: Integration tests and documentation

### Week 2: Core Plugins
- Day 1-2: ArchECS tests
- Day 3: Config tests
- Day 4-5: Resource & Resource.NuGet tests

### Week 3: Specialized Plugins
- Day 1-2: Audio & Analytics tests
- Day 3: WebSocket tests
- Day 4-5: Framework library tests

### Week 4: Polish & Documentation
- Day 1-2: Integration tests
- Day 3-4: Performance tests
- Day 5: Documentation and examples

## Success Criteria

- [ ] All framework plugins have test projects
- [ ] Test coverage > 70% for high-priority plugins
- [ ] All tests pass in CI/CD
- [ ] Integration tests verify multi-provider scenarios
- [ ] Documentation includes test examples
- [ ] Test execution time < 5 minutes for all tests

## Dependencies

### Required Packages
- xunit (test framework)
- FluentAssertions (assertions)
- Moq (mocking)
- coverlet.collector (coverage)
- Microsoft.NET.Test.Sdk (test SDK)

### Optional Packages
- Testcontainers (for integration tests)
- Bogus (test data generation)
- AutoFixture (test object creation)

## Next Steps

1. ✅ Create test plan (this document)
2. ⏳ Create Diagnostics.Tests project
3. ⏳ Create Diagnostics.Sentry.Tests project
4. ⏳ Implement core test cases
5. ⏳ Add to solution file
6. ⏳ Configure CI/CD
7. ⏳ Document test patterns

---

**Status**: Ready for Implementation  
**First Priority**: WingedBean.Plugins.Diagnostics.Tests  
**Estimated Total Effort**: 40-50 hours across all phases
