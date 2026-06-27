using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Assembles a Lab C line-follower course at scene load from the modular TrackPiece prefabs
/// (Assets/Prefabs/TrackPieces), randomized per attempt, then wires the autograder: a
/// DestinationPoint checkpoint at each step along the correct path and a DestinationStop at
/// the goal. Runs in Awake BEFORE <see cref="AutograderManager"/> (negative execution order)
/// so the manager discovers the generated tasks normally. Pieces snap on a 10 m grid by
/// port-matching; the walk is self-avoiding (occupancy + bounds). At intersections the through
/// (correct) arm gets the highest-priority colour present (RED &gt; GREEN &gt; BLUE) on a random
/// physical exit, and the dead-end arms are capped — so the route can't be memorized.
/// Re-entering Play re-randomizes; there is no baked layout.
/// </summary>
[DefaultExecutionOrder(-50)]
public class TrackGenerator : MonoBehaviour
{
    public enum Rule { StraightLine, CurvedLine, Perpendicular, ColorPriorityI, ColorPriorityII, FloorMaze, WindingLine }

    #region Set in Unity Editor
    [SerializeField] private Rule rule = Rule.StraightLine;
    [SerializeField] private float totalPoints = 1f;
    [SerializeField] private int mazeLoops = 5;

    [Header("Piece prefabs")]
    [SerializeField] private TrackPiece spawnPrefab;
    [SerializeField] private TrackPiece straightPrefab;
    [SerializeField] private TrackPiece slantRPrefab;
    [SerializeField] private TrackPiece slantLPrefab;
    [SerializeField] private TrackPiece cornerPrefab;
    [SerializeField] private TrackPiece cornerLPrefab;
    [SerializeField] private TrackPiece tPrefab;
    [SerializeField] private TrackPiece crossPrefab;
    [SerializeField] private TrackPiece goalPrefab;

    [Header("Materials (line priority RED > GREEN > BLUE)")]
    [SerializeField] private Material redFlat;
    [SerializeField] private Material greenFlat;
    [SerializeField] private Material blueFlat;
    [SerializeField] private Material parkZoneMaterial;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material padMaterial;

    [Header("Lab E altitude gates (0 = pure Lab C line)")]
    [SerializeField] private int gateCount = 0;
    [SerializeField] private GameObject gatePrefab;
    #endregion

    private const float Cell = 10f;
    private const float WallHeight = 12f;
    private const float WallThickness = 0.5f;
    private const float LineWidth = 0.75f;
    private const float CheckpointWidth = 7f;
    private const float CheckpointHeight = 12f;
    private const float CheckpointY = 6f;
    private const float FinishWidth = 6f;
    private const int BoundCells = 8;   // walk stays within +-80 m
    private const float GateMinAltitude = 3f;  // Lab E gate centre height range
    private const float GateMaxAltitude = 10f;
    private const float GateApertureWidth = 8f;
    private const float GateApertureHeight = 7f;

    /// <summary>Whether this tier varies the main-path colour (the intersection tiers only).</summary>
    private bool VariesColor => this.rule == Rule.ColorPriorityI || this.rule == Rule.ColorPriorityII || this.rule == Rule.FloorMaze;

    private struct Cursor { public Vector3 pos; public Vector3 dir; }

    private Transform trackRoot;
    private Transform managerTransform;
    private int uiLayer;
    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private Material currentColor;
    private readonly List<Pose> mainPath = new List<Pose>(); // through-exit poses (pos + facing) in order
    private Vector3 goalPosition;

    private void Awake()
    {
        this.uiLayer = LayerMask.NameToLayer("UI");
        AutograderManager manager = Object.FindFirstObjectByType<AutograderManager>();
        if (manager == null)
        {
            Debug.LogError("TrackGenerator: no AutograderManager in scene.");
            return;
        }
        this.managerTransform = manager.transform;
        this.trackRoot = new GameObject("GeneratedTrack").transform;

        this.BuildCourse();
        this.CreateTasks();

        MapLineCourse map = Object.FindFirstObjectByType<MapLineCourse>();
        if (map != null)
        {
            map.RebuildFromScene();
        }
    }

