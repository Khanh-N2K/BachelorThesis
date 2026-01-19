// WHCASolver.cs - Windowed Hierarchical Cooperative A* for Real-Time Games
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

/// <summary>
/// WHCA* (Windowed Hierarchical Cooperative A*) Solver
/// Ultra-fast MAPF algorithm for real-time games with 100+ agents
/// 
/// Key Features:
/// - Plans in short time windows (typically 8-16 steps)
/// - Very low computational overhead
/// - Designed for frequent replanning
/// - Handles dynamic environments well
/// 
/// Trade-offs:
/// - Paths are 30-50% longer than optimal
/// - May occasionally get stuck (needs replanning)
/// - Best used with periodic replanning in game loop
/// </summary>
public class WHCASolver
{
    private readonly char[,] map;
    private readonly int width, height;
    private readonly int windowSize;
    private readonly bool verboseLogging;
    private readonly bool allowDiagonal;

    // Reservation table: tracks occupied space-time positions
    private Dictionary<(Int2 pos, int time), int> reservationTable;
    private Dictionary<int, List<Int2>> completedPaths;

    public int TotalExpansions { get; private set; }
    public int WindowSearches { get; private set; }
    public int AgentsPlanned { get; private set; }

    /// <summary>
    /// Create WHCA* solver
    /// </summary>
    /// <param name="map">2D grid map</param>
    /// <param name="windowSize">Look-ahead window (default 8, larger = better paths but slower)</param>
    /// <param name="allowDiagonal">Allow 8-way movement (default true)</param>
    /// <param name="verboseLogging">Enable debug output</param>
    public WHCASolver(char[,] map, int windowSize = 8, bool allowDiagonal = true, bool verboseLogging = false)
    {
        this.map = map;
        height = map.GetLength(0);
        width = map.GetLength(1);
        this.windowSize = Math.Max(4, Math.Min(windowSize, 32)); // Clamp 4-32
        this.allowDiagonal = allowDiagonal;
        this.verboseLogging = verboseLogging;
    }

    /// <summary>
    /// Find paths for all agents using WHCA* algorithm
    /// Compatible with CBS solver interface
    /// </summary>
    public Dictionary<int, List<Int2>> FindPaths(List<ScenarioData> scenarios, CancellationToken token = default)
    {
        TotalExpansions = 0;
        WindowSearches = 0;
        AgentsPlanned = 0;

        Console.WriteLine($"[WHCA*] === Windowed Planning with {scenarios.Count} scenarios ===");
        Console.WriteLine($"[WHCA*] Map size: {width}x{height}");
        Console.WriteLine($"[WHCA*] Window size: {windowSize}");
        Console.WriteLine($"[WHCA*] Diagonal movement: {allowDiagonal}");

        // Validate inputs
        foreach (var s in scenarios)
        {
            if (!Inside(s.start) || !Walkable(s.start))
            {
                Debug.LogError($"[WHCA*] ERROR: Agent {s.agentId} start {s.start} invalid");
                return null;
            }

            if (!Inside(s.goal) || !Walkable(s.goal))
            {
                Debug.LogError($"[WHCA*] ERROR: Agent {s.agentId} goal {s.goal} invalid");
                return null;
            }
        }

        var startTime = DateTime.Now;

        // Initialize
        reservationTable = new Dictionary<(Int2, int), int>();
        completedPaths = new Dictionary<int, List<Int2>>();

        // FIX #7: Randomize agent ordering to avoid starvation
        var random = new Random();
        var orderedAgents = scenarios
            .Select(s => new
            {
                AgentId = s.agentId,
                Start = s.start,
                Goal = s.goal,
                Distance = Heuristic(s.start, s.goal),
                RandomTie = random.Next() // Add randomization
            })
            .OrderByDescending(x => x.Distance)
            .ThenBy(x => x.RandomTie) // Randomize ties
            .ToList();

        if (verboseLogging)
        {
            Console.WriteLine($"[WHCA*] Planning order (longest paths first):");
            for (int i = 0; i < Math.Min(5, orderedAgents.Count); i++)
            {
                var a = orderedAgents[i];
                Console.WriteLine(
                    $"[WHCA*]   {i + 1}. Agent {a.AgentId} (est. {a.Distance} steps)"
                );
            }
        }

        // PLAN EACH AGENT SEQUENTIALLY
        int failedAgents = 0;
        foreach (var item in orderedAgents)
        {
            if (token.IsCancellationRequested)
                break;

            int agentId = item.AgentId;
            var start = item.Start;
            var goal = item.Goal;

            var path = PlanAgentWindowed(agentId, start, goal, token);

            if (path == null)
            {
                path = new List<Int2> { start };
                failedAgents++;
            }

            completedPaths[agentId] = path;
            AgentsPlanned++;
        }

        var totalTime = (DateTime.Now - startTime).TotalSeconds;
        int totalCost = completedPaths.Values.Sum(p => p.Count - 1);
        double avgExpansions = AgentsPlanned > 0 ? (double)TotalExpansions / AgentsPlanned : 0;

        Console.WriteLine($"[WHCA*] ═══════════════════════════════════");
        Console.WriteLine($"[WHCA*] ✓ PLANNING COMPLETE!");
        Console.WriteLine($"[WHCA*] Time: {totalTime:F3}s");
        Console.WriteLine($"[WHCA*] Solution cost: {totalCost}");
        Console.WriteLine($"[WHCA*] Agents planned: {AgentsPlanned} ({failedAgents} failed)");
        Console.WriteLine($"[WHCA*] Window searches: {WindowSearches}");
        Console.WriteLine($"[WHCA*] Total expansions: {TotalExpansions}");
        Console.WriteLine($"[WHCA*] Avg expansions/agent: {avgExpansions:F1}");
        Console.WriteLine($"[WHCA*] Avg time/agent: {(totalTime / scenarios.Count * 1000):F2}ms");
        Console.WriteLine($"[WHCA*] ═══════════════════════════════════");

        // STEP 4.1 — Skip validation when no agents exist
        if (completedPaths.Count > 0)
        {
            if (!ValidateSolution(completedPaths))
            {
                Console.WriteLine("[WHCA*] WARNING: Solution has conflicts!");
            }
        }
        else if (verboseLogging)
        {
            Console.WriteLine("[WHCA*] No agents planned. Validation skipped.");
        }

        return completedPaths;
    }

