// CBSSolverCore.cs - Correct CBS Implementation
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class CBSSolver
{
    private readonly char[,] map;
    private readonly int width, height;
    private readonly bool verboseLogging;

    public int HighLevelExpanded { get; private set; }
    public int LowLevelExpanded { get; private set; }

    public CBSSolver(char[,] map, bool verboseLogging = false)
    {
        this.map = map;
        height = map.GetLength(0);
        width = map.GetLength(1);
        this.verboseLogging = verboseLogging;
    }

    public Dictionary<int, List<Int2>> FindPaths(List<(Int2 start, Int2 goal)> agents, CancellationToken token = default)
    {
        HighLevelExpanded = 0;
        LowLevelExpanded = 0;

        Console.WriteLine($"[CBS] === Starting CBS with {agents.Count} agents ===");
        Console.WriteLine($"[CBS] Map size: {width}x{height}");

        // Validate inputs
        for (int i = 0; i < agents.Count; i++)
        {
            if (!Inside(agents[i].start) || !Walkable(agents[i].start))
            {
                Console.WriteLine($"[CBS] ERROR: Agent {i} start {agents[i].start} is invalid");
                return null;
            }
            if (!Inside(agents[i].goal) || !Walkable(agents[i].goal))
            {
                Console.WriteLine($"[CBS] ERROR: Agent {i} goal {agents[i].goal} is invalid");
                return null;
            }
        }

        // HIGH LEVEL: Create root CT node with empty constraints
        var root = new CTNode
        {
            Constraints = new List<Constraint>(),
            Solution = new Dictionary<int, List<Int2>>()
        };

        // LOW LEVEL: Compute initial paths for all agents (no constraints)
        var startTime = DateTime.Now;
        Console.WriteLine("[CBS] Computing initial paths...");

        for (int i = 0; i < agents.Count; i++)
        {
            var path = LowLevelSearch(i, agents[i].start, agents[i].goal, root.Constraints, token);
            if (path == null)
            {
                Console.WriteLine($"[CBS] FAILED: No initial path for agent {i} from {agents[i].start} to {agents[i].goal}");
                return null;
            }
            root.Solution[i] = path;

            if (verboseLogging)
                Console.WriteLine($"[CBS]   Agent {i}: path length {path.Count}");
        }

        root.Cost = ComputeSolutionCost(root.Solution);
        Console.WriteLine($"[CBS] Initial solution cost: {root.Cost}");

        // Validate initial solution
        var initialConflict = FindFirstConflict(root.Solution);
        if (initialConflict == null)
        {
            Console.WriteLine("[CBS] ✓ Initial solution is conflict-free!");
            return root.Solution;
        }

        Console.WriteLine($"[CBS] Initial solution has conflicts, starting CBS...");

        // HIGH LEVEL: CBS search tree
        var open = new SortedSet<CTNode>(new CTNodeComparer());
        open.Add(root);

        int iteration = 0;
        int maxIterations = 500000;

        while (open.Count > 0 && !token.IsCancellationRequested)
        {
            iteration++;
            HighLevelExpanded++;

            // Get best node (lowest cost)
            var current = open.Min;
            open.Remove(current);

            if (verboseLogging && iteration % 100 == 0)
            {
                Console.WriteLine($"[CBS] Iteration {iteration}: cost={current.Cost}, open={open.Count}, constraints={current.Constraints.Count}");
            }

            // CONFLICT DETECTION: Find first conflict in current solution
            var conflict = FindFirstConflict(current.Solution);

            // BASE CASE: No conflicts = solution found
            if (conflict == null)
            {
                Console.WriteLine($"[CBS] ═══════════════════════════════════");
                Console.WriteLine($"[CBS] ✓ SOLUTION FOUND!");
                Console.WriteLine($"[CBS] Iterations: {iteration}");
                Console.WriteLine($"[CBS] Time: {(DateTime.Now - startTime).TotalSeconds:F2}s");
                Console.WriteLine($"[CBS] Solution cost: {current.Cost}");
                Console.WriteLine($"[CBS] High-level nodes: {HighLevelExpanded}");
                Console.WriteLine($"[CBS] Low-level expansions: {LowLevelExpanded}");
                Console.WriteLine($"[CBS] ═══════════════════════════════════");

                // Final validation
                if (!ValidateSolution(current.Solution))
                {
                    Console.WriteLine("[CBS] ERROR: Final solution has conflicts!");
                    return null;
                }

                return current.Solution;
            }

            if (verboseLogging)
            {
                Console.WriteLine($"[CBS] Iter {iteration}: Found {conflict}");
            }

            // BRANCHING: Create two children - one constraining each agent
            // Child 1: Add constraint for agent A1
            var child1 = CreateChildWithConstraint(current, conflict, conflict.Agent1, agents, token);
            if (child1 != null)
            {
                open.Add(child1);
                if (verboseLogging)
                    Console.WriteLine($"[CBS]   → Child 1: agent {conflict.Agent1} constrained, cost={child1.Cost}");
            }

            // Child 2: Add constraint for agent A2
            var child2 = CreateChildWithConstraint(current, conflict, conflict.Agent2, agents, token);
            if (child2 != null)
            {
                open.Add(child2);
                if (verboseLogging)
                    Console.WriteLine($"[CBS]   → Child 2: agent {conflict.Agent2} constrained, cost={child2.Cost}");
            }

            if (iteration >= maxIterations)
            {
                Console.WriteLine($"[CBS] Max iterations ({maxIterations}) reached");
                break;
            }
        }

        Console.WriteLine($"[CBS] No solution found after {iteration} iterations");
        return null;
    }

    // HIGH LEVEL: Create child node by adding constraint and replanning
    private CTNode CreateChildWithConstraint(CTNode parent, Conflict conflict, int agentToConstrain,
        List<(Int2 start, Int2 goal)> agents, CancellationToken token)
    {
        // Create new CT node inheriting parent's constraints and solution
        var child = new CTNode
        {
            Constraints = new List<Constraint>(parent.Constraints),
            Solution = new Dictionary<int, List<Int2>>(parent.Solution)
        };

        // Add appropriate constraint based on conflict type
        Constraint newConstraint;

        if (conflict.IsVertex)
        {
            // Vertex conflict: prevent agent from being at position at time t
            newConstraint = Constraint.CreateVertex(agentToConstrain, conflict.Pos, conflict.Time);
        }
        else
        {
            // Edge conflict: prevent agent from traversing edge at time t
            if (agentToConstrain == conflict.Agent1)
            {
                // Constrain agent1's movement from→to
                newConstraint = Constraint.CreateEdge(agentToConstrain, conflict.FromPos, conflict.ToPos, conflict.Time);
            }
            else
            {
                // Constrain agent2's movement to→from (reversed)
                newConstraint = Constraint.CreateEdge(agentToConstrain, conflict.ToPos, conflict.FromPos, conflict.Time);
            }
        }

        child.Constraints.Add(newConstraint);

        // LOW LEVEL: Replan path for constrained agent
        var (start, goal) = agents[agentToConstrain];
        var newPath = LowLevelSearch(agentToConstrain, start, goal, child.Constraints, token);

        if (newPath == null)
        {
            // No valid path exists with this constraint
            return null;
        }

        // Update solution with new path
        child.Solution[agentToConstrain] = newPath;
        child.Cost = ComputeSolutionCost(child.Solution);

        return child;
    }

    // LOW LEVEL: Space-time A* for single agent with constraints
    private List<Int2> LowLevelSearch(int agentId, Int2 start, Int2 goal,
        List<Constraint> allConstraints, CancellationToken token)
    {
        // Filter to only this agent's constraints
        var myConstraints = allConstraints.Where(c => c.AgentId == agentId).ToList();

        // Find maximum time any constraint applies
        int maxConstraintTime = myConstraints.Any() ? myConstraints.Max(c => c.Time) : 0;

        // Space-time A* search
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

        // Time horizon for search
        int maxTime = Math.Max(Heuristic(start, goal) * 4, maxConstraintTime + 50);

        while (open.Count > 0 && !token.IsCancellationRequested)
        {
            var current = open.Min;
            open.Remove(current);
            LowLevelExpanded++;

            var state = (current.Position, current.Time);

            if (!closed.Add(state))
                continue;

            // Goal test: reached goal and no future constraints block it
            if (current.Position == goal)
            {
                // Check if goal is safe at this time
                bool goalBlocked = myConstraints.Any(c =>
                    c.IsVertex && c.Position == goal && c.Time == current.Time);

                if (!goalBlocked)
                {
                    return ReconstructPath(current, goal, myConstraints);
                }
            }

            // Depth limit
            if (current.Time >= maxTime)
                continue;

            // Generate successors: wait + 4 cardinal + 4 diagonal moves
            foreach (var nextPos in GetSuccessors(current.Position))
            {
                if (!Inside(nextPos) || !Walkable(nextPos))
                    continue;

                // Diagonal move validation (prevent corner cutting)
                if (IsDiagonalMove(current.Position, nextPos))
                {
                    if (!IsDiagonalMoveLegal(current.Position, nextPos))
                        continue;
                }

                int nextTime = current.Time + 1;
                var nextState = (nextPos, nextTime);

                if (closed.Contains(nextState))
                    continue;

                // Check vertex constraint: can't be at nextPos at nextTime
                if (IsVertexConstrained(nextPos, nextTime, myConstraints))
                    continue;

                // Check edge constraint: can't move from current.Position to nextPos at current.Time
                if (IsEdgeConstrained(current.Position, nextPos, current.Time, myConstraints))
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

    // Generate all possible successor positions (wait + 8 directions)
    private IEnumerable<Int2> GetSuccessors(Int2 pos)
    {
        // Wait action
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
        // Both adjacent cells must be walkable
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

    private bool IsVertexConstrained(Int2 pos, int time, List<Constraint> constraints)
    {
        return constraints.Any(c => c.IsVertex && c.Position == pos && c.Time == time);
    }

    private bool IsEdgeConstrained(Int2 from, Int2 to, int time, List<Constraint> constraints)
    {
        // Edge constraint: can't move from 'from' to 'to' at timestep 'time'
        return constraints.Any(c => c.IsEdge && c.Time == time && c.FromPos == from && c.ToPos == to);
    }

    private int Heuristic(Int2 from, Int2 to)
    {
        // Chebyshev distance (allows diagonal movement)
        return Math.Max(Math.Abs(from.x - to.x), Math.Abs(from.y - to.y));
    }

    private List<Int2> ReconstructPath(SpaceTimeNode goalNode, Int2 goal, List<Constraint> constraints)
    {
        var path = new List<Int2>();
        var current = goalNode;

        // Build path backwards
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }

        path.Reverse();

        // Extend path if goal has future vertex constraints
        int arrivalTime = goalNode.Time;
        int latestConstraintTime = constraints
            .Where(c => c.IsVertex && c.Position == goal)
            .Select(c => c.Time)
            .DefaultIfEmpty(arrivalTime)
            .Max();

        // Wait at goal until safe
        while (path.Count <= latestConstraintTime + 1)
        {
            path.Add(goal);
        }

        return path;
    }

    // CONFLICT DETECTION: Find first conflict in solution
    private Conflict FindFirstConflict(Dictionary<int, List<Int2>> solution)
    {
        int maxLength = solution.Values.Max(p => p.Count);

        for (int t = 0; t < maxLength; t++)
        {
            // Check vertex conflicts: two agents at same position at time t
            var positionMap = new Dictionary<Int2, int>();

            foreach (var kvp in solution)
            {
                int agentId = kvp.Key;
                Int2 pos = GetPositionAtTime(kvp.Value, t);

                if (positionMap.TryGetValue(pos, out int otherAgent))
                {
                    // Vertex conflict found
                    return Conflict.CreateVertex(agentId, otherAgent, pos, t);
                }
                positionMap[pos] = agentId;
            }

            // Check edge conflicts: two agents swap positions
            if (t > 0)
            {
                var agentIds = solution.Keys.ToList();
                for (int i = 0; i < agentIds.Count; i++)
                {
                    for (int j = i + 1; j < agentIds.Count; j++)
                    {
                        int agent1 = agentIds[i];
                        int agent2 = agentIds[j];

                        var path1 = solution[agent1];
                        var path2 = solution[agent2];

                        Int2 pos1_prev = GetPositionAtTime(path1, t - 1);
                        Int2 pos1_curr = GetPositionAtTime(path1, t);
                        Int2 pos2_prev = GetPositionAtTime(path2, t - 1);
                        Int2 pos2_curr = GetPositionAtTime(path2, t);

                        // Edge conflict: agents swap positions
                        if (pos1_prev == pos2_curr && pos1_curr == pos2_prev && pos1_prev != pos1_curr)
                        {
                            return Conflict.CreateEdge(agent1, agent2, pos1_prev, pos1_curr, t - 1);
                        }
                    }
                }
            }
        }

        return null; // No conflicts
    }

    private Int2 GetPositionAtTime(List<Int2> path, int time)
    {
        // If time exceeds path length, agent stays at goal
        return time < path.Count ? path[time] : path[^1];
    }

    private int ComputeSolutionCost(Dictionary<int, List<Int2>> solution)
    {
        // Sum of individual path costs (SIC - Sum of Individual Costs)
        return solution.Values.Sum(path => path.Count - 1);
    }

    private bool ValidateSolution(Dictionary<int, List<Int2>> solution)
    {
        var conflict = FindFirstConflict(solution);
        if (conflict != null)
        {
            Console.WriteLine($"[VALIDATE] Found conflict: {conflict}");
            return false;
        }
        return true;
    }

    // ============== DATA STRUCTURES ==============

    private class SpaceTimeNode
    {
        private static int _nextId = 0;
        public readonly int Id = _nextId++;

        public Int2 Position;
        public int Time;
        public int G; // Cost from start
        public int H; // Heuristic to goal
        public int F => G + H;
        public SpaceTimeNode Parent;
    }

    private class SpaceTimeNodeComparer : IComparer<SpaceTimeNode>
    {
        public int Compare(SpaceTimeNode a, SpaceTimeNode b)
        {
            int cmp = a.F.CompareTo(b.F);
            if (cmp != 0) return cmp;
            cmp = a.H.CompareTo(b.H); // Tie-break by heuristic
            if (cmp != 0) return cmp;
            return a.Id.CompareTo(b.Id); // Tie-break by ID
        }
    }

    private class CTNode
    {
        private static int _nextId = 0;
        public readonly int Id = _nextId++;

        public List<Constraint> Constraints;
        public Dictionary<int, List<Int2>> Solution;
        public int Cost;
    }

    private class CTNodeComparer : IComparer<CTNode>
    {
        public int Compare(CTNode a, CTNode b)
        {
            // Order by cost (lower is better)
            int cmp = a.Cost.CompareTo(b.Cost);
            if (cmp != 0) return cmp;
            return a.Id.CompareTo(b.Id);
        }
    }

    public class Constraint
    {
        public int AgentId;
        public int Time;
        public bool IsVertex;
        public bool IsEdge;

        // Vertex constraint fields
        public Int2 Position;

        // Edge constraint fields
        public Int2 FromPos;
        public Int2 ToPos;

        public static Constraint CreateVertex(int agentId, Int2 position, int time)
        {
            return new Constraint
            {
                AgentId = agentId,
                Position = position,
                Time = time,
                IsVertex = true,
                IsEdge = false
            };
        }

        public static Constraint CreateEdge(int agentId, Int2 from, Int2 to, int time)
        {
            return new Constraint
            {
                AgentId = agentId,
                FromPos = from,
                ToPos = to,
                Time = time,
                IsVertex = false,
                IsEdge = true
            };
        }

        public override string ToString()
        {
            if (IsVertex)
                return $"V(agent={AgentId}, pos={Position}, t={Time})";
            else
                return $"E(agent={AgentId}, {FromPos}→{ToPos}, t={Time})";
        }
    }

    private class Conflict
    {
        public int Agent1;
        public int Agent2;
        public int Time;
        public bool IsVertex;

        // Vertex conflict
        public Int2 Pos;

        // Edge conflict
        public Int2 FromPos;
        public Int2 ToPos;

        public static Conflict CreateVertex(int agent1, int agent2, Int2 pos, int time)
        {
            return new Conflict
            {
                Agent1 = agent1,
                Agent2 = agent2,
                Pos = pos,
                Time = time,
                IsVertex = true
            };
        }

        public static Conflict CreateEdge(int agent1, int agent2, Int2 from, Int2 to, int time)
        {
            return new Conflict
            {
                Agent1 = agent1,
                Agent2 = agent2,
                FromPos = from,
                ToPos = to,
                Time = time,
                IsVertex = false
            };
        }

        public override string ToString()
        {
            if (IsVertex)
                return $"Vertex(agents {Agent1},{Agent2} @ {Pos} at t={Time})";
            else
                return $"Edge(agents {Agent1},{Agent2} swap {FromPos}↔{ToPos} at t={Time})";
        }
    }
}