    private void BuildCourse()
    {
        // Spawn: pad at world origin (where the drone spawns), exit facing +Z.
        TrackPiece spawn = Instantiate(this.spawnPrefab, this.trackRoot);
        spawn.transform.SetPositionAndRotation(new Vector3(-Cell / 2f, 0f, -Cell / 2f), Quaternion.identity);
        this.MarkOccupied(spawn);
        this.currentColor = this.RandomColor();
        this.ColorPiece(spawn, this.currentColor);
        Cursor cursor = this.ExitCursor(spawn, this.FirstExitPort(spawn));

        bool lastWasSegment = false;
        foreach (TrackPiece[] candidates in this.BuildSteps())
        {
            TrackPiece prefab = this.PickFitting(candidates, cursor, true);
            if (prefab == null)
            {
                break; // boxed in — end the course here and drop the goal
            }
            TrackPiece piece = this.PlacePiece(prefab, cursor);
            this.MarkOccupied(piece);

            if (this.IsIntersection(piece))
            {
                this.AddJointDisc(cursor.pos, this.currentColor);
                cursor = this.HandleIntersection(piece);
                lastWasSegment = false;
            }
            else
            {
                // Vary the main path on the intersection tiers: a second+ consecutive regular
                // segment steps 1-2 down the priority chain, so straight runs aren't one
                // monotonous colour. The simple tiers (straight/curved/perpendicular) stay one colour.
                if (lastWasSegment && this.VariesColor)
                {
                    this.currentColor = this.ShiftDownPriority(this.currentColor);
                }
                this.AddJointDisc(cursor.pos, this.currentColor);
                this.ColorPiece(piece, this.currentColor);
                int exit = this.FirstExitPort(piece);
                this.mainPath.Add(new Pose(piece.WorldPortPosition(exit), Quaternion.LookRotation(piece.WorldPortDirection(exit))));
                cursor = this.ExitCursor(piece, exit);
                lastWasSegment = true;
            }
        }

        TrackPiece goal = this.PlacePiece(this.goalPrefab, cursor);
        this.MarkOccupied(goal);
        this.ColorPiece(goal, this.currentColor);
        this.AddJointDisc(cursor.pos, this.currentColor);
        this.goalPosition = goal.transform.TransformPoint(new Vector3(Cell / 2f, 0f, Cell / 2f));
    }

    /// <summary>Per-rule list of steps; each step is a set of candidate prefabs to try in order.</summary>
    private List<TrackPiece[]> BuildSteps()
    {
        TrackPiece[] singles = { this.straightPrefab, this.slantRPrefab, this.slantLPrefab, this.cornerPrefab, this.cornerLPrefab };
        TrackPiece[] slants = { this.slantRPrefab, this.slantLPrefab, this.straightPrefab };
        TrackPiece[] corners = { this.cornerPrefab, this.cornerLPrefab };
        TrackPiece[] inter = { this.tPrefab, this.crossPrefab };
        TrackPiece[] straightOnly = { this.straightPrefab };
        TrackPiece[] tOnly = { this.tPrefab };

        List<TrackPiece[]> steps = new List<TrackPiece[]>();
        switch (this.rule)
        {
            case Rule.StraightLine:
                for (int i = 0; i < 3; i++) steps.Add(straightOnly);
                break;
            case Rule.CurvedLine:
                for (int i = 0; i < 3; i++) steps.Add(slants);
                break;
            case Rule.Perpendicular:
                steps.Add(straightOnly); steps.Add(corners); steps.Add(straightOnly);
                break;
            case Rule.ColorPriorityI:
                steps.Add(tOnly);
                for (int i = 0; i < 3; i++) steps.Add(singles);
                steps.Add(tOnly);
                break;
            case Rule.ColorPriorityII:
                for (int i = 0; i < 3; i++) steps.Add(singles);
                for (int i = 0; i < 3; i++) steps.Add(inter);
                for (int i = 0; i < 3; i++) steps.Add(singles);
                break;
            case Rule.FloorMaze:
                // Start > 5x (intersection > segment > segment) > End.
                for (int i = 0; i < this.mazeLoops; i++)
                {
                    steps.Add(inter);
                    steps.Add(singles);
                    steps.Add(singles);
                }
                break;
            case Rule.WindingLine:
                // Lab E "complex track": a longer single-path line (enough points for several gates).
                for (int i = 0; i < 6; i++) steps.Add(singles);
                break;
        }
        return steps;
    }