    /// <summary>
    /// Plan single agent using windowed A* approach
    /// Repeatedly plans in short windows until goal is reached
    /// </summary>
    private List<Int2> PlanAgentWindowed(int agentId, Int2 start, Int2 goal, CancellationToken token)
    {
        var fullPath = new List<Int2> { start };
        var currentPos = start;
        int currentTime = 0;
        int maxTotalSteps = Heuristic(start, goal) * 8 + 100; // Safety limit

        // FIX #5: Better stuck detection with position history
        int stuckCounter = 0;
        var recentPositions = new Queue<Int2>();
        int historySize = windowSize;

        // FIX #1: Reserve as we go, don't use temporary reservations
        reservationTable[(start, 0)] = agentId;

        while (currentPos != goal && currentTime < maxTotalSteps && !token.IsCancellationRequested)
        {
            // Plan within current window
            var windowPath = PlanSingleWindow(agentId, currentPos, goal, currentTime, token);

            if (windowPath == null || windowPath.Count <= 1)
            {
                // Stuck - try waiting in place
                stuckCounter++;

                // If stuck too long, give up
                if (stuckCounter > windowSize * 3)
                {
                    if (verboseLogging)
                        Console.WriteLine($"[WHCA*] Agent {agentId} stuck at {currentPos} for {stuckCounter} steps");

                    // FIX #1: Clear all reservations on failure
                    ClearAgentReservations(agentId);
                    return null;
                }

                // Wait action - keep current position
                currentTime++;
                fullPath.Add(currentPos);
                reservationTable[(currentPos, currentTime)] = agentId;
                continue;
            }

            // FIX #5: Check for oscillation/slow progress
            recentPositions.Enqueue(currentPos);
            if (recentPositions.Count > historySize)
                recentPositions.Dequeue();

            // Check if we're oscillating (visiting same position multiple times)
            if (recentPositions.Count >= historySize)
            {
                int duplicates = recentPositions.Count(p => p == currentPos);
                if (duplicates > historySize / 2 && currentPos != goal)
                {
                    if (verboseLogging)
                        Console.WriteLine($"[WHCA*] Agent {agentId} oscillating at {currentPos}");

                    ClearAgentReservations(agentId);
                    return null;
                }
            }

            // Extract next position from window
            if (windowPath.Count > 1)
            {
                Int2 nextPos = windowPath[1];

                // Check if making progress
                if (nextPos == currentPos && nextPos != goal)
                {
                    stuckCounter++;
                }
                else
                {
                    stuckCounter = 0;
                }

                currentPos = nextPos;
                currentTime++;
                fullPath.Add(currentPos);
                reservationTable[(currentPos, currentTime)] = agentId;
            }
            else
            {
                // Already at goal in this window
                break;
            }
        }

        if (currentPos != goal)
        {
            if (verboseLogging)
                Console.WriteLine($"[WHCA*] Agent {agentId} didn't reach goal (pos={currentPos}, goal={goal}, steps={currentTime})");

            ClearAgentReservations(agentId);
            return null;
        }

        // Success - path is already reserved, just return it
        return fullPath;
    }

