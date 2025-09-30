using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WingedBean.Host.Core;
using WingedBean.Host.Unity.Security;

namespace WingedBean.Host.Unity.Tests
{
    [TestFixture]
    public class UnityPluginSecurityTests
    {
        private UnityPluginPermissionEnforcer _permissionEnforcer;
        private PluginManifest _manifest;
        private const string TestPluginId = "test.security.plugin";

        [SetUp]
        public void SetUp()
        {
            _permissionEnforcer = new UnityPluginPermissionEnforcer();

            _manifest = new PluginManifest
            {
                Id = TestPluginId,
                Version = "1.0.0",
                Name = "Test Security Plugin",
                Security = new SecurityConfig
                {
                    Permissions = new PermissionsConfig
                    {
                        Unity = new UnityPermissions
                        {
                            CanCreateGameObjects = true,
                            CanDestroyGameObjects = true,
                            CanAddComponents = true,
                            CanRemoveComponents = false,
                            CanAccessSceneManager = true,
                            CanAccessInput = false,
                            CanAccessRendering = true,
                            AllowedComponentTypes = new[] { "TestComponent", "AnotherComponent" }
                        }
                    }
                }
            };
        }

        [Test]
        public void IsPermissionGranted_WithGrantedGameObjectCreation_ReturnsTrue()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act
            var isGranted = _permissionEnforcer.IsPermissionGranted(TestPluginId, "unity.gameobject.create");

            // Assert
            Assert.IsTrue(isGranted);
        }

        [Test]
        public void IsPermissionGranted_WithDeniedComponentRemoval_ReturnsFalse()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act
            var isGranted = _permissionEnforcer.IsPermissionGranted(TestPluginId, "unity.component.remove");