    #region Placement + occupancy
    private TrackPiece PickFitting(TrackPiece[] candidates, Cursor cursor, bool spread)
    {
        List<TrackPiece> fitting = new List<TrackPiece>();
        foreach (TrackPiece c in candidates)
        {
            if (this.FootprintFree(c, cursor)) fitting.Add(c);
        }
        if (fitting.Count == 0)
        {
            return null;
        }
        Shuffle(fitting);
        if (!spread)
        {
            return fitting[0];
        }
        // Reward spreading out: prefer the placement whose forward exit is farthest from the
        // origin and sits in the most open space, so the walk doesn't circle back and box itself in.
        TrackPiece best = fitting[0];
        float bestScore = float.NegativeInfinity;
        foreach (TrackPiece c in fitting)
        {
            float score = this.SpreadScore(c, cursor) + Random.value * 4f;
            if (score > bestScore)
            {
                bestScore = score;
                best = c;
            }
        }
        return best;
    }

    private float SpreadScore(TrackPiece prefab, Cursor cursor)
    {
        var (pos, rot) = this.ComputePlacement(prefab, cursor);
        Vector3 exitPos = pos;
        float bestAlign = float.NegativeInfinity;
        for (int i = 0; i < prefab.Ports.Length; i++)
        {
            if (prefab.Ports[i].role != TrackPiece.PortRole.Exit) continue;
            float align = Vector3.Dot(rot * prefab.Ports[i].localDirection, cursor.dir);
            if (align > bestAlign)
            {
                bestAlign = align;
                exitPos = rot * prefab.Ports[i].localPosition + pos;
            }
        }
        float distInCells = new Vector2(exitPos.x, exitPos.z).magnitude / Cell;
        return distInCells * 5f + this.Openness(WorldCell(exitPos));
    }

