# HybridCLR Setup Guide for WingedBean Unity Plugin System

This guide walks you through setting up HybridCLR in your Unity project to enable hot-reload functionality for WingedBean plugins.

## Table of Contents

1. [What is HybridCLR?](#what-is-hybridclr)
2. [Prerequisites](#prerequisites)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Integration with WingedBean](#integration-with-wingedbean)
6. [Testing Hot-Reload](#testing-hot-reload)
7. [Troubleshooting](#troubleshooting)

## What is HybridCLR?

HybridCLR is a revolutionary Unity hot update solution that enables:

- **Hot Assembly Loading**: Load C# assemblies at runtime without restarting
- **Hot Code Updates**: Update game logic without rebuilding the entire application
- **Cross-Platform Support**: Works on all Unity-supported platforms
- **Zero Performance Overhead**: Native execution speed after loading

The WingedBean Unity Plugin System leverages HybridCLR to provide seamless hot-reload capabilities for plugins.

## Prerequisites

- Unity 2022.3 LTS or later
- .NET 9.0 SDK
- Git (for HybridCLR installation)
- Visual Studio or VS Code with C# extension

## Installation

### 1. Install HybridCLR Package

There are two ways to install HybridCLR:

#### Option A: Package Manager (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/focus-creative-games/hybridclr_unity.git`
4. Click `Add`

#### Option B: Manual Installation

1. Clone the HybridCLR repository:
```bash
git clone https://github.com/focus-creative-games/hybridclr_unity.git
```

2. Copy the `hybridclr_unity` folder to your Unity project's `Packages` directory

### 2. Install HybridCLR Native Runtime

After installing the package, you need to install the native runtime:

1. Open `HybridCLR > Installer` from the Unity menu
2. Click `Install` to download and install the native runtime
3. Wait for the installation to complete

## Configuration

### 1. Basic HybridCLR Setup

#### Enable HybridCLR in Build Settings

1. Open `HybridCLR > Settings` from Unity menu
2. Configure the following settings:

```csharp
// HybridCLR Settings
Enable HybridCLR: ✓ Enabled
Hot Update Assemblies: Add your plugin assemblies
AOT Generic References: Auto-generate
```

#### Configure Hot Update Assemblies

In the HybridCLR Settings window, add your plugin assemblies to the `Hot Update Assemblies` list:

- `WingedBean.Host.Unity`
- `SampleUnityPlugin` (or your plugin assembly name)
- Any other plugin assemblies you want to hot-reload

### 2. Unity Project Configuration

#### Player Settings

Configure Unity Player Settings for HybridCLR:

1. Open `File > Build Settings > Player Settings`
2. Under `Configuration`:
   - Set `Scripting Backend` to `IL2CPP`
   - Set `Api Compatibility Level` to `.NET Standard 2.1`
   - Enable `Allow 'unsafe' code` if needed

#### IL2CPP Settings

1. In Player Settings, under `Publishing Settings > IL2CPP`:
   - Set `C++ Compiler Configuration` to `Release` for production
   - Enable `Script Debugging` for development builds

### 3. WingedBean Integration Configuration

#### Create HybridCLR Configuration Script

Create `Assets/Scripts/HybridCLRConfig.cs`:

```csharp
using HybridCLR;
using System.Collections.Generic;
using UnityEngine;

namespace WingedBean.Unity.HybridCLR
{
    [CreateAssetMenu(fileName = "HybridCLRConfig", menuName = "WingedBean/HybridCLR Config")]
    public class HybridCLRConfig : ScriptableObject
    {
        [Header("Hot Update Assemblies")]
        [SerializeField] private List<string> _hotUpdateAssemblies = new List<string>
        {
            "WingedBean.Host.Unity",
            "SampleUnityPlugin"
        };

        [Header("AOT Metadata Assemblies")]
        [SerializeField] private List<string> _aotMetadataAssemblies = new List<string>
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
            "UnityEngine.CoreModule.dll"
        };

        public List<string> HotUpdateAssemblies => _hotUpdateAssemblies;
        public List<string> AOTMetadataAssemblies => _aotMetadataAssemblies;
    }
}
```

#### Create HybridCLR Bootstrap

Create `Assets/Scripts/HybridCLRBootstrap.cs`:

```csharp
using HybridCLR;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace WingedBean.Unity.HybridCLR
{
    public class HybridCLRBootstrap : MonoBehaviour
    {
        [SerializeField] private HybridCLRConfig _config;
        [SerializeField] private bool _loadOnStart = true;

        private void Start()
        {
            if (_loadOnStart)
            {
                StartCoroutine(InitializeHybridCLR());
            }
        }

        public IEnumerator InitializeHybridCLR()
        {
            Debug.Log("[HybridCLR] Initializing HybridCLR...");

            // Load AOT metadata assemblies
            yield return StartCoroutine(LoadAOTMetadataAssemblies());

            // Load hot update assemblies
            yield return StartCoroutine(LoadHotUpdateAssemblies());

            Debug.Log("[HybridCLR] HybridCLR initialization complete");

            // Initialize WingedBean plugin system
            yield return StartCoroutine(InitializePluginSystem());
        }

        private IEnumerator LoadAOTMetadataAssemblies()
        {
            Debug.Log("[HybridCLR] Loading AOT metadata assemblies...");

            foreach (var assembly in _config.AOTMetadataAssemblies)
            {
                var path = GetAssemblyPath(assembly);
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                    Debug.Log($"[HybridCLR] Loaded AOT metadata: {assembly}");
                }
                else
                {
                    Debug.LogWarning($"[HybridCLR] AOT metadata not found: {path}");
                }

                yield return null; // Yield to prevent frame drops
            }
        }

        private IEnumerator LoadHotUpdateAssemblies()
        {
            Debug.Log("[HybridCLR] Loading hot update assemblies...");

            foreach (var assembly in _config.HotUpdateAssemblies)
            {
                var path = GetAssemblyPath(assembly + ".dll");
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    System.Reflection.Assembly.Load(bytes);
                    Debug.Log($"[HybridCLR] Loaded hot update assembly: {assembly}");
                }
                else
                {
                    Debug.LogWarning($"[HybridCLR] Hot update assembly not found: {path}");
                }

                yield return null;
            }
        }

        private IEnumerator InitializePluginSystem()
        {
            Debug.Log("[HybridCLR] Initializing WingedBean plugin system...");

            // Find and initialize the plugin host
            var pluginHost = FindObjectOfType<UnityPluginHost>();
            if (pluginHost != null)
            {
                yield return StartCoroutine(pluginHost.InitializeAsync());
            }
            else
            {
                Debug.LogError("[HybridCLR] UnityPluginHost not found in scene");
            }
        }

        private string GetAssemblyPath(string assemblyName)
        {
            // Try different possible paths
            var paths = new[]
            {
                Path.Combine(Application.streamingAssetsPath, "HybridCLR", assemblyName),
                Path.Combine(Application.persistentDataPath, "HybridCLR", assemblyName),
                Path.Combine(Application.dataPath, "Plugins", assemblyName)
            };

            foreach (var path in paths)
            {
                if (File.Exists(path))
                    return path;
            }

            return Path.Combine(Application.streamingAssetsPath, "HybridCLR", assemblyName);
        }
    }
}
```

### 4. Build Process Configuration

#### Create Build Helper

Create `Assets/Editor/HybridCLRBuildHelper.cs`:

```csharp
#if UNITY_EDITOR
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WingedBean.Unity.HybridCLR.Editor
{
    public static class HybridCLRBuildHelper
    {
        [MenuItem("WingedBean/HybridCLR/Build Hot Update Assemblies")]
        public static void BuildHotUpdateAssemblies()
        {
            Debug.Log("[HybridCLR] Building hot update assemblies...");

            // Generate AOT generic references
            PrebuildCommand.GenerateAll();

            // Build hot update assemblies
            CompileDllCommand.CompileDll(EditorUserBuildSettings.activeBuildTarget);

            Debug.Log("[HybridCLR] Hot update assemblies built successfully");
        }

        [MenuItem("WingedBean/HybridCLR/Copy Assemblies to StreamingAssets")]
        public static void CopyAssembliesToStreamingAssets()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var hotUpdateDllsPath = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(buildTarget);
            var streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets", "HybridCLR");

            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
            }

            // Copy hot update assemblies
            var hotUpdateAssemblies = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;
            foreach (var assembly in hotUpdateAssemblies)
            {
                var srcPath = Path.Combine(hotUpdateDllsPath, assembly + ".dll");
                var dstPath = Path.Combine(streamingAssetsPath, assembly + ".dll");

                if (File.Exists(srcPath))
                {
                    File.Copy(srcPath, dstPath, true);
                    Debug.Log($"[HybridCLR] Copied {assembly}.dll to StreamingAssets");
                }
            }

            // Copy AOT metadata assemblies
            var aotMetadataPath = SettingsUtil.GetAssembliesPostIl2CppStripDir(buildTarget);
            var aotAssemblies = new[]
            {
                "mscorlib.dll",
                "System.dll",
                "System.Core.dll",
                "UnityEngine.CoreModule.dll"
            };

            foreach (var assembly in aotAssemblies)
            {
                var srcPath = Path.Combine(aotMetadataPath, assembly);
                var dstPath = Path.Combine(streamingAssetsPath, assembly);

                if (File.Exists(srcPath))
                {
                    File.Copy(srcPath, dstPath, true);
                    Debug.Log($"[HybridCLR] Copied AOT metadata {assembly} to StreamingAssets");
                }
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("WingedBean/HybridCLR/Build and Copy All")]
        public static void BuildAndCopyAll()
        {
            BuildHotUpdateAssemblies();
            CopyAssembliesToStreamingAssets();
        }
    }
}
#endif
```

## Integration with WingedBean

### 1. Update Plugin Loader

The `HybridClrPluginLoader` is already configured to work with HybridCLR. Ensure it's properly integrated:

```csharp
// In your Unity scene setup
public class PluginSystemInitializer : MonoBehaviour
{
    private async void Start()
    {
        // Wait for HybridCLR initialization
        var bootstrap = FindObjectOfType<HybridCLRBootstrap>();
        if (bootstrap != null)
        {
            yield return StartCoroutine(bootstrap.InitializeHybridCLR());
        }

        // Initialize plugin system
        var services = new ServiceCollection();
        services.AddWingedBeanHost();
        services.AddUnityPluginSupport();

        var serviceProvider = services.BuildServiceProvider();
        var pluginHost = serviceProvider.GetRequiredService<IPluginHost>();

        await pluginHost.InitializeAsync();
    }
}
```

### 2. Configure Plugin Manifest for HybridCLR

Update your plugin manifests to work with HybridCLR:

```json
{
  "id": "com.example.hotreloadplugin",
  "version": "1.0.0",
  "name": "Hot Reload Plugin",
  
  "entryPoint": {
    "unity": "./HotReloadPlugin.dll"
  },
  
  "unity": {
    "supportsHotReload": true,
    "hotReloadMode": "hybridclr",
    "monoBehaviourComponents": ["HotReloadBehaviour"]
  },
  
  "hotReload": {
    "enabled": true,
    "watchFiles": ["*.dll", "*.json"],
    "preserveState": true
  }
}
```

## Testing Hot-Reload

### 1. Create Test Scene

Create a test scene with the following setup:

1. **Empty GameObject** with `HybridCLRBootstrap` script
2. **Empty GameObject** with `UnityPluginHost` script
3. **Canvas** with UI for testing hot-reload

### 2. Test Hot-Reload Functionality

Create a test script to verify hot-reload:

```csharp
public class HotReloadTester : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private string _pluginPath = "Assets/Plugins/TestPlugin";
    [SerializeField] private string _pluginId = "com.test.hotreloadplugin";

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Button _reloadButton;
    [SerializeField] private UnityEngine.UI.Text _statusText;

    private UnityPluginHost _pluginHost;

    private void Start()
    {
        _pluginHost = FindObjectOfType<UnityPluginHost>();
        
        if (_reloadButton != null)
        {
            _reloadButton.onClick.AddListener(TestHotReload);
        }

        UpdateStatus("Ready for hot-reload testing");
    }

    public async void TestHotReload()
    {
        UpdateStatus("Triggering hot-reload...");

        try
        {
            var hotReloadManager = _pluginHost.ServiceProvider
                .GetRequiredService<UnityPluginHotReloadManager>();

            await hotReloadManager.ReloadPluginAsync(_pluginId);
            UpdateStatus("Hot-reload completed successfully");
        }
        catch (System.Exception ex)
        {
            UpdateStatus($"Hot-reload failed: {ex.Message}");
            Debug.LogError($"Hot-reload error: {ex}");
        }
    }

    private void UpdateStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = $"Status: {message}";
        }
        
        Debug.Log($"[HotReloadTester] {message}");
    }
}
```

### 3. Development Workflow

1. **Initial Build**: Build your project with HybridCLR support
2. **Make Changes**: Modify your plugin code
3. **Rebuild Plugin**: Use `WingedBean/HybridCLR/Build Hot Update Assemblies`
4. **Copy Assemblies**: Use `WingedBean/HybridCLR/Copy Assemblies to StreamingAssets`
5. **Test Hot-Reload**: Trigger hot-reload in your running application

## Troubleshooting

### Common Issues

#### 1. Assembly Not Found

**Problem**: HybridCLR cannot find hot update assemblies

**Solutions**:
- Verify assembly names in HybridCLR settings
- Check file paths in StreamingAssets
- Ensure assemblies are built for correct platform

#### 2. AOT Metadata Loading Fails

**Problem**: Error loading AOT metadata assemblies

**Solutions**:
- Regenerate AOT metadata: `HybridCLR > Generate > All`
- Check IL2CPP build logs for errors
- Verify Unity version compatibility

#### 3. Hot-Reload State Loss

**Problem**: Component state is lost during hot-reload

**Solutions**:
- Implement `IStatefulComponent` interface properly
- Check state serialization/deserialization logic
- Verify hot-reload manager configuration

#### 4. Performance Issues

**Problem**: Slow hot-reload or runtime performance

**Solutions**:
- Optimize state preservation logic
- Reduce assembly size and dependencies
- Use Release builds for performance testing

### Debug Configuration

Enable debug logging for troubleshooting:

```csharp
// In your bootstrap or initialization code
public void EnableHybridCLRDebugLogging()
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log("[HybridCLR] Debug logging enabled");
    
    // Enable HybridCLR internal logging
    HybridCLR.RuntimeApi.SetLogLevel(HybridCLR.LogLevel.Debug);
    #endif
}
```

### Build Configuration Checklist

- [ ] HybridCLR package installed and configured
- [ ] IL2CPP scripting backend enabled
- [ ] Hot update assemblies configured in HybridCLR settings
- [ ] AOT metadata assemblies generated
- [ ] Assemblies copied to StreamingAssets
- [ ] Plugin manifests configured for hot-reload
- [ ] Test scene set up with proper components

## Best Practices

1. **Incremental Development**: Test hot-reload with small changes first
2. **State Management**: Design components with hot-reload in mind
3. **Error Handling**: Implement robust error handling for hot-reload failures
4. **Performance Monitoring**: Monitor hot-reload performance and optimize as needed
5. **Version Management**: Use semantic versioning for hot-reload compatibility

## Conclusion

HybridCLR integration with the WingedBean Unity Plugin System provides powerful hot-reload capabilities that can significantly improve development productivity. By following this setup guide and best practices, you can create a robust development environment that supports seamless plugin updates without application restarts.

For advanced configuration and troubleshooting, refer to the [HybridCLR documentation](https://hybridclr.doc.code-philosophy.com/) and the WingedBean plugin system API reference.
