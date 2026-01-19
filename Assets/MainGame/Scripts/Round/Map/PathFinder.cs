using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class PathFinder
{
    private static char[,] _mapMatrix;

    public static void SetMapMatrix(char[,] mapMatrix)
    {
        _mapMatrix = mapMatrix;
    }

    public static void SetCellWalkable(Vector2Int coord, bool walkable = true)
    {
        _mapMatrix[coord.y, coord.x] = walkable ? '.' : '@';
    }

    public static async UniTask<Dictionary<int, List<Int2>>> GetPath(List<ScenarioData> scenarioList)
    {
        // var solver = new CBSSolver(_mapMatrix, verboseLogging: true);
        // var solver = new PrioritizedPlanningSolver(_mapMatrix, verboseLogging: true);
        var solver = new WHCASolver(_mapMatrix, verboseLogging: true);
        await UniTask.SwitchToThreadPool();
        var paths = solver.FindPaths(scenarioList);
        await UniTask.SwitchToMainThread();
        return paths;
    }
}
