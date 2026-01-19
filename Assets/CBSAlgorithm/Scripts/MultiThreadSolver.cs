// MultiThreadedCBSSolver.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class MultiThreadedCBSSolver
{
    public static async Task<Dictionary<int, List<Vector2Int>>> SolveAsync(
        MapLoader map,
        List<ScenarioData> scenarios,
        CancellationToken token = default)
    {
        var core = new WHCASolver(map.MapData);
        var agents = new List<(Int2, Int2)>();

        var result = await Task.Run(() => core.FindPaths(scenarios, token), token);
        if (result == null) return null;

        // convert back to UnityEngine.Vector2Int
        var converted = new Dictionary<int, List<Vector2Int>>();
        foreach (var kv in result)
        {
            converted[kv.Key] = kv.Value.ConvertAll(p => new Vector2Int(p.x, p.y));
        }
        return converted;
    }
}
