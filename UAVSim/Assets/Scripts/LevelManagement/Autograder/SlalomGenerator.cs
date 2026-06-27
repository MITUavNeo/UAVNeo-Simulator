using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lab F — Drone Slalom. Assembles a corridor of solid colored balls at scene load (per-trial, so it
/// can't be hardcoded) and grades sidedness with a correct-side <see cref="DestinationPoint"/> per
/// ball — the same points-based task the racecar cone objective used, so there is no new grading
/// code. The corridor reuses the Lab C straight/slant <see cref="TrackPiece"/> kit (its channel = the
/// "4 walls" the ball centres between) with the floor line suppressed. Each ball is a state-machine
/// transition for the student: detect its colour (forward camera, Lab B CV) and pass it on the
/// dictated side/level — RED left, BLUE right, GREEN above, YELLOW below (relative to travel).
/// Runs before <see cref="AutograderManager"/> (negative execution order) so it discovers the tasks.
/// </summary>
[DefaultExecutionOrder(-50)]
public class SlalomGenerator : MonoBehaviour
{
    #region Set in Unity Editor
    [SerializeField] private int ballCount = 5;
    [SerializeField] private bool allowSlants = false;  // curved tiers alternate straight/slant
    [SerializeField] private int colorCount = 2;        // 2 or 4: how many colours alternate
    [SerializeField] private float totalPoints = 5f;
    [SerializeField] private TrackPiece straightPrefab;
    [SerializeField] private TrackPiece slantRPrefab;
    [SerializeField] private TrackPiece slantLPrefab;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Material redFlat;
    [SerializeField] private Material blueFlat;
    [SerializeField] private Material greenFlat;
    [SerializeField] private Material yellowFlat;
    #endregion

    private enum BallColor { Red, Blue, Green, Yellow }

    private const float Cell = 10f;
    private const float BallY = 6f;        // middle of the 12 m corridor walls
    private const float BallScale = 1f / 3f; // Ball.prefab is 3 m; shrink to ~1 m (Lab B stoplight size)
    private const float SideOffset = 2f;   // objective zone offset (inner edge ~ the small ball's edge)
    private const int BoundCells = 8;

    private struct Cursor { public Vector3 pos; public Vector3 dir; }

    private Transform slalomRoot;
    private Transform managerTransform;
    private int uiLayer;
    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();

    private void Awake()
    {
        this.uiLayer = LayerMask.NameToLayer("UI");
        AutograderManager manager = Object.FindFirstObjectByType<AutograderManager>();
        if (manager == null)
        {
            Debug.LogError("SlalomGenerator: no AutograderManager in scene.");
            return;
        }
        this.managerTransform = manager.transform;
        this.slalomRoot = new GameObject("GeneratedSlalom").transform;
        this.Build();
    }

    private void Build()
    {
        // colour sequence: K distinct colours (shuffled) cycled across the balls
        BallColor[] all = { BallColor.Red, BallColor.Blue, BallColor.Green, BallColor.Yellow };
        Shuffle(all);
        int k = Mathf.Clamp(this.colorCount, 1, 4);

        Cursor cursor = new Cursor { pos = Vector3.zero, dir = Vector3.forward };
        float pointsEach = this.totalPoints / Mathf.Max(1, this.ballCount);

        for (int i = 0; i < this.ballCount; i++)
        {
            TrackPiece prefab = this.PickPiece(cursor, i);
            if (prefab == null) break;
            TrackPiece piece = this.PlacePiece(prefab, cursor);
            SuppressLine(piece);

            int entry = piece.EntryPortIndex();
            int exit = FirstExitPort(piece);
            Vector3 a = piece.WorldPortPosition(entry);
            Vector3 b = piece.WorldPortPosition(exit);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 tangent = b - a; tangent.y = 0f; tangent = tangent.normalized;

            Vector3 ballPos = new Vector3(mid.x, BallY, mid.z);
            BallColor color = all[i % k];
            this.PlaceBall(ballPos, color);
            this.MakeObjective(ballPos, tangent, color, pointsEach);

            cursor = new Cursor { pos = b, dir = piece.WorldPortDirection(exit) };
        }
    }

    /// <summary>Straight tiers: always straight. Curved tiers: alternate straight / a fitting slant.</summary>
    private TrackPiece PickPiece(Cursor cursor, int i)
    {
        if (!this.allowSlants || i % 2 == 0)
        {
            return this.straightPrefab;
        }
        TrackPiece[] slants = Random.value < 0.5f
            ? new[] { this.slantRPrefab, this.slantLPrefab }
            : new[] { this.slantLPrefab, this.slantRPrefab };
        foreach (TrackPiece s in slants)
        {
            if (this.FootprintFree(s, cursor)) return s;
        }
        return this.straightPrefab; // neither slant fits -> stay straight
    }

    private void PlaceBall(Vector3 pos, BallColor color)
    {
        GameObject ball = Instantiate(this.ballPrefab, pos, Quaternion.identity, this.slalomRoot);
        ball.transform.localScale = Vector3.one * BallScale;
        Material mat = this.ColorMaterial(color);

        MeshRenderer renderer = ball.GetComponentInChildren<MeshRenderer>();
        GameObject model = renderer != null ? renderer.gameObject : ball;
        if (renderer != null) renderer.sharedMaterial = mat;

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true; // hold the fixed mid-height

        // solid box collider (swap the prefab's sphere) so the drone can't fly through
        foreach (Collider c in ball.GetComponentsInChildren<Collider>()) Destroy(c);
        model.AddComponent<BoxCollider>(); // default size 1 * model scale 3 = ~3 m box

        MapLandmark landmark = ball.AddComponent<MapLandmark>();
        landmark.SetIconColor(mat.color);
    }

    /// <summary>The correct-side DestinationPoint, offset into the ball's travel frame and sized to
    /// cover that side of the channel only (RED left / BLUE right / GREEN above / YELLOW below).</summary>
    private void MakeObjective(Vector3 ballPos, Vector3 tangent, BallColor color, float points)
    {
        Quaternion rot = Quaternion.LookRotation(tangent, Vector3.up);
        Vector3 localOffset;
        Vector3 size;
        switch (color)
        {
            case BallColor.Red:   localOffset = new Vector3(-SideOffset, 0f, 0f); size = new Vector3(3f, 10f, 3f); break; // left
            case BallColor.Blue:  localOffset = new Vector3(SideOffset, 0f, 0f);  size = new Vector3(3f, 10f, 3f); break; // right
            case BallColor.Green: localOffset = new Vector3(0f, SideOffset, 0f);  size = new Vector3(9f, 3f, 3f);  break; // above
            default:              localOffset = new Vector3(0f, -SideOffset, 0f); size = new Vector3(9f, 3f, 3f);  break; // yellow below
        }

        GameObject go = new GameObject("SlalomCheckpoint");
        go.layer = this.uiLayer;
        go.transform.SetPositionAndRotation(ballPos + rot * localOffset, rot);
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        DestinationPoint task = go.AddComponent<DestinationPoint>();
        task.SetPoints(points);
        go.transform.SetParent(this.managerTransform, true);
    }

    #region Piece placement (reuses the TrackPiece API; mirrors TrackGenerator's grid walk)
    private TrackPiece PlacePiece(TrackPiece prefab, Cursor cursor)
    {
        var (pos, rot) = this.ComputePlacement(prefab, cursor);
        TrackPiece piece = Instantiate(prefab, pos, rot, this.slalomRoot);
        this.MarkOccupied(piece);
        return piece;
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
        foreach (Vector2Int c in prefab.FootprintCells)
        {
            Vector3 w = rot * new Vector3(c.x * Cell + Cell / 2f, 0f, c.y * Cell + Cell / 2f) + pos;
            Vector2Int cell = WorldCell(w);
            if (Mathf.Abs(cell.x) > BoundCells || Mathf.Abs(cell.y) > BoundCells || this.occupied.Contains(cell)) return false;
        }
        return true;
    }

    private void MarkOccupied(TrackPiece piece)
    {
        foreach (Vector2Int c in piece.FootprintCells)
        {
            Vector3 w = piece.transform.TransformPoint(new Vector3(c.x * Cell + Cell / 2f, 0f, c.y * Cell + Cell / 2f));
            this.occupied.Add(WorldCell(w));
        }
    }

    private static Vector2Int WorldCell(Vector3 p)
    {
        return new Vector2Int(Mathf.RoundToInt(p.x / Cell), Mathf.RoundToInt(p.z / Cell));
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

    private static int FirstExitPort(TrackPiece piece)
    {
        for (int i = 0; i < piece.Ports.Length; i++)
        {
            if (piece.Ports[i].role == TrackPiece.PortRole.Exit) return i;
        }
        return -1;
    }

    private static void SuppressLine(TrackPiece piece)
    {
        foreach (TrackPiece.Branch branch in piece.Branches)
        {
            foreach (MeshRenderer r in branch.renderers)
            {
                if (r != null) r.enabled = false;
            }
        }
    }
    #endregion

    private Material ColorMaterial(BallColor c)
    {
        switch (c)
        {
            case BallColor.Red: return this.redFlat;
            case BallColor.Blue: return this.blueFlat;
            case BallColor.Green: return this.greenFlat;
            default: return this.yellowFlat;
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
