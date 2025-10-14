# Solution File Additions Required

**Date**: 2025-10-14  
**Purpose**: Add test projects and Sentry provider to WingedBean.sln

## Projects to Add

### 1. Sentry Provider Plugin
```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Diagnostics.Sentry", "framework\src\plugins\WingedBean.Plugins.Diagnostics.Sentry\WingedBean.Plugins.Diagnostics.Sentry.csproj", "{GUID-SENTRY}"
EndProject
```

### 2. Framework Test Projects (9 projects)

#### Diagnostics Tests
```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Diagnostics.Tests", "framework\tests\WingedBean.Plugins.Diagnostics.Tests\WingedBean.Plugins.Diagnostics.Tests.csproj", "{GUID-DIAG-TEST}"
EndProject

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Diagnostics.Sentry.Tests", "framework\tests\WingedBean.Plugins.Diagnostics.Sentry.Tests\WingedBean.Plugins.Diagnostics.Sentry.Tests.csproj", "{GUID-SENTRY-TEST}"
EndProject
```

#### Core Plugin Tests
```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.ArchECS.Tests", "framework\tests\WingedBean.Plugins.ArchECS.Tests\WingedBean.Plugins.ArchECS.Tests.csproj", "{GUID-ARCHECS-TEST}"
EndProject

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Config.Tests", "framework\tests\WingedBean.Plugins.Config.Tests\WingedBean.Plugins.Config.Tests.csproj", "{GUID-CONFIG-TEST}"
EndProject

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Resource.Tests", "framework\tests\WingedBean.Plugins.Resource.Tests\WingedBean.Plugins.Resource.Tests.csproj", "{GUID-RESOURCE-TEST}"
EndProject

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Resource.NuGet.Tests", "framework\tests\WingedBean.Plugins.Resource.NuGet.Tests\WingedBean.Plugins.Resource.NuGet.Tests.csproj", "{GUID-RESOURCE-NUGET-TEST}"
EndProject

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Audio.Tests", "framework\tests\WingedBean.Plugins.Audio.Tests\WingedBean.Plugins.Audio.Tests.csproj", "{GUID-AUDIO-TEST}"
EndProject

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Analytics.Tests", "framework\tests\WingedBean.Plugins.Analytics.Tests\WingedBean.Plugins.Analytics.Tests.csproj", "{GUID-ANALYTICS-TEST}"
EndProject

Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.WebSocket.Tests", "framework\tests\WingedBean.Plugins.WebSocket.Tests\WingedBean.Plugins.WebSocket.Tests.csproj", "{GUID-WEBSOCKET-TEST}"
EndProject
```

## Instructions

### Option 1: Manual Addition (Recommended)
1. Open `WingedBean.sln` in Visual Studio or Rider
2. Right-click solution → Add → Existing Project
3. Navigate to each test project and add it
4. IDE will generate GUIDs automatically
5. Save solution

### Option 2: Command Line (Automated)
```bash
cd development/dotnet

# Add Sentry provider
dotnet sln WingedBean.sln add framework/src/plugins/WingedBean.Plugins.Diagnostics.Sentry/WingedBean.Plugins.Diagnostics.Sentry.csproj

# Add test projects
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.Diagnostics.Tests/WingedBean.Plugins.Diagnostics.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.Diagnostics.Sentry.Tests/WingedBean.Plugins.Diagnostics.Sentry.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.ArchECS.Tests/WingedBean.Plugins.ArchECS.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.Config.Tests/WingedBean.Plugins.Config.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.Resource.Tests/WingedBean.Plugins.Resource.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.Resource.NuGet.Tests/WingedBean.Plugins.Resource.NuGet.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.Audio.Tests/WingedBean.Plugins.Audio.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.Analytics.Tests/WingedBean.Plugins.Analytics.Tests.csproj
dotnet sln WingedBean.sln add framework/tests/WingedBean.Plugins.WebSocket.Tests/WingedBean.Plugins.WebSocket.Tests.csproj
```

### Option 3: Script (Batch)
```bash
#!/bin/bash
cd development/dotnet

# Array of projects to add
projects=(
  "framework/src/plugins/WingedBean.Plugins.Diagnostics.Sentry/WingedBean.Plugins.Diagnostics.Sentry.csproj"
  "framework/tests/WingedBean.Plugins.Diagnostics.Tests/WingedBean.Plugins.Diagnostics.Tests.csproj"
  "framework/tests/WingedBean.Plugins.Diagnostics.Sentry.Tests/WingedBean.Plugins.Diagnostics.Sentry.Tests.csproj"
  "framework/tests/WingedBean.Plugins.ArchECS.Tests/WingedBean.Plugins.ArchECS.Tests.csproj"
  "framework/tests/WingedBean.Plugins.Config.Tests/WingedBean.Plugins.Config.Tests.csproj"
  "framework/tests/WingedBean.Plugins.Resource.Tests/WingedBean.Plugins.Resource.Tests.csproj"
  "framework/tests/WingedBean.Plugins.Resource.NuGet.Tests/WingedBean.Plugins.Resource.NuGet.Tests.csproj"
  "framework/tests/WingedBean.Plugins.Audio.Tests/WingedBean.Plugins.Audio.Tests.csproj"
  "framework/tests/WingedBean.Plugins.Analytics.Tests/WingedBean.Plugins.Analytics.Tests.csproj"
  "framework/tests/WingedBean.Plugins.WebSocket.Tests/WingedBean.Plugins.WebSocket.Tests.csproj"
)

# Add each project
for project in "${projects[@]}"; do
  echo "Adding $project..."
  dotnet sln WingedBean.sln add "$project"
done

echo "✅ All projects added to solution!"
```

## Verification

After adding projects, verify:

```bash
# List all projects in solution
dotnet sln WingedBean.sln list

# Build solution
dotnet build WingedBean.sln

# Run tests
dotnet test WingedBean.sln
```

## Expected Output

```
Project(s) successfully added to the solution file.
```

Then:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Troubleshooting

### Issue: "Project already in solution"
**Solution**: Project was already added, skip it

### Issue: "Could not find project file"
**Solution**: Verify path is correct relative to solution file

### Issue: Build errors after adding
**Solution**: 
1. Check project references are correct
2. Verify Directory.Packages.props has all dependencies
3. Run `dotnet restore` first

## Next Steps After Addition

1. ✅ Verify solution loads in IDE
2. ✅ Build entire solution
3. ✅ Run all tests
4. ✅ Check for any compilation errors
5. ✅ Generate coverage report

---

**Recommended Approach**: Use **Option 2 (Command Line)** for automation and consistency.
