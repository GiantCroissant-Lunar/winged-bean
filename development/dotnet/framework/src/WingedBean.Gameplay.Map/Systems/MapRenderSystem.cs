using Arch.Core;
using WingedBean.Gameplay.Map.Components;
using WingedBean.Gameplay.Map.Data;

namespace WingedBean.Gameplay.Map.Systems;

/// <summary>
/// System that provides map rendering utilities.
/// </summary>
public class MapRenderSystem
{
    private readonly World _world;

    public MapRenderSystem(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Gets the character representation for a tile type.
    /// </summary>
    public char GetTileChar(TileType type)
    {
        return type switch
        {
            TileType.Empty => ' ',
            TileType.Floor => '.',
            TileType.Wall => '#',
            TileType.Door => '+',
            TileType.StairsUp => '<',
            TileType.StairsDown => '>',
            TileType.Water => '~',
            TileType.Chasm => '≈',
            _ => '?'
        };
    }

    /// <summary>
    /// Renders the map to a string for console display.
    /// </summary>
    public string RenderMap(bool showOnlyVisible = false)
    {
        var mapQuery = new QueryDescription().WithAll<MapComponent>();
        var result = new System.Text.StringBuilder();

        _world.Query(in mapQuery, (ref MapComponent mapComp) =>
        {
            var map = mapComp.Map;

            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    var tile = map.GetTile(x, y);

                    // Check visibility settings
                    if (showOnlyVisible)
                    {
                        if (tile.IsVisible)
                        {
                            // Show visible tiles normally
                            result.Append(GetTileChar(tile.Type));
                        }
                        else if (tile.IsExplored)
                        {
                            // Show explored but not visible tiles dimmed
                            result.Append('·');
                        }
                        else
                        {
                            // Unknown tiles
                            result.Append(' ');
                        }
                    }
                    else
                    {
                        // Show all tiles
                        result.Append(GetTileChar(tile.Type));
                    }
                }
                result.AppendLine();
            }
        });

        return result.ToString();
    }

    /// <summary>
    /// Renders the map with entities overlaid.
    /// </summary>
    public string RenderMapWithEntities(char playerChar = '@', char npcChar = 'M')
    {
        var mapQuery = new QueryDescription().WithAll<MapComponent>();
        string[,]? grid = null;
        var width = 0;
        var height = 0;

        // First, render the base map
        _world.Query(in mapQuery, (ref MapComponent mapComp) =>
        {
            var map = mapComp.Map;
            width = map.Width;
            height = map.Height;
            grid = new string[height, width];

            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    var tile = map.GetTile(x, y);
                    grid[y, x] = GetTileChar(tile.Type).ToString();
                }
            }
        });

        if (grid == null)
        {
            return string.Empty;
        }

        // Overlay entities
        var entityQuery = new QueryDescription().WithAll<PositionComponent>();
        _world.Query(in entityQuery, (Entity entity, ref PositionComponent pos) =>
        {
            if (pos.X >= 0 && pos.X < width && pos.Y >= 0 && pos.Y < height)
            {
                // For now, just use generic char - can be extended with a RenderComponent
                grid[pos.Y, pos.X] = playerChar.ToString();
            }
        });

        // Build final string
        var result = new System.Text.StringBuilder();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                result.Append(grid[y, x]);
            }
            result.AppendLine();
        }

        return result.ToString();
    }
}
