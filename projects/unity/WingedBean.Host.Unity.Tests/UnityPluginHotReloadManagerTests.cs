using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WingedBean.Host.Core;
using WingedBean.Host.Unity.Core;
using WingedBean.Host.Unity.HotReload;

namespace WingedBean.Host.Unity.Tests
{
    [TestFixture]
    public class UnityPluginHotReloadManagerTests
    {
        private UnityPluginHotReloadManager _hotReloadManager;
        private IServiceProvider _serviceProvider;
        private MockPluginRegistry _pluginRegistry;
        private LoadedUnityPlugin _testPlugin;
        private PluginManifest _manifest;

        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            _serviceProvider = services.BuildServiceProvider();

            _pluginRegistry = new MockPluginRegistry();
            _hotReloadManager = new UnityPluginHotReloadManager(_serviceProvider, _pluginRegistry);

            _manifest = new PluginManifest
            {
                Id = "test.hotreload.plugin",
                Version = "1.0.0",
                Name = "Test Hot Reload Plugin",
                Unity = new UnityPluginSettings
                {
                    MonoBehaviourComponents = new[] { "HotReloadTestBehaviour" }
                }
            };

            _testPlugin = new LoadedUnityPlugin(
                _manifest,
                typeof(HotReloadTestBehaviour).Assembly,
                _serviceProvider,
                new[] { typeof(HotReloadTestBehaviour) }
            );

            _pluginRegistry.RegisterPlugin(_testPlugin);
        }

        [TearDown]
        public void TearDown()
        {
            _hotReloadManager?.Dispose();
            
            // Clean up any remaining test behaviours
            var behaviours = UnityEngine.Object.FindObjectsOfType<HotReloadTestBehaviour>();
            foreach (var behaviour in behaviours)
            {
                UnityEngine.Object.DestroyImmediate(behaviour.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator UpdatePlugin_PreservesComponentState()
        {
            // Arrange
            var initTask = _testPlugin.InitializeAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);

            var originalBehaviour = UnityEngine.Object.FindObjectOfType<HotReloadTestBehaviour>();
            originalBehaviour.TestCounter = 5;
            originalBehaviour.TestMessage = "Before Update";

            var newManifest = new PluginManifest
            {
                Id = _manifest.Id,
                Version = "1.1.0",
                Name = _manifest.Name,
                Unity = _manifest.Unity
            };

            var newPlugin = new LoadedUnityPlugin(
                newManifest,
                typeof(HotReloadTestBehaviour).Assembly,
                _serviceProvider,
                new[] { typeof(HotReloadTestBehaviour) }
            );

            // Act
            var updateTask = _hotReloadManager.UpdatePluginAsync(_testPlugin, newPlugin);
            yield return new WaitUntil(() => updateTask.IsCompleted);

            // Assert
            Assert.IsTrue(updateTask.IsCompletedSuccessfully);
            
            var updatedBehaviour = UnityEngine.Object.FindObjectOfType<HotReloadTestBehaviour>();
            Assert.IsNotNull(updatedBehaviour);
            Assert.AreEqual(5, updatedBehaviour.TestCounter);
            Assert.AreEqual("Before Update", updatedBehaviour.TestMessage);
        }

        [UnityTest]
        public IEnumerator UpdatePlugin_HandlesNewComponents()
        {
            // Arrange
            var initTask = _testPlugin.InitializeAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);

            var newManifest = new PluginManifest
            {
                Id = _manifest.Id,
                Version = "1.1.0",
                Name = _manifest.Name,
                Unity = new UnityPluginSettings
                {
                    MonoBehaviourComponents = new[] { "HotReloadTestBehaviour", "NewTestBehaviour" }
                }
            };

            var newPlugin = new LoadedUnityPlugin(
                newManifest,
                typeof(HotReloadTestBehaviour).Assembly,
                _serviceProvider,
                new[] { typeof(HotReloadTestBehaviour), typeof(NewTestBehaviour) }
            );

            // Act
            var updateTask = _hotReloadManager.UpdatePluginAsync(_testPlugin, newPlugin);
            yield return new WaitUntil(() => updateTask.IsCompleted);

            // Assert
            Assert.IsTrue(updateTask.IsCompletedSuccessfully);
            
            var originalBehaviours = UnityEngine.Object.FindObjectsOfType<HotReloadTestBehaviour>();
            var newBehaviours = UnityEngine.Object.FindObjectsOfType<NewTestBehaviour>();
            
            Assert.IsNotEmpty(originalBehaviours);
            Assert.IsNotEmpty(newBehaviours);
        }

        [UnityTest]
        public IEnumerator UpdatePlugin_HandlesRemovedComponents()
        {
            // Arrange
            var initTask = _testPlugin.InitializeAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);

            var newManifest = new PluginManifest
            {
                Id = _manifest.Id,
                Version = "1.1.0",
                Name = _manifest.Name,
                Unity = new UnityPluginSettings
                {
                    MonoBehaviourComponents = new string[0] // No components
                }
            };

            var newPlugin = new LoadedUnityPlugin(
                newManifest,
                typeof(HotReloadTestBehaviour).Assembly,
                _serviceProvider,
                new Type[0]
            );

            // Act
            var updateTask = _hotReloadManager.UpdatePluginAsync(_testPlugin, newPlugin);
            yield return new WaitUntil(() => updateTask.IsCompleted);

            // Assert
            Assert.IsTrue(updateTask.IsCompletedSuccessfully);
            
            var remainingBehaviours = UnityEngine.Object.FindObjectsOfType<HotReloadTestBehaviour>();
            Assert.IsEmpty(remainingBehaviours);
        }

