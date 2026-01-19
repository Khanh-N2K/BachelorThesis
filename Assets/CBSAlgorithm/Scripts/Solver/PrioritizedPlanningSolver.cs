// PrioritizedPlanningSolver.cs - Fast MAPF for Real-Time Games
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// Reuse Int2 from CBS
// public struct Int2 { ... } - same as CBS version

/// <summary>
/// Prioritized Planning solver - fast, non-optimal MAPF algorithm
/// Perfect for real-time games where speed > optimality
/// Typically 10-100x faster than CBS with ~10-30% longer paths
/// </summary>
public class PrioritizedPlanningSolver
{
    private readonly char[,] map;
    private readonly int width, height;
    private readonly bool verboseLogging;

    // Reservation table: tracks which positions are occupied at each timestep
    private Dictionary<(Int2 pos, int time), int> reservationTable;
    private Dictionary<int, List<Int2>> completedPaths;

    public int LowLevelExpanded { get; private set; }
    public int AgentsPlanned { get; private set; }

    public PrioritizedPlanningSolver(char[,] map, bool verboseLogging = false)
    {
        this.map = map;
        height = map.GetLength(0);
        width = map.GetLength(1);
        this.verboseLogging = verboseLogging;
    }

    /// <summary>
    /// Find paths using Prioritized Planning algorithm
    /// Compatible with CBS solver interface
    /// </summary>
    public Dictionary<int, List<Int2>> FindPaths(List<(Int2 start, Int2 goal)> agents, CancellationToken token = default)
    {
        LowLevelExpanded = 0;
        AgentsPlanned = 0;

        Console.WriteLine($"[PP] === Prioritized Planning with {agents.Count} agents ===");
        Console.WriteLine($"[PP] Map size: {width}x{height}");

        // Validate inputs
        for (int i = 0; i < agents.Count; i++)
        {
            if (!Inside(agents[i].start) || !Walkable(agents[i].start))
            {
                Console.WriteLine($"[PP] ERROR: Agent {i} start {agents[i].start} is invalid");
                return null;
            }
            if (!Inside(agents[i].goal) || !Walkable(agents[i].goal))
            {
                Console.WriteLine($"[PP] ERROR: Agent {i} goal {agents[i].goal} is invalid");
                return null;
            }
        }

        var startTime = DateTime.Now;

        // Initialize reservation table
        reservationTable = new Dictionary<(Int2, int), int>();
        completedPaths = new Dictionary<int, List<Int2>>();

        // PRIORITIZATION: Sort agents by distance to goal (longest first)
        // This heuristic often produces better results
        var prioritizedAgents = agents
            .Select((agent, idx) => new { Index = idx, Agent = agent, Distance = Heuristic(agent.start, agent.goal) })
            .OrderByDescending(x => x.Distance)
            .ToList();

        if (verboseLogging)
        {
            Console.WriteLine("[PP] Agent priorities (longest path first):");
            for (int i = 0; i < Math.Min(5, prioritizedAgents.Count); i++)
            {
                var a = prioritizedAgents[i];
                Console.WriteLine($"[PP]   Priority {i + 1}: Agent {a.Index} (dist={a.Distance})");
            }
        }

        // SEQUENTIAL PLANNING: Plan each agent in priority order
        foreach (var item in prioritizedAgents)
        {
            if (token.IsCancellationRequested)
                break;

            int agentId = item.Index;
            var (start, goal) = item.Agent;

            if (verboseLogging && AgentsPlanned % 20 == 0)
            {
                Console.WriteLine($"[PP] Planning agent {AgentsPlanned}/{agents.Count}...");
            }

            // Plan path avoiding all previously planned agents
            var path = PlanSingleAgent(agentId, start, goal, token);

            if (path == null)
            {
                Console.WriteLine($"[PP] FAILED: No path for agent {agentId} from {start} to {goal}");
                Console.WriteLine($"[PP] This usually means the problem is too constrained for Prioritized Planning");
                Console.WriteLine($"[PP] Try CBS for optimal solution or reduce agent density");
                return null;
            }

            // Reserve this agent's path
            completedPaths[agentId] = path;
            ReservePath(agentId, path);
            AgentsPlanned++;
        }

        var totalTime = (DateTime.Now - startTime).TotalSeconds;
        int totalCost = completedPaths.Values.Sum(p => p.Count - 1);

        Console.WriteLine($"[PP] ═══════════════════════════════════");
        Console.WriteLine($"[PP] ✓ SOLUTION FOUND!");
        Console.WriteLine($"[PP] Time: {totalTime:F3}s");
        Console.WriteLine($"[PP] Solution cost: {totalCost}");
        Console.WriteLine($"[PP] Agents planned: {AgentsPlanned}");
        Console.WriteLine($"[PP] Low-level expansions: {LowLevelExpanded}");
        Console.WriteLine($"[PP] Avg time per agent: {(totalTime / agents.Count * 1000):F1}ms");
        Console.WriteLine($"[PP] ═══════════════════════════════════");

        // Validate solution
        if (!ValidateSolution(completedPaths))
        {
            Console.WriteLine("[PP] WARNING: Solution has conflicts!");
            return null;
        }

        return completedPaths;
    }

