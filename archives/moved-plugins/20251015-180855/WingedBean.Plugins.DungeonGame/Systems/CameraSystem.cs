using Microsoft.Extensions.Logging;
using Plate.Camera2D;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Adapters;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Map;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// ECS system that updates camera position and calculates viewport bounds.
/// Wraps Plate.Camera2D.CameraController for ECS integration.
/// </summary>
public class CameraSystem : IECSSystem
{
    private readonly ILogger<CameraSystem>? _logger;
    private readonly CameraController _controller;

    /// <summary>
    /// Create a new camera system.
    /// </summary>
    /// <param name="logger">Optional logger</param>
    public CameraSystem(ILogger<CameraSystem>? logger = null)
    {
        _logger = logger;
        _controller = new CameraController();
    }

    /// <summary>
    /// Execute camera system - update camera position and viewport bounds.
    /// </summary>
    /// <param name="world">ECS world</param>
    /// <param name="deltaTime">Time since last update in seconds</param>
    public void Execute(IWorld world, float deltaTime)
    {
        // Get camera entity
        var cameraQuery = world.CreateQuery<CameraComponent>();
        var cameraEntity = cameraQuery.FirstOrDefault();

        if (cameraEntity == default)
        {
            _logger?.LogDebug("No camera entity found");
            return;
        }

        var cameraComp = world.GetComponent<CameraComponent>(cameraEntity);
        var camera = cameraComp.Camera;

        // Set map bounds if available and not already set
        var map = world.GetSingleton<DungeonMap>();
        if (map != null && !camera.MapBounds.HasValue)
        {
            camera.MapBounds = new Plate.Camera2D.Rectangle(0, 0, map.Width, map.Height);
            _logger?.LogInformation("Camera map bounds set to {Width}x{Height}", map.Width, map.Height);
        }

        // Get follow target
        ICameraTarget? target = null;
        if (cameraComp.FollowTarget.HasValue)
        {
            target = new PositionCameraTarget(world, cameraComp.FollowTarget.Value);
        }

        // Update camera using Plate.Camera2D
        _controller.Update(camera, target, deltaTime);

        // Calculate viewport bounds
        var bounds = _controller.CalculateViewportBounds(camera);

        // Store viewport bounds as singleton
        var boundsComp = world.GetSingleton<ViewportBoundsComponent>();
        if (boundsComp == null)
        {
            boundsComp = new ViewportBoundsComponent { Bounds = bounds };
            world.SetSingleton(boundsComp);
            _logger?.LogInformation("Viewport bounds singleton created");
        }
        else
        {
            boundsComp.Bounds = bounds;
        }

        _logger?.LogTrace("Camera at ({X:F2}, {Y:F2}), viewport: ({MinX}, {MinY}) to ({MaxX}, {MaxY})",
            camera.X, camera.Y, bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY);
    }

    /// <summary>
    /// Move camera manually (for free camera mode).
    /// </summary>
    /// <param name="world">ECS world</param>
    /// <param name="deltaX">X offset to move</param>
    /// <param name="deltaY">Y offset to move</param>
    public void MoveCamera(IWorld world, float deltaX, float deltaY)
    {
        var cameraQuery = world.CreateQuery<CameraComponent>();
        var cameraEntity = cameraQuery.FirstOrDefault();

        if (cameraEntity == default)
        {
            _logger?.LogWarning("Cannot move camera - no camera entity found");
            return;
        }

        var cameraComp = world.GetComponent<CameraComponent>(cameraEntity);
        _controller.MoveCamera(cameraComp.Camera, deltaX, deltaY);

        _logger?.LogDebug("Camera moved by ({DeltaX}, {DeltaY})", deltaX, deltaY);
    }
}