        [Test]
        public void CanUpdatePlugin_WithCompatiblePlugin_ReturnsTrue()
        {
            // Arrange
            var newManifest = new PluginManifest
            {
                Id = _manifest.Id,
                Version = "1.1.0",
                Name = _manifest.Name,
                Unity = _manifest.Unity
            };

            var newPlugin = new LoadedUnityPlugin(
                newManifest,
                typeof(HotReloadTestBehaviour).Assembly,
                _serviceProvider,
                new[] { typeof(HotReloadTestBehaviour) }
            );

            // Act
            var canUpdate = _hotReloadManager.CanUpdatePlugin(_testPlugin, newPlugin);

            // Assert
            Assert.IsTrue(canUpdate);
        }

        [Test]
        public void CanUpdatePlugin_WithDifferentId_ReturnsFalse()
        {
            // Arrange
            var newManifest = new PluginManifest
            {
                Id = "different.plugin.id",
                Version = "1.1.0",
                Name = _manifest.Name,
                Unity = _manifest.Unity
            };

            var newPlugin = new LoadedUnityPlugin(
                newManifest,
                typeof(HotReloadTestBehaviour).Assembly,
                _serviceProvider,
                new[] { typeof(HotReloadTestBehaviour) }
            );

            // Act
            var canUpdate = _hotReloadManager.CanUpdatePlugin(_testPlugin, newPlugin);

            // Assert
            Assert.IsFalse(canUpdate);
        }

        [Test]
        public void CanUpdatePlugin_WithNonUnityPlugin_ReturnsFalse()
        {
            // Arrange
            var mockPlugin = new MockLoadedPlugin("test.plugin", "1.0.0");

            var newManifest = new PluginManifest
            {
                Id = "test.plugin",
                Version = "1.1.0",
                Name = "Test Plugin"
            };

            var newPlugin = new LoadedUnityPlugin(
                newManifest,
                typeof(HotReloadTestBehaviour).Assembly,
                _serviceProvider,
                new[] { typeof(HotReloadTestBehaviour) }
            );

            // Act
            var canUpdate = _hotReloadManager.CanUpdatePlugin(mockPlugin, newPlugin);

            // Assert
            Assert.IsFalse(canUpdate);
        }

        private class MockPluginRegistry : IPluginRegistry
        {
            private readonly Dictionary<string, ILoadedPlugin> _plugins = new();

            public void RegisterPlugin(ILoadedPlugin plugin)
            {
                _plugins[plugin.Id] = plugin;
            }

            public ILoadedPlugin? GetPlugin(string pluginId)
            {
                return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
            }

            public IEnumerable<ILoadedPlugin> GetAllPlugins()
            {
                return _plugins.Values;
            }

            public void UnregisterPlugin(string pluginId)
            {
                _plugins.Remove(pluginId);
            }
        }

        private class MockLoadedPlugin : ILoadedPlugin
        {
            public string Id { get; }
            public string Version { get; }
            public string Name => "Mock Plugin";
            public bool IsLoaded => true;
            public bool IsInitialized => true;

            public MockLoadedPlugin(string id, string version)
            {
                Id = id;
                Version = version;
            }

            public Task InitializeAsync() => Task.CompletedTask;
            public Task ShutdownAsync() => Task.CompletedTask;
            public Task<Dictionary<string, object>> GetStateAsync() => Task.FromResult(new Dictionary<string, object>());
            public Task RestoreStateAsync(Dictionary<string, object> state) => Task.CompletedTask;
        }
    }

    // Test behaviours for hot reload testing
    public class HotReloadTestBehaviour : MonoBehaviour, IStatefulComponent
    {
        public int TestCounter { get; set; }
        public string TestMessage { get; set; } = string.Empty;

        public Dictionary<string, object> GetState()
        {
            return new Dictionary<string, object>
            {
                ["TestCounter"] = TestCounter,
                ["TestMessage"] = TestMessage
            };
        }

        public void RestoreState(Dictionary<string, object> state)
        {
            if (state.TryGetValue("TestCounter", out var counter))
                TestCounter = Convert.ToInt32(counter);
                
            if (state.TryGetValue("TestMessage", out var message))
                TestMessage = message?.ToString() ?? string.Empty;
        }
    }

    public class NewTestBehaviour : MonoBehaviour, IStatefulComponent
    {
        public float TestValue { get; set; }

        public Dictionary<string, object> GetState()
        {
            return new Dictionary<string, object>
            {
                ["TestValue"] = TestValue
            };
        }

        public void RestoreState(Dictionary<string, object> state)
        {
            if (state.TryGetValue("TestValue", out var value))
                TestValue = Convert.ToSingle(value);
        }
    }
}
