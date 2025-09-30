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

namespace WingedBean.Host.Unity.Tests
{
    [TestFixture]
    public class LoadedUnityPluginTests
    {
        private LoadedUnityPlugin _plugin;
        private GameObject _testGameObject;
        private PluginManifest _manifest;
        private IServiceProvider _serviceProvider;
        private TestStatefulBehaviour _testBehaviour;

        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            _serviceProvider = services.BuildServiceProvider();

            _manifest = new PluginManifest
            {
                Id = "test.unity.plugin",
                Version = "1.0.0",
                Name = "Test Unity Plugin",
                Unity = new UnityPluginSettings
                {
                    MonoBehaviourComponents = new[] { "TestStatefulBehaviour" },
                    PersistAcrossScenes = false
                }
            };

            _testGameObject = new GameObject("TestPlugin");
            _plugin = new LoadedUnityPlugin(
                _manifest,
                typeof(TestStatefulBehaviour).Assembly,
                _serviceProvider,
                new[] { typeof(TestStatefulBehaviour) }
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        [UnityTest]
        public IEnumerator Initialize_CreatesMonoBehaviourComponents()
        {
            // Act
            var initTask = _plugin.InitializeAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);

            // Assert
            Assert.IsTrue(initTask.IsCompletedSuccessfully);
            
            var behaviours = UnityEngine.Object.FindObjectsOfType<TestStatefulBehaviour>();
            Assert.IsNotEmpty(behaviours);
            
            _testBehaviour = behaviours[0];
            Assert.IsNotNull(_testBehaviour);
            Assert.IsTrue(_testBehaviour.IsInitialized);
        }

        [UnityTest]
        public IEnumerator Shutdown_DestroysManagedComponents()
        {
            // Arrange
            var initTask = _plugin.InitializeAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);
            
            var initialBehaviours = UnityEngine.Object.FindObjectsOfType<TestStatefulBehaviour>();
            Assert.IsNotEmpty(initialBehaviours);

            // Act
            var shutdownTask = _plugin.ShutdownAsync();
            yield return new WaitUntil(() => shutdownTask.IsCompleted);

            // Assert
            Assert.IsTrue(shutdownTask.IsCompletedSuccessfully);
            
            var remainingBehaviours = UnityEngine.Object.FindObjectsOfType<TestStatefulBehaviour>();
            Assert.IsEmpty(remainingBehaviours);
        }

        [UnityTest]
        public IEnumerator GetState_ReturnsComponentStates()
        {
            // Arrange
            var initTask = _plugin.InitializeAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);
            
            var behaviour = UnityEngine.Object.FindObjectOfType<TestStatefulBehaviour>();
            behaviour.TestValue = 42;
            behaviour.TestString = "Test State";

            // Act
            var state = await _plugin.GetStateAsync();

            // Assert
            Assert.IsNotNull(state);
            Assert.IsTrue(state.ContainsKey("TestStatefulBehaviour"));
            
            var componentState = state["TestStatefulBehaviour"] as Dictionary<string, object>;
            Assert.IsNotNull(componentState);
            Assert.AreEqual(42, componentState["TestValue"]);
            Assert.AreEqual("Test State", componentState["TestString"]);
        }

        [UnityTest]
        public IEnumerator RestoreState_RestoresComponentStates()
        {
            // Arrange
            var initTask = _plugin.InitializeAsync();
            yield return new WaitUntil(() => initTask.IsCompleted);
            
            var originalBehaviour = UnityEngine.Object.FindObjectOfType<TestStatefulBehaviour>();
            originalBehaviour.TestValue = 42;
            originalBehaviour.TestString = "Restored State";
            
            var state = await _plugin.GetStateAsync();

            // Shutdown and reinitialize
            var shutdownTask = _plugin.ShutdownAsync();
            yield return new WaitUntil(() => shutdownTask.IsCompleted);
            
            var reinitTask = _plugin.InitializeAsync();
            yield return new WaitUntil(() => reinitTask.IsCompleted);

            // Act
            await _plugin.RestoreStateAsync(state);

            // Assert
            var restoredBehaviour = UnityEngine.Object.FindObjectOfType<TestStatefulBehaviour>();
            Assert.IsNotNull(restoredBehaviour);
            Assert.AreEqual(42, restoredBehaviour.TestValue);
            Assert.AreEqual("Restored State", restoredBehaviour.TestString);
        }

        [Test]
        public void GetPluginGameObjects_ReturnsOnlyManagedObjects()
        {
            // Arrange
            var unmanagedObject = new GameObject("Unmanaged");
            
            // Act
            var managedObjects = _plugin.GetPluginGameObjects();

            // Assert
            Assert.IsNotNull(managedObjects);
            Assert.IsFalse(managedObjects.Contains(unmanagedObject));
            
            UnityEngine.Object.DestroyImmediate(unmanagedObject);
        }

        [Test]
        public void Metadata_ReturnsCorrectInformation()
        {
            // Act & Assert
            Assert.AreEqual("test.unity.plugin", _plugin.Id);
            Assert.AreEqual("1.0.0", _plugin.Version);
            Assert.AreEqual("Test Unity Plugin", _plugin.Name);
            Assert.IsTrue(_plugin.IsLoaded);
            Assert.IsFalse(_plugin.IsInitialized); // Not initialized yet
        }
    }

    // Test MonoBehaviour for testing
    public class TestStatefulBehaviour : MonoBehaviour, IStatefulComponent
    {
        public int TestValue { get; set; }
        public string TestString { get; set; } = string.Empty;
        public bool IsInitialized { get; private set; }

        private void Start()
        {
            IsInitialized = true;
        }

        public Dictionary<string, object> GetState()
        {
            return new Dictionary<string, object>
            {
                ["TestValue"] = TestValue,
                ["TestString"] = TestString,
                ["IsInitialized"] = IsInitialized
            };
        }

        public void RestoreState(Dictionary<string, object> state)
        {
            if (state.TryGetValue("TestValue", out var testValue))
                TestValue = Convert.ToInt32(testValue);
                
            if (state.TryGetValue("TestString", out var testString))
                TestString = testString?.ToString() ?? string.Empty;
                
            if (state.TryGetValue("IsInitialized", out var isInitialized))
                IsInitialized = Convert.ToBoolean(isInitialized);
        }
    }
}
