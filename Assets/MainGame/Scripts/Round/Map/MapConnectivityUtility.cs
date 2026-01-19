using System.Collections.Generic;
using UnityEngine;

public static class MapConnectivityUtility
{
    private static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public static void FixClosedAreas(MapBlockType[,] map, Vector2Int mapSize)
    {
        bool[,] visited = new bool[mapSize.y, mapSize.x];

        // 1️⃣ Start flood fill from center (or any guaranteed reachable cell)
        Vector2Int start = new Vector2Int(
            mapSize.x / 2,
            mapSize.y / 2
        );

        if (map[start.y, start.x] == MapBlockType.Obstacle)
            map[start.y, start.x] = MapBlockType.Empty;

        FloodFill(map, mapSize, start, visited);

        // 2️⃣ Find unreachable empty cells
        for (int y = 0; y < mapSize.y; y++)
        {
            for (int x = 0; x < mapSize.x; x++)
            {
                if (map[y, x] == MapBlockType.Empty && !visited[y, x])
                {
                    BreakRing(map, mapSize, new Vector2Int(x, y));
                    FloodFill(map, mapSize, start, visited);
                }
            }
        }
    }

    private static void FloodFill(
        MapBlockType[,] map,
        Vector2Int size,
        Vector2Int start,
        bool[,] visited)
    {
        Queue<Vector2Int> queue = new();
        queue.Enqueue(start);
        visited[start.y, start.x] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in Neighbors)
            {
                Vector2Int next = current + dir;

                if (!InBounds(next, size))
                    continue;

                if (visited[next.y, next.x])
                    continue;

                if (map[next.y, next.x] == MapBlockType.Obstacle)
                    continue;

                visited[next.y, next.x] = true;
                queue.Enqueue(next);
            }
        }
    }

    private static void BreakRing(
        MapBlockType[,] map,
        Vector2Int size,
        Vector2Int isolatedCell)
    {
        foreach (var dir in Neighbors)
        {
            Vector2Int neighbor = isolatedCell + dir;

            if (!InBounds(neighbor, size))
                continue;

            if (map[neighbor.y, neighbor.x] == MapBlockType.Obstacle)
            {
                // 🔨 Break the wall
                map[neighbor.y, neighbor.x] = MapBlockType.Empty;
                return;
            }
        }
    }

    private static bool InBounds(Vector2Int pos, Vector2Int size)
    {
        return pos.x >= 0 && pos.y >= 0 &&
               pos.x < size.x && pos.y < size.y;
    }
}