    /// <summary>
    /// Plan path for single agent avoiding all reserved space-time positions
    /// </summary>
    private List<Int2> PlanSingleAgent(int agentId, Int2 start, Int2 goal, CancellationToken token)
    {
        var open = new SortedSet<SpaceTimeNode>(new SpaceTimeNodeComparer());
        var closed = new HashSet<(Int2 pos, int time)>();
        var gScore = new Dictionary<(Int2 pos, int time), int>();

        var startNode = new SpaceTimeNode
        {
            Position = start,
            Time = 0,
            G = 0,
            H = Heuristic(start, goal),
            Parent = null
        };

        open.Add(startNode);
        gScore[(start, 0)] = 0;

        // Adaptive time horizon
        int baseDistance = Heuristic(start, goal);
        int maxTime = baseDistance * 6 + 100; // Generous time limit

        while (open.Count > 0 && !token.IsCancellationRequested)
        {
            var current = open.Min;
            open.Remove(current);
            LowLevelExpanded++;

            var state = (current.Position, current.Time);

            if (!closed.Add(state))
                continue;

            // Goal test
            if (current.Position == goal)
            {
                // Check if path to goal is safe
                if (!IsReserved(goal, current.Time, agentId))
                {
                    return ReconstructPath(current, goal);
                }
            }

            // Time limit
            if (current.Time >= maxTime)
                continue;

            // Expand successors
            foreach (var nextPos in GetSuccessors(current.Position))
            {
                if (!Inside(nextPos) || !Walkable(nextPos))
                    continue;

                // Validate diagonal moves
                if (IsDiagonalMove(current.Position, nextPos))
                {
                    if (!IsDiagonalMoveLegal(current.Position, nextPos))
                        continue;
                }

                int nextTime = current.Time + 1;
                var nextState = (nextPos, nextTime);

                if (closed.Contains(nextState))
                    continue;

                // COLLISION AVOIDANCE: Check if position is reserved
                if (IsReserved(nextPos, nextTime, agentId))
                    continue;

                // EDGE COLLISION: Check if swapping with another agent
                if (IsEdgeReserved(current.Position, nextPos, current.Time, agentId))
                    continue;

                int tentativeG = current.G + 1;

                if (gScore.TryGetValue(nextState, out int existingG) && tentativeG >= existingG)
                    continue;

                gScore[nextState] = tentativeG;

                var successor = new SpaceTimeNode
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

        return null; // No path found
    }

    private bool IsReserved(Int2 pos, int time, int agentId)
    {
        if (reservationTable.TryGetValue((pos, time), out int occupyingAgent))
        {
            return occupyingAgent != agentId;
        }
        return false;
    }

    private bool IsEdgeReserved(Int2 from, Int2 to, int time, int agentId)
    {
        // Check if another agent is moving from 'to' to 'from' at the same time
        // This prevents edge collisions (swapping)
        foreach (var kvp in completedPaths)
        {
            int otherAgent = kvp.Key;
            if (otherAgent == agentId)
                continue;

            var otherPath = kvp.Value;
            if (time < otherPath.Count - 1)
            {
                Int2 otherFrom = otherPath[time];
                Int2 otherTo = otherPath[time + 1];

                // Check for edge swap
                if (otherFrom == to && otherTo == from)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void ReservePath(int agentId, List<Int2> path)
    {
        for (int t = 0; t < path.Count; t++)
        {
            var key = (path[t], t);
            reservationTable[key] = agentId;
        }

        // Also reserve goal position for future timesteps
        // This prevents other agents from blocking the goal
        Int2 goalPos = path[^1];
        for (int t = path.Count; t < path.Count + 50; t++)
        {
            var key = (goalPos, t);
            if (!reservationTable.ContainsKey(key))
            {
                reservationTable[key] = agentId;
            }
        }
    }

    private IEnumerable<Int2> GetSuccessors(Int2 pos)
    {
        // Wait action (often important for avoiding conflicts)
        yield return pos;

        // Cardinal directions
        yield return new Int2(pos.x + 1, pos.y);
        yield return new Int2(pos.x - 1, pos.y);
        yield return new Int2(pos.x, pos.y + 1);
        yield return new Int2(pos.x, pos.y - 1);

        // Diagonal directions
        yield return new Int2(pos.x + 1, pos.y + 1);
        yield return new Int2(pos.x + 1, pos.y - 1);
        yield return new Int2(pos.x - 1, pos.y + 1);
        yield return new Int2(pos.x - 1, pos.y - 1);
    }

    private bool IsDiagonalMove(Int2 from, Int2 to)
    {
        return from.x != to.x && from.y != to.y;
    }

    private bool IsDiagonalMoveLegal(Int2 from, Int2 to)
    {
        var corner1 = new Int2(from.x, to.y);
        var corner2 = new Int2(to.x, from.y);
        return Walkable(corner1) && Walkable(corner2);
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
        // Chebyshev distance for 8-way movement
        return Math.Max(Math.Abs(from.x - to.x), Math.Abs(from.y - to.y));
    }

    private List<Int2> ReconstructPath(SpaceTimeNode goalNode, Int2 goal)
    {
        var path = new List<Int2>();
        var current = goalNode;

        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }

        path.Reverse();

        // Minimal extension - just ensure we stay at goal
        if (path[^1] != goal)
        {
            path.Add(goal);
        }

        return path;
    }

    private bool ValidateSolution(Dictionary<int, List<Int2>> solution)
    {
        int maxLength = solution.Values.Max(p => p.Count);

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
                    Console.WriteLine($"[PP-VALIDATE] Conflict at t={t}, pos={kvp.Key}: agents {string.Join(",", kvp.Value)}");
                    return false;
                }
            }
        }

        return true;
    }

    private Int2 GetPositionAtTime(List<Int2> path, int time)
    {
        return time < path.Count ? path[time] : path[^1];
    }

    // ============== DATA STRUCTURES ==============

    private class SpaceTimeNode
    {
        private static int _nextId = 0;
        public readonly int Id = _nextId++;

        public Int2 Position;
        public int Time;
        public int G;
        public int H;
        public int F => G + H;
        public SpaceTimeNode Parent;
    }

    private class SpaceTimeNodeComparer : IComparer<SpaceTimeNode>
    {
        public int Compare(SpaceTimeNode a, SpaceTimeNode b)
        {
            int cmp = a.F.CompareTo(b.F);
            if (cmp != 0) return cmp;
            cmp = a.H.CompareTo(b.H);
            if (cmp != 0) return cmp;
            return a.Id.CompareTo(b.Id);
        }
    }
}
