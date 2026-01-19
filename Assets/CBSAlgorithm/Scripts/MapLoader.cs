using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Threading;
using Random = UnityEngine.Random;

public class MapLoader : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite dirt;
    public Sprite rock;
    public Sprite swamp;
    public Sprite water;

    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("CBS Settings")]
    public int maxAgents = 30; // LIMIT AGENTS FOR TESTING
    public float solveTimeoutSeconds = 30f;

    // Store parsed map data
    public int Width { get; private set; }
    public int Height { get; private set; }
    public char[,] MapData { get; private set; }
    public GameObject[,] TileObjects { get; private set; }

    // Store scenario entries
    public List<ScenarioData> Scenarios { get; private set; } = new();

    public void RenderMap()
    {
        if (MapData == null)
        {
            Debug.LogError("No map data loaded!");
            return;
        }

        // Clean old tiles
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        TileObjects = new GameObject[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Sprite sprite = CharToSprite(MapData[y, x]);
                if (sprite != null)
                {
                    GameObject tile = CreateTile(x, Height - y - 1, sprite);
                    TileObjects[y, x] = tile;
                }
            }
        }

        Debug.Log($"[MapLoader] Rendered {Width}x{Height} map");
    }

    Sprite CharToSprite(char c)
    {
        return c switch
        {
            '.' or 'G' => dirt,
            '@' or 'O' or 'T' => rock,
            'S' => swamp,
            'W' => water,
            _ => null
        };
    }

    GameObject CreateTile(int x, int y, Sprite sprite)
    {
        GameObject go = new GameObject($"Tile_{x}_{y}");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(x, y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        return go;
    }

    public void LoadFromString(string mapText)
    {
        string[] lines = mapText.Split('\n');
        int mapStart = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("height")) Height = int.Parse(lines[i].Split(' ')[1]);
            if (lines[i].StartsWith("width")) Width = int.Parse(lines[i].Split(' ')[1]);
            if (lines[i].StartsWith("map")) { mapStart = i + 1; break; }
        }

        MapData = new char[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            string row = lines[mapStart + y].Trim();
            for (int x = 0; x < Width; x++)
            {
                MapData[y, x] = row[x];
            }
        }

        Debug.Log($"[MapLoader] Loaded map: {Width}x{Height}");
    }

    public void LoadScenariosFromString(string scenText)
    {
        Scenarios.Clear();
        string[] lines = scenText.Split('\n');

        int id = 0;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("version"))
                continue;

            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9)
                continue;

            int startX = int.Parse(parts[4]);
            int startY = int.Parse(parts[5]);
            int goalX = int.Parse(parts[6]);
            int goalY = int.Parse(parts[7]);

            Scenarios.Add(new ScenarioData(id, new Int2(startX, startY), new Int2(goalX, goalY)));
            id++;
        }

        // LIMIT AGENTS
        int originalCount = Scenarios.Count;
        if (Scenarios.Count > maxAgents)
        {
            Scenarios = Scenarios.Take(maxAgents).ToList();
            Debug.LogWarning($"[MapLoader] Limited to {maxAgents} agents (from {originalCount})");
        }

        Debug.Log($"[MapLoader] Loaded {Scenarios.Count} scenarios");

        // Validate scenarios
        ValidateScenarios();
    }

    private void ValidateScenarios()
    {
        if (MapData == null)
        {
            Debug.LogWarning("[MapLoader] Can't validate - no map loaded");
            return;
        }

        string walkable = ".GS@O";
        int invalidCount = 0;

        foreach (var s in Scenarios)
        {
            int id = s.agentId;

            if (s.start.x < 0 || s.start.x >= Width || s.start.y < 0 || s.start.y >= Height)
            {
                Debug.LogError($"[MapLoader] Agent {id}: Start {s.start} out of bounds!");
                invalidCount++;
                continue;
            }

            if (s.goal.x < 0 || s.goal.x >= Width || s.goal.y < 0 || s.goal.y >= Height)
            {
                Debug.LogError($"[MapLoader] Agent {id}: Goal {s.goal} out of bounds!");
                invalidCount++;
                continue;
            }

            char startChar = MapData[s.start.y, s.start.x];
            char goalChar = MapData[s.goal.y, s.goal.x];

            if (!walkable.Contains(startChar))
            {
                Debug.LogWarning($"[MapLoader] Agent {id}: Start on '{startChar}' at {s.start}");
                invalidCount++;
            }

            if (!walkable.Contains(goalChar))
            {
                Debug.LogWarning($"[MapLoader] Agent {id}: Goal on '{goalChar}' at {s.goal}");
                invalidCount++;
            }
        }

        if (invalidCount == 0)
            Debug.Log($"[MapLoader] ✓ All {Scenarios.Count} scenarios validated");
        else
            Debug.LogWarning($"[MapLoader] ⚠ {invalidCount} scenarios have issues");
    }

    public void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab not assigned!");
            return;
        }

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Enemy_"))
                Destroy(child.gameObject);
        }

        Debug.Log($"[SpawnEnemies] Spawning {Scenarios.Count} enemies");

        foreach (var scen in Scenarios)
        {
            int yFlipped = Height - scen.start.y - 1;
            Vector3 pos = new Vector3(scen.start.x, yFlipped, 0);

            GameObject enemy = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
            enemy.name = $"Enemy_{scen.agentId}";

            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
        }

        Debug.Log($"[SpawnEnemies] ✓ Spawned {Scenarios.Count} enemies");
    }

    public float timestep = 0.5f;

    public async void SolveAndAnimate()
    {
        if (Scenarios.Count == 0)
        {
            Debug.LogError("[Solve] No scenarios loaded!");
            return;
        }

        Debug.Log($"[Solve] ═══════════════════════════════════");
        Debug.Log($"[Solve] Starting CBS with {Scenarios.Count} agents");
        Debug.Log($"[Solve] Timeout: {solveTimeoutSeconds}s");
        Debug.Log($"[Solve] ═══════════════════════════════════");

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(solveTimeoutSeconds));

        try
        {
            var startTime = Time.realtimeSinceStartup;
            var paths = await MultiThreadedCBSSolver.SolveAsync(this, Scenarios, cts.Token);
            //var paths = await CBSSolver.SolveAsync(this, Scenarios, cts.Token);
            var elapsedTime = Time.realtimeSinceStartup - startTime;

            if (paths == null)
            {
                Debug.LogError($"[Solve] ✗ CBS failed after {elapsedTime:F2}s");
                Debug.LogError($"[Solve] Try: 1) Fewer agents, 2) Different scenario, 3) Check map connectivity");
                return;
            }

            Debug.Log($"[Solve] ═══════════════════════════════════");
            Debug.Log($"[Solve] ✓ SUCCESS in {elapsedTime:F2}s!");
            Debug.Log($"[Solve] Found paths for {paths.Count} agents");
            Debug.Log($"[Solve] ═══════════════════════════════════");

            StartCoroutine(AnimateAgents(paths));
        }
        catch (OperationCanceledException)
        {
            Debug.LogError($"[Solve] ✗ TIMEOUT after {solveTimeoutSeconds}s!");
            Debug.LogError($"[Solve] CBS couldn't solve with {Scenarios.Count} agents");
            Debug.LogError($"[Solve] Try reducing maxAgents in inspector or increase timeout");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Solve] ✗ ERROR: {ex.Message}");
            Debug.LogError($"[Solve] {ex.StackTrace}");
        }
    }

    //public void SolveAndAnimate_FakeCBS()
    //{
    //    // Spawn enemies if not already
    //    var enemies = GetComponentsInChildren<SpriteRenderer>()
    //        .Where(sr => sr.gameObject.name.StartsWith("Enemy"))
    //        .Select(sr => sr.transform)
    //        .ToList();

    //    if (enemies.Count == 0)
    //    {
    //        Debug.LogWarning("[FakeCBS] No enemies found, spawning first...");
    //        SpawnEnemies();
    //        enemies = GetComponentsInChildren<SpriteRenderer>()
    //            .Where(sr => sr.gameObject.name.StartsWith("Enemy"))
    //            .Select(sr => sr.transform)
    //            .ToList();
    //    }

    //    // Generate fake straight-line paths (but within map)
    //    Dictionary<Transform, List<Vector3>> paths = new();
    //    foreach (var (scen, idx) in Scenarios.Select((s, i) => (s, i)))
    //    {
    //        if (idx >= enemies.Count)
    //            break;

    //        Vector3 start = new(scen.StartX, Height - scen.StartY - 1, 0);
    //        Vector3 goal = new(scen.GoalX, Height - scen.GoalY - 1, 0);
    //        List<Vector3> path = new();

    //        int steps = Random.Range(10, 20);
    //        for (int i = 0; i <= steps; i++)
    //        {
    //            float t = i / (float)steps;
    //            Vector3 p = Vector3.Lerp(start, goal, t);
    //            path.Add(p);
    //        }

    //        paths[enemies[idx]] = path;
    //    }

    //    // Start fake CBS
    //    var fakeCBS = GetComponent<FakeCBSSolver>();
    //    if (fakeCBS == null)
    //        fakeCBS = gameObject.AddComponent<FakeCBSSolver>();

    //    fakeCBS.Initialize(this, enemies, paths);
    //    fakeCBS.StartFakeCBS();
    //}


    public System.Collections.IEnumerator AnimateAgents(Dictionary<int, List<Vector2Int>> paths)
    {
        if (paths == null)
        {
            Debug.LogError("[Animate] Paths is null!");
            yield break;
        }

        var enemies = GetComponentsInChildren<SpriteRenderer>()
            .Where(sr => sr.gameObject.name.StartsWith("Enemy"))
            .Select(sr => sr.transform)
            .ToList();

        Debug.Log($"[Animate] Found {enemies.Count} enemies, {paths.Count} paths");

        var pathsByTransform = new Dictionary<Transform, List<Vector2Int>>();
        foreach (var kv in paths)
        {
            int agentId = kv.Key;
            var path = kv.Value;

            if (agentId < 0 || agentId >= enemies.Count)
            {
                Debug.LogWarning($"[Animate] Agent {agentId} out of range");
                continue;
            }

            var transform = enemies[agentId];
            pathsByTransform[transform] = path;

            if (agentId < 3)
                Debug.Log($"[Animate] Agent {agentId}: path length = {path.Count}");
        }

        int maxSteps = paths.Values.Max(p => p.Count);
        Debug.Log($"[Animate] Max path length: {maxSteps} steps");

        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);

        float pauseTime = timestep * 0.5f;

        for (int step = 0; step < maxSteps; step++)
        {
            int animatedThisStep = 0;

            foreach (var kvp in pathsByTransform)
            {
                var transform = kvp.Key;
                var path = kvp.Value;

                if (path.Count <= step)
                    continue;

                animatedThisStep++;

                Vector2Int pos = path[step];
                int yFlipped = Height - pos.y - 1;

                if (transform == null)
                {
                    Debug.LogWarning("[Animate] Transform is null!");
                    continue;
                }

                Vector3 targetPos = new Vector3(pos.x, yFlipped, 0);

                if (step == path.Count - 1)
                {
                    // Final position
                    transform.DOMove(targetPos, timestep)
                        .SetEase(Ease.Linear)
                        .OnComplete(() =>
                        {
                            if (transform != null)
                                Destroy(transform.gameObject);
                        });
                }
                else
                {
                    transform.DOMove(targetPos, timestep).SetEase(Ease.Linear);
                }
            }

            if (step % 10 == 0 || step == 0)
                Debug.Log($"[Animate] Step {step}/{maxSteps}: {animatedThisStep} agents");

            yield return new WaitForSeconds(timestep + pauseTime);
        }

        Debug.Log("[Animate] ✓ Animation complete!");
    }
}