    private int Openness(Vector2Int cell)
    {
        int count = 0;
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dz = -2; dz <= 2; dz++)
            {
                Vector2Int c = new Vector2Int(cell.x + dx, cell.y + dz);
                if (Mathf.Abs(c.x) <= BoundCells && Mathf.Abs(c.y) <= BoundCells && !this.occupied.Contains(c)) count++;
            }
        }
        return count;
    }

    private (Vector3 pos, Quaternion rot) ComputePlacement(TrackPiece prefab, Cursor cursor)
    {
        int entry = prefab.EntryPortIndex();
        Vector3 e = prefab.Ports[entry].localPosition;
        Vector3 d = prefab.Ports[entry].localDirection;
        Quaternion rot = Quaternion.Euler(0f, YawFromTo(d, -cursor.dir), 0f);
        return (cursor.pos - rot * e, rot);
    }

    private bool FootprintFree(TrackPiece prefab, Cursor cursor)
    {
        var (pos, rot) = this.ComputePlacement(prefab, cursor);
        foreach (Vector2Int fc in prefab.FootprintCells)
        {
            Vector3 c = rot * new Vector3(fc.x * Cell + Cell / 2f, 0f, fc.y * Cell + Cell / 2f) + pos;
            Vector2Int cell = WorldCell(c);
            if (Mathf.Abs(cell.x) > BoundCells || Mathf.Abs(cell.y) > BoundCells || this.occupied.Contains(cell))
            {
                return false;
            }
        }
        return true;
    }

    private TrackPiece PlacePiece(TrackPiece prefab, Cursor cursor)
    {
        TrackPiece piece = Instantiate(prefab, this.trackRoot);
        var (pos, rot) = this.ComputePlacement(prefab, cursor);
        piece.transform.SetPositionAndRotation(pos, rot);
        return piece;
    }

    private void MarkOccupied(TrackPiece piece)
    {
        foreach (Vector2Int fc in piece.FootprintCells)
        {
            Vector3 c = piece.transform.TransformPoint(new Vector3(fc.x * Cell + Cell / 2f, 0f, fc.y * Cell + Cell / 2f));
            this.occupied.Add(WorldCell(c));
        }
    }

    private static Vector2Int WorldCell(Vector3 worldCenter)
    {
        return new Vector2Int(Mathf.RoundToInt(worldCenter.x / Cell), Mathf.RoundToInt(worldCenter.z / Cell));
    }

    private Cursor ExitCursor(TrackPiece piece, int portIndex)
    {
        return new Cursor { pos = piece.WorldPortPosition(portIndex), dir = piece.WorldPortDirection(portIndex) };
    }

    private int FirstExitPort(TrackPiece piece)
    {
        for (int i = 0; i < piece.Ports.Length; i++)
        {
            if (piece.Ports[i].role == TrackPiece.PortRole.Exit) return i;
        }
        return 0;
    }

    private bool IsIntersection(TrackPiece piece)
    {
        return piece.PieceCategory == TrackPiece.Category.TIntersection
            || piece.PieceCategory == TrackPiece.Category.CrossIntersection;
    }

    private static float YawFromTo(Vector3 from, Vector3 to)
    {
        for (int k = 0; k < 4; k++)
        {
            float yaw = k * 90f;
            if ((Quaternion.Euler(0f, yaw, 0f) * from - to).sqrMagnitude < 0.01f) return yaw;
        }
        return 0f;
    }
    #endregion

    #region Intersections + colour
    private Cursor HandleIntersection(TrackPiece piece)
    {
        Material entryColor = this.currentColor;
        this.ColorArm(piece, -1, entryColor); // entry stub keeps the incoming colour

        List<int> exits = new List<int>();
        for (int i = 0; i < piece.Ports.Length; i++)
        {
            if (piece.Ports[i].role == TrackPiece.PortRole.Exit) exits.Add(i);
        }

        // Prefer a through exit that leads into free space.
        List<int> free = new List<int>();
        foreach (int p in exits)
        {
            if (this.CellFreeBeyond(piece, p)) free.Add(p);
        }
        List<int> pool = free.Count > 0 ? free : exits;
        int throughPort = pool[Random.Range(0, pool.Count)];

        Material[] colors = this.PickArmColors(exits.Count); // colors[0] = highest priority
        this.ColorArm(piece, throughPort, colors[0]);
        this.ColorArm(piece, -2, colors[0]); // centre joint reads as the through colour
        int ci = 1;
        foreach (int p in exits)
        {
            if (p == throughPort) continue;
            this.ColorArm(piece, p, colors[ci]);
            this.BuildFalsePath(this.ExitCursor(piece, p), colors[ci]);
            ci++;
        }

        this.currentColor = colors[0];
        this.mainPath.Add(new Pose(piece.WorldPortPosition(throughPort), Quaternion.LookRotation(piece.WorldPortDirection(throughPort))));
        return this.ExitCursor(piece, throughPort);
    }

    /// <summary>
    /// A dead-end decoy off an intersection: 1-2 regular pieces (space permitting) in the
    /// dead-end's colour, ending in a wall cap and a "false spawn" pad so it looks legitimate.
    /// </summary>
    private void BuildFalsePath(Cursor cursor, Material color)
    {
        TrackPiece[] singles = { this.straightPrefab, this.slantRPrefab, this.slantLPrefab, this.cornerPrefab, this.cornerLPrefab };
        int length = Random.Range(1, 3);
        TrackPiece lastPiece = null;
        for (int k = 0; k < length; k++)
        {
            TrackPiece prefab = this.PickFitting(singles, cursor, false);
            if (prefab == null) break;
            TrackPiece piece = this.PlacePiece(prefab, cursor);
            this.MarkOccupied(piece);
            this.AddJointDisc(cursor.pos, color);
            this.ColorPiece(piece, color);
            cursor = this.ExitCursor(piece, this.FirstExitPort(piece));
            lastPiece = piece;
        }
        this.AddJointDisc(cursor.pos, color);
        this.CapAt(cursor.pos, cursor.dir);
        if (lastPiece != null)
        {
            // Pad on the line (centroid of the piece's line strips), so it sits on a slant's
            // diagonal / a corner's bend instead of floating off at the cell centre.
            this.AddFalseSpawnPad(this.LineCenter(lastPiece));
        }
    }

    /// <summary>The centroid of a piece's line strips — a point that lies on the line for any piece shape.</summary>
    private Vector3 LineCenter(TrackPiece piece)
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        foreach (TrackPiece.Branch branch in piece.Branches)
        {
            foreach (MeshRenderer renderer in branch.renderers)
            {
                if (renderer != null) { sum += renderer.transform.position; count++; }
            }
        }
        return count > 0 ? sum / count : piece.transform.position;
    }

    private void AddFalseSpawnPad(Vector3 worldPos)
    {
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "FalseSpawnPad";
        Destroy(pad.GetComponent<Collider>());
        pad.transform.SetParent(this.trackRoot, true);
        pad.transform.position = new Vector3(worldPos.x, 0.04f, worldPos.z);
        pad.transform.localScale = new Vector3(5f, 0.04f, 5f);
        pad.GetComponent<MeshRenderer>().sharedMaterial = this.padMaterial;
    }

    private bool CellFreeBeyond(TrackPiece piece, int exitPort)
    {
        Vector3 pos = piece.WorldPortPosition(exitPort);
        Vector3 dir = piece.WorldPortDirection(exitPort);
        Vector2Int cell = WorldCell(pos + dir * (Cell / 2f));
        return Mathf.Abs(cell.x) <= BoundCells && Mathf.Abs(cell.y) <= BoundCells && !this.occupied.Contains(cell);
    }

    private void ColorArm(TrackPiece piece, int exitPortIndex, Material material)
    {
        for (int i = 0; i < piece.Branches.Length; i++)
        {
            if (piece.Branches[i].exitPortIndex == exitPortIndex)
            {
                piece.SetBranchColor(i, material);
                return;
            }
        }
    }

    /// <summary>n distinct priority colours, [0] highest. n&gt;=3 -> R,G,B; n==2 -> a random pair.</summary>
    private Material[] PickArmColors(int n)
    {
        Material[] all = { this.redFlat, this.greenFlat, this.blueFlat };
        if (n >= 3)
        {
            return new[] { all[0], all[1], all[2] };
        }
        int a = Random.Range(0, 3);
        int b = Random.Range(0, 2);
        if (b >= a) b++;
        int lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
        return new[] { all[lo], all[hi] };
    }

    private void CapAt(Vector3 pos, Vector3 dir)
    {
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "DeadEndCap";
        cap.transform.SetParent(this.trackRoot, true);
        cap.transform.position = new Vector3(pos.x, WallHeight / 2f, pos.z) + dir * (WallThickness / 2f);
        bool eastWest = Mathf.Abs(dir.x) > 0.5f;
        cap.transform.localScale = eastWest
            ? new Vector3(WallThickness, WallHeight, Cell)
            : new Vector3(Cell, WallHeight, WallThickness);
        cap.GetComponent<MeshRenderer>().sharedMaterial = this.wallMaterial;
        this.occupied.Add(WorldCell(pos + dir * (Cell / 2f)));
    }

    private Material RandomColor()
    {
        int i = Random.Range(0, 3);
        return i == 0 ? this.redFlat : (i == 1 ? this.greenFlat : this.blueFlat);
    }

    /// <summary>Steps the colour 1-2 places down the RED&gt;GREEN&gt;BLUE chain (wrapping), so it always changes.</summary>
    private Material ShiftDownPriority(Material current)
    {
        Material[] chain = { this.redFlat, this.greenFlat, this.blueFlat };
        int idx = current == this.redFlat ? 0 : (current == this.greenFlat ? 1 : 2);
        return chain[(idx + Random.Range(1, 3)) % 3];
    }

    private void ColorPiece(TrackPiece piece, Material material)
    {
        for (int i = 0; i < piece.Branches.Length; i++)
        {
            piece.SetBranchColor(i, material);
        }
    }

    private void AddJointDisc(Vector3 worldPos, Material material)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Joint";
        Destroy(disc.GetComponent<Collider>());
        disc.transform.SetParent(this.trackRoot, true);
        disc.transform.position = new Vector3(worldPos.x, 0.03f, worldPos.z);
        disc.transform.localScale = new Vector3(LineWidth, 0.03f, LineWidth);
        disc.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    #endregion

    #region Autograder tasks
    private void CreateTasks()
    {
        // Lab E: pick gateCount evenly-spaced main-path points to carry an altitude gate; the
        // rest stay planar line checkpoints. Lab C: gateCount 0 -> all line checkpoints.
        HashSet<int> gatePoints = new HashSet<int>();
        int n = this.mainPath.Count;
        if (this.gateCount > 0 && this.gatePrefab != null && n > 0)
        {
            for (int g = 0; g < this.gateCount; g++)
            {
                gatePoints.Add(Mathf.Clamp((g + 1) * n / (this.gateCount + 1), 0, n - 1));
            }
        }

        float pointsEach = this.totalPoints / (n + 1);
        for (int i = 0; i < n; i++)
        {
            if (gatePoints.Contains(i))
            {
                this.MakeGate(this.mainPath[i], pointsEach);
            }
            else
            {
                this.MakeCheckpoint(this.mainPath[i].position, pointsEach);
            }
        }
        this.MakeFinish(this.goalPosition, pointsEach);
    }

    /// <summary>A Lab E altitude gate: the AR gate straddling the line at a randomized height,
    /// with a 3-D DestinationPoint that requires the right x/z (line) AND y (gate altitude).</summary>
    private void MakeGate(Pose pathPose, float points)
    {
        float altitude = Random.Range(GateMinAltitude, GateMaxAltitude);
        Vector3 pos = new Vector3(pathPose.position.x, altitude, pathPose.position.z);
        Quaternion rot = pathPose.rotation; // gate faces along the line direction

        Instantiate(this.gatePrefab, pos, rot, this.trackRoot);

        GameObject go = new GameObject("GateCheckpoint");
        go.layer = this.uiLayer;
        go.transform.SetPositionAndRotation(pos, rot);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(GateApertureWidth, GateApertureHeight, 1f);
        DestinationPoint task = go.AddComponent<DestinationPoint>();
        task.SetPoints(points);
        go.transform.SetParent(this.managerTransform, true);
    }

    private void MakeCheckpoint(Vector3 worldPos, float points)
    {
        GameObject go = new GameObject("Checkpoint");
        go.layer = this.uiLayer;
        go.transform.position = new Vector3(worldPos.x, CheckpointY, worldPos.z);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(CheckpointWidth, CheckpointHeight, CheckpointWidth);
        DestinationPoint task = go.AddComponent<DestinationPoint>(); // base Awake disables it
        task.SetPoints(points);
        go.transform.SetParent(this.managerTransform, true);
    }

    private void MakeFinish(Vector3 worldPos, float points)
    {
        // Invisible grading trigger; the visible green zone is authored in the Goal prefab.
        // A renderer must exist (DestinationStop.Awake caches GetComponentInChildren<Renderer>().material).
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Finish";
        go.layer = this.uiLayer;
        go.transform.position = new Vector3(worldPos.x, CheckpointY, worldPos.z);
        go.transform.localScale = new Vector3(FinishWidth, CheckpointHeight, FinishWidth);
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = this.parkZoneMaterial;
        renderer.enabled = false;
        ((BoxCollider)go.GetComponent<Collider>()).isTrigger = true;
        DestinationStop task = go.AddComponent<DestinationStop>(); // Awake caches material, disables
        task.SetPoints(points);
        go.transform.SetParent(this.managerTransform, true);
    }
    #endregion

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
