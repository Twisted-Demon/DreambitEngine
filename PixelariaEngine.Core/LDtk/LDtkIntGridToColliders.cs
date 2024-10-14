using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PixelariaEngine;

public static class LDtkIntGridToColliders
{
    public static List<List<Point>> FindConnectedRegions(int[,] grid)
    {
        var visited = new bool[grid.GetLength(0), grid.GetLength(1)];
        var regions = new List<List<Point>>();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] == 1 && !visited[x, y])
                {
                    var region = new List<Point>();
                    FloodFill(grid, visited, x, y, region);
                    regions.Add(region);
                }
            }
        }

        return regions;
    }

    private static void FloodFill(int[,] grid, bool[,] visited, int x, int y, List<Point> region)
    {
        if (x < 0 || y < 0 || x >= grid.GetLength(0) || y >= grid.GetLength(1)) return;
        if (visited[x, y] || grid[x, y] == 0) return;

        visited[x, y] = true;
        region.Add(new Point(x, y));

        FloodFill(grid, visited, x + 1, y, region);
        FloodFill(grid, visited, x - 1, y, region);
        FloodFill(grid, visited, x, y + 1, region);
        FloodFill(grid, visited, x, y - 1, region);
    }

    public static Polygon TracePolygon(List<Point> region)
    {
        var vertices = new List<Point>();

        var current = region[0];
        var start = current;
        var direction = new Point(1, 0); //start moving right

        do
        {
            vertices.Add(current * new Point(4, 4));
            current = GetNextEdge(region, current, ref direction);
        } while (current != start);

        return new Polygon
        {
            Vertices = vertices.Select(p => new Vector2(p.X, p.Y)).ToArray()
        };
    }

    private static Point GetNextEdge(List<Point> region, Point current, ref Point direction)
    {
        
        // Clockwise directions: Right -> Down -> Left -> Up
        var directions = new []
        {
            new Point(1, 0),  // Right
            new Point(0, 1),  // Down
            new Point(-1, 0), // Left
            new Point(0, -1)  // Up
        };
        
        // Loop through directions and check if the next tile is part of the region
        for (int i = 0; i < directions.Length; i++)
        {
            var nextDirection = directions[i];
            var next = current + nextDirection;

            // Check if the next tile is part of the region and is an outer edge
            if (region.Contains(next) && IsOuterEdge(current, region))
            {
                direction = nextDirection;  // Update direction
                return next;                // Move to next tile
            }
        }

        // If no valid edge is found, return the current tile (shouldn't happen if valid polygons exist)
        return current;
    }
    
    
    private static bool IsOuterEdge(Point current, List<Point> region)
    {
        // Check if the 'next' tile is at the boundary of the region
        // This means that at least one of its neighbors is NOT in the region
        Point[] neighbors = new Point[]
        {
            new Point(1, 0),  // Right
            new Point(-1, 0), // Left
            new Point(0, 1),  // Down
            new Point(0, -1)  // Up
        };

        foreach (var neighbor in neighbors)
        {
            Point neighborPos = current + neighbor;
            if (!region.Contains(neighborPos))
            {
                return true; // This is an outer edge if a neighbor is not part of the region
            }
        }

        return false; // It's an internal point if all neighbors are part of the region
    }
}