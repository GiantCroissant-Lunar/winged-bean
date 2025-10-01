using UnityEngine;
using Microsoft.Extensions.DependencyInjection;
using WingedBean.Host;
using WingedBean.Host.Unity;
using System.Threading.Tasks;

namespace WingedBean.Unity.Plugins.Sample
{
    /// <summary>
    /// Sample Unity plugin demonstrating MonoBehaviour integration and state preservation
    /// </summary>
    public class SampleUnityPlugin : IPluginActivator
    {
        private IUnityPluginServices? _unityServices;

        /// <summary>
        /// Activate the plugin
        /// </summary>
        public async Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
        {
            // Register plugin services
            services.AddSingleton<ISampleService, SampleService>();

            // Get Unity-specific services
            _unityServices = hostServices.GetService<IUnityPluginServices>();

            Debug.Log("Sample Unity Plugin activated!");

            // Add sample MonoBehaviour component
            _unityServices?.AddComponent<SamplePluginBehaviour>();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Deactivate the plugin
        /// </summary>
        public async Task DeactivateAsync(CancellationToken ct = default)
        {
            // Clean up components
            _unityServices?.RemoveComponent<SamplePluginBehaviour>();

            Debug.Log("Sample Unity Plugin deactivated!");

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Sample service provided by the plugin
    /// </summary>
    public interface ISampleService
    {
        string GetMessage();
        void DoWork();
    }

    /// <summary>
    /// Implementation of sample service
    /// </summary>
    public class SampleService : ISampleService
    {
        private int _workCount = 0;

        public string GetMessage()
        {
            return $"Hello from Sample Unity Plugin! Work done: {_workCount} times";
        }

        public void DoWork()
        {
            _workCount++;
            Debug.Log($"Sample service doing work #{_workCount}");
        }
    }

    /// <summary>
    /// Sample MonoBehaviour component with state preservation
    /// </summary>
    public class SamplePluginBehaviour : MonoBehaviour, IPluginComponent, IStatefulComponent
    {
        [Header("Plugin State")]
        public float rotationSpeed = 90f;
        public Color cubeColor = Color.blue;
        public Vector3 targetPosition = Vector3.zero;

        [Header("Runtime State")]
        public int updateCount = 0;
        public float totalTime = 0f;

        private GameObject _sampleCube;
        private ISampleService? _sampleService;

        /// <summary>
        /// Initialize the component
        /// </summary>
        public async Task InitializeAsync(CancellationToken ct = default)
        {
            Debug.Log("SamplePluginBehaviour initializing...");

            // Create a sample cube to demonstrate Unity integration
            _sampleCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _sampleCube.name = "PluginSampleCube";
            _sampleCube.transform.position = targetPosition;

            // Set cube color
            var renderer = _sampleCube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = cubeColor;
            }

            Debug.Log("Sample Unity Plugin Component initialized!");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup the component
        /// </summary>
        public async Task CleanupAsync(CancellationToken ct = default)
        {
            Debug.Log("SamplePluginBehaviour cleaning up...");

            if (_sampleCube != null)
            {
                DestroyImmediate(_sampleCube);
            }

            Debug.Log("Sample Unity Plugin Component cleaned up!");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Unity Update method
        /// </summary>
        private void Update()
        {
            updateCount++;
            totalTime += Time.deltaTime;

            // Rotate the sample cube
            if (_sampleCube != null)
            {
                _sampleCube.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);

                // Move cube in a sine wave pattern
                var newPos = targetPosition + Vector3.up * Mathf.Sin(totalTime) * 2f;
                _sampleCube.transform.position = newPos;
            }

            // Do work every 60 frames (approximately once per second at 60fps)
            if (updateCount % 60 == 0)
            {
                _sampleService?.DoWork();
            }
        }

        /// <summary>
        /// Save component state for hot-reload
        /// </summary>
        public async Task<object?> SaveStateAsync(CancellationToken ct = default)
        {
            var state = new SamplePluginState
            {
                RotationSpeed = rotationSpeed,
                CubeColor = cubeColor,
                TargetPosition = targetPosition,
                UpdateCount = updateCount,
                TotalTime = totalTime,
                CubePosition = _sampleCube?.transform.position ?? Vector3.zero,
                CubeRotation = _sampleCube?.transform.rotation ?? Quaternion.identity
            };

            Debug.Log($"Saving plugin state: UpdateCount={updateCount}, TotalTime={totalTime:F2}");
            return await Task.FromResult(state);
        }

        /// <summary>
        /// Restore component state after hot-reload
        /// </summary>
        public async Task RestoreStateAsync(object? state, CancellationToken ct = default)
        {
            if (state is SamplePluginState pluginState)
            {
                rotationSpeed = pluginState.RotationSpeed;
                cubeColor = pluginState.CubeColor;
                targetPosition = pluginState.TargetPosition;
                updateCount = pluginState.UpdateCount;
                totalTime = pluginState.TotalTime;

                // Restore cube state if it exists
                if (_sampleCube != null)
                {
                    _sampleCube.transform.position = pluginState.CubePosition;
                    _sampleCube.transform.rotation = pluginState.CubeRotation;

                    var renderer = _sampleCube.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = cubeColor;
                    }
                }

                Debug.Log($"Restored plugin state: UpdateCount={updateCount}, TotalTime={totalTime:F2}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Start method - resolve services
        /// </summary>
        private void Start()
        {
            // Try to resolve the sample service (this would work if DI integration is properly set up)
            // In a real implementation, you'd need proper service locator integration
            Debug.Log("SamplePluginBehaviour started!");
        }

        /// <summary>
        /// Draw debug information
        /// </summary>
        private void OnGUI()
        {
            var rect = new Rect(10, 300, 400, 150);
            GUI.Box(rect, "Sample Unity Plugin Status");

            GUI.Label(new Rect(20, 320, 380, 20), $"Update Count: {updateCount}");
            GUI.Label(new Rect(20, 340, 380, 20), $"Total Time: {totalTime:F2}s");
            GUI.Label(new Rect(20, 360, 380, 20), $"Rotation Speed: {rotationSpeed:F1}°/s");
            GUI.Label(new Rect(20, 380, 380, 20), $"Cube Color: {cubeColor}");

            if (_sampleCube != null)
            {
                GUI.Label(new Rect(20, 400, 380, 20), $"Cube Position: {_sampleCube.transform.position}");
            }
        }
    }

    /// <summary>
    /// State data for the sample plugin component
    /// </summary>
    [System.Serializable]
    public class SamplePluginState
    {
        public float RotationSpeed { get; set; }
        public Color CubeColor { get; set; }
        public Vector3 TargetPosition { get; set; }
        public int UpdateCount { get; set; }
        public float TotalTime { get; set; }
        public Vector3 CubePosition { get; set; }
        public Quaternion CubeRotation { get; set; }
    }
}
