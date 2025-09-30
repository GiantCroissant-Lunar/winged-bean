using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WingedBean.Host.Core;
using WingedBean.Host.Unity.Core;
using WingedBean.Host.Unity.Loaders;

namespace WingedBean.Host.Unity.Tests
{
    [TestFixture]
    public class HybridClrPluginLoaderTests
    {
        private HybridClrPluginLoader _loader;
        private IServiceProvider _serviceProvider;
        private string _testPluginPath;
        private PluginManifest _testManifest;

        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IPluginSecurity, MockPluginSecurity>();
            _serviceProvider = services.BuildServiceProvider();

            _loader = new HybridClrPluginLoader(_serviceProvider);

            // Create a test plugin directory
            _testPluginPath = Path.Combine(Application.temporaryCachePath, "TestPlugin");
            Directory.CreateDirectory(_testPluginPath);

            _testManifest = new PluginManifest
            {
                Id = "test.unity.plugin",
                Version = "1.0.0",
                Name = "Test Unity Plugin",
                Description = "Test plugin for Unity",
                EntryPoint = new EntryPointConfig
                {
                    Unity = "./TestPlugin.dll"
                },
                Unity = new UnityPluginSettings
                {
                    MinUnityVersion = "2022.3.0",
                    SupportedPlatforms = new[] { "StandaloneWindows64" },
                    MonoBehaviourComponents = new[] { "TestBehaviour" },
                    InitializationOrder = 0
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testPluginPath))
            {
                Directory.Delete(_testPluginPath, true);
            }
        }

        [Test]
        public void CanLoadPlugin_WithValidUnityManifest_ReturnsTrue()
        {
            // Arrange
            var context = new PluginLoadContext(_testPluginPath, _testManifest);

            // Act
            var canLoad = _loader.CanLoadPlugin(context);

            // Assert
            Assert.IsTrue(canLoad);
        }

        [Test]
        public void CanLoadPlugin_WithoutUnitySettings_ReturnsFalse()
        {
            // Arrange
            _testManifest.Unity = null;
            var context = new PluginLoadContext(_testPluginPath, _testManifest);

            // Act
            var canLoad = _loader.CanLoadPlugin(context);

            // Assert
            Assert.IsFalse(canLoad);
        }

        [Test]
        public void CanLoadPlugin_WithIncompatibleUnityVersion_ReturnsFalse()
        {
            // Arrange
            _testManifest.Unity.MinUnityVersion = "2025.1.0"; // Future version
            var context = new PluginLoadContext(_testPluginPath, _testManifest);

            // Act
            var canLoad = _loader.CanLoadPlugin(context);

            // Assert
            Assert.IsFalse(canLoad);
        }

        [Test]
        public void CanLoadPlugin_WithUnsupportedPlatform_ReturnsFalse()
        {
            // Arrange
            _testManifest.Unity.SupportedPlatforms = new[] { "iOS" }; // Assuming we're testing on Windows
            var context = new PluginLoadContext(_testPluginPath, _testManifest);

            // Act
            var canLoad = _loader.CanLoadPlugin(context);

            // Assert
            Assert.IsFalse(canLoad);
        }

        [UnityTest]
        public IEnumerator LoadPlugin_WithValidPlugin_CreatesLoadedUnityPlugin()
        {
            // Arrange
            CreateMockPluginAssembly();
            var context = new PluginLoadContext(_testPluginPath, _testManifest);

            // Act
            var loadTask = _loader.LoadPluginAsync(context);
            yield return new WaitUntil(() => loadTask.IsCompleted);

            // Assert
            Assert.IsTrue(loadTask.IsCompletedSuccessfully);
            var plugin = loadTask.Result;
            Assert.IsNotNull(plugin);
            Assert.IsInstanceOf<LoadedUnityPlugin>(plugin);
        }

        [UnityTest]
        public IEnumerator LoadPlugin_WithMissingAssembly_ThrowsException()
        {
            // Arrange
            var context = new PluginLoadContext(_testPluginPath, _testManifest);

            // Act
            var loadTask = _loader.LoadPluginAsync(context);
            yield return new WaitUntil(() => loadTask.IsCompleted);

            // Assert
            Assert.IsTrue(loadTask.IsFaulted);
            Assert.IsInstanceOf<FileNotFoundException>(loadTask.Exception?.InnerException);
        }

        private void CreateMockPluginAssembly()
        {
            // Create a minimal plugin assembly file for testing
            var assemblyPath = Path.Combine(_testPluginPath, "TestPlugin.dll");
            var mockAssemblyBytes = new byte[] { 0x4D, 0x5A }; // Minimal PE header
            File.WriteAllBytes(assemblyPath, mockAssemblyBytes);
        }

        private class MockPluginSecurity : IPluginSecurity
        {
            public Task<bool> ValidatePluginAsync(PluginLoadContext context)
            {
                return Task.FromResult(true);
            }

            public bool IsPermissionGranted(string pluginId, string permission)
            {
                return true;
            }

            public void EnforcePermission(string pluginId, string permission)
            {
                // No-op for testing
            }
        }
    }
}
