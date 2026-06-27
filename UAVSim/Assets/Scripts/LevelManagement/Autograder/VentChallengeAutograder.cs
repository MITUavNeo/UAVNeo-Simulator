using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime path randomizer for the Lab B (Vent Challenge) autograder. On scene load it
/// picks a random valid self-avoiding path through the stoplight grid, colors each
/// on-path stoplight with the heading-relative turn toward the next intersection, makes
/// the final stoplight red, moves the green park gate into the approach corridor just
/// before that red light, and gives off-path stoplights random non-red decoy colors.
///
/// This stops students from hard-coding a fixed solution while guaranteeing every path
/// is valid: backtracking ensures each turn lands on another stoplight (never a dead
/// end), the only red is the final light, and the green win zone always sits in the
/// channel before it. Simple tiers force the first turn (e.g. Right Turn) with turns = 1.
/// </summary>
public class VentChallengeAutograder : MonoBehaviour
{
    /// <summary>A turn relative to the drone's current heading (never a U-turn).</summary>
    public enum Turn { Straight, Left, Right }

    #region Set in Unity Editor
    /// <summary>Number of responsive turns the drone makes before the final red light.</summary>
    [SerializeField]
    private int turns = 3;

    /// <summary>When true, the first turn is fixed to <see cref="forcedFirstTurn"/> (simple tiers).</summary>
    [SerializeField]
    private bool forceFirstTurn = false;

    /// <summary>The fixed first turn for simple tiers (Right Turn / Left Turn / Go Straight).</summary>
    [SerializeField]
    private Turn forcedFirstTurn = Turn.Straight;

    /// <summary>Spacing between adjacent intersections, in meters (must match the grid).</summary>
    [SerializeField]
    private float gridSpacing = 24f;

    /// <summary>How many meters before the red light the green park gate sits.</summary>
    [SerializeField]
    private float gateMargin = 6f;

    /// <summary>The green park gate (a DestinationStop volume) moved to the win location.</summary>
    [SerializeField]
    private Transform parkGate;
    #endregion

    // Signal indices on Stoplight: 0=Stop(red), 1=Left(orange), 2=Straight(green), 3=Right(blue).
    private const int RED = 0, ORANGE = 1, GREEN = 2, BLUE = 3;

    // Cardinal directions as grid steps; index 0=N(+z), 1=E(+x), 2=S(-z), 3=W(-x).
    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    private Dictionary<Vector2Int, Stoplight> grid;
    private float gateY = 7f;

    private void Awake()
    {
        this.BuildGrid();
        this.Generate();
    }

    /// <summary>Snap every stoplight to an integer grid cell so we can reason about adjacency.</summary>
    private void BuildGrid()
    {
        this.grid = new Dictionary<Vector2Int, Stoplight>();
        foreach (Stoplight light in FindObjectsByType<Stoplight>(FindObjectsSortMode.None))
        {
            Vector3 p = light.transform.position;
            this.grid[this.Cell(p)] = light;
            this.gateY = p.y;
        }
    }

    private Vector2Int Cell(Vector3 worldPos)
    {
        return new Vector2Int(Mathf.RoundToInt(worldPos.x / this.gridSpacing), Mathf.RoundToInt(worldPos.z / this.gridSpacing));
    }

    private Vector3 World(Vector2Int cell)
    {
        return new Vector3(cell.x * this.gridSpacing, this.gateY, cell.y * this.gridSpacing);
    }

    private void Generate()
    {
        if (this.grid.Count == 0)
        {
            Debug.LogError("[VentChallengeAutograder] No stoplights found in the scene.");
            return;
        }

        // Start at the entrance: the stoplight nearest x=0 with the smallest z; heading north.
        Vector2Int start = this.FindStartCell();
        int startHeading = 0; // North

        List<Vector2Int> path = new List<Vector2Int>();
        List<int> headings = new List<int>();
        if (!this.BuildPath(start, startHeading, path, headings))
        {
            Debug.LogError($"[VentChallengeAutograder] Could not build a valid {this.turns}-turn path from {start}.");
            return;
        }

        // Color each on-path stoplight with the turn that leads to the next; final = red.
        HashSet<Vector2Int> onPath = new HashSet<Vector2Int>(path);
        for (int i = 0; i < this.turns; i++)
        {
            Turn turn = this.TurnBetween(headings[i], headings[i + 1]);
            this.grid[path[i]].SetSignal(this.ColorFor(turn));
        }
        this.grid[path[this.turns]].SetSignal(RED);

        // Off-path stoplights get random non-red decoy colors so only the final light is red.
        foreach (KeyValuePair<Vector2Int, Stoplight> kv in this.grid)
        {
            if (!onPath.Contains(kv.Key))
            {
                kv.Value.SetSignal(Random.Range(1, 4)); // 1..3 (no red)
            }
        }

        // Place the green park gate in the approach corridor, gateMargin meters before the red.
        if (this.parkGate != null)
        {
            int lastHeading = headings[this.turns];
            Vector3 approach = new Vector3(Dirs[lastHeading].x, 0f, Dirs[lastHeading].y);
            Vector3 redPos = this.World(path[this.turns]);
            this.parkGate.position = new Vector3(redPos.x, this.gateY, redPos.z) - approach * this.gateMargin;
        }
    }

    /// <summary>The entrance stoplight: nearest the center column, smallest z.</summary>
    private Vector2Int FindStartCell()
    {
        Vector2Int best = default;
        bool found = false;
        foreach (Vector2Int cell in this.grid.Keys)
        {
            if (!found || cell.y < best.y || (cell.y == best.y && Mathf.Abs(cell.x) < Mathf.Abs(best.x)))
            {
                best = cell;
                found = true;
            }
        }
        return best;
    }

    /// <summary>
    /// Backtracking self-avoiding walk of <see cref="turns"/> moves. Fills path/headings with
    /// turns+1 cells. Returns false if no valid path exists.
    /// </summary>
    private bool BuildPath(Vector2Int cur, int heading, List<Vector2Int> path, List<int> headings)
    {
        path.Add(cur);
        headings.Add(heading);

        if (path.Count == this.turns + 1)
        {
            return true;
        }

        foreach (Turn turn in this.MoveOrder(path.Count))
        {
            int newHeading = this.ApplyTurn(heading, turn);
            Vector2Int next = cur + Dirs[newHeading];
            if (this.grid.ContainsKey(next) && !path.Contains(next))
            {
                if (this.BuildPath(next, newHeading, path, headings))
                {
                    return true;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        headings.RemoveAt(headings.Count - 1);
        return false;
    }

    /// <summary>The turns to try at this step: forced on the first move of a simple tier, else shuffled.</summary>
    private IEnumerable<Turn> MoveOrder(int pathCount)
    {
        if (pathCount == 1 && this.forceFirstTurn)
        {
            return new[] { this.forcedFirstTurn };
        }

        List<Turn> order = new List<Turn> { Turn.Straight, Turn.Left, Turn.Right };
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order;
    }

    private int ApplyTurn(int heading, Turn turn)
    {
        switch (turn)
        {
            case Turn.Right: return (heading + 1) & 3;
            case Turn.Left: return (heading + 3) & 3;
            default: return heading; // Straight
        }
    }

    private Turn TurnBetween(int from, int to)
    {
        if (to == ((from + 1) & 3)) return Turn.Right;
        if (to == ((from + 3) & 3)) return Turn.Left;
        return Turn.Straight;
    }

    private int ColorFor(Turn turn)
    {
        switch (turn)
        {
            case Turn.Right: return BLUE;
            case Turn.Left: return ORANGE;
            default: return GREEN; // Straight
        }
    }
}
