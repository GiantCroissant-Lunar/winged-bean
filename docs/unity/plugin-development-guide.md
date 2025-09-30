# Unity Plugin Development Guide

This guide walks you through creating Unity plugins for the WingedBean plugin system, from basic setup to advanced features.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Plugin Structure](#plugin-structure)
3. [MonoBehaviour Integration](#monobehaviour-integration)
4. [State Management](#state-management)
5. [Service Integration](#service-integration)
6. [Security Configuration](#security-configuration)
7. [Hot-Reload Support](#hot-reload-support)
8. [Testing Your Plugin](#testing-your-plugin)
9. [Deployment](#deployment)

## Quick Start

### 1. Create Plugin Project

Create a new .NET Standard 2.1 class library:

```bash
dotnet new classlib -n MyUnityPlugin -f netstandard2.1
cd MyUnityPlugin
```

### 2. Add Required References

Update your `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <OutputPath>../UnityProject/Assets/Plugins/MyUnityPlugin</OutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="UnityEngine">
      <HintPath>$(UNITY_PATH)/Editor/Data/Managed/UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(UNITY_PATH)/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../WingedBean.Host.Unity/WingedBean.Host.Unity.csproj" />
  </ItemGroup>
</Project>
```

### 3. Create Plugin Manifest

Create `plugin.json` in your project root:

```json
{
  "id": "com.mycompany.myunityplugin",
  "version": "1.0.0",
  "name": "My Unity Plugin",
  "description": "A sample Unity plugin",
  "author": "My Company",
  "license": "MIT",

  "entryPoint": {
    "unity": "./MyUnityPlugin.dll"
  },

  "unity": {
    "minUnityVersion": "2022.3.0",
    "supportedPlatforms": [
      "StandaloneWindows64",
      "StandaloneOSX",
      "StandaloneLinux64"
    ],
    "monoBehaviourComponents": [
      "MyPluginBehaviour"
    ],
    "persistAcrossScenes": false,
    "initializationOrder": 0
  },

  "security": {
    "permissions": {
      "unity": {
        "canCreateGameObjects": true,
        "canAddComponents": true,
        "allowedComponentTypes": [
          "MyPluginBehaviour"
        ]
      }
    }
  }
}
```

### 4. Create Your First Component

Create `MyPluginBehaviour.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using WingedBean.Host.Unity.Core;

namespace MyCompany.MyUnityPlugin
{
    public class MyPluginBehaviour : MonoBehaviour, IStatefulComponent
    {
        [SerializeField] private string _playerName = "Player";
        [SerializeField] private int _score = 0;
        [SerializeField] private float _timer = 0f;

        public string PlayerName
        {
            get => _playerName;
            set => _playerName = value;
        }

        public int Score
        {
            get => _score;
            set => _score = value;
        }

        private void Start()
        {
            Debug.Log($"[MyPlugin] Started with player: {_playerName}, score: {_score}");
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            // Simple gameplay logic
            if (_timer >= 1f)
            {
                _score += 10;
                _timer = 0f;
                Debug.Log($"[MyPlugin] Score updated: {_score}");
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"Plugin: {_playerName}");
            GUILayout.Label($"Score: {_score}");
            GUILayout.Label($"Timer: {_timer:F1}s");
            GUILayout.EndArea();
        }

        // State preservation for hot-reload
        public Dictionary<string, object> GetState()
        {
            return new Dictionary<string, object>
            {
                ["playerName"] = _playerName,
                ["score"] = _score,
                ["timer"] = _timer,
                ["position"] = transform.position,
                ["rotation"] = transform.rotation
            };
        }

        public void RestoreState(Dictionary<string, object> state)
        {
            if (state.TryGetValue("playerName", out var name))
                _playerName = name?.ToString() ?? "Player";

            if (state.TryGetValue("score", out var score))
                _score = Convert.ToInt32(score);

            if (state.TryGetValue("timer", out var timer))
                _timer = Convert.ToSingle(timer);

            if (state.TryGetValue("position", out var pos) && pos is Vector3 position)
                transform.position = position;

            if (state.TryGetValue("rotation", out var rot) && rot is Quaternion rotation)
                transform.rotation = rotation;

            Debug.Log($"[MyPlugin] State restored: {_playerName}, score: {_score}");
        }
    }
}
```

## Plugin Structure

### Recommended Directory Structure

```
MyUnityPlugin/
├── src/
│   ├── Components/
│   │   ├── MyPluginBehaviour.cs
│   │   └── UIController.cs
│   ├── Services/
│   │   ├── IGameService.cs
│   │   └── GameService.cs
│   ├── Data/
│   │   └── GameData.cs
│   └── PluginEntryPoint.cs
├── tests/
│   └── MyPluginTests.cs
├── plugin.json
└── MyUnityPlugin.csproj
```

### Plugin Entry Point

Create a main entry point for your plugin:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Host.Core;

namespace MyCompany.MyUnityPlugin
{
    [PluginEntryPoint]
    public class MyPluginEntryPoint : IPluginEntryPoint
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Register your services
            services.AddSingleton<IGameService, GameService>();
            services.AddTransient<GameDataProcessor>();
        }

        public async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<MyPluginEntryPoint>>();
            logger.LogInformation("MyUnityPlugin initialized successfully");

            // Perform any initialization logic
            var gameService = serviceProvider.GetRequiredService<IGameService>();
            await gameService.InitializeAsync();
        }
    }
}
```

## MonoBehaviour Integration

### Creating Unity Components

Unity components should inherit from `MonoBehaviour` and optionally implement `IStatefulComponent`:

```csharp
public class PlayerController : MonoBehaviour, IStatefulComponent
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 10f;

    [Header("State")]
    [SerializeField] private int _health = 100;
    [SerializeField] private Vector3 _spawnPoint;

    private Rigidbody _rigidbody;
    private bool _isGrounded;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _spawnPoint = transform.position;
    }

    private void Update()
    {
        HandleMovement();
        HandleJumping();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0f, vertical) * _moveSpeed * Time.deltaTime;
        transform.Translate(movement);
    }

    private void HandleJumping()
    {
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    public void TakeDamage(int damage)
    {
        _health -= damage;
        if (_health <= 0)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        _health = 100;
        transform.position = _spawnPoint;
        _rigidbody.velocity = Vector3.zero;
    }

    // State preservation
    public Dictionary<string, object> GetState()
    {
        return new Dictionary<string, object>
        {
            ["health"] = _health,
            ["position"] = transform.position,
            ["rotation"] = transform.rotation,
            ["velocity"] = _rigidbody.velocity,
            ["spawnPoint"] = _spawnPoint
        };
    }

    public void RestoreState(Dictionary<string, object> state)
    {
        if (state.TryGetValue("health", out var health))
            _health = Convert.ToInt32(health);

        if (state.TryGetValue("position", out var pos) && pos is Vector3 position)
            transform.position = position;

        if (state.TryGetValue("rotation", out var rot) && rot is Quaternion rotation)
            transform.rotation = rotation;

        if (state.TryGetValue("velocity", out var vel) && vel is Vector3 velocity)
            _rigidbody.velocity = velocity;

        if (state.TryGetValue("spawnPoint", out var spawn) && spawn is Vector3 spawnPoint)
            _spawnPoint = spawnPoint;
    }
}
```

### Component Dependencies

Use dependency injection to access services:

```csharp
public class UIManager : MonoBehaviour
{
    private IGameService _gameService;
    private ILogger<UIManager> _logger;

    private void Start()
    {
        // Get services from the plugin host
        var serviceProvider = FindObjectOfType<UnityPluginHost>().ServiceProvider;
        _gameService = serviceProvider.GetRequiredService<IGameService>();
        _logger = serviceProvider.GetRequiredService<ILogger<UIManager>>();

        _logger.LogInformation("UIManager initialized");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Start Game"))
        {
            _gameService.StartGame();
        }

        if (GUILayout.Button("Save Game"))
        {
            _gameService.SaveGame();
        }
    }
}
```

## State Management

### Simple State Preservation

For basic data types and Unity objects:

```csharp
public Dictionary<string, object> GetState()
{
    return new Dictionary<string, object>
    {
        // Basic types
        ["playerName"] = _playerName,
        ["score"] = _score,
        ["isActive"] = gameObject.activeInHierarchy,

        // Unity types
        ["position"] = transform.position,
        ["rotation"] = transform.rotation,
        ["scale"] = transform.localScale,

        // Collections (must be serializable)
        ["inventory"] = _inventory.ToArray(),
        ["settings"] = _gameSettings.ToDictionary()
    };
}
```

### Complex State Management

For complex objects, use serialization:

```csharp
[Serializable]
public class GameState
{
    public string playerName;
    public int level;
    public float[] position;
    public Dictionary<string, int> inventory;
}

public Dictionary<string, object> GetState()
{
    var gameState = new GameState
    {
        playerName = _playerName,
        level = _currentLevel,
        position = new[] { transform.position.x, transform.position.y, transform.position.z },
        inventory = _inventory
    };

    return new Dictionary<string, object>
    {
        ["gameState"] = JsonUtility.ToJson(gameState)
    };
}

public void RestoreState(Dictionary<string, object> state)
{
    if (state.TryGetValue("gameState", out var stateJson))
    {
        var gameState = JsonUtility.FromJson<GameState>(stateJson.ToString());
        _playerName = gameState.playerName;
        _currentLevel = gameState.level;
        _inventory = gameState.inventory;

        if (gameState.position?.Length == 3)
        {
            transform.position = new Vector3(
                gameState.position[0],
                gameState.position[1],
                gameState.position[2]
            );
        }
    }
}
```

## Service Integration

### Defining Services

Create service interfaces and implementations:

```csharp
public interface IGameService
{
    Task InitializeAsync();
    void StartGame();
    void SaveGame();
    void LoadGame();
    event Action<int> ScoreChanged;
}

public class GameService : IGameService
{
    private readonly ILogger<GameService> _logger;
    private int _currentScore;

    public event Action<int>? ScoreChanged;

    public GameService(ILogger<GameService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Game service initializing...");

        // Load game data, connect to servers, etc.
        await LoadGameDataAsync();

        _logger.LogInformation("Game service initialized");
    }

    public void StartGame()
    {
        _currentScore = 0;
        ScoreChanged?.Invoke(_currentScore);
        _logger.LogInformation("Game started");
    }

    public void SaveGame()
    {
        // Save game state to persistent storage
        PlayerPrefs.SetInt("CurrentScore", _currentScore);
        _logger.LogInformation($"Game saved with score: {_currentScore}");
    }

    public void LoadGame()
    {
        _currentScore = PlayerPrefs.GetInt("CurrentScore", 0);
        ScoreChanged?.Invoke(_currentScore);
        _logger.LogInformation($"Game loaded with score: {_currentScore}");
    }

    private async Task LoadGameDataAsync()
    {
        // Simulate async loading
        await Task.Delay(100);
    }
}
```

### Service Registration

Register services in your plugin entry point:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Core services
    services.AddSingleton<IGameService, GameService>();
    services.AddTransient<IScoreCalculator, ScoreCalculator>();

    // Data services
    services.AddSingleton<IDataRepository, JsonDataRepository>();

    // UI services
    services.AddTransient<IUIFactory, UIFactory>();
}
```

## Security Configuration

### Permission Configuration

Configure permissions in your `plugin.json`:

```json
{
  "security": {
    "permissions": {
      "fileSystem": {
        "canRead": true,
        "canWrite": true,
        "allowedPaths": ["./GameData", "./SaveFiles"]
      },
      "unity": {
        "canCreateGameObjects": true,
        "canDestroyGameObjects": true,
        "canAddComponents": true,
        "canRemoveComponents": true,
        "canAccessSceneManager": true,
        "canAccessInput": true,
        "canAccessRendering": true,
        "allowedComponentTypes": [
          "PlayerController",
          "UIManager",
          "GameLogic"
        ]
      }
    }
  }
}
```

### Permission Checking

Check permissions before performing restricted operations:

```csharp
public class SecurePlayerController : MonoBehaviour
{
    private IUnityPluginPermissionEnforcer _permissionEnforcer;
    private const string PluginId = "com.mycompany.myunityplugin";

    private void Start()
    {
        var host = FindObjectOfType<UnityPluginHost>();
        _permissionEnforcer = host.ServiceProvider
            .GetRequiredService<IUnityPluginPermissionEnforcer>();
    }

    public void CreatePowerup()
    {
        // Check permission before creating GameObject
        if (!_permissionEnforcer.IsPermissionGranted(PluginId, "unity.gameobject.create"))
        {
            Debug.LogWarning("Permission denied: Cannot create GameObjects");
            return;
        }

        var powerup = new GameObject("Powerup");
        // Add components, etc.
    }

    public void AddComponent<T>() where T : Component
    {
        try
        {
            // This will throw if permission not granted
            _permissionEnforcer.EnforcePermission(PluginId, "unity.component.add");

            gameObject.AddComponent<T>();
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.LogError($"Permission denied: {ex.Message}");
        }
    }
}
```

## Hot-Reload Support

### Implementing Hot-Reload

Ensure your components support hot-reload:

```csharp
public class HotReloadableComponent : MonoBehaviour, IStatefulComponent
{
    // Mark important fields as serialized for inspection
    [SerializeField] private int _version = 1;
    [SerializeField] private bool _debugMode = false;

    // Runtime state that should persist
    private Dictionary<string, object> _runtimeData = new();
    private List<GameObject> _managedObjects = new();

    private void Start()
    {
        Debug.Log($"Component loaded - Version: {_version}, Debug: {_debugMode}");
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // Initialization logic that can be re-run safely
        if (_managedObjects.Count == 0)
        {
            CreateManagedObjects();
        }
    }

    private void CreateManagedObjects()
    {
        // Create objects that should persist across reloads
        for (int i = 0; i < 3; i++)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = new Vector3(i * 2, 0, 0);
            _managedObjects.Add(obj);
        }
    }

    // Comprehensive state preservation
    public Dictionary<string, object> GetState()
    {
        return new Dictionary<string, object>
        {
            // Component configuration
            ["version"] = _version,
            ["debugMode"] = _debugMode,

            // Runtime state
            ["runtimeData"] = new Dictionary<string, object>(_runtimeData),

            // Managed objects
            ["managedObjectPositions"] = _managedObjects
                .Where(obj => obj != null)
                .Select(obj => new {
                    name = obj.name,
                    position = obj.transform.position
                })
                .ToArray(),

            // Unity transform
            ["transform"] = new {
                position = transform.position,
                rotation = transform.rotation,
                scale = transform.localScale
            }
        };
    }

    public void RestoreState(Dictionary<string, object> state)
    {
        // Restore configuration
        if (state.TryGetValue("version", out var version))
            _version = Convert.ToInt32(version);

        if (state.TryGetValue("debugMode", out var debug))
            _debugMode = Convert.ToBoolean(debug);

        // Restore runtime data
        if (state.TryGetValue("runtimeData", out var runtime) &&
            runtime is Dictionary<string, object> runtimeDict)
        {
            _runtimeData = new Dictionary<string, object>(runtimeDict);
        }

        // Restore managed objects
        if (state.TryGetValue("managedObjectPositions", out var positions))
        {
            RestoreManagedObjects(positions);
        }

        // Restore transform
        if (state.TryGetValue("transform", out var transformData))
        {
            RestoreTransform(transformData);
        }

        Debug.Log($"State restored - Version: {_version}, Objects: {_managedObjects.Count}");
    }

    private void RestoreManagedObjects(object positionsData)
    {
        // Implementation depends on your serialization approach
        // This is a simplified example
    }

    private void RestoreTransform(object transformData)
    {
        // Restore transform properties
        // Implementation details depend on your serialization
    }

    private void OnDestroy()
    {
        // Clean up managed objects
        foreach (var obj in _managedObjects.Where(o => o != null))
        {
            DestroyImmediate(obj);
        }
        _managedObjects.Clear();
    }
}
```

### Testing Hot-Reload

Create a test script to verify hot-reload functionality:

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class HotReloadTester : MonoBehaviour
{
    [Header("Hot-Reload Testing")]
    [SerializeField] private string _pluginId = "com.mycompany.myunityplugin";

    [ContextMenu("Trigger Hot-Reload")]
    public void TriggerHotReload()
    {
        var host = FindObjectOfType<UnityPluginHost>();
        if (host != null)
        {
            host.ReloadPlugin(_pluginId);
        }
    }

    [ContextMenu("Reload All Plugins")]
    public void ReloadAllPlugins()
    {
        var host = FindObjectOfType<UnityPluginHost>();
        if (host != null)
        {
            host.ReloadAllPlugins();
        }
    }
}

// Custom editor for easier testing
[CustomEditor(typeof(HotReloadTester))]
public class HotReloadTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        var tester = (HotReloadTester)target;

        if (GUILayout.Button("Trigger Hot-Reload"))
        {
            tester.TriggerHotReload();
        }

        if (GUILayout.Button("Reload All Plugins"))
        {
            tester.ReloadAllPlugins();
        }
    }
}
#endif
```

## Testing Your Plugin

### Unit Tests

Create unit tests for your plugin services:

```csharp
using NUnit.Framework;
using Microsoft.Extensions.Logging.Abstractions;

[TestFixture]
public class GameServiceTests
{
    private GameService _gameService;

    [SetUp]
    public void SetUp()
    {
        _gameService = new GameService(NullLogger<GameService>.Instance);
    }

    [Test]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        // Act
        await _gameService.InitializeAsync();

        // Assert
        // Add assertions based on your service behavior
        Assert.Pass("Initialization completed");
    }

    [Test]
    public void StartGame_ShouldResetScore()
    {
        // Arrange
        bool scoreChanged = false;
        int newScore = -1;
        _gameService.ScoreChanged += score => {
            scoreChanged = true;
            newScore = score;
        };

        // Act
        _gameService.StartGame();

        // Assert
        Assert.IsTrue(scoreChanged);
        Assert.AreEqual(0, newScore);
    }
}
```

### Integration Tests

Test Unity-specific functionality:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class PlayerControllerTests
{
    private GameObject _playerObject;
    private PlayerController _playerController;

    [SetUp]
    public void SetUp()
    {
        _playerObject = new GameObject("TestPlayer");
        _playerObject.AddComponent<Rigidbody>();
        _playerController = _playerObject.AddComponent<PlayerController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_playerObject != null)
        {
            Object.DestroyImmediate(_playerObject);
        }
    }

    [Test]
    public void TakeDamage_ShouldReduceHealth()
    {
        // Arrange
        var initialHealth = 100;

        // Act
        _playerController.TakeDamage(25);

        // Assert
        // You would need to expose health or add a getter
        // Assert.AreEqual(75, _playerController.Health);
    }

    [UnityTest]
    public IEnumerator StatePreservation_ShouldMaintainData()
    {
        // Arrange
        _playerController.transform.position = new Vector3(5, 0, 5);
        _playerController.TakeDamage(30);

        // Act
        var state = _playerController.GetState();
        _playerController.RestoreState(state);

        // Wait a frame for Unity to process
        yield return null;

        // Assert
        Assert.AreEqual(new Vector3(5, 0, 5), _playerController.transform.position);
    }
}
```