    /// <summary>
    /// FIX #1: Clear all reservations for an agent
    /// </summary>
    private void ClearAgentReservations(int agentId)
    {
        var keysToRemove = reservationTable
            .Where(kvp => kvp.Value == agentId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            reservationTable.Remove(key);
        }
    }

    /// <summary>
    /// Plan a single window using space-time A*
    /// Returns path within the window or null if no path exists
    /// </summary>
    private List<Int2> PlanSingleWindow(int agentId, Int2 start, Int2 goal, int startTime, CancellationToken token)
    {
        WindowSearches++;

        var open = new SortedSet<WindowNode>(new WindowNodeComparer());
        var closed = new HashSet<(Int2 pos, int time)>();
        var gScore = new Dictionary<(Int2 pos, int time), int>();

        var startNode = new WindowNode
        {
            Position = start,
            Time = startTime,
            G = 0,
            H = Heuristic(start, goal),
            Parent = null
        };

        open.Add(startNode);
        gScore[(start, startTime)] = 0;

        int windowEndTime = startTime + windowSize;
        int expansionLimit = windowSize * windowSize * 4; // Prevent infinite loops
        int expansions = 0;

        while (open.Count > 0 && !token.IsCancellationRequested && expansions < expansionLimit)
        {
            var current = open.Min;
            open.Remove(current);
            TotalExpansions++;
            expansions++;

            var state = (current.Position, current.Time);

            if (closed.Contains(state))
                continue;

            closed.Add(state);

            // SUCCESS: Reached goal within window
            if (current.Position == goal)
            {
                return ReconstructWindowPath(current);
            }

            // FIX #3: Window boundary check (should be > not >=)
            if (current.Time > windowEndTime)
            {
                return ReconstructWindowPath(current);
            }

            // EXPAND NEIGHBORS
            foreach (var nextPos in GetSuccessors(current.Position))
            {
                if (!Inside(nextPos) || !Walkable(nextPos))
                    continue;

                // Validate diagonal movement
                if (allowDiagonal && IsDiagonalMove(current.Position, nextPos))
                {
                    // FIX #6: Check both static and dynamic diagonal legality
                    if (!IsDiagonalMoveLegal(current.Position, nextPos, current.Time, agentId))
                        continue;
                }

                int nextTime = current.Time + 1;
                var nextState = (nextPos, nextTime);

                if (closed.Contains(nextState))
                    continue;

                // VERTEX COLLISION CHECK: Is this space-time position reserved?
                if (IsReserved(nextPos, nextTime, agentId))
                    continue;

                // EDGE COLLISION CHECK: Prevent swapping
                if (IsEdgeCollision(current.Position, nextPos, current.Time, nextTime, agentId))
                    continue;

                int moveCost = 1; // Both move and wait cost 1
                int tentativeG = current.G + moveCost;

                if (gScore.TryGetValue(nextState, out int existingG) && tentativeG >= existingG)
                    continue;

                gScore[nextState] = tentativeG;

                var successor = new WindowNode
                {
                    Position = nextPos,
                    Time = nextTime,
                    G = tentativeG,
                    H = Heuristic(nextPos, goal),
                    Parent = current
                };

                open.Add(successor);
            }
        }

        // No path found in this window
        return null;
    }

    private bool IsReserved(Int2 pos, int time, int agentId)
    {
        if (reservationTable.TryGetValue((pos, time), out int occupyingAgent))
        {
            return occupyingAgent != agentId;
        }
        return false;
    }

