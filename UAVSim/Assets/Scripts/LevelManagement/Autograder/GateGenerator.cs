using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lab D — Gate Navigation. Assembles a randomized AR-gate course at scene load (per-trial, so
/// it can't be hardcoded) from the Lab A <see cref="Gate"/> prefab, then grades fly-through with
/// a DestinationPoint at each correct gate. Runs in Awake BEFORE <see cref="AutograderManager"/>
/// (negative execution order) so the manager discovers the tasks it parents under it. The sim has
/// no AR API — students detect the gates' 4 corner ArUco markers themselves (forward camera) to
/// centre and fly through; this script only places AR-tagged gates and the grading triggers.
/// In the rows tiers each row offers several differently-coloured gates and the highest-priority
/// colour present (RED &gt; GREEN &gt; BLUE) is the correct one; the rest are decoys.
/// </summary>
[DefaultExecutionOrder(-50)]
public class GateGenerator : MonoBehaviour
{
    public enum Rule { OneGate, TwoGates, ThreeGates, TwoRows, ThreeRows }

    #region Set in Unity Editor
    [SerializeField] private Rule rule = Rule.OneGate;
    [SerializeField] private float totalPoints = 3f;
    [SerializeField] private GameObject gatePrefab;
    [SerializeField] private Material redFlat;
    [SerializeField] private Material greenFlat;
    [SerializeField] private Material blueFlat;
    #endregion

    private const float StartZ = 22f;       // first gate ahead of the centre spawn
    private const float Spacing = 18f;      // gap between gates / rows along +Z
    private const float XRange = 14f;       // lateral spread of a sequence gate
    private const float RowXSpacing = 11f;  // lateral gap between gates within a row
    private const float MinAltitude = 8f;
    private const float MaxAltitude = 22f;
    private const float YawJitter = 12f;
    private const float ApertureWidth = 8f; // trigger ~ the 9x8 gate opening, inset a little
    private const float ApertureHeight = 7f;

    private Transform gateRoot;
    private Transform managerTransform;
    private int uiLayer;
    private readonly List<Pose> checkpoints = new List<Pose>();

    private void Awake()
    {
        this.uiLayer = LayerMask.NameToLayer("UI");
        AutograderManager manager = Object.FindFirstObjectByType<AutograderManager>();
        if (manager == null)
        {
            Debug.LogError("GateGenerator: no AutograderManager in scene.");
            return;
        }
        this.managerTransform = manager.transform;
        this.gateRoot = new GameObject("GeneratedGates").transform;

        this.Build();
        this.CreateTasks();
    }

    private void Build()
    {
        switch (this.rule)
        {
            case Rule.OneGate: this.BuildSequence(1); break;
            case Rule.TwoGates: this.BuildSequence(2); break;
            case Rule.ThreeGates: this.BuildSequence(3); break;
            case Rule.TwoRows: this.BuildRows(2); break;
            case Rule.ThreeRows: this.BuildRows(3); break;
        }
    }

    /// <summary>A forward run of n gates at randomized lateral offset + altitude; thread in order.</summary>
    private void BuildSequence(int n)
    {
        for (int i = 0; i < n; i++)
        {
            Pose pose = this.RandomGatePose(StartZ + i * Spacing, Random.Range(-XRange, XRange));
            this.PlaceGate(pose, this.RandomColor());
            this.checkpoints.Add(pose);
        }
    }

    /// <summary>r rows; each row spreads several differently-coloured gates; correct = highest priority.</summary>
    private void BuildRows(int r)
    {
        for (int i = 0; i < r; i++)
        {
            float z = StartZ + i * Spacing;
            int k = Random.Range(2, 4); // 2 or 3 gates this row
            Material[] colors = this.PickRowColors(k); // colors[0] = highest priority
            Material correct = colors[0];
            Shuffle(colors); // highest now sits at a random physical slot
            float[] xs = SpreadX(k);

            Pose correctPose = default;
            for (int j = 0; j < k; j++)
            {
                Pose pose = this.RandomGatePose(z, xs[j]);
                this.PlaceGate(pose, colors[j]);
                if (colors[j] == correct)
                {
                    correctPose = pose;
                }
            }
            this.checkpoints.Add(correctPose); // only the correct gate is graded
        }
    }

    private Pose RandomGatePose(float z, float x)
    {
        float y = Random.Range(MinAltitude, MaxAltitude);
        Quaternion rot = Quaternion.Euler(0f, Random.Range(-YawJitter, YawJitter), 0f);
        return new Pose(new Vector3(x, y, z), rot);
    }

    private void PlaceGate(Pose pose, Material color)
    {
        GameObject gate = Instantiate(this.gatePrefab, pose.position, pose.rotation, this.gateRoot);
        foreach (MeshRenderer renderer in gate.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.name.StartsWith("Neon"))
            {
                renderer.sharedMaterial = color;
            }
        }
        MapLandmark landmark = gate.GetComponent<MapLandmark>();
        if (landmark != null)
        {
            landmark.SetIconColor(color.color);
        }
    }

    private void CreateTasks()
    {
        float pointsEach = this.totalPoints / Mathf.Max(1, this.checkpoints.Count);
        foreach (Pose pose in this.checkpoints)
        {
            GameObject go = new GameObject("GateCheckpoint");
            go.layer = this.uiLayer;
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(ApertureWidth, ApertureHeight, 1f);
            DestinationPoint task = go.AddComponent<DestinationPoint>(); // base Awake disables it
            task.SetPoints(pointsEach);
            go.transform.SetParent(this.managerTransform, true);
        }
    }

    private Material RandomColor()
    {
        int i = Random.Range(0, 3);
        return i == 0 ? this.redFlat : (i == 1 ? this.greenFlat : this.blueFlat);
    }

    /// <summary>k distinct colours, [0] = highest priority (RED>GREEN>BLUE).</summary>
    private Material[] PickRowColors(int k)
    {
        Material[] all = { this.redFlat, this.greenFlat, this.blueFlat };
        if (k >= 3)
        {
            return new[] { all[0], all[1], all[2] };
        }
        int a = Random.Range(0, 3);
        int b = Random.Range(0, 2);
        if (b >= a) b++;
        return new[] { all[Mathf.Min(a, b)], all[Mathf.Max(a, b)] };
    }

    private static float[] SpreadX(int k)
    {
        float[] xs = new float[k];
        float total = (k - 1) * RowXSpacing;
        for (int j = 0; j < k; j++)
        {
            xs[j] = -total / 2f + j * RowXSpacing;
        }
        return xs;
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