            // Assert
            Assert.IsFalse(isGranted);
        }

        [Test]
        public void IsPermissionGranted_WithUnregisteredPlugin_ReturnsFalse()
        {
            // Act
            var isGranted = _permissionEnforcer.IsPermissionGranted(TestPluginId, "unity.gameobject.create");

            // Assert
            Assert.IsFalse(isGranted);
        }

        [Test]
        public void EnforcePermission_WithGrantedPermission_DoesNotThrow()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act & Assert
            Assert.DoesNotThrow(() => _permissionEnforcer.EnforcePermission(TestPluginId, "unity.gameobject.create"));
        }

        [Test]
        public void EnforcePermission_WithDeniedPermission_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => 
                _permissionEnforcer.EnforcePermission(TestPluginId, "unity.input.access"));
        }

        [Test]
        public void EnforcePermission_WithUnregisteredPlugin_ThrowsUnauthorizedAccessException()
        {
            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => 
                _permissionEnforcer.EnforcePermission(TestPluginId, "unity.gameobject.create"));
        }

        [Test]
        public void IsComponentTypeAllowed_WithAllowedType_ReturnsTrue()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act
            var isAllowed = _permissionEnforcer.IsComponentTypeAllowed(TestPluginId, "TestComponent");

            // Assert
            Assert.IsTrue(isAllowed);
        }

        [Test]
        public void IsComponentTypeAllowed_WithDisallowedType_ReturnsFalse()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act
            var isAllowed = _permissionEnforcer.IsComponentTypeAllowed(TestPluginId, "DisallowedComponent");

            // Assert
            Assert.IsFalse(isAllowed);
        }

        [Test]
        public void IsComponentTypeAllowed_WithNoRestrictions_ReturnsTrue()
        {
            // Arrange
            _manifest.Security.Permissions.Unity.AllowedComponentTypes = null;
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act
            var isAllowed = _permissionEnforcer.IsComponentTypeAllowed(TestPluginId, "AnyComponent");

            // Assert
            Assert.IsTrue(isAllowed);
        }

        [Test]
        public void UnregisterPlugin_RemovesPluginPermissions()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);
            Assert.IsTrue(_permissionEnforcer.IsPermissionGranted(TestPluginId, "unity.gameobject.create"));

            // Act
            _permissionEnforcer.UnregisterPlugin(TestPluginId);

            // Assert
            Assert.IsFalse(_permissionEnforcer.IsPermissionGranted(TestPluginId, "unity.gameobject.create"));
        }

        [Test]
        public void GetUnityPermissions_WithRegisteredPlugin_ReturnsCorrectPermissions()
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act
            var permissions = _permissionEnforcer.GetUnityPermissions(TestPluginId);

            // Assert
            Assert.IsNotNull(permissions);
            Assert.IsTrue(permissions.CanCreateGameObjects);
            Assert.IsTrue(permissions.CanDestroyGameObjects);
            Assert.IsTrue(permissions.CanAddComponents);
            Assert.IsFalse(permissions.CanRemoveComponents);
            Assert.IsTrue(permissions.CanAccessSceneManager);
            Assert.IsFalse(permissions.CanAccessInput);
            Assert.IsTrue(permissions.CanAccessRendering);
            Assert.Contains("TestComponent", permissions.AllowedComponentTypes);
            Assert.Contains("AnotherComponent", permissions.AllowedComponentTypes);
        }

        [Test]
        public void GetUnityPermissions_WithUnregisteredPlugin_ReturnsNull()
        {
            // Act
            var permissions = _permissionEnforcer.GetUnityPermissions(TestPluginId);

            // Assert
            Assert.IsNull(permissions);
        }

        [Test]
        public void RegisterPlugin_WithNullUnityPermissions_SetsDefaultDenyAll()
        {
            // Arrange
            var manifestWithoutUnityPerms = new PluginManifest
            {
                Id = TestPluginId,
                Version = "1.0.0",
                Name = "Test Plugin"
            };

            // Act
            _permissionEnforcer.RegisterPlugin(manifestWithoutUnityPerms);
            var permissions = _permissionEnforcer.GetUnityPermissions(TestPluginId);

            // Assert
            Assert.IsNotNull(permissions);
            Assert.IsFalse(permissions.CanCreateGameObjects);
            Assert.IsFalse(permissions.CanDestroyGameObjects);
            Assert.IsFalse(permissions.CanAddComponents);
            Assert.IsFalse(permissions.CanRemoveComponents);
            Assert.IsFalse(permissions.CanAccessSceneManager);
            Assert.IsFalse(permissions.CanAccessInput);
            Assert.IsFalse(permissions.CanAccessRendering);
        }

        [Test]
        public void RegisterPlugin_WithNullSecurity_SetsDefaultDenyAll()
        {
            // Arrange
            var manifestWithoutSecurity = new PluginManifest
            {
                Id = TestPluginId,
                Version = "1.0.0",
                Name = "Test Plugin",
                Security = null
            };

            // Act
            _permissionEnforcer.RegisterPlugin(manifestWithoutSecurity);
            var permissions = _permissionEnforcer.GetUnityPermissions(TestPluginId);

            // Assert
            Assert.IsNotNull(permissions);
            Assert.IsFalse(permissions.CanCreateGameObjects);
            Assert.IsFalse(permissions.CanDestroyGameObjects);
        }

        [TestCase("unity.gameobject.create", true)]
        [TestCase("unity.gameobject.destroy", true)]
        [TestCase("unity.component.add", true)]
        [TestCase("unity.component.remove", false)]
        [TestCase("unity.scene.access", true)]
        [TestCase("unity.input.access", false)]
        [TestCase("unity.rendering.access", true)]
        [TestCase("invalid.permission", false)]
        public void PermissionChecking_WithVariousPermissions_ReturnsExpectedResults(string permission, bool expected)
        {
            // Arrange
            _permissionEnforcer.RegisterPlugin(_manifest);

            // Act
            var actual = _permissionEnforcer.IsPermissionGranted(TestPluginId, permission);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }

    [TestFixture]
    public class UnityPluginSecurityVerifierTests
    {
        private UnityPluginSecurityVerifier _securityVerifier;
        private PluginLoadContext _context;
        private PluginManifest _manifest;

        [SetUp]
        public void SetUp()
        {
            _securityVerifier = new UnityPluginSecurityVerifier();

            _manifest = new PluginManifest
            {
                Id = "test.plugin",
                Version = "1.0.0",
                Name = "Test Plugin",
                Unity = new UnityPluginSettings
                {
                    MinUnityVersion = "2022.3.0",
                    SupportedPlatforms = new[] { "StandaloneWindows64" }
                },
                Security = new SecurityConfig
                {
                    RequireSignature = false,
                    Permissions = new PermissionsConfig
                    {
                        Unity = new UnityPermissions
                        {
                            CanCreateGameObjects = true
                        }
                    }
                }
            };

            _context = new PluginLoadContext("/test/path", _manifest);
        }

        [Test]
        public void ValidatePluginAsync_WithValidUnityPlugin_ReturnsTrue()
        {
            // Act
            var result = _securityVerifier.ValidatePluginAsync(_context);

            // Assert
            Assert.IsTrue(result.Result);
        }

        [Test]
        public void ValidatePluginAsync_WithIncompatibleUnityVersion_ReturnsFalse()
        {
            // Arrange
            _manifest.Unity.MinUnityVersion = "2025.1.0"; // Future version
            
            // Act
            var result = _securityVerifier.ValidatePluginAsync(_context);

            // Assert
            Assert.IsFalse(result.Result);
        }

        [Test]
        public void ValidatePluginAsync_WithUnsupportedPlatform_ReturnsFalse()
        {
            // Arrange
            _manifest.Unity.SupportedPlatforms = new[] { "iOS" }; // Assuming we're on Windows
            
            // Act
            var result = _securityVerifier.ValidatePluginAsync(_context);

            // Assert
            Assert.IsFalse(result.Result);
        }

        [Test]
        public void ValidatePluginAsync_WithNonUnityPlugin_ReturnsTrue()
        {
            // Arrange
            _manifest.Unity = null;
            
            // Act
            var result = _securityVerifier.ValidatePluginAsync(_context);

            // Assert
            Assert.IsTrue(result.Result); // Non-Unity plugins pass through
        }
    }
}