    private bool IsEdgeCollision(Int2 from, Int2 to, int fromTime, int toTime, int agentId)
    {
        // Skip edge collision check for wait actions
        if (from == to)
            return false;

        // FIX #2: Improved edge collision detection
        // Check if another agent is moving from 'to' to 'from' at the same time (swap)
        if (reservationTable.TryGetValue((to, fromTime), out int otherAtTo))
        {
            if (otherAtTo != agentId)
            {
                // Check if that agent will be at 'from' next timestep
                if (reservationTable.TryGetValue((from, toTime), out int otherAtFrom))
                {
                    if (otherAtFrom == otherAtTo)
                    {
                        // Confirmed swap: other agent moving from 'to' to 'from'
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private IEnumerable<Int2> GetSuccessors(Int2 pos)
    {
        // Wait action first (important for avoiding conflicts)
        yield return pos;

        // Cardinal directions (always available)
        yield return new Int2(pos.x + 1, pos.y);
        yield return new Int2(pos.x - 1, pos.y);
        yield return new Int2(pos.x, pos.y + 1);
        yield return new Int2(pos.x, pos.y - 1);

        // Diagonal directions (if enabled)
        if (allowDiagonal)
        {
            yield return new Int2(pos.x + 1, pos.y + 1);
            yield return new Int2(pos.x + 1, pos.y - 1);
            yield return new Int2(pos.x - 1, pos.y + 1);
            yield return new Int2(pos.x - 1, pos.y - 1);
        }
    }

    private bool IsDiagonalMove(Int2 from, Int2 to)
    {
        return from.x != to.x && from.y != to.y;
    }

    // FIX #6: Check both static and dynamic diagonal legality
    private bool IsDiagonalMoveLegal(Int2 from, Int2 to, int time, int agentId)
    {
        // Both adjacent cells must be walkable (no corner cutting)
        var corner1 = new Int2(from.x, to.y);
        var corner2 = new Int2(to.x, from.y);

        if (!Walkable(corner1) || !Walkable(corner2))
            return false;

        // Check if other agents occupy the corner positions at this time
        if (IsReserved(corner1, time + 1, agentId) || IsReserved(corner2, time + 1, agentId))
            return false;

        return true;
    }

    private bool Inside(Int2 p) => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;

    private bool Walkable(Int2 p)
    {
        if (!Inside(p)) return false;
        char c = map[p.y, p.x];
        return c == '.' || c == 'S' || c == 'G';
    }

    private int Heuristic(Int2 from, Int2 to)
    {
        if (allowDiagonal)
        {
            // Chebyshev distance for 8-way movement
            return Math.Max(Math.Abs(from.x - to.x), Math.Abs(from.y - to.y));
        }
        else
        {
            // Manhattan distance for 4-way movement
            return Math.Abs(from.x - to.x) + Math.Abs(from.y - to.y);
        }
    }

    private List<Int2> ReconstructWindowPath(WindowNode node)
    {
        var path = new List<Int2>();
        var current = node;

        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }

    private bool ValidateSolution(Dictionary<int, List<Int2>> solution)
    {
        // FIX #9: Validate entire path length, not just first 1000 steps
        if (solution == null || solution.Count == 0)
        {
            if (verboseLogging)
                Console.WriteLine("[WHCA*-VALIDATE] No agents to validate.");
            return true;
        }

        int maxLength = solution.Values.Max(p => p.Count);
        bool hasConflicts = false;

        for (int t = 0; t < maxLength; t++)
        {
            var positions = new Dictionary<Int2, List<int>>();

            foreach (var kvp in solution)
            {
                int agentId = kvp.Key;
                Int2 pos = GetPositionAtTime(kvp.Value, t);

                if (!positions.ContainsKey(pos))
                    positions[pos] = new List<int>();
                positions[pos].Add(agentId);
            }

            foreach (var kvp in positions)
            {
                if (kvp.Value.Count > 1)
                {
                    if (verboseLogging || !hasConflicts) // Only log first conflict unless verbose
                    {
                        Console.WriteLine($"[WHCA*-VALIDATE] Conflict at t={t}, pos={kvp.Key}: agents {string.Join(",", kvp.Value)}");
                    }
                    hasConflicts = true;
                }
            }
        }

        return !hasConflicts;
    }

    private Int2 GetPositionAtTime(List<Int2> path, int time)
    {
        return time < path.Count ? path[time] : path[^1];
    }

    // ============== DATA STRUCTURES ==============

    private class WindowNode
    {
        private static int _nextId = 0;
        public readonly int Id = _nextId++;

        public Int2 Position;
        public int Time;
        public int G; // Cost from start
        public int H; // Heuristic to goal
        public int F => G + H;
        public WindowNode Parent;
    }

    private class WindowNodeComparer : IComparer<WindowNode>
    {
        public int Compare(WindowNode a, WindowNode b)
        {
            // Sort by F-cost (lower is better)
            int cmp = a.F.CompareTo(b.F);
            if (cmp != 0) return cmp;

            // Tie-break: prefer nodes closer to goal (lower H)
            cmp = a.H.CompareTo(b.H);
            if (cmp != 0) return cmp;

            // Final tie-break: node ID
            return a.Id.CompareTo(b.Id);
        }
    }
}