## Deployment

### Build Configuration

Set up automated building in your `.csproj`:

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <ItemGroup>
    <FilesToCopy Include="$(OutDir)$(AssemblyName).dll" />
    <FilesToCopy Include="$(OutDir)$(AssemblyName).pdb" Condition="'$(Configuration)' == 'Debug'" />
    <FilesToCopy Include="plugin.json" />
  </ItemGroup>

  <Copy SourceFiles="@(FilesToCopy)"
        DestinationFolder="$(OutputPath)"
        SkipUnchangedFiles="true" />

  <Message Text="Plugin built and copied to Unity project" Importance="high" />
</Target>
```

### Package Structure

Your final plugin package should contain:

```
MyUnityPlugin/
├── MyUnityPlugin.dll      # Main plugin assembly
├── MyUnityPlugin.pdb      # Debug symbols (optional)
├── plugin.json            # Plugin manifest
├── Assets/                # Unity assets (optional)
│   ├── Prefabs/
│   ├── Materials/
│   └── Scripts/
└── README.md              # Plugin documentation
```

### Version Management

Use semantic versioning and update your manifest:

```json
{
  "version": "1.2.3",
  "compatibility": {
    "minHostVersion": "1.0.0",
    "maxHostVersion": "2.0.0"
  }
}
```

### Distribution

Consider different distribution methods:

1. **Direct Copy**: Copy plugin folder to Unity project
2. **Package Manager**: Create Unity packages
3. **Asset Store**: Distribute through Unity Asset Store
4. **Custom Repository**: Host plugins on your own server

This guide provides a comprehensive foundation for Unity plugin development. Refer to the API documentation and sample projects for more detailed examples and advanced use cases